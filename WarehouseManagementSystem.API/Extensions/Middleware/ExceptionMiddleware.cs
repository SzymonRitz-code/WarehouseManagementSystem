using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.Domain.Exceptions;

namespace WarehouseManagementSystem.API.Extensions.Middleware
{
    // API/Middleware/ExceptionMiddleware.cs
    /// <summary>
    /// Middleware for handling exceptions in the WMS application.
    /// </summary>
    /// <param name="next"></param>
    /// <param name="logger"></param>
    public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        /// <summary>
        /// Invokes the middleware to handle exceptions that occur during the processing of HTTP requests.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Handles the exception and generates an appropriate HTTP response based on the type of exception.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var (statusCode, title) = ex switch
            {
                NotFoundDomainException => (StatusCodes.Status404NotFound, ex.Message),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, ex.Message),
                ArgumentException => (StatusCodes.Status400BadRequest, ex.Message),
                DomainException => (StatusCodes.Status422UnprocessableEntity, ex.Message),
                InvalidOperationException => (StatusCodes.Status409Conflict, ex.Message),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var detail = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : ex.Message;

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
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
