using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Model.AuditDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;
using WarehouseManagementSystem.Infrastructure.Persistence.Repositories;

namespace WarehouseManagementSystem.Tests.Repositories
{
    public class AuditLogRepositoryTests
    {
        private WarehouseManagementSystemDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<WarehouseManagementSystemDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // osobna baza na każdy test
                .Options;

            return new WarehouseManagementSystemDbContext(options);
        }

        [Fact]
        public async Task GetFilteredAsync_Should_FilterByEntityName()
        {
            // Arrange
            var context = CreateDbContext();
            var repo = new AuditLogRepository(context);

            var log1 = new AuditLog { Id = Guid.NewGuid(), EntityName = "Entity1", PerformedAt = DateTime.UtcNow.AddMinutes(-1) };
            var log2 = new AuditLog { Id = Guid.NewGuid(), EntityName = "Entity2", PerformedAt = DateTime.UtcNow };
            context.AuditLogs.AddRange(log1, log2);
            await context.SaveChangesAsync();

            // Act
            var result = await repo.GetFilteredAsync("Entity2", null, null);

            // Assert
            result.Should().HaveCount(1);
            result.First().EntityName.Should().Be("Entity2");
        }

        [Fact]
        public async Task GetFilteredAsync_Should_FilterByEntityId()
        {
            // Arrange
            var context = CreateDbContext();
            var repo = new AuditLogRepository(context);

            var entityId = Guid.NewGuid();
            var log1 = new AuditLog { Id = Guid.NewGuid(), EntityId = entityId, PerformedAt = DateTime.UtcNow };
            var log2 = new AuditLog { Id = Guid.NewGuid(), EntityId = Guid.NewGuid(), PerformedAt = DateTime.UtcNow };
            context.AuditLogs.AddRange(log1, log2);
            await context.SaveChangesAsync();

            // Act
            var result = await repo.GetFilteredAsync(null, entityId, null);

            // Assert
            result.Should().HaveCount(1);
            result.First().EntityId.Should().Be(entityId);
        }

        [Fact]
        public async Task GetFilteredAsync_Should_FilterByPerformedById()
        {
            // Arrange
            var context = CreateDbContext();
            var repo = new AuditLogRepository(context);

            var userId = Guid.NewGuid();
            var log1 = new AuditLog { Id = Guid.NewGuid(), PerformedById = userId, PerformedAt = DateTime.UtcNow };
            var log2 = new AuditLog { Id = Guid.NewGuid(), PerformedById = Guid.NewGuid(), PerformedAt = DateTime.UtcNow };
            context.AuditLogs.AddRange(log1, log2);
            await context.SaveChangesAsync();

            // Act
            var result = await repo.GetFilteredAsync(null, null, userId);

            // Assert
            result.Should().HaveCount(1);
            result.First().PerformedById.Should().Be(userId);
        }

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
                EntityName = "Entity1",
                EntityId = entityId,
                PerformedById = userId,
                PerformedAt = DateTime.UtcNow
            };
            var log2 = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = "Entity1",
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

        [Fact]
        public async Task GetFilteredAsync_Should_OrderByPerformedAtDescending()
        {
            // Arrange
            var context = CreateDbContext();
            var repo = new AuditLogRepository(context);

            var log1 = new AuditLog { Id = Guid.NewGuid(), PerformedAt = DateTime.UtcNow.AddMinutes(-10) };
            var log2 = new AuditLog { Id = Guid.NewGuid(), PerformedAt = DateTime.UtcNow };
            context.AuditLogs.AddRange(log1, log2);
            await context.SaveChangesAsync();

            // Act
            var result = (await repo.GetFilteredAsync(null, null, null)).ToList();

            // Assert
            result[0].PerformedAt.Should().BeAfter(result[1].PerformedAt);
        }
    }
}