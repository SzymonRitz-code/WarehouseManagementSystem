using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Domain.Model.InventoryDomain;

public class ProductBatch
{
    #region Properties

    public Guid Id { get; private set; }
    public string BatchNumber { get; private set; } = null!;

    public DateOnly? ExpirationDate { get; private set; }
    public DateOnly? ManufacturedDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public UserSnapshot CreatedByUser { get; private set; }

    public Guid ProductId { get; private set; }

    public virtual Product Product { get; private set; }

    #endregion

    #region Constructors

    // EF Core
    private ProductBatch() { }

    public ProductBatch(
        Guid productId,
        string batchNumber,
        UserSnapshot createdByUser,
        DateOnly? manufacturedDate = null,
        DateOnly? expirationDate = null)
    {
        Id = Guid.NewGuid();
        ProductId = productId;

        SetBatchNumber(batchNumber);
        SetManufacturingDates(manufacturedDate, expirationDate);

        CreatedAt = DateTimeOffset.UtcNow;
        CreatedByUser = createdByUser;
    }

    #endregion

    #region Domain Behavior

    public void SetBatchNumber(string batchNumber)
    {
        if (string.IsNullOrWhiteSpace(batchNumber))
        {
            throw new ArgumentException("Batch number is required.");
        }

        if (batchNumber.Length > 50)
        {
            throw new ArgumentException("Batch number cannot exceed 50 characters.");
        }

        BatchNumber = batchNumber.Trim();
    }

    public void SetManufacturingDates(DateOnly? manufacturedDate, DateOnly? expirationDate)
    {
        if (manufacturedDate.HasValue &&
            manufacturedDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentException("Manufactured date cannot be in the future.");
        }

        if (expirationDate.HasValue && manufacturedDate.HasValue &&
            expirationDate.Value < manufacturedDate.Value)
        {
            throw new ArgumentException("Expiration date cannot be earlier than manufactured date.");
        }

        ManufacturedDate = manufacturedDate;
        ExpirationDate = expirationDate;
    }

    public bool IsExpired()
    {
        return !ExpirationDate.HasValue ? false : ExpirationDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public bool ExpiresSoon(int daysThreshold)
    {
        if (!ExpirationDate.HasValue)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return ExpirationDate.Value <= today.AddDays(daysThreshold);
    }

    #endregion
}
