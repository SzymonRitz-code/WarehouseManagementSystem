 namespace WarehouseManagementSystem.Domain.Exceptions;

// DocumentNotFoundException.cs
public class DocumentNotFoundException(Guid documentId)
    : Exception($"Document {documentId} was not found.");

// DocumentNotInDraftStateException.cs
public class DocumentNotInDraftStateException(Guid documentId)
    : Exception($"Document {documentId} is not in Draft state.");

// CannotConfirmEmptyDocumentException.cs
public class CannotConfirmEmptyDocumentException(Guid documentId)
    : Exception($"Document {documentId} cannot be confirmed without items.");

// InsufficientStockException.cs
public class InsufficientStockException(Guid productId, decimal requested, decimal available)
    : Exception($"Insufficient stock for product {productId}. Requested: {requested}, Available: {available}.");

// DocumentAlreadyCancelledException.cs
public class DocumentAlreadyCancelledException(Guid documentId)
    : Exception($"Document {documentId} is already cancelled.");
