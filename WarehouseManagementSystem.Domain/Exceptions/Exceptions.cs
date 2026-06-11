namespace WarehouseManagementSystem.Domain.Exceptions;

public abstract class DomainException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

// DocumentNotFoundException.cs
public class DocumentNotFoundException(Guid documentId)
    : DomainException("DOCUMENT_NOT_FOUND", $"Document {documentId} was not found.");

// DocumentNotInDraftStateException.cs
public class DocumentNotInDraftStateException(Guid documentId)
    : DomainException("DOCUMENT_NOT_IN_DRAFT_STATE", $"Document {documentId} is not in Draft state.");

// CannotConfirmEmptyDocumentException.cs
public class CannotConfirmEmptyDocumentException(Guid documentId)
    : DomainException("CANNOT_CONFIRM_EMPTY_DOCUMENT", $"Document {documentId} cannot be confirmed without items.");

// InsufficientStockException.cs
public class InsufficientStockException(Guid productId, decimal requested, decimal available)
    : DomainException("INSUFFICIENT_STOCK", $"Insufficient stock for product {productId}. Requested: {requested}, Available: {available}.");

// DocumentAlreadyCancelledException.cs
public class DocumentAlreadyCancelledException(Guid documentId)
    : DomainException("DOCUMENT_ALREADY_CANCELLED", $"Document {documentId} is already cancelled.");
