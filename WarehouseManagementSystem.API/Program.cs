using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using System.Text.Json.Serialization;
using WarehouseManagementSystem.API.Extensions;
using WarehouseManagementSystem.API.Services.Documents;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.API.Services.Stocks;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Services;
using WarehouseManagementSystem.Infrastructure.Persistence;
using WarehouseManagementSystem.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Dodałem NewtonssoftJson bo obługuje patchDocument Serializacja Enumów z tego powodu powinna być w nim dodana inaczej dojdzie do zgrzytu między dwoma konwerterami
// Dodanie konwertera przy polu(w klasyczny sposób) nie jest wtedy obsługiwane.
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    //options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<WarehouseManagementSystemDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WarehouseManagementSystemConnection")));

// Services
builder.Services.AddScoped<IDocumentCommandService, DocumentCommandService>();
builder.Services.AddScoped<IGoodsIssueService, GoodsIssueService>();
builder.Services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
builder.Services.AddScoped<IStockTransferService, StockTransferService>();
builder.Services.AddScoped<IProductBatchQueryService, ProductBatchQueryService>();


builder.Services.AddScoped<IDocumentQueryService, DocumentQueryService>();
builder.Services.AddScoped<IStockQueryService, StockQueryService>();


builder.Services.AddScoped<IStockReservationService, StockReservationService>();

builder.Services.AddScoped<IStockService, StockService>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddTransient<IDocumentNumberGenerator,DocumentNumberGenerator>();
builder.Services.AddHostedService<ReservationExpirationJob>();
builder.Services.AddSingleton<ISystemClock, SystemClock>();


builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddWmsMappings();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWmsClient",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
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

app.UseAuthorization();

app.MapControllers();

app.Run();
