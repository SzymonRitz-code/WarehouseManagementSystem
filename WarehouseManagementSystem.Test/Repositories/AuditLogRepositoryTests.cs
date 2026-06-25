using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Model.AuditDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;
using WarehouseManagementSystem.Infrastructure.Persistence.Repositories;

namespace WarehouseManagementSystem.Tests.Repositories
{
    /// <summary>
    /// Tests for the <see cref="AuditLogRepository"/> class, focusing on its filtering capabilities and ensuring that it correctly retrieves audit logs based on various criteria such as entity name, entity ID, and performed by ID.
    /// </summary>
    public class AuditLogRepositoryTests
    {
        private WarehouseManagementSystemDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<WarehouseManagementSystemDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // osobna baza na każdy test
                .Options;

            return new WarehouseManagementSystemDbContext(options);
        }

        /// <summary>
        /// Tests that the GetFilteredAsync method correctly filters audit logs by entity name. It sets up two audit logs with different entity names, saves them to the in-memory database, and then retrieves logs filtered by one of the entity names. The test asserts that only the log with the specified entity name is returned.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetFilteredAsync_Should_FilterByEntityName()
        {
            // Arrange
            var context = CreateDbContext();
            var repo = new AuditLogRepository(context);

            var log1 = new AuditLog
            {
                Id = Guid.NewGuid(),
                OldValues = "Test",
                NewValues = "Test1",
                Operation = "Operation1",
                EntityName = "Entity1",
                PerformedAt = DateTime.UtcNow.AddMinutes(-1)
            };
            var log2 = new AuditLog
            {
                Id = Guid.NewGuid(),
                OldValues = "Test",
                NewValues = "Test2",
                Operation = "Operation2",
                EntityName = "Entity2",
                PerformedAt = DateTime.UtcNow
            };
            context.AuditLogs.AddRange(log1, log2);

            await context.SaveChangesAsync();

            // Act
            var result = await repo.GetFilteredAsync("Entity2", null, null);

            // Assert
            result.Should().HaveCount(1);
            result.First().EntityName.Should().Be("Entity2");
        }

        /// <summary>
        /// Tests that the GetFilteredAsync method correctly filters audit logs by entity ID. It sets up two audit logs with different entity IDs, saves them to the in-memory database, and then retrieves logs filtered by one of the entity IDs. The test asserts that only the log with the specified entity ID is returned.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetFilteredAsync_Should_FilterByEntityId()
        {
            // Arrange
            var context = CreateDbContext();
            var repo = new AuditLogRepository(context);

            var entityId = Guid.NewGuid();
            var log1 = new AuditLog
            {
                Id = Guid.NewGuid(),
                OldValues = "Test",
                NewValues = "Test1",
                Operation = "Operation1",
                EntityName = "Entity1",
                EntityId = entityId,
                PerformedAt = DateTime.UtcNow
            };
            var log2 = new AuditLog
            {
                Id = Guid.NewGuid(),
                OldValues = "Test",
                NewValues = "Test2",
                Operation = "Operation2",
                EntityName = "Entity2",
                EntityId = Guid.NewGuid(),
                PerformedAt = DateTime.UtcNow
            };
            context.AuditLogs.AddRange(log1, log2);
            await context.SaveChangesAsync();

            // Act
            var result = await repo.GetFilteredAsync(null, entityId, null);

            // Assert
            result.Should().HaveCount(1);
            result.First().EntityId.Should().Be(entityId);
        }

        /// <summary>
        /// Tests that the GetFilteredAsync method correctly filters audit logs by the ID of the user who performed the operation. It sets up two audit logs with different performed by IDs, saves them to the in-memory database, and then retrieves logs filtered by one of the performed by IDs. The test asserts that only the log with the specified performed by ID is returned.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetFilteredAsync_Should_FilterByPerformedById()
        {
            // Arrange
            var context = CreateDbContext();
            var repo = new AuditLogRepository(context);

            var userId = Guid.NewGuid();
            var log1 = new AuditLog
            {
                Id = Guid.NewGuid(),
                OldValues = "Test",
                NewValues = "Test1",
                Operation = "Operation1",
                EntityName = "Entity1",
                PerformedById = userId,
                PerformedAt = DateTime.UtcNow
            };
            var log2 = new AuditLog
            {
                Id = Guid.NewGuid(),
                OldValues = "Test",
                NewValues = "Test2",
                Operation = "Operation2",
                EntityName = "Entity2",
                PerformedById = Guid.NewGuid(),
                PerformedAt = DateTime.UtcNow
            };
            context.AuditLogs.AddRange(log1, log2);
            await context.SaveChangesAsync();

            // Act
            var result = await repo.GetFilteredAsync(null, null, userId);

            // Assert
            result.Should().HaveCount(1);
            result.First().PerformedById.Should().Be(userId);
        }

        /// <summary>
        /// Tests that the GetFilteredAsync method correctly applies multiple filters together. It sets up two audit logs with different combinations of entity names, entity IDs, and performed by IDs, saves them to the in-memory database, and then retrieves logs filtered by a specific combination of these criteria. The test asserts that only the log matching all specified filters is returned.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetFilteredAsync_Should_ApplyAllFiltersTogether()
        {
            // Arrange
            var context = CreateDbContext();
            var repo = new AuditLogRepository(context);

            var entityId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var log1 = new AuditLog
            {
                Id = Guid.NewGuid(),
                OldValues = "Test",
                NewValues = "Test1",
                Operation = "Operation1",
                EntityName = "Entity1",
                EntityId = entityId,
                PerformedById = userId,
                PerformedAt = DateTime.UtcNow
            };
            var log2 = new AuditLog
            {
                Id = Guid.NewGuid(),
                OldValues = "Test",
                NewValues = "Test2",
                Operation = "Operation2",
                EntityName = "Entity2",
                EntityId = Guid.NewGuid(),
                PerformedById = userId,
                PerformedAt = DateTime.UtcNow
            };

            context.AuditLogs.AddRange(log1, log2);
            await context.SaveChangesAsync();

            // Act
            var result = await repo.GetFilteredAsync("Entity1", entityId, userId);

            // Assert
            result.Should().HaveCount(1);
            var matched = result.First();
            matched.EntityName.Should().Be("Entity1");
            matched.EntityId.Should().Be(entityId);
            matched.PerformedById.Should().Be(userId);
        }

        /// <summary>
        /// Tests that the GetFilteredAsync method returns audit logs ordered by the PerformedAt property in descending order. It sets up two audit logs with different PerformedAt timestamps, saves them to the in-memory database, and then retrieves all logs without any filters. The test asserts that the first log in the result has a later PerformedAt timestamp than the second log, confirming that the ordering is correct.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetFilteredAsync_Should_OrderByPerformedAtDescending()
        {
            // Arrange
            var context = CreateDbContext();
            var repo = new AuditLogRepository(context);

            var log1 = new AuditLog
            {
                Id = Guid.NewGuid(),
                OldValues = "Test",
                NewValues = "Test1",
                Operation = "Operation1",
                EntityName = "Entity1",
                PerformedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            var log2 = new AuditLog
            {
                Id = Guid.NewGuid(),
                OldValues = "Test",
                NewValues = "Test2",
                Operation = "Operation2",
                EntityName = "Entity2",
                PerformedAt = DateTime.UtcNow
            };
            context.AuditLogs.AddRange(log1, log2);
            await context.SaveChangesAsync();

            // Act
            var result = (await repo.GetFilteredAsync(null, null, null)).ToList();

            // Assert
            result[0].PerformedAt.Should().BeAfter(result[1].PerformedAt);
        }
    }
}