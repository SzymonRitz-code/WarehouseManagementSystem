using FluentValidation;
using WarehouseManagementSystem.API.DTO;

namespace WarehouseManagementSystem.API.Validators;

public class DocumentItemCommandDtoValidator : AbstractValidator<DocumentItemCommandDto>
{
    public DocumentItemCommandDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
    }
}
