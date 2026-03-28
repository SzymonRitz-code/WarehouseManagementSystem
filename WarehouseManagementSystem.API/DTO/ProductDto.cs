using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;
public class ProductDto
{
    public Guid Id { get; set; }
    [Required, StringLength(50)]
    public string? SKU { get; set; }
    [Required, StringLength(200)]
    public string? Name { get; set; }
    public string? Description { get; set; }
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UnitOfMeasure Unit { get; set; }
    [property: Required]
    public bool? RequiresBatch { get; set; }
    [property: Required]
    public bool? IsActive { get; set; }
    [Range(0, double.MaxValue)]
    public decimal? Weight { get; set; }
    [Range(0, double.MaxValue)]
    public decimal? Volume { get; set; }
    [property: Required]
    public DateTimeOffset CreatedAt { get; set; }
}
