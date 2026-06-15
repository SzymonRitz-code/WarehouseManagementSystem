using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.User
{
    public interface IUserService
    {
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
