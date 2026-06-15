namespace WarehouseManagementSystem.Domain.Exceptions;

public abstract class DomainException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

public abstract class NotFoundDomainException(string errorCode, string message)
    : DomainException(errorCode, message);

// DocumentNotFoundException.cs
public class DocumentNotFoundException(Guid documentId)
    : NotFoundDomainException("DOCUMENT_NOT_FOUND", $"Document {documentId} was not found.");

// StockNotFoundException.cs
public class StockNotFoundException(Guid stockId)
    : NotFoundDomainException("STOCK_NOT_FOUND", $"Stock {stockId} was not found.");

// ReservationNotFoundException.cs
public class ReservationNotFoundException(Guid reservationId)
    : NotFoundDomainException("RESERVATION_NOT_FOUND", $"Reservation {reservationId} was not found.");

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

public class MissingTargetWarehouseForMmDocumentException(Guid documentId)
    : DomainException("TARGET_WAREHOUSE_REQUIRED_FOR_MM", $"Document {documentId} requires a target warehouse for MM confirmation.");

public class MissingSourceWarehouseForDocumentException(Guid documentId)
    : DomainException("SOURCE_WAREHOUSE_REQUIRED", $"Document {documentId} requires a source warehouse.");

public class MissingSourceZoneForDocumentException(Guid documentId)
    : DomainException("SOURCE_ZONE_REQUIRED", $"Document {documentId} requires a source zone.");

public class MissingTargetZoneForDocumentException(Guid documentId)
    : DomainException("TARGET_ZONE_REQUIRED", $"Document {documentId} requires a target zone.");
