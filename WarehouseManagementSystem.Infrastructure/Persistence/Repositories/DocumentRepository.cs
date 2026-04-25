using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly WarehouseManagementSystemDbContext _context;

    public DocumentRepository(WarehouseManagementSystemDbContext context)
    {
        _context = context;
    }

    public void Add(Document entity)
    {
        _context.Add(entity);
    }

    public bool Any(Expression<Func<Document, bool>> predicate)
    {
        return _context.Documents.Any(predicate);
    }

    public void Delete(Document entity)
    {
        _context.Documents.Remove(entity);
    }

    public Document Find(Guid id)
    {
        return _context.Documents.Find(id);
    }

    public async Task<Document> FindAsync(Guid id)
    {
        return await _context.Documents.FindAsync(id);
    }
    public async Task<Document> GetDocumentWithItems(Guid id)
    {
        return await _context.Documents.Include(d => d.Items).FirstOrDefaultAsync(d => d.Id == id);
    }

    public Document Update(Document entity)
    {
        return _context.Documents.Update(entity).Entity;
    }

    public void UpdateRange(IEnumerable<Document> entities)
    {
        _context.Documents.UpdateRange(entities);
    }
}