using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Domain.Model.AuditDomain
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public string EntityName { get; set; }
        public Guid EntityId { get; set; }
        public string Operation { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public DateTimeOffset PerformedAt { get; set; }
        public string? IpAddress { get; set; }

        public Guid PerformedById { get; set; }

        public UserSnapshot PerformedBy { get; set; }

    }
}
