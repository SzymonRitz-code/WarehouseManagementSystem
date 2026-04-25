using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.User
{
    public class UserService
    {
        public static UserSnapshot GetUser()
        {
            return new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"),"Testomir.Testowski@gmail.com", "Testomir");
        }
    }
}
