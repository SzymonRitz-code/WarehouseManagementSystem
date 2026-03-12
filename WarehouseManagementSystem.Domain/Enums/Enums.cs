namespace WarehouseManagementSystem.Domain.Enums;

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
    Transfer,
    Completed,
    Cancelled
}
public enum ReservationStatus
{
    Active = 1,
    Released = 2,
    Fulfilled = 3,
    Cancelled = 4,
    Expired = 5
}

public enum TemperatureType
{
    Ambient = 0,
    Cold = 1,
    Frozen = 2
}
public enum UnitOfMeasure
{
    Piece,
    Kilogram,
    Gram,
    Liter,
    Milliliter,
    Meter,
    SquareMeter,
    CubicMeter,
    Pallet,
    Box
}