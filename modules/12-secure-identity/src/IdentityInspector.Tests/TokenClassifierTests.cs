using System.Text;
using System.Text.Json;
using IdentityInspector;
using Xunit;

namespace IdentityInspector.Tests;

public class TokenClassifierTests
{
    // Builds an unsigned JWT (header.payload.) from a claims object — enough for
    // the offline inspector, which never validates signatures.
    private static string MakeJwt(object claims)
    {
        static string B64Url(byte[] b) =>
            Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        string header = B64Url(Encoding.UTF8.GetBytes("{\"alg\":\"none\",\"typ\":\"JWT\"}"));
        string payload = B64Url(JsonSerializer.SerializeToUtf8Bytes(claims));
        return $"{header}.{payload}.";
    }

    [Fact]
    public void DelegatedUser_When_ScpPresent()
    {
        string jwt = MakeJwt(new
        {
            aud = "https://search.azure.com",
            appid = "11111111-1111-1111-1111-111111111111",
            oid = "22222222-2222-2222-2222-222222222222",
            upn = "estimator@contoso.com",
            scp = "user_impersonation"
        });

        var facts = TokenClassifier.Classify(jwt);

        Assert.Equal(CallerKind.DelegatedUser, facts.Kind);
        Assert.Equal("https://search.azure.com", facts.Audience);
        Assert.Equal("user_impersonation", facts.Scopes);
    }

    [Fact]
    public void Service_When_AppOnly_NoScopes()
    {
        string jwt = MakeJwt(new
        {
            aud = "https://storage.azure.com",
            appid = "33333333-3333-3333-3333-333333333333",
            oid = "44444444-4444-4444-4444-444444444444",
            idtyp = "app"
        });

        var facts = TokenClassifier.Classify(jwt);

        Assert.Equal(CallerKind.Service, facts.Kind);
        Assert.Equal("https://storage.azure.com", facts.Audience);
        Assert.Null(facts.Scopes);
    }

    [Fact]
    public void User_When_UpnPresent_NoScopes()
    {
        string jwt = MakeJwt(new
        {
            aud = "https://graph.microsoft.com",
            preferred_username = "estimator@contoso.com",
            oid = "55555555-5555-5555-5555-555555555555"
        });

        var facts = TokenClassifier.Classify(jwt);

        Assert.Equal(CallerKind.User, facts.Kind);
        Assert.Equal("estimator@contoso.com", facts.UserPrincipal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    public void Throws_On_Malformed(string bad)
    {
        Assert.Throws<ArgumentException>(() => TokenClassifier.Classify(bad));
    }

    [Fact]
    public void Base64UrlDecode_Handles_Padding()
    {
        // "hi" -> base64url "aGk" (no padding); must decode correctly.
        byte[] decoded = TokenClassifier.Base64UrlDecode("aGk");
        Assert.Equal("hi", Encoding.UTF8.GetString(decoded));
    }
}
