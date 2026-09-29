# Kontekst projektu: WarehouseManagementSystem

_Pakiet przekazania kontekstu do użycia na innym komputerze / w nowym czacie. Wygenerowano: 2026-08-11._

## Cel projektu

`WarehouseManagementSystem` jest projektem edukacyjnym WMS, rozwijanym jako laboratorium architektury .NET, DDD i niezawodnych integracji asynchronicznych. Celem jest nie tylko działający CRUD, ale możliwość wyjaśnienia decyzji projektowych na rozmowie rekrutacyjnej: granic transakcji, idempotencji, Outbox/Inbox, retry, DLQ, eventual consistency i reconciliation.

## Architektura i obecny kierunek

- WMS jest systemem centralnym; rozwiązanie ma podział `API` / `Domain` / `Infrastructure` oraz klienta.
- `API` oraz `Idp` są aplikacjami webowymi (`Microsoft.NET.Sdk.Web`, `WebApplication.CreateBuilder`) i mogą działać pod IIS/IIS Express.
- `FakeERP`, `FakeBilling` i `FakeShipping` są workerami (`Microsoft.NET.Sdk.Worker`, `Host.CreateApplicationBuilder`) bez HTTP pipeline. Powinny działać jako niezależne procesy/hosted services (np. Docker, Windows Service), nie pod IIS — IIS może usypiać/recyklować worker bez ruchu HTTP.
- RabbitMQ jest brokerem wiadomości; nie jest Outboxem. Azure Service Bus pełni podobną rolę jako usługa zarządzana. „Message bus” może oznaczać styl komunikacji albo bibliotekę-abstrakcję (np. MassTransit).
- `docker-compose.yml` uruchamia RabbitMQ w wariancie management. Lokalny panel: `http://localhost:15672`, login/hasło `guest` / `guest`.

## Wdrożone integracje i ich rola

### WMS -> Shipping

Potwierdzenie dokumentu WMS tworzy `DocumentConfirmedIntegrationEvent` w Transactional Outbox. Worker publikuje komunikat do RabbitMQ; FakeShipping konsumuje go, stosuje retry, DLQ i idempotencję/`ProcessedMessages`, a następnie tworzy symulowaną wysyłkę.

### WMS -> Billing

FakeBilling jest drugim, niezależnym konsumentem potwierdzeń dokumentu. Ćwiczy idempotencję techniczną (`MessageId`) i biznesową: ten sam dokument nie może zostać rozliczony drugi raz (np. przez `ExternalOrderId` / `SourceDocumentId`).

### FakeERP -> WMS -> FakeERP

ERP tworzy lokalne `ErpWarehouseOrder` i w tej samej transakcji odkłada wiadomość do własnego Outboxa. Publikuje command-like `erp.document.create` do kolejki `wms.erp-document-create`. WMS zapisuje Inbox/`ProcessedMessage`, tworzy dokument przez istniejącą logikę aplikacyjną (nie bezpośredni `DbContext`) i ACKuje dopiero po trwałym zapisie. Po późniejszym potwierdzeniu dokumentu WMS publikuje potwierdzenie z tym samym `CorrelationId`; ERP aktualizuje zlecenie na `Confirmed`. Redelivery nie może utworzyć drugiego dokumentu ani ponownie zmienić stanu zlecenia.

## Najważniejsze zasady integracyjne

- Zmiana biznesowa i wpis Outboxa są zapisywane w jednej transakcji. Publikacja do brokera jest późniejszym zadaniem workera; nie publikować bezpośrednio z kontrolera ani tuż po `SaveChanges` w command service.
- Zakładać `at-least-once delivery`. Consumer może paść po zapisie skutku, ale przed ACK, więc musi być idempotentny.
- Używać: `MessageId`, `CorrelationId`, `OccurredAt`, `ProcessedAt`, statusów Outboxa (`Pending`, `Published`, `Failed`), retry count, Inbox/`ProcessedMessages` i DLQ.
- Kontrakty integracyjne są stabilnymi DTO w osobnym projekcie kontraktów; nie przesyłać encji EF ani modeli domenowych. Zmiany kontraktów zachowywać kompatybilne wstecz.
- Eventy wyrażają fakty biznesowe. Komunikat ERP do utworzenia dokumentu jest świadomie command-like i wymaga jednoznacznego ownershipu procesu.

## Monitoring i reconciliation

Monitoring odpowiada na pytanie „czy proces działa teraz?” — obserwuje logi, alerty, kolejki, retry i opóźnienia. Reconciliation odpowiada „czy dane i skutki biznesowe po obu stronach są zgodne?” — okresowo porównuje źródła danych i raportuje rozbieżności.

Minimalny moduł Monitoring/Reconciliation jest dla tego projektu **must-have** (nie musi być osobnym fake systemem), bo domyka pytanie rekrutacyjne: „skąd wiesz, że wszystkie wiadomości zostały rozliczone?”. Ma raportować:

- potwierdzone dokumenty WMS bez shipmentu;
- dokumenty WZ bez faktury;
- zlecenia ERP bez utworzonego albo potwierdzonego dokumentu WMS;
- rekordy Outbox `Failed` i `Abandoned`;
- wiadomości w DLQ;
- opóźnienie: `OccurredAt` -> publikacja -> końcowy skutek biznesowy.

