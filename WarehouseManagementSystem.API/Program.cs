using Microsoft.EntityFrameworkCore;
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<WarehouseManagementSystemDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WarehouseManagementSystemConnection")));

// Services
builder.Services.AddScoped<IDocumentCommandService, DocumentCommandService>();
builder.Services.AddScoped<IGoodsIssueService, GoodsIssueService>();
builder.Services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
builder.Services.AddScoped<IStockTransferService, StockTransferService>();


builder.Services.AddScoped<IDocumentQueryService, DocumentQueryService>();
builder.Services.AddScoped<IStockQueryService, StockQueryService>();


//builder.Services.AddScoped<IStockReservationService, StockReservationService>();

builder.Services.AddScoped<IStockService, StockService>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddTransient<IDocumentNumberGenerator,DocumentNumberGenerator>();
builder.Services.AddHostedService<ReservationExpirationJob>();
builder.Services.AddSingleton<ISystemClock, SystemClock>();


builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddWmsMappings();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
