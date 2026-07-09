using System.Text;
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.KnowledgeBases;
using Azure.Search.Documents.KnowledgeBases.Models;
using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

// -----------------------------------------------------------------------------
// Track 2 — Identity-native row-level security (PREVIEW).
//
// Two modes:
//   dotnet run -- --seed-index   one-time: create a permission-filtered index
//                                and push rate rows tagged with Entra group ids
//   dotnet run                   start the web app; signed-in user only sees
//                                the rate rows their Entra groups may access
//
// Enforcement is inside Azure AI Search: the app passes the user's token via
// x-ms-query-source-authorization and writes NO filter logic.
// -----------------------------------------------------------------------------

// Delegated scope for calling Azure AI Search on behalf of the signed-in user.
const string SearchUserScope = "https://search.azure.com/user_impersonation";

if (args.Contains("--seed-index"))
{
    await IndexSeeder.RunAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(new[] { SearchUserScope })
    .AddInMemoryTokenCaches();

builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

app.MapGet("/", [Authorize] async (HttpContext http, ITokenAcquisition tokenAcquisition, IConfiguration config) =>
{
    string searchEndpoint = config["Search:Endpoint"]!;
    string knowledgeBaseName = config["Search:KnowledgeBaseName"]!;
    string? knowledgeSourceName = config["Search:KnowledgeSourceName"];
    int maxOutputDocuments = config.GetValue<int?>("Search:MaxOutputDocuments") ?? 50;
    string userName = http.User.Identity?.Name ?? "user";

    // Acquire a token for Azure AI Search on behalf of the signed-in user.
    string userToken = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { SearchUserScope });

    // The service credential authenticates the app to the search service;
    // the user token drives per-identity row trimming.
    var kbClient = new KnowledgeBaseRetrievalClient(
        endpoint: new Uri(searchEndpoint),
        knowledgeBaseName: knowledgeBaseName,
        credential: new DefaultAzureCredential());

    var request = new KnowledgeBaseRetrievalRequest();
    request.Messages.Add(new KnowledgeBaseMessage(
        content: new[] { new KnowledgeBaseMessageTextContent("List all rate rows available to me.") })
    {
        Role = "user"
    });

    // Widen the number of grounding rows returned. Agentic retrieval defaults to a
    // few top-ranked docs; MaxOutputDocuments raises the cap so the user sees all
    // their permitted rows. NO filter is set — Azure AI Search still trims rows by
    // the user's Entra claims against the index permission-filter field.
    if (!string.IsNullOrWhiteSpace(knowledgeSourceName))
    {
        request.KnowledgeSourceParams.Add(new SearchIndexKnowledgeSourceParams(knowledgeSourceName)
        {
            MaxOutputDocuments = maxOutputDocuments,
            // "List all my rows" is a weak semantic match, so the default reranker
            // relevance cutoff drops all but the top row. Set 0 to return every
            // retrieved (identity-trimmed) row up to MaxOutputDocuments.
            RerankerThreshold = 0
        });
    }

    Response<KnowledgeBaseRetrievalResponse> response =
        await kbClient.RetrieveAsync(request, querySourceAuthorization: userToken);

    var sb = new StringBuilder();
    sb.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>Contoso Rate RLS</title>");
    sb.Append("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:2rem;max-width:900px}pre{white-space:pre-wrap;background:#f6f8fa;padding:1rem;border-radius:6px}</style></head><body>");
    sb.Append($"<h2>Rates visible to {System.Net.WebUtility.HtmlEncode(userName)}</h2>");
    sb.Append("<p>Rows are trimmed by your Microsoft Entra identity — the app applies no filter.</p>");
    sb.Append("<p><a href=\"/MicrosoftIdentity/Account/SignOut\">Sign out</a></p><pre>");
    foreach (var message in response.Value.Response)
    {
        foreach (var content in message.Content)
        {
            if (content is KnowledgeBaseMessageTextContent text)
            {
                sb.Append(System.Net.WebUtility.HtmlEncode(text.Text));
                sb.Append('\n');
            }
        }
    }
    sb.Append("</pre></body></html>");
    return Results.Content(sb.ToString(), "text/html");
});

