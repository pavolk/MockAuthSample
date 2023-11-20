using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MockAuthSample;

public class Program
{
    public static readonly string ISSUER = "emsys";
    public static readonly string AUDIENCE = "emsys-cloud";

    

    public static SymmetricSecurityKey SigningKey 
    {
        get => new SymmetricSecurityKey(Encoding.UTF8.GetBytes("this is some dummy signing key..."));
    }

    public static IResult GetToken(string user)
    {
        var parts = user.Split('@');
        if (parts.Length < 2) { return Results.BadRequest(); }

        var userName = parts[0];
        var organisationName = parts[1];

        var key = Encoding.ASCII.GetBytes("this is some dummy key...");

        var jwtSecurityToken = new JwtSecurityToken(
            issuer: ISSUER,
            audience: AUDIENCE,
            claims: new List<Claim>() {
                    new Claim(JwtRegisteredClaimNames.Name, userName),
                    new Claim("tid", organisationName),
            },
            expires: DateTime.Now.AddMinutes(10),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        );

        return Results.Ok(new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken));
    }


    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options => {

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme,
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
                Description = "JWT Authorization header using the Bearer scheme.",
                Name = JwtBearerDefaults.AuthenticationScheme,
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme
            });

            options.OperationFilter<SecurityRequirementsOperationFilter>(new object[] { true, JwtBearerDefaults.AuthenticationScheme });

        });


        builder.Services.AddAuthentication(options => {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options => {

            options.TokenValidationParameters = new TokenValidationParameters {
                ValidIssuer = Program.ISSUER,
                ValidAudience = Program.AUDIENCE,
                IssuerSigningKey = SigningKey,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
            };
        });

        builder.Services.AddAuthorization();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options => {
                options.EnableTryItOutByDefault();
                options.DisplayOperationId();
                options.DisplayRequestDuration();
            });
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();

        app.UseAuthorization();


        app.MapGet("api/login/token", (string user) => Program.GetToken(user));

        app.MapControllers();

        app.Run();
    }
}
