using Microsoft.EntityFrameworkCore.Storage;
using OrderHub.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models.CommercialDocuments;
using OrderHub.Domain.Models;
using OrderHub.Application.Interfaces.Services;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly INumberSequenceService _numberSequenceService;
    private IDbContextTransaction _transaction;

    public UnitOfWork(AppDbContext context, INumberSequenceService numberSequenceService)
    {
        _context = context;
        _numberSequenceService = numberSequenceService;
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

    public async Task<int> SaveChangesAsync(
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
        var pendingRecords = _context.ChangeTracker.Entries<IWorkRecord>()
            .Where(entry => entry.State == EntityState.Added && !entry.Entity.HasWorkRecordNumber)
            .Select(entry => entry.Entity)
            .ToList();
        IDbContextTransaction numberingTransaction = null;
        if (pendingRecords.Count > 0 && _context.Database.CurrentTransaction is null)
            numberingTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var record in pendingRecords)
            {
                var partyId = record.NumberingPartyId;
                var createdAt = ((ModelBase)record).CreatedAt;
                var allocation = await _numberSequenceService.NextMonthlyAsync(
                    record.NumberSequenceKind,
                    partyId,
                    new DateTimeOffset(DateTime.SpecifyKind(createdAt, DateTimeKind.Utc)),
                    cancellationToken);
                record.AssignWorkRecordNumber(partyId, allocation.Number, allocation.Year, allocation.Month);
            }

            var affected = await _context.SaveChangesAsync(cancellationToken);
            if (numberingTransaction is not null)
                await numberingTransaction.CommitAsync(cancellationToken);
            return affected;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            if (numberingTransaction is not null) await numberingTransaction.RollbackAsync(CancellationToken.None);
            foreach (var record in pendingRecords) record.ClearWorkRecordNumber();
            throw new PersistenceConflictException(exception);
        }
        catch (DbUpdateException exception)
        {
            if (numberingTransaction is not null) await numberingTransaction.RollbackAsync(CancellationToken.None);
            foreach (var record in pendingRecords) record.ClearWorkRecordNumber();
            throw new PersistenceException(exception.Message, exception);
        }
        catch
        {
            if (numberingTransaction is not null) await numberingTransaction.RollbackAsync(CancellationToken.None);
            foreach (var record in pendingRecords) record.ClearWorkRecordNumber();
            throw;
        }
        finally
        {
            if (numberingTransaction is not null) await numberingTransaction.DisposeAsync();
        }
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
        catch (System.Data.Common.DbException exception)
        {
            throw new PersistenceException("The transaction could not be committed.", exception);
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
