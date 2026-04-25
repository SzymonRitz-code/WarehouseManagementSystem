namespace WarehouseManagementSystem.Domain.ValueObjects;
public class UserSnapshot
{
    public Guid Id { get; }
    public string Email { get; }
    public string Name { get; }

    private UserSnapshot() { } // dla EF

    public UserSnapshot(Guid id, string email, string name)
    {
        Id = id;
        Name = name;
        Email = email;
    }
}
