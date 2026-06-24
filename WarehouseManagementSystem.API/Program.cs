using System.IdentityModel.Tokens.Jwt;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using WarehouseManagementSystem.API;
using WarehouseManagementSystem.API.Extensions;
using WarehouseManagementSystem.API.Extensions.Middleware;
using WarehouseManagementSystem.API.Services.AuditLogs;
using WarehouseManagementSystem.API.Services.Documents;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.API.Services.Seed;
using WarehouseManagementSystem.API.Services.Stocks;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Services;
using WarehouseManagementSystem.Infrastructure.Persistence;
using WarehouseManagementSystem.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);


JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // wyłączenie mapowania claimów JWT na standardowe nazwy claimów w .NET, dzięki temu nazwy claimów w tokenie JWT będą takie same jak w aplikacji, bez tego np. "sub" byłby mapowany na ClaimTypes.NameIdentifier, a "role" na ClaimTypes.Role, co może powodować problemy z autoryzacją jeśli w tokenie są niestandardowe nazwy claimów.
// Add services to the container.
// Dodałem NewtonssoftJson bo obługuje patchDocument. Serializacja Enumów z tego powodu powinna być w nim dodana inaczej dojdzie do zgrzytu między dwoma konwerterami
// Dodanie konwertera przy polu(w klasyczny sposób) nie jest wtedy obsługiwane.
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new AuthorizeFilter()); // dodanie globalnego filtra autoryzacji, który wymaga uwierzytelnienia dla wszystkich endpointów, chyba że zostanie to nadpisane przez atrybut [AllowAnonymous] na poziomie kontrolera lub akcji.

    options.ReturnHttpNotAcceptable = true;

    options.CacheProfiles.Add(HttpCacheProfiles.ReferenceData, new CacheProfile
    {
        Duration = HttpCacheProfiles.ReferenceDataDuration,
        Location = ResponseCacheLocation.Client,
        VaryByQueryKeys = ["*"]
    });
    options.CacheProfiles.Add(HttpCacheProfiles.OperationalData, new CacheProfile
    {
        Duration = HttpCacheProfiles.OperationalDataDuration,
        Location = ResponseCacheLocation.Client,
        VaryByQueryKeys = ["*"]
    });
    options.CacheProfiles.Add(HttpCacheProfiles.VolatileData, new CacheProfile
    {
        Duration = HttpCacheProfiles.VolatileDataDuration,
        Location = ResponseCacheLocation.Client,
        VaryByQueryKeys = ["*"]
    });
    options.CacheProfiles.Add(HttpCacheProfiles.AuditData, new CacheProfile
    {
        Duration = HttpCacheProfiles.AuditDataDuration,
        Location = ResponseCacheLocation.Client,
        VaryByQueryKeys = ["*"]
    }); // dodanie cache dla wszystkich endpointów które nie zmieniają się często i mogą być przechowywane w pamięci podręcznej przez określony czas.
}).AddNewtonsoftJson(options => // dodałem NewtonsoftJson bo obługuje patchDocument. Serializacja Enumów z tego powodu powinna być w nim dodana inaczej dojdzie do zgrzytu między dwoma konwerterami
{
    //options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
}).AddWmsApiBehavior();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()  // JWT token dla serwera
    {
        Name = "Authorization", // nazwa nagłówka, w którym klient będzie przesyłał token JWT
        Type = SecuritySchemeType.ApiKey, // określa, że token będzie przesyłany jako klucz API (w nagłówku)
        Scheme = "Bearer", // nazwa schematu uwierzytelniania, używana w nagłówku (np. "Bearer")
        BearerFormat = "JWT", // format tokenu, informacja dla klienta, że jest to token JWT
        In = ParameterLocation.Header, // określa, że token będzie przesyłany w nagłówku HTTP
        Description = "JWT Authorization header using the Bearer scheme."
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement // wymóg uwierzytelniania dla wszystkich endpointów, klient musi przesłać token JWT w nagłówku
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(ctx.Configuration));
builder.Services.AddDbContext<WarehouseManagementSystemDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("WarehouseManagementSystemConnection")
    );

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging()
               .EnableDetailedErrors();
    }
});

