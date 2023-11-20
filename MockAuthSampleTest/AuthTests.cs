using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace MockAuthSampleTest;

class TestWebApplicationFactory : WebApplicationFactory<MockAuthSample.Program>
{
    class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { 
                new Claim(ClaimTypes.Name, "pavol"),
                new Claim("tenantid", "emsys")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            var result = AuthenticateResult.Success(ticket);

            return Task.FromResult(result);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseTestServer()
            .ConfigureTestServices(services => {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        "Test", options => { });
                services.AddAuthorization(opts => {
                    opts.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .AddAuthenticationSchemes("Test")
                        .RequireAuthenticatedUser()
                        .Build();
                });
            })
            ;

    }
}

class ClientLoggingHandler : DelegatingHandler
{
    public ClientLoggingHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    protected void WriteLine(string message) => Debug.WriteLine(message);
    protected void WriteLineJson(string json)
    {
        var prettyJson = JToken.Parse(json).ToString(Newtonsoft.Json.Formatting.Indented);
        WriteLine(prettyJson);
    }


    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        WriteLine("Request:");
        WriteLine(request.ToString());
        if (request.Content != null) {
            var content = await request.Content.ReadAsStringAsync();
            var contentType = request.Content!.Headers?.ContentType;
            if (contentType?.MediaType == "application/json") {
                WriteLineJson(content);
            } else {
                WriteLine(content);
            }
        }
        WriteLine("");

        var response = await base.SendAsync(request, cancellationToken);

        WriteLine("Response:");
        WriteLine(response.ToString());
        if (response.Content != null) {
            var content = await response.Content.ReadAsStringAsync();
            var contentType = response.Content!.Headers?.ContentType;
            if (contentType?.MediaType == "application/json") {
                WriteLineJson(content);
            } else {
                WriteLine(content);
            }
        }
        WriteLine("");

        return response;
    }
}


[TestClass]
public class AuthTests
{
    private static WebApplicationFactory<MockAuthSample.Program> factory;

    static AuthTests()
    {
        //factory = new WebApplicationFactory<MockAuthSample.Program>();
        factory = new TestWebApplicationFactory();
    }

    [TestMethod]
    public async Task Get_SecurePageIsReturnedForAnAuthenticatedUser()
    {
        // Arrange
        var client = factory.CreateDefaultClient(new DelegatingHandler[] {
                                new ClientLoggingHandler(new HttpClientHandler()) });

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
