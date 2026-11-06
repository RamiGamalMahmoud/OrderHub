using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Domain.Services.Pricing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OrderHub.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Infrastructure.Features.OrderDrafts;
internal class DraftReadService : IDraftReadService
{
    private readonly AppDbContextFactory _dbContextFactory;
    private readonly ILogger<DraftReadService> _logger;

    public DraftReadService(AppDbContextFactory appDbContextFactory, ILogger<DraftReadService> logger = null)
    {
        _dbContextFactory = appDbContextFactory;
        _logger = logger ?? NullLogger<DraftReadService>.Instance;
    }

    public async Task<IReadOnlyList<OrderDraftSummary>> GetUserDraftsAsync(int userId, CancellationToken cancellationToken = default)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();

        IReadOnlyList<DraftRecord> draftRecords = await dbContext.DraftRecords
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken);

        var summaries = new List<OrderDraftSummary>();
        foreach (var draftRecord in draftRecords)
        {
            try
            {
                Draft draft = JsonSerializer.Deserialize<Draft>(draftRecord.Data);
                if (draft?.Data?.Items is null)
                    throw new JsonException("Draft data is incomplete or invalid.");
                summaries.Add(new OrderDraftSummary(
                    draftRecord.Id,
                    ToShortId(draftRecord.Id),
                    draft.Data.ClientName,
                    draft.Data.Items.Count,
                    draftRecord.LastModified,
                    BuildProductSummary(draft.Data.Items),
                    CalculateTotal(draft.Data)));
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "Skipping malformed draft {DraftId} in summaries.", draftRecord.Id);
            }
        }
        return summaries;
    }

    public async Task<Draft> GetOwnedAsync(Guid id, int userId, CancellationToken cancellationToken = default)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();
        DraftRecord draftRecord = await dbContext.DraftRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (draftRecord is null || draftRecord.Data is null)
            return null;

        var draft = JsonSerializer.Deserialize<Draft>(draftRecord.Data);
        return draft is null ? null : draft with
        {
            Id = draftRecord.Id,
            UserId = draftRecord.UserId,
            LastModified = draftRecord.LastModified
        };
    }

    private static string ToShortId(Guid id) => id.ToString("N")[..8].ToUpperInvariant();

    private static string BuildProductSummary(IReadOnlyList<OrderDraftItem> items)
    {
        if (items.Count == 0)
            return string.Empty;

        var names = items
            .Take(2)
            .Select(item => item.ProductName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
        var summary = string.Join("، ", names);

        if (items.Count > 2)
            summary = string.IsNullOrEmpty(summary) ? $"+{items.Count - 2}" : $"{summary} +{items.Count - 2}";

        return summary;
    }

    private static decimal CalculateTotal(OrderDraftData data)
    {
        if (data.Items.Count == 0)
            return 0;

        try
        {
            var pricing = PricingCalculator.Calculate(data.Items
                .Select(item => new PricingItem(item.ProductId, item.ProductName, item.Quantity, item.Price, item.VAT))
                .ToList());

            var financial = OrderFinancialCalculator.Calculate(
                pricing, data.DeliveryCost, data.DiscountType, data.DiscountValue);

            return financial.IsSuccess ? financial.Value.FinalTotal : pricing.TotalAmount;
        }
        catch (ArgumentException)
        {
            // An in-progress draft may still hold pricing values the calculator rejects.
            return 0;
        }
    }
}
