using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Domain.Model.CatalogDomain;

public class Product
{
    #region Constructors

    private Product() { } // EF

    public Product(
        string sku,
        string name,
        UnitOfMeasure unit,
        bool requiresBatch,
        UserSnapshot createdByUser,
        decimal? weight = null,
        decimal? volume = null,
        string? description = null)
    {
        Id = Guid.NewGuid();
        SetSku(sku);
        SetName(name);
        SetUnit(unit);
        SetDescription(description);
        SetWeight(weight);
        SetVolume(volume);

        RequiresBatch = requiresBatch;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedByUser = createdByUser;
    }

    #endregion

    #region Properties

    public Guid Id { get; private set; }
    public string SKU { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public UnitOfMeasure Unit { get; private set; }
    public bool RequiresBatch { get; private set; }
    public bool IsActive { get; private set; }
    public decimal? Weight { get; private set; }
    public decimal? Volume { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public UserSnapshot CreatedByUser { get; private set; }

    #endregion

    #region Setters and Status Operations

    public void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("SKU cannot be empty.");
        }

        SKU = sku.Trim().ToUpperInvariant();
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.");
        }

        Name = name.Trim();
    }

    public void SetUnit(UnitOfMeasure unit)
    {
        Unit = unit;
    }

    public void SetDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }

    public void SetWeight(decimal? weight)
    {
        if (weight < 0)
        {
            throw new ArgumentException("Weight cannot be negative.");
        }

        Weight = weight;
    }

    public void SetVolume(decimal? volume)
    {
        if (volume < 0)
        {
            throw new ArgumentException("Volume cannot be negative.");
        }

        Volume = volume;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void RequireBatchTracking() => RequiresBatch = true;
    public void DisableBatchTracking() => RequiresBatch = false;

    #endregion
}
