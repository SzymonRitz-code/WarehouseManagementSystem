using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace WarehouseManagementSystem.API.ServiceExtensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddWmsAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var authority = configuration["Authentication:Authority"] ?? "https://localhost:44380";
        var metadataAddress = configuration["Authentication:MetadataAddress"]
                              ?? $"{authority.TrimEnd('/')}/.well-known/openid-configuration";
        var audience = configuration["Authentication:Audience"] ?? "wmsApi";
        var validIssuer = configuration["Authentication:ValidIssuer"] ?? authority;
        var requireHttpsMetadata = configuration.GetValue("Authentication:RequireHttpsMetadata", true);

        System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.Authority = authority;
                o.MetadataAddress = metadataAddress;
                o.RequireHttpsMetadata = requireHttpsMetadata;
                o.Audience = audience;
                o.MapInboundClaims = false;

                o.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = JwtClaimTypes.Name,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = validIssuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                };

                if (environment.IsDevelopment())
                {
                    o.BackchannelHttpHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) =>
                        {
                            Console.WriteLine($"=== CERT VALIDATION: {cert?.Subject}, errors: {errors}");
                            return true;
                        }
                    };
                    o.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = ctx =>
                        {
                            Console.WriteLine($"=== AUTH FAILED: {ctx.Exception}");
                            return Task.CompletedTask;
                        }
                    };
                }
            });

        services.AddAuthorization(o =>
        {
            o.AddPolicy("fullaccess", p => p.RequireClaim(JwtClaimTypes.Scope, "wms.api"));
        });

        return services;
    }
}
