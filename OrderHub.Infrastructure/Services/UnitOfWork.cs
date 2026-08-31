using Microsoft.EntityFrameworkCore.Storage;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Models.CommercialDocuments;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction _transaction;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException(
                "A transaction is already active.");
        }

        _transaction = await _context.Database.BeginTransactionAsync(
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var entry in _context.ChangeTracker.Entries<InvoiceItem>())
        {
            var item = entry.Entity;

            Debug.WriteLine(
                $"InvoiceItem => " +
                $"ProductId: {item.ProductId}, " +
                $"InvoiceId: {item.InvoiceId}, " +
                $"Invoice: {item.Invoice?.Id}");
        }
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException(
                "No active transaction exists.");
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException(
                "No active transaction exists.");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}