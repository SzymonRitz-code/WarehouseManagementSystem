# Analiza Porównawcza Architektur: WMS vs DDD-Fundamentals

## Spis Treści
1. [Przegląd Architektur](#przegląd-architektur)
2. [WMS Architecture - Event-Driven CQRS](#wms-architecture---event-driven-cqrs)
3. [DDD-Fundamentals Architecture - Pure DDD](#ddd-fundamentals-architecture---pure-ddd)
4. [Porównanie Kluczowych Komponentów](#porównanie-kluczowych-komponentów)
5. [Pros and Cons](#pros-and-cons)
6. [Konsekwencje Architektoniczne](#konsekwencje-architektoniczne)

---

## Przegląd Architektur

### WMS (WarehouseManagementSystem)
**Typ:** Event-Driven Architecture z CQRS (Command Query Responsibility Segregation)

```
┌─────────────────────────────────────────────────────────┐
│                    API Controllers                       │
│         (Command Service + Query Service)               │
└───────────────────┬─────────────────────────────────────┘
					│
		┌───────────┴───────────┐
		│                       │
		v                       v
┌──────────────────┐    ┌──────────────────┐
│  Domain Models   │    │  Application     │
│  (Aggregates)    │    │  Services        │
│  with Events     │    │  (Commands/Query)│
└──────────────────┘    └──────────────────┘
		│                       │
		└───────────┬───────────┘
					│
		┌───────────v───────────┐
		│    UnitOfWork         │
		│  (Transaction Mgmt)   │
		└───────────┬───────────┘
					│
		┌───────────v───────────┐
		│    DbContext (EF)     │
		└───────────┬───────────┘
					│
		┌───────────v───────────┐
		│    SQL Server DB      │
		└───────────────────────┘
```

### DDD-Fundamentals (ClinicManagement)
**Typ:** Clean DDD Architecture z Domain Events + MediatR

```
┌─────────────────────────────────────────────────────────┐
│                   FastEndpoints                          │
│              (Vertical Slice Architecture)              │
└───────────────┬───────────────────────────────────────────┘
				│
		┌───────v────────┐
		│   MediatR      │
		│  (Mediator)    │
		└───────┬────────┘
				│
		┌───────v──────────────────┐
		│  Domain Aggregates       │
		│  with Domain Events      │
		│  (BaseEntity<TId>)       │
		└───────┬──────────────────┘
				│
		┌───────v──────────────────┐
		│  Specifications Pattern  │
		│  (Ardalis)               │
		└───────┬──────────────────┘
				│
		┌───────v──────────────────┐
		│  AppDbContext (EF)       │
		│  Event Publisher         │
		└───────┬──────────────────┘
				│
		┌───────v──────────────────┐
		│    SQL Server DB         │
		└──────────────────────────┘
```

---

## WMS Architecture - Event-Driven CQRS

### Cechy Charakterystyczne:
1. **CQRS Pattern**: Strict separation Command-Query
2. **UnitOfWork Pattern**: Explicitne zarządzanie transakcjami
3. **Repository Pattern**: Dedykowane repo dla każdej agregatu
4. **DTOs**: Wyraźne mapowanie Domain → API
5. **Service Layer**: Oddzielne CommandService i QueryService
6. **Event Publishing**: Asynchroniczny event system

### Przykład - Document Aggregate:
```csharp
// Domain Model - zawiera businesslogic
public class Document
{
	public Guid Id { get; }
	public DocumentStatus Status { get; }

	// Business logic wewnątrz agregatu
	public void Confirm(UserSnapshot confirmedBy)
	{
		if (Status != DocumentStatus.Draft)
			throw new InvalidOperationException();

		ConfirmedByUser = confirmedBy;
		Status = DocumentStatus.Confirmed;
		// Domain event zostanie opublikowany
		AddDomainEvent(new DocumentConfirmedEvent(this));
	}
}

// Application Service (CQRS Command)
public class CreateDocumentCommand
{
	public async Task<CreateDocumentResponse> ExecuteAsync(CreateDocumentRequest request)
	{
		using (var uow = new UnitOfWork(_context))
		{
			var document = new Document(...);
			await uow.Documents.AddAsync(document);
			await uow.SaveChangesAsync(); // Explicit transaction control

			// Event publishing gebeurt tutaj
			return response;
		}
	}
}

// Controller - orchestration
[HttpPost]
public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentRequest request)
{
	var response = await _commandService.ExecuteAsync(request);
	return Ok(response);
}
```

### Transaction Management (UnitOfWork):
```
Kontrola w Application Service:
1.Create UnitOfWork (new DbContext)
2. Get Repository (IDocumentRepository)
3. Perform operations
4. Call SaveChangesAsync()
5. Dispose UnitOfWork

=> JAWNE zarządzanie transakcjami
```

### Query Service:
```csharp
public class DocumentQueryService
{
	// Direktnie DbContext dla reads (bez UnitOfWork)
	public async Task<DocumentListDto[]> GetDocumentsAsync()
	{
		return await _context.Documents
			.AsNoTracking()
			.ProjectTo<DocumentListDto>()
			.ToListAsync();
	}
}
```

### Event Handling:
```csharp
// Domain Event
public class DocumentConfirmedEvent : DomainEvent
{
	public Document Document { get; }
	// ... payload
}

// Event Handler (asynchroniczny)
public class DocumentConfirmedEventHandler : INotificationHandler<DocumentConfirmedEvent>
{
	public async Task Handle(DocumentConfirmedEvent notification, CancellationToken ct)
	{
		// Side effects tutaj
		await _fakeShippingService.NotifyShippingAsync(...);
	}
}
```

---

## DDD-Fundamentals Architecture - Pure DDD

### Cechy Charakterystyczne:
1. **Vertical Slice Architecture**: Każdy endpoint zawiera pełny flow
2. **FastEndpoints**: Zamiast Controllers, mamy endpoint klasy
3. **Specifications Pattern**: Deklaratywne query specyfikacje (Ardalis)
4. **MediatR**: Command/Query na poziomie domain events
5. **Aggregate Roots**: Wyraźnie zaznaczone IAggregateRoot interface
6. **Event Publishing**: Automatic w AppDbContext.SaveChangesAsync()

### Przykład - Room Aggregate:
```csharp
// Aggregate Root - jawnie zaznaczony IAggregateRoot
public class Room : BaseEntity<int>, IAggregateRoot
{
	public string Name { get; set; }

	private Room() { }

	public Room(int id, string name)
	{
		Id = id;
		Name = name;
	}
}

// BaseEntity zawiera domain events
public abstract class BaseEntity<TId>
{
	public TId Id { get; set; }
	public List<BaseDomainEvent> Events = new(); // Automatic event tracking
}

// FastEndpoint - combines Request + Response + Handler
public class List : Endpoint<ListRoomRequest, ListRoomResponse>
{
	private readonly IRepository<Room> _repository;
	private readonly IMapper _mapper;

	public override void Configure()
	{
		Get("api/rooms");
		AllowAnonymous();
	}

	public override async Task<ListRoomResponse> ExecuteAsync(
		ListRoomRequest request, 
		CancellationToken ct)
	{
		var rooms = await _repository.ListAsync(ct);
		return new ListRoomResponse { Rooms = _mapper.Map<List<RoomDto>>(rooms) };
	}
}
```

### Transaction Management:
```
Kontrola w AppDbContext.SaveChangesAsync():
1. MediatR.Publish() happens automatycznie
2. Tracking all domain events w BaseEntity.Events
3. SaveChangesAsync() publishes all events
4. Nema explicit UnitOfWork - DbContext jest jednostką

=> IMPLICIT zarządzanie transakcjami
```

### Event Publishing (Automatic):
```csharp
public override async Task<int> SaveChangesAsync(
	CancellationToken cancellationToken = new())
{
	int result = await base.SaveChangesAsync(cancellationToken);

	// Automatycznie publikuj domain events
	var entitiesWithEvents = ChangeTracker
		.Entries()
		.Select(e => e.Entity as BaseEntity<Guid>)
		.Where(e => e?.Events != null && e.Events.Any())
		.ToArray();

	foreach (var entity in entitiesWithEvents)
	{
		var events = entity.Events.ToArray();
		entity.Events.Clear();
		foreach (var domainEvent in events)
		{
			await _mediator.Publish(domainEvent);
		}
	}

	return result;
}
```

---

## Porównanie Kluczowych Komponentów

### 1. Zarządzanie Agregami

| Aspekt | WMS (CQRS) | DDD-Fundamentals |
|--------|-----------|-----------------|
| **Aggregate Identification** | Convention (Domain folder structure) | Explicit: IAggregateRoot interface |
| **Domain Events Tracking** | Explicit AddDomainEvent() call | Implicit: BaseEntity.Events list |
| **Event Clearing** | Manual in CommandService | Automatic in DbContext |
| **Invariant Enforcement** | Via factory methods i private setters | Via constructors i private fields |

### 2. API Layer

| Aspekt | WMS (CQRS) | DDD-Fundamentals |
|--------|-----------|-----------------|
| **Endpoint Definition** | Controller classes | Endpoint<TRequest,TResponse> |
| **Routing** | [HttpPost], [HttpGet] attributes | Configure() method |
| **Dependency Injection** | Constructor injection | Constructor injection |
| **Request/Response** | Separate DTOs | Built into Endpoint |
| **Validation** | FluentValidation | FastEndpoints validation |
| **Architecture Style** | MVC/MVVM | Vertical Slice |

### 3. Query Handling

| Aspekt | WMS (CQRS) | DDD-Fundamentals |
|--------|-----------|-----------------|
| **Pattern** | QueryService (dedicated class) | IRepository<T> generic |
| **Specifications** | Custom LINQ queries | Ardalis.Specification |
| **Projection** | AutoMapper ProjectTo | AutoMapper ProjectTo |
| **NoTracking** | Explicit .AsNoTracking() | Specification handles it |

### 4. Command Handling

| Aspekt | WMS (CQRS) | DDD-Fundamentals |
|--------|-----------|-----------------|
| **Pattern** | CommandService | MediatR Command |
| **Transaction Control** | UnitOfWork explicitly | DbContext + SaveChangesAsync |
| **Repository Access** | Via UnitOfWork properties | Via IRepository<T> |
| **Event Publishing** | Manual via event service | Automatic in SaveChangesAsync |

### 5. Infrastructure

| Aspekt | WMS (CQRS) | DDD-Fundamentals |
|--------|-----------|-----------------|
| **DbContext** | Passed to repositories | Injected + Mediator for events |
| **Unit of Work** | Explicit pattern | Implicit (DbContext = UoW) |
| **Repository Interface** | Custom per aggregate | Generic IRepository<T> |
| **Event Dispatcher** | Via DomainEventService | Via MediatR in DbContext |

---

## Pros and Cons

### WMS (Event-Driven CQRS)

#### ✅ Advantages:

1. **Explicit Control**
   - Jawne zarządzanie transakcjami (UnitOfWork)
   - Klarowne gdzie co się dzieje
   - Easy debugging i tracing

2. **Clear Separation of Concerns**
   - CommandService vs QueryService
   - Domain Model nie mieszany z infrastructure
   - DTOs jasne oddzielają domain od API

3. **Scalability**
   - Łatwo skalować read/write paths niezależnie
   - Event-driven architecture dla asynchronicznych workflows
   - Natural fit dla microservices

4. **Flexibility**
   - Dokładna kontrola nad transakcjami
   - Można implementować complex transaction scenarios
   - Easy retry logic w commands

5. **Testing**
   - Klarowne mockowanie UnitOfWork
   - Services można testować oddzielnie
   - Clear input/output boundaries

#### ❌ Disadvantages:

1. **Boilerplate Code**
   - CommandService + QueryService dla każdej agregatu
   - Explicit UnitOfWork management
   - Manual event publishing

2. **Complexity**
   - Więcej abstrakcji (UnitOfWork, Repositories, Services)
   - Longer learning curve
   - Więcej plików do maintenance

3. **Transaction Management**
   - Implicit DbContext changes easy to miss
   - Manual SaveChangesAsync() must be called
   - Distributed transaction handling complex

4. **Runtime Errors**
   - Easy to forget SaveChangesAsync()
   - Event handlers może nie być registered
   - Repository operations outside UnitOfWork podem ser problemáticos

---

### DDD-Fundamentals (Pure DDD)

#### ✅ Advantages:

1. **Clean & Concise**
   - Vertical Slice = mniej boilerplate
   - Endpoint zawiera pełny flow
   - Mniej abstrakcji warstw

2. **FastEndpoints**
   - Simplicity - jeden Endpoint class
   - Built-in validation
   - Czysty API design

3. **Automatic Event Publishing**
   - Events publikowane automatycznie w SaveChangesAsync()
   - No manual event service calls
   - Guaranteed event consistency

4. **Clear Aggregate Boundaries**
   - IAggregateRoot interface explicita
   - Easy understanding of domain model
   - Self-documenting code

5. **Specification Pattern**
   - Deklaratywne query composition
   - Reusable specifications
   - Type-safe queries

#### ❌ Disadvantages:

1. **Implicit Transaction Management**
   - DbContext = Unit of Work (implicit)
   - Easy to miss SaveChangesAsync() calls
   - Distributed transactions harder

2. **Limited Separation**
   - Read/Write paths less separated
   - Harder to optimize separately
   - Query optimization less flexible

3. **Generic Repository**
   - IRepository<T> può ser troppo generica
   - Custom query methods need extension
   - Specification learning curve (Ardalis)

4. **Scalability Concerns**
   - Less natural for CQRS read model optimization
   - Event model embedded in DbContext
   - Vertical slices możliwe bottleneck po growth

5. **Testing Complexity**
   - DbContext jako dependency trudne do mockowania
   - Integration tests more necessary
   - Endpoint testing mniej isolowane

---

## Konsekwencje Architektoniczne

### 1. Dodanie Nowej Funkcjonalności

**WMS Approach:**
```
1. Create Domain Model Method
2. Create CommandService (Command + Handler)
3. Create QueryService (if needed)
4. Create Controller Action
5. Add Event Handler(s)
6. Register services in DI
7. Add tests for each layer
```
**Effort**: Średni-Wysoki (~5-7 files)

**DDD Approach:**
```
1. Create/Modify Domain Aggregate
2. Create Endpoint<TRequest, TResponse>
3. Add Specification (if complex query)
4. Register Endpoint in Program.cs
5. Add tests
```
**Effort**: Niski (~2-3 files)

### 2. Transakcje i Consistency

**WMS:**
```csharp
using (var uow = new UnitOfWork(context))
{
	var aggregate = await uow.Repository.GetAsync(id);
	aggregate.DoSomething();
	await uow.SaveChangesAsync(); // ← Must be called explicitly
}
```

**DDD:**
```csharp
var aggregate = await repo.GetByIdAsync(id);
aggregate.DoSomething();
await context.SaveChangesAsync(); // ← Automatic with MediatR integration
```

**Konsekwencja**: WMS wymaga więcej uwagi na transaction boundaries, ale oferuje precyzyjną kontrolę.

### 3. Event-Driven Workflows

**WMS:**
```csharp
// Explicit event publishing w CommandService
var @event = new DocumentConfirmedEvent(document);
await _eventPublisher.PublishAsync(@event);
```

**DDD:**
```csharp
// Automatic w AppDbContext
document.Events.Add(new DocumentConfirmedEvent(document));
await context.SaveChangesAsync(); // MediatR publishes automatically
```

**Konsekwencja**: DDD gwarantuje event consistency (eventos = saved data), WMS wymaga careful implementation.

### 4. Query Optimization

**WMS:**
```csharp
// Dedykowana QueryService może mieć custom optimized queries
public class DocumentQueryService
{
	public async Task<DocumentListDto[]> GetDocumentsOptimizedAsync()
	{
		return await _context.Documents
			.Where(d => d.Status != DocumentStatus.Cancelled)
			.Include(d => d.Items)
			.AsNoTracking()
			.Select(d => new DocumentListDto { ... })
			.ToListAsync();
	}
}
```

**DDD:**
```csharp
// Specifications pattern dla reusable query logic
public class ActiveDocumentsSpecification : Specification<Document>
{
	public ActiveDocumentsSpecification()
	{
		Query.Where(d => d.Status != DocumentStatus.Cancelled)
			 .Include(d => d.Items);
	}
}

var documents = await _repo.ListAsync(new ActiveDocumentsSpecification());
```

**Konsekwencja**: WMS lepiej dla query-intensive scenarios, DDD lepiej dla maintainability.

### 5. Testing Strategy

**WMS:**
```
Domain Tests → CommandService Tests → Controller Tests → Integration Tests
```
- Każdy layer testuje się oddzielnie
- Klarowne mocking boundaries
- Więcej unit tests

**DDD:**
```
Domain Tests → Endpoint Tests → Integration Tests
```
- Mniej layers ale Endpoints trudne do isolation
- Więcej integration tests
- Szybsze do napisania ale wolniejsze do runu

---

## Rekomendacje

### Kiedy wybrać **WMS (CQRS)**:
- ✅ Large scale systems (1000+ queries/sec)
- ✅ Complex transaction scenarios
- ✅ Team z dużym doświadczeniem architektonicznym
- ✅ Separation of read/write optimization crucial
- ✅ Distributed transactions required
- ✅ Microservices architecture planned

### Kiedy wybrać **DDD-Fundamentals (Pure DDD)**:
- ✅ Smaller to medium projects (100-500 queries/sec)
- ✅ Quick time to market important
- ✅ Team preferuje simplicity
- ✅ Single database deployment
- ✅ Vertical slice architecture fits domain
- ✅ Learning curve vs maintenance trade-off

### Hybrid Approach:
Kombinacja obu:
```
- Use DDD for core domain logic (pure aggregates)
- Use CQRS for complex queries (separate read models)
- Use FastEndpoints dla simpler endpoints
- Use Controllers dla complex API logic
- Use MediatR dla command coordination
```

---

## Podsumowanie

| Wymiar | WMS (CQRS) | DDD-Fundamentals |
|--------|-----------|-----------------|
| **Learning Curve** | Średni | Łatwy |
| **Boilerplate** | Dużo | Mało |
| **Flexibility** | Wysoka | Średnia |
| **Scalability** | Wysoka | Średnia |
| **Maintainability** | Średnia | Wysoka |
| **Testing Effort** | Wysoki | Średni |
| **Performance Tuning** | Łatwy | Trudny |
| **Time to Market** | Wolny | Szybki |

Każda architektura ma swoje miejsce. Wybór zależy od **wymagań projektu**, **wielkości zespołu**, i **długoterminowych celów skalowania**.
