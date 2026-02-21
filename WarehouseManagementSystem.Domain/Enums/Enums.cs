using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarehouseManagementSystem.Domain.Enums
{
    public enum DocumentType
    {
        PZ,
        WZ,
        MM,
        ADJ
    }

    public enum DocumentStatus
    {
        Draft,
        Confirmed,
        Cancelled
    }

    public class Document
    {
        public Guid Id { get; set; }
        public DocumentType Type { get; set; }
        public DocumentStatus Status { get; set; }
    }
}
