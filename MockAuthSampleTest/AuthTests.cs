using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MockAuthSampleTest;

[TestClass]
public class AuthTests
{
    private static WebApplicationFactory<MockAuthSample.Program> factory;

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        factory = new WebApplicationFactory<MockAuthSample.Program>();
    }

    [TestMethod]
    public async Task Get_SecurePageIsReturnedForAnAuthenticatedUser()
    {
        // Arrange
        var client = factory.WithWebHostBuilder(builder =>
            {
                builder
                .ConfigureTestServices(services =>
                {
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            "Test", options => { });
                });
            })
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Test");

        //Act
        var response = await client.GetAsync("/Secure");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        factory.Dispose();
    }
}

#region snippet4
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "Test user") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}
#endregion