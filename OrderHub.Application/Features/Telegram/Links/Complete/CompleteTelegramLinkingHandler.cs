using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Telegram.Links.Complete;
internal class CompleteTelegramLinkingHandler : IRequestHandler<CompleteTelegramLinkingCommand, Result>
{
    private readonly ITelegramAccountRepository _telegramAccountRepository;
    private readonly ITelegramLinkRepository _telegramLinkRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteTelegramLinkingHandler(
        ITelegramAccountRepository telegramAccountRepository,
        ITelegramLinkRepository telegramLinkRepository,
        IUnitOfWork unitOfWork)
    {
        _telegramAccountRepository = telegramAccountRepository;
        _telegramLinkRepository = telegramLinkRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CompleteTelegramLinkingCommand request, CancellationToken cancellationToken)
    {
        var link = await _telegramLinkRepository.GetByToken(request.Token);

        if (link is null)
            return Result.Failure("رابط الربط غير صالح.");

        if (link.IsUsed)
            return Result.Failure("تم استخدام رابط الربط من قبل.");

        if (link.IsExpired(DateTime.UtcNow))
            return Result.Failure("انتهت صلاحية رابط الربط.");

        var telegramAccount = await _telegramAccountRepository.GetByTelegramUserId(request.TelegramUserId);

        var existingAccount = await _telegramAccountRepository.GetByOwner(link.OwnerType, link.OwnerId);

        if (existingAccount is not null || telegramAccount is not null)
            return Result.Failure("حساب Telegram مربوط بالفعل.");

        TelegramAccount newAccount = new TelegramAccount(
            link.OwnerType,
            link.OwnerId,
            request.TelegramUserId,
            request.ChatId,
            request.Username,
            request.FirstName,
            request.LastName);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            link.MarkAsUsed();
            _telegramAccountRepository.Add(newAccount);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return Result.Success();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

    }
}
