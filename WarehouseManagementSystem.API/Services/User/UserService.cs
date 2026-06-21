using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.User
{
    /// <summary>
    /// Defines operations for reading data of the currently authenticated user.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Gets a user snapshot from the HTTP context.
        /// </summary>
        /// <param name="context">HTTP context containing authenticated user data.</param>
        /// <returns>User snapshot with identifier, name, and email address.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the token does not contain a user identifier or the identifier has an invalid format.</exception>
        UserSnapshot GetUser(HttpContext context);
    }
    public class UserService : IUserService
    {
        //public UserSnapshot GetUser()
        //{
        //    return new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Testomir.Testowski@gmail.com", "Testomir");
        //}

        public UserSnapshot GetUser(HttpContext context)
        {
            var sub = context.User.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException("User identity not found in token.");

            if (!Guid.TryParse(sub, out var userId))
                throw new UnauthorizedAccessException("User identity in token is invalid.");

            var name = context.User.FindFirst("name")?.Value ?? "Unknown";
            var email = context.User.FindFirst("email")?.Value ?? string.Empty;

            return new UserSnapshot(
                id: userId,
                name: name,
                email: email
            );
        }
    }
}
