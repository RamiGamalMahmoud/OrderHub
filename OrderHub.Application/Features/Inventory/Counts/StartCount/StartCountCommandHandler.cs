using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Counts.StartCount;

internal sealed class StartCountCommandHandler(
    IWarehouseRepository warehouses,
    IInventoryCountRepository counts,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<StartCountCommand, Result<CountStartResult>>
{
    public async Task<Result<CountStartResult>> Handle(
        StartCountCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<CountStartResult>.Failure("يجب تسجيل الدخول لبدء الجرد.", "Unauthorized");

        if (!Enum.IsDefined(request.Scope))
            return Result<CountStartResult>.Failure("نطاق الجرد غير صالح.");

        Warehouse warehouse = await warehouses.GetByKeeperAsync(userId, cancellationToken);
        if (warehouse is null)
        {
            return Result<CountStartResult>.Failure(
                "بدء الجرد متاح لأمين المخزن المرتبط بمخزن فقط.",
                "Forbidden");
        }

        if (!warehouse.IsActive)
            return Result<CountStartResult>.Failure("المخزن غير نشط؛ لا يمكن بدء جرد عليه.", "Conflict");

        IReadOnlyList<CountableProductRow> countable = await counts.GetCountableProductsAsync(
            warehouse.Id,
            cancellationToken);

        Result<List<int>> selection = ResolveProducts(request, countable);
        if (!selection.IsSuccess)
            return Result<CountStartResult>.Failure(selection.ErrorMessage, selection.ErrorCode);

        CountableProductRow blocked = countable.FirstOrDefault(
            product => selection.Value.Contains(product.ProductId) && product.HasOpenCount);
        if (blocked is not null)
        {
            return Result<CountStartResult>.Failure(
                $"يوجد جرد مفتوح للصنف «{blocked.ProductName}» في هذا المخزن؛ أكمله أو ألغِه أولًا.",
                "Conflict");
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        Result<InventoryCount> creation = InventoryCount.Start(
            warehouse.Id,
            request.Scope,
            userId,
            now);
        if (!creation.IsSuccess)
            return Result<CountStartResult>.Failure(creation.ErrorMessage, creation.ErrorCode);

        InventoryCount count = creation.Value;

        var transactionActive = false;
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionActive = true;

            foreach (int productId in selection.Value)
            {
                BookQuantities book = await counts.GetBookQuantitiesAsync(
                    warehouse.Id,
                    productId,
                    now,
                    cancellationToken);

                Result<InventoryCountLine> added = count.AddLine(
                    productId,
                    book.SoundQty,
                    book.DamagedQty,
                    now);
                if (!added.IsSuccess)
                    return Result<CountStartResult>.Failure(added.ErrorMessage, added.ErrorCode);
            }

            counts.Add(count);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            transactionActive = false;
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return Result<CountStartResult>.Success(
                new CountStartResult(count.Id, count.Lines.Count));
        }
        catch (PersistenceConflictException)
        {
            return Result<CountStartResult>.Failure(
                "يوجد جرد مفتوح لأحد الأصناف المحددة؛ حدّث الصفحة وأعد المحاولة.",
                "Conflict");
        }
        catch (PersistenceException)
        {
            return Result<CountStartResult>.Failure(
                "تعذر بدء الجرد. تأكد ألا يوجد جرد مفتوح للأصناف المحددة ثم أعد المحاولة.",
                "Conflict");
        }
        catch (ArgumentException exception)
        {
            return Result<CountStartResult>.Failure(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Result<CountStartResult>.Failure(exception.Message, "Conflict");
        }
        finally
        {
            if (transactionActive)
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
    }

    private static Result<List<int>> ResolveProducts(
        StartCountCommand request,
        IReadOnlyList<CountableProductRow> countable)
    {
        if (countable.Count == 0)
            return Result<List<int>>.Failure("لا توجد أصناف لها رصيد في هذا المخزن لبدء الجرد.");

        if (request.Scope == InventoryCountScope.Full)
            return Result<List<int>>.Success(countable.Select(product => product.ProductId).ToList());

        if (request.ProductIds is null || request.ProductIds.Count == 0)
            return Result<List<int>>.Failure("اختر صنفًا واحدًا على الأقل للجرد.");

        if (request.ProductIds.Any(productId => productId <= 0))
            return Result<List<int>>.Failure("أحد الأصناف المحددة غير صالح.");

        if (request.ProductIds.Distinct().Count() != request.ProductIds.Count)
            return Result<List<int>>.Failure("لا يمكن تكرار الصنف في الجرد الواحد.");

        HashSet<int> available = countable.Select(product => product.ProductId).ToHashSet();
        int[] missing = request.ProductIds.Where(productId => !available.Contains(productId)).ToArray();
        if (missing.Length > 0)
        {
            return Result<List<int>>.Failure(
                $"أحد الأصناف المحددة لا يملك رصيدًا في هذا المخزن ({missing[0].ToString(CultureInfo.InvariantCulture)}).");
        }

        return Result<List<int>>.Success(request.ProductIds.Distinct().ToList());
    }
}
