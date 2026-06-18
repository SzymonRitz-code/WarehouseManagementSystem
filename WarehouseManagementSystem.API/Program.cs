using System.IdentityModel.Tokens.Jwt;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
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


JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
// Add services to the container.
// Dodałem NewtonssoftJson bo obługuje patchDocument. Serializacja Enumów z tego powodu powinna być w nim dodana inaczej dojdzie do zgrzytu między dwoma konwerterami
// Dodanie konwertera przy polu(w klasyczny sposób) nie jest wtedy obsługiwane.
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    //options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()  // JWT token dla serwera
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
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
    cfg.AddWmsMappings();
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
        o.Audience = "wmsApi";
        o.MapInboundClaims = false; //

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
                Console.WriteLine($"=== KEY RESOLVER called, kid: {kid}");
                var client = new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
                });
                var json = client.GetStringAsync("https://localhost:44380/.well-known/openid-configuration/jwks").Result;
                Console.WriteLine($"=== JWKS: {json}");
                var keys = new JsonWebKeySet(json);
                return keys.GetSigningKeys();
            }
        };
        //o.BackchannelHttpHandler = new HttpClientHandler
        //{
        //    ServerCertificateCustomValidationCallback =
        //        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        //};
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
    });
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("fullaccess", p => p.RequireClaim(JwtClaimTypes.Scope, "wms.api"));
    //o.AddPolicy("isadmin", p => p.RequireClaim(JwtClaimTypes.Role, "admin")); // Możliwe dodanie ról 
    //o.AddPolicy("isemployee", p => p.RequireClaim("employeeno"));
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowWmsClient");
app.UseSerilogRequestLogging();

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
