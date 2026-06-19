using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace WarehouseManagementSystem.API.Extensions;

public static class ApiBehaviorExtensions // dodanie niestandardowego zachowania dla błędów walidacji w API, aby zwracać odpowiedź 422 Unprocessable Entity zamiast domyślnej odpowiedzi 400 Bad Request.
{
    public static IMvcBuilder AddWmsApiBehavior(this IMvcBuilder mvcBuilder)
    {
        return mvcBuilder.ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetailsFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                var validationProblemDetails = problemDetailsFactory.CreateValidationProblemDetails(
                    context.HttpContext,
                    context.ModelState,
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Validation errors occurred.",
                    detail: "See the errors field for details.",
                    instance: context.HttpContext.Request.Path);

                return new UnprocessableEntityObjectResult(validationProblemDetails)
                {
                    ContentTypes = { "application/problem+json" }
                };
            };
        });
    }
}
