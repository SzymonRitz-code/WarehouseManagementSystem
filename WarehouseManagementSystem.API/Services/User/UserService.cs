using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.User
{
    public class UserService
    {
        public static UserSnapshot GetUser()
        {
            return new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"),"Testomir.Testowski@gmail.com", "Testomir");
        }

        // TODO - Dokończyć implementację użytkownika - Przetestować czy po zalogowaniu użytkownik jest w sub
        public static UserSnapshot GetUser(HttpContext context)
        {
            var sub = context.User.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException("User identity not found in token.");

            var name = context.User.FindFirst("name")?.Value ?? "Unknown";
            var email = context.User.FindFirst("email")?.Value ?? string.Empty;

            return new UserSnapshot(
                id: Guid.Parse(sub),
                name: name,
                email: email
            );
        }
    }
}
