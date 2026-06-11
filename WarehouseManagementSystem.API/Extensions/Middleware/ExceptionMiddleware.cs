using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.Domain.Exceptions;

namespace WarehouseManagementSystem.API.Extensions.Middleware
{
    // API/Middleware/ExceptionMiddleware.cs
    public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var (statusCode, title) = ex switch
            {
                DocumentNotFoundException => (404, "Document not found"),
                DomainException => (422, "Business rule violated"),
                ArgumentException => (400, "Invalid input"),
                _ => (500, "Internal server error")
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = ex.Message,
                Instance = context.Request.Path
            };

            if (ex is DomainException domainException)
            {
                problem.Extensions["errorCode"] = domainException.ErrorCode;
            }

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
