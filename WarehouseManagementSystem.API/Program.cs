using Microsoft.EntityFrameworkCore;
using Serilog;
using WarehouseManagementSystem.API.Extensions.Middleware;
using WarehouseManagementSystem.API.ServiceExtensions;
using WarehouseManagementSystem.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddWmsMvc();
builder.Services.AddWmsSwagger();
builder.Services.AddWmsInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddWmsApplicationServices();
builder.Services.AddWmsMessaging(builder.Configuration);
builder.Services.AddWmsAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddWmsHealthChecks(builder.Configuration);

builder.Services.AddResponseCaching();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWmsClient", policy =>
    {
        policy.WithOrigins("https://localhost:4200", "https://localhost:4201")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger"));
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowWmsClient");
app.UseResponseCaching();
app.UseSerilogRequestLogging();

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program { }