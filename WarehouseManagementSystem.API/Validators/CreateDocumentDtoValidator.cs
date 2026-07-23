using FluentValidation;
using WarehouseManagementSystem.API.DTO;

namespace WarehouseManagementSystem.API.Validators;

public class CreateDocumentDtoValidator : AbstractValidator<CreateDocumentDto>
{
    public CreateDocumentDtoValidator()
    {
        RuleFor(x => x.DocumentDate)
            .NotEmpty().WithMessage("DocumentDate is required.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid document type.");

        RuleFor(x => x.SourceWarehouseId)
            .NotEmpty().WithMessage("SourceWarehouseId is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.")
            .When(x => x.Notes != null);

        RuleFor(x => x.Items)
            .NotNull().WithMessage("Items are required.")
            .NotEmpty().WithMessage("Document must have at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("ProductId is required.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        });
    }
}