app.Run();

// -----------------------------------------------------------------------------
// One-time index seeder: builds a permission-filtered index and pushes the rate
// rows from Azure SQL, tagging each with the Entra group id that may see it.
// -----------------------------------------------------------------------------
static class IndexSeeder
{
    public static async Task RunAsync()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        string searchEndpoint = config["Search:Endpoint"]!;
        string indexName = config["Search:SecuredIndexName"] ?? "rate-library-secured";
        string sqlConnectionString = config["Sql:ConnectionString"]!;

        // owner_group (SQL) -> Entra group object id (from setup step 5).
        var groupIds = config.GetSection("Groups").GetChildren()
            .ToDictionary(c => c.Key, c => c.Value!, StringComparer.OrdinalIgnoreCase);

        var credential = new DefaultAzureCredential();
        var indexClient = new SearchIndexClient(new Uri(searchEndpoint), credential);

        // 1. Create the index with a group-ids permission filter field.
        var index = new SearchIndex(indexName)
        {
            PermissionFilterOption = SearchIndexPermissionFilterOption.Enabled,
            Fields =
            {
                new SimpleField("rate_id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SearchableField("content_text"),
                new SimpleField("trade_item", SearchFieldDataType.String) { IsFilterable = true, IsSortable = true },
                new SimpleField("category", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("division", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("region", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("unit", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("rate_aud", SearchFieldDataType.Double) { IsFilterable = true, IsSortable = true },
                new SearchField("group_ids", SearchFieldDataType.Collection(SearchFieldDataType.String))
                {
                    IsFilterable = true,
                    PermissionFilter = PermissionFilter.GroupIds
                }
            },
            // Semantic configuration is REQUIRED for a Foundry IQ searchIndex
            // knowledge source (agentic retrieval ranks results semantically). The
            // portal greys out the index as "missing semantic configuration" without
            // this. content_text is the only searchable field, so it drives ranking.
            SemanticSearch = new SemanticSearch
            {
                DefaultConfigurationName = "rate-semantic",
                Configurations =
                {
                    new SemanticConfiguration("rate-semantic", new SemanticPrioritizedFields
                    {
                        ContentFields = { new SemanticField("content_text") }
                    })
                }
            }
        };

        await indexClient.CreateOrUpdateIndexAsync(index);
        Console.WriteLine($"Index '{indexName}' created/updated with permission filtering.");

        // 2. Read rate rows from Azure SQL and tag each with its Entra group id.
        var docs = new List<SearchDocument>();
        await using (var conn = new SqlConnection(sqlConnectionString))
        {
            await conn.OpenAsync();
            const string sql = "SELECT rate_id, category, division, trade_item, unit, region, rate_aud, owner_group, content_text FROM dbo.rate_library";
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string ownerGroup = reader.GetString(7);
                if (!groupIds.TryGetValue(ownerGroup, out string? groupId))
                {
                    Console.WriteLine($"WARN: no group id mapped for owner_group '{ownerGroup}' — row will be inaccessible.");
                    groupId = null;
                }

                docs.Add(new SearchDocument
                {
                    ["rate_id"] = reader.GetInt32(0).ToString(),
                    ["category"] = reader.GetString(1),
                    ["division"] = reader.GetString(2),
                    ["trade_item"] = reader.GetString(3),
                    ["unit"] = reader.GetString(4),
                    ["region"] = reader.GetString(5),
                    ["rate_aud"] = (double)reader.GetDecimal(6),
                    ["content_text"] = reader.GetString(8),
                    ["group_ids"] = groupId is null ? Array.Empty<string>() : new[] { groupId }
                });
            }
        }

        // 3. Push documents into the index.
        var searchClient = new SearchClient(new Uri(searchEndpoint), indexName, credential);
        await searchClient.MergeOrUploadDocumentsAsync(docs);
        Console.WriteLine($"Pushed {docs.Count} permission-tagged rate rows into '{indexName}'.");
        Console.WriteLine("Now wrap this index as a searchIndex knowledge source and add it to a knowledge base.");
    }
}
