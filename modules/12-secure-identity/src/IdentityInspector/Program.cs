using System.Text;
using System.Text.Json;

namespace IdentityInspector;

/// <summary>
/// The kind of caller a JWT represents. Teaching aid for Module 12 — it shows
/// that "who is calling" is a claims decision, not a guess.
/// </summary>
public enum CallerKind
{
    Unknown,
    /// <summary>Delegated user token (has a user scope <c>scp</c>) — e.g. OBO passthrough.</summary>
    DelegatedUser,
    /// <summary>Interactive user token (user claims, no app-only markers).</summary>
    User,
    /// <summary>Application / service-principal token (app-only). Agent identities and managed identities appear here.</summary>
    Service
}

public sealed record TokenFacts(
    CallerKind Kind,
    string? Audience,
    string? AppId,
    string? ObjectId,
    string? UserPrincipal,
    string? Scopes,
    string Explanation);

/// <summary>
/// Decodes a JWT payload (offline, no signature validation) and classifies the
/// caller. Signature validation is intentionally omitted — this is an inspector,
/// not an authenticator.
/// </summary>
public static class TokenClassifier
{
    public static TokenFacts Classify(string jwt)
    {
        var payload = DecodePayload(jwt);

        string? aud = GetString(payload, "aud");
        string? appid = GetString(payload, "appid") ?? GetString(payload, "azp");
        string? oid = GetString(payload, "oid");
        string? upn = GetString(payload, "upn")
                      ?? GetString(payload, "preferred_username")
                      ?? GetString(payload, "unique_name");
        string? scp = GetString(payload, "scp");
        string? idtyp = GetString(payload, "idtyp");

        bool hasScopes = !string.IsNullOrWhiteSpace(scp);
        bool appOnly = string.Equals(idtyp, "app", StringComparison.OrdinalIgnoreCase)
                       || (!hasScopes && string.IsNullOrWhiteSpace(upn) && !string.IsNullOrWhiteSpace(appid));

        CallerKind kind;
        string why;
        if (hasScopes)
        {
            kind = CallerKind.DelegatedUser;
            why = "Delegated token: 'scp' (user scopes) present — the app acts on behalf of the signed-in user (OBO).";
        }
        else if (appOnly)
        {
            kind = CallerKind.Service;
            why = "App-only token: no user scopes and an 'appid'/idtyp=app — a service principal (agent identity or managed identity).";
        }
        else if (!string.IsNullOrWhiteSpace(upn))
        {
            kind = CallerKind.User;
            why = "User token: user principal claim present, no app-only markers.";
        }
        else
        {
            kind = CallerKind.Unknown;
            why = "Could not classify from available claims.";
        }

        return new TokenFacts(kind, aud, appid, oid, upn, scp, why);
    }

    /// <summary>Decodes the JWT payload segment into a JSON element. Throws on malformed input.</summary>
    public static JsonElement DecodePayload(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            throw new ArgumentException("Token is empty.", nameof(jwt));
        }

        string[] parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            throw new ArgumentException("Not a JWT (expected header.payload.signature).", nameof(jwt));
        }

        byte[] bytes = Base64UrlDecode(parts[1]);
        using var doc = JsonDocument.Parse(bytes);
        return doc.RootElement.Clone();
    }

    public static byte[] Base64UrlDecode(string input)
    {
        string s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }

    private static string? GetString(JsonElement obj, string name)
        => obj.ValueKind == JsonValueKind.Object
           && obj.TryGetProperty(name, out var v)
           && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: dotnet run -- <jwt>");
            Console.Error.WriteLine("Decodes a JWT (no signature check) and classifies the caller.");
            return 1;
        }

        try
        {
            var facts = TokenClassifier.Classify(args[0]);
            var sb = new StringBuilder();
            sb.AppendLine($"Caller kind : {facts.Kind}");
            sb.AppendLine($"Audience    : {facts.Audience ?? "(none)"}");
            sb.AppendLine($"App ID      : {facts.AppId ?? "(none)"}");
            sb.AppendLine($"Object ID   : {facts.ObjectId ?? "(none)"}");
            sb.AppendLine($"User        : {facts.UserPrincipal ?? "(none)"}");
            sb.AppendLine($"Scopes      : {facts.Scopes ?? "(none)"}");
            sb.AppendLine();
            sb.AppendLine(facts.Explanation);
            Console.WriteLine(sb.ToString());
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
    }
}
