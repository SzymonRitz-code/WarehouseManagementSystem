# Przegląd i realizacja zadań — 20 października 2026

Przegląd obejmuje własny kod backendu, klienta Angular, dokumentację i konfigurację repozytorium.
Biblioteki dostawców, zależności oraz pliki wygenerowane nie są listą zadań WMS.
Zmiany integracji i realizacji zadań są niezależne od eksperymentalnych lookupów. Na późniejszą prośbę usunięto LookupsController, jego DTO oraz aliasy tras api/app; dotychczasowe adresy API pozostały.

## Rozliczenie oznaczeń

| Obszar i pierwotne zadanie | Ustalenie i rozwiązanie |
| --- | --- |
| Pobieranie partii zgodnie z UI | Już zaimplementowane: klient korzysta z tras produktu, a query service zwraca DTO z sumami stanów i ilością dostępną. Usunięto nieaktualne oznaczenie i doprecyzowano XML summary. |
| Angular: dane produktu przychodzą później | Już rozwiązane przez ngIf z async i aliasem product: zawartość powstaje po odebraniu produktu. Usunięto nieaktualny komentarz. |
| Asynchroniczny konsument | Shipping i ERP używają AsyncEventingBasicConsumer oraz await; fabryka włącza asynchroniczny dispatch. |
| Rozdzielenie błędów | Niepoprawny JSON/kontrakt lokalnego shipping trafia do DLQ, a awarie przetwarzania korzystają z limitowanego retry. Kopia retry jest potwierdzana przez brokera przed ACK oryginału. |
| Walidacja semantyczna i schemat | DocumentConfirmedContract.Validate sprawdza identyfikatory, typ, użytkownika, pozycje, dodatnie ilości i obsługiwane wersje. |
| Wyścig deduplikacji | Obsługa konfliktu indeksu istniała. Uzupełniono ją o weryfikację trwałego efektu po DocumentId, również przy wyścigu z odbudową projekcji. |
| Kolejność i wersjonowanie | Kontrakt zawiera wersję schematu i agregatu. Domena dopuszcza jedno potwierdzenie: wersję 1 reprezentuje trwały, unikalny DocumentId projekcji. Inne wersje są odrzucane. |
| Reconciliation i odbudowa | Nowy okresowy proces porównuje potwierdzone dokumenty z lokalnymi projekcjami i odtwarza wyłącznie brakujące rekordy, po maksymalnie 100 na przebieg. |
| Awaria przed ACK i równoczesne dostawy | Dodano test ponownej dostawy po zapisie oraz test równoległych dostaw na SQL Server: jeden shipment i jeden znacznik przetworzenia. |
| Recovery, heartbeat, nazwa połączenia | Włączono recovery/topology recovery, heartbeat 30 s, nazwę wms-api i ponawianie początkowego połączenia konsumentów. |
| Długowieczne połączenie publishera | Worker zachowuje zdrowy kanał/połączenie; po błędzie odtwarza sesję. Pętla stosuje backoff i jitter. |
| Publisher confirms i brak routingu | Confirms już istniały. Dodano mandatory i obsługę BasicReturn: zwrócona wiadomość nie dostaje statusu Published. |
| Kilka instancji publishera | Transakcja SQL Server i blokady UPDLOCK/READPAST/READCOMMITTEDLOCK zapewniają wyłączne pobranie wpisów. Zakończenie procesu zwalnia blokady przez rollback. |
| Retry publishera | Limit prób, NextAttemptAt i Abandoned już istniały. Dodano wykładniczy backoff z jitterem i log błędu wymagającego interwencji po wyczerpaniu prób. |
| Retencja i metryki | Published: domyślnie 30 dni, czyszczenie po 500 wpisów. Instrumenty .NET mierzą backlog, wiek, czas publikacji i błędy. |
| Testy awarii publishera | Dodano awarię przed publikacją, po publikacji przed zapisem oraz ponowienie po commicie; dodatkowo test dwóch publisherów na SQL Server. |
| Retencja deduplikacji | Przyjęto trwałe zachowanie identyfikatorów: ręczny replay nie ma limitu wieku. Kasowanie znaczników bez ograniczenia replay osłabiłoby idempotencję. Retencja payloadów jest niezależna. |

## Metody i problem, który rozwiązują

