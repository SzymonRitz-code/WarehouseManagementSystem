namespace WarehouseManagementSystem.Domain.Model.SecurityDomain
{
    public class User
    {
        public Guid Id { get; set; }         // ID z IdP
        public string Email { get; set; }    // opcjonalnie, do wyświetlania
        public string Name { get; set; }     // opcjonalnie
    }

}
