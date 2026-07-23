using FluentValidation;
using WarehouseManagementSystem.API.DTO;

namespace WarehouseManagementSystem.API.Validators;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(50).WithMessage("SKU cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Unit)
            .IsInEnum().WithMessage("Invalid unit of measure.");

        RuleFor(x => x.Weight)
            .GreaterThanOrEqualTo(0).WithMessage("Weight cannot be negative.")
            .When(x => x.Weight.HasValue);

        RuleFor(x => x.Volume)
            .GreaterThanOrEqualTo(0).WithMessage("Volume cannot be negative.")
            .When(x => x.Volume.HasValue);
    }
}