// Services
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IDocumentCommandService, DocumentCommandService>();
builder.Services.AddScoped<IDocumentQueryService, DocumentQueryService>();
builder.Services.AddScoped<IProductQueryService, ProductQueryService>();
builder.Services.AddScoped<IWarehouseQueryService, WarehouseQueryService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IStockQueryService, StockQueryService>();
builder.Services.AddScoped<IStockReservationService, StockReservationService>();
builder.Services.AddScoped<IProductBatchQueryService, ProductBatchQueryService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();



builder.Services.AddTransient<IDocumentNumberGenerator, DocumentNumberGenerator>();
builder.Services.AddHostedService<DatabaseSeedingHostedService>();
builder.Services.AddHostedService<ReservationExpirationJob>();
builder.Services.AddSingleton<ISystemClock, SystemClock>();
builder.Services.AddSingleton<IUserService, UserService>();


builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddWmsMappings(); // extension method to add all mappings for the WMS application
});



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWmsClient",
        policy =>
        {
            policy.WithOrigins("https://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = "https://localhost:44380";
        o.MetadataAddress = "https://localhost:44380/.well-known/openid-configuration";
        o.RequireHttpsMetadata = true; // tymczasowo można na falce
        o.Audience = "wmsApi"; // musi być zgodne z aud w tokenie JWT, który jest generowany przez IdentityServer, jeśli nie jest zgodne to będzie błąd 401 Unauthorized
        o.MapInboundClaims = false; // wyłączenie mapowania claimów, dzięki temu nazwy claimów w tokenie JWT będą takie same jak w aplikacji,
                                    // bez tego np. "sub" byłby mapowany na ClaimTypes.NameIdentifier, a "role" na ClaimTypes.Role,
                                    // co może powodować problemy z autoryzacją jeśli w tokenie są niestandardowe nazwy claimów

        o.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = JwtClaimTypes.Name,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidIssuer = "https://localhost:44380",
            ValidateAudience = true,
            ValidAudience = "wmsApi",
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                // Konflikt między System.IdentityModel a Duende IdentityModel

                // Dodałem własny resolver kluczy, który pobiera klucze z endpointu JWKS IdentityServer,
                // ponieważ domyślny resolver nie działa poprawnie z Duende IdentityModel i nie znajduje kluczy podpisujących,
                // co powoduje błąd 401 Unauthorized przy próbie uwierzytelnienia tokenu JWT.
                // TODO : W przyszłości można rozważyć użycie IdentityModel.AspNetCore.OAuth2Introspection, który obsługuje introspekcję tokenów JWT i może być bardziej odpowiedni dla Duende IdentityServer.
                var client = new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
                });
                var json = client.GetStringAsync("https://localhost:44380/.well-known/openid-configuration/jwks").Result;
                var keys = new JsonWebKeySet(json);
                return keys.GetSigningKeys();
            }
        };
        //o.BackchannelHttpHandler = new HttpClientHandler
        //{
        //    ServerCertificateCustomValidationCallback =
        //        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        //};
        // dodane na potrzeby sprawdzenia certyfikatu podczas developmentu, ponieważ IdentityServer jest hostowany na localhost z self-signed certyfikatem,
        // który nie jest zaufany przez system operacyjny, więc trzeba go zaakceptować ręcznie w kodzie, żeby móc testować uwierzytelnianie JWT podczas developmentu.
        // W produkcji ten kod powinien być usunięty, a certyfikat powinien być wystawiony przez zaufany urząd certyfikacji.

        if (builder.Environment.IsDevelopment())
        {
            o.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) =>
                {
                    Console.WriteLine($"=== CERT VALIDATION: {cert?.Subject}, errors: {errors}");
                    return true;
                }
            };
            // Dodałem logowanie błędów uwierzytelniania JWT, ponieważ podczas developmentu często pojawiają się problemy z konfiguracją IdentityServer i tokenami JWT,
            // więc dodatkowe logi pomagają zdiagnozować co jest nie tak, np. czy problemem jest certyfikat, czy konfiguracja tokena, czy coś innego.
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
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("fullaccess", p => p.RequireClaim(JwtClaimTypes.Scope, "wms.api"));
    //o.AddPolicy("isadmin", p => p.RequireClaim(JwtClaimTypes.Role, "admin")); // Możliwe dodanie ról 
    //o.AddPolicy("isemployee", p => p.RequireClaim("employeeno"));
});
builder.Services.AddResponseCaching();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowWmsClient");
app.UseResponseCaching();
app.UseSerilogRequestLogging();

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
