using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.Domain.Model.Documents;

public class DocumentSequence
{
    public Guid Id { get; set; }

    /// <summary>
    /// Typ dokumentu (PZ, WZ, MM itd.)
    /// </summary>
    public DocumentType Type { get; set; }

    /// <summary>
    /// Rok numeracji
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Opcjonalnie numeracja per magazyn
    /// </summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>
    /// Ostatnio użyty numer
    /// </summary>
    public int LastNumber { get; set; }
}
