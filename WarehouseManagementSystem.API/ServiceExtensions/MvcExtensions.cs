using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using WarehouseManagementSystem.API.Extensions;
using WarehouseManagementSystem.API.Validators;

namespace WarehouseManagementSystem.API.ServiceExtensions;

public static class MvcExtensions
{
    public static IServiceCollection AddWmsMvc(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add(new AuthorizeFilter());
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
            });
        })
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        })
        .AddWmsApiBehavior();

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<CreateDocumentDtoValidator>();
        services.AddEndpointsApiExplorer();

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"));
        });

        return services;
    }
}
