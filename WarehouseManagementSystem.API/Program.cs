using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.DataAccessLayer;
using WarehouseManagementSystem.Domain.Model.AuditDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<WarehouseManagementSystemDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WarehouseManagementSystemConnection")));

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<AuditLog, AuditLogDto>()
        .ForMember(dto => dto.PerformedByName, opt => opt.MapFrom(a => a.PerformedBy.Name))
        .ForMember(dto => dto.PerformedByEmail, opt => opt.MapFrom(a => a.PerformedBy.Email)).ReverseMap();
    cfg.CreateMap<Stock, StockDto>().ReverseMap();
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
