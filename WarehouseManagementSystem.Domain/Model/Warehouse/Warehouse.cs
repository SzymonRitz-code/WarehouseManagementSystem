using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Domain.Model.WarehouseDomain;

public class Warehouse
{
    private readonly List<WarehouseZone> _zones = new();

    private Warehouse() { } // EF Core

    public Warehouse(
        string code,
        string name,
        string country,
        string city,
        string address)
    {
        Id = Guid.NewGuid();
        SetCode(code);
        SetName(name);
        SetLocation(country, city, address);

        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Country { get; private set; }
    public string City { get; private set; }
    public string Address { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<WarehouseZone> Zones => _zones.AsReadOnly();

    public ICollection<Document> SourceDocuments { get; set; }
    public ICollection<Document> TargetDocuments { get; set; }
    public ICollection<Stock> Stocks { get; set; }

    // ===== Business Methods =====

    public void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Warehouse code cannot be empty.");

        Code = code.Trim().ToUpperInvariant();
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Warehouse name cannot be empty.");

        Name = name.Trim();
    }

    public void SetLocation(string country, string city, string address)
    {
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required.");

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.");

        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.");

        Country = country.Trim();
        City = city.Trim();
        Address = address.Trim();
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        if (_zones.Any())
            throw new InvalidOperationException("Cannot deactivate warehouse with active zones.");

        if (Stocks.Any())
            throw new InvalidOperationException("Cannot deactivate warehouse containing stock.");

        IsActive = false;
    }

    // ===== Zones Management =====

    public WarehouseZone AddZone(
        string code,
        string name,
        TemperatureType temperatureType,
        bool isPickingZone)
    {
        if (_zones.Any(z => z.Code == code))
            throw new InvalidOperationException($"Zone with code '{code}' already exists.");

        var zone = new WarehouseZone(
            code,
            name,
            temperatureType,
            isPickingZone,
            Id);

        _zones.Add(zone);
        return zone;
    }

    public void RemoveZone(Guid zoneId)
    {
        var zone = _zones.FirstOrDefault(z => z.Id == zoneId);

        if (zone == null)
            throw new InvalidOperationException("Zone not found.");

        if (zone.Stocks.Any())
            throw new InvalidOperationException("Cannot remove zone containing stock.");

        _zones.Remove(zone);
    }

    public WarehouseZone GetZone(Guid zoneId)
    {
        var zone = _zones.FirstOrDefault(z => z.Id == zoneId);

        if (zone == null)
            throw new InvalidOperationException("Zone not found.");

        return zone;
    }
}