- `PublishPendingMessagesAsync`: zapobiega pobraniu tych samych wpisów przez wiele instancji i uznaniu niepotwierdzonej wiadomości za opublikowaną. Zapisuje wynik partii w transakcji; zbiera metryki i usuwa stare opublikowane payloady.
- `GetChannel`: usuwa koszt otwierania połączenia przy każdym pollingu; odbudowuje uszkodzoną sesję.
- `OutboxPublisherWorker.ExecuteAsync`: utrzymuje publikację po awarii brokera i ogranicza tempo prób ponownego połączenia.
- `OutboxMessageRetry.MarkFailed`: rozkłada ponowienia w czasie i zatrzymuje nieskończone próby dla uszkodzonych wiadomości.
- `ConfirmedMessagePublisher.Publish`: chroni oryginalną dostawę przed utratą, gdy publikacja jej kopii retry nie zostanie przyjęta lub nie ma routingu.
- `DocumentConfirmedContract.Validate`: nie pozwala traktować poprawnego składniowo JSON jako poprawnego zdarzenia biznesowego bez walidacji treści.
- `ShippingDocumentConfirmedConsumer.HandleMessageAsync`: zapisuje efekt i znacznik razem oraz bezpiecznie obsługuje powtórki i wyścigi dostaw.
- `ShippingProjectionReconciler.ReconcileAsync`: wykrywa i odbudowuje brakujące lokalne projekcje, których nie naprawi samo retry istniejących wiadomości.

Opisy problemów dodano również do XML `<summary>` metod implementacji.

## Dodatkowy błąd wykryty przez regresję

Lokalny alias kontrolera audytu powodował przypisanie nazwy GetAuditLog do dwóch różnych szablonów
tras. ASP.NET Core odrzuca taką konfigurację już przy starcie aplikacji. Zachowano oba adresy,
a nazwę trasy przypisano wyłącznie do oryginalnej, jawnej trasy. Istniejące testy API weryfikują
uruchomienie hosta i odpowiedzi autoryzacji.

## Zakres i ograniczenia

Lokalna projekcja Shipping jest opcjonalna (`Messaging:LocalShippingProjectionEnabled`, domyślnie false)
i ma własne kolejki. Domyślny scenariusz Compose nadal używa zewnętrznego FakeShipping.
Reconciliation dotyczy tabeli WMS ShippingShipments; nie modyfikuje zewnętrznej bazy ani nie uruchamia
ponownie fizycznej wysyłki. Nowy strumień zdarzeń lifecycle wymaga osobnego kontraktu i polityki kolejności.

Publisher trzyma blokady bazy w czasie publikacji partii: to świadomy kompromis prostoty i wyłączności.
Gwarancją pozostaje at-least-once; awaria po przyjęciu przez RabbitMQ przed commitem może spowodować
powtórną publikację. Testy SQL używają rzeczywistego SQL Server, a zachowania brokera symulują przez mocki;
nie zastępują pełnego testu awarii rzeczywistego klastra RabbitMQ.
Instrumenty i logi są dostępne dla kolektora; wysyłka alertów wymaga konfiguracji monitoringu wdrożenia.

## Weryfikacja

- Kompilacja całego rozwiązania zakończona powodzeniem. Istniejące ostrzeżenia kompilatora pozostają poza zakresem tego przeglądu.
- Pełna regresja: 342 testy, 337 zaliczonych i 5 błędów startu API spowodowanych konfliktem nazw tras.
- Po poprawce tras oraz końcowych zmianach w integracji ponowiono wszystkie zmienione obszary i testy API: **22/22 zaliczone**, w tym 5 testów na rzeczywistym SQL Server.
- Pozostałe 320 testów przeszło w pełnym przebiegu. Nie powtarzano ich po zmianie dotyczącej tras i integracji wiadomości.
- Testy awarii obejmują publikację przed wysłaniem, po wysłaniu przed commitem i po commicie; test równoczesnych dostaw wymusza spotkanie obu operacji przed zapisem, żeby rzeczywiście sprawdzić konflikt unikalności.
- Testy obejmują również walidację, backoff, potwierdzenie i routing kopii retry, ponowne dostarczenie oraz idempotentną odbudowę brakującej projekcji.
- Kontrola różnic nie wykazała błędów whitespace. Ponowne wyszukanie nie znalazło pozostawionych znaczników niewykonanej pracy we własnym kodzie.
- Klient Angular zmieniono wyłącznie przez usunięcie nieaktualnego komentarza; nie wykonywano osobnego buildu klienta.