Sama liczba komunikatów w kolejce nie stanowi dowodu poprawnego rozliczenia. Należy sprawdzić Outbox, Inbox/ProcessedMessages, DLQ, metryki oraz oczekiwany efekt biznesowy (np. `DocumentConfirmed` -> `ShipmentCreated`).

## DDD: agregaty i transakcje

`IAggregateRoot` to marker granicy modelu/repozytorium; sam w sobie nie uruchamia transakcji. Transakcję wyznacza przypadek użycia.

Proponowane rooty:

- `Document` z `DocumentItem`;
- `Stock` z `StockReservation`;
- `Product`;
- `ProductBatch` (niezależny, referencjonowany przez zapas/dokument);
- prawdopodobnie `Warehouse` z `WarehouseZone` — decyzję trzeba ujednolicić z API/repozytoriami.

Repozytoria powinny być wystawiane dla rootów. `DocumentItem` i `StockReservation` są dziećmi agregatów. `WarehouseZone` wymaga decyzji: albo jest dzieckiem `Warehouse` (wtedy nie osobne API/repozytorium), albo niezależnym rootem (a reguły między nim i magazynem realizuje application service w transakcji).

Kluczowe granice transakcji:

- potwierdzenie dokumentu: `Document` + wiele `Stock` + `DocumentSequence` + audyt + Outbox; obecna transakcja `Serializable` jest uzasadniona;
- metody aktualizujące stock podczas `ConfirmDocumentAsync` nie powinny wykonywać `SaveChangesAsync` dla każdej pozycji — jedno zapisanie na końcu komendy;
- rezerwacja/zwolnienie/realizacja/wygaśnięcie: jedna transakcja dla jednego `Stock`, z `RowVersion`;
- transfer między lokalizacjami: jedna transakcja dla dwóch `Stock`, pobieranych w stałej kolejności dla ograniczenia deadlocków.

Znana luka: `StockReservation` nie ma `DocumentId`, przez co anulowanie dokumentu może odnaleźć rezerwację pośrednio po produkcie/strefie i zwolnić cudzą rezerwację. Dodać `DocumentId` (opcjonalny dla rezerwacji niedokumentowych) i tworzyć rezerwacje z jednoznacznym źródłem.

Zdarzenia domenowe były dotąd tylko logowane i czyszczone przed zapisem; należy albo usunąć pozorny dispatcher, albo poprawnie przekształcać zdarzenia `Document` do Outboxa w tej samej transakcji.

## Kolejność dalszej nauki/rozwoju

1. Utrzymać i rozumieć fundament: RabbitMQ, Outbox, publisher worker, retry, DLQ, idempotent consumer.
2. Domknąć minimalny Monitoring/Reconciliation jako worker albo moduł administracyjny.
3. Po kursie DDD przejrzeć granice agregatów i kontrakty eventów.
4. Dopiero później rozważyć `Inventory Mirror` (projekcja, replay, ordering, naprawa rozjazdów).
5. Na wyższym poziomie: saga/process manager dla procesu wieloetapowego.

Wystarczający, mocny zestaw edukacyjny: WMS, Shipping, Billing, ERP i Monitoring/Reconciliation. Nie warto dodawać wielu systemów o identycznym zachowaniu; ważniejsze jest pokrycie różnych klas problemów.

## Historia ważnych commitów

- `5268cd5` — `feat(domain): record document lifecycle events`
- `fbdb33e` — `refactor(warehouse): encapsulate aggregate collections`
- `65c5f36` — `fix(cache): scope stampede locks per service instance`
- `7a17f47` — `feat(documents): return optimized read models`
- `a504e47` — `feat(api): modularize configuration and validation`
- `bf4c6c8` — `test(api): add controller and SQL Server integration coverage`
- `16ae250` — `feat(contracts): extract document confirmation event contract`
- `a734fe7` — `refactor(shipping): isolate consumer retry policy`
- `26a2098` — `feat(billing): add idempotent document billing consumer`
- `515d989` — `refactor(code): normalize formatting and imports`
- `14278a5` — `feat(erp): add idempotent ERP document inbox flow`
- `28a8a6a` — `refactor(tests): format integration event fixtures`
- `dd0c17c` — `docs(erp): describe inbox and outbox flow`

Historyczna weryfikacja po tych zmianach: build przechodził bez błędów; raportowano 333 istniejące ostrzeżenia. Zestaw testów obejmował 298 przechodzących testów, a 21 testów Testcontainers wymagało działającego Dockera.

## Jak użyć na drugim laptopie

1. Skopiuj ten plik razem z repozytorium (najlepiej przez Git).
2. W nowym czacie Codexa dołącz `PROJECT_CONTEXT.md` albo napisz: „Przeczytaj `PROJECT_CONTEXT.md` i traktuj go jako utrwalony kontekst projektu.”
3. Następnie wskaż konkretne zadanie; plik zawiera decyzje i ustalenia, a kod w repozytorium pozostaje źródłem prawdy dla aktualnego stanu implementacji.
