using MediatR;
using Microsoft.Extensions.Options;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Options;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Telegram.Links.Create;
internal class CreateTelegramLinkHandler :
    IRequestHandler<CreateTelegramLinkCommand, Result<CreateTelegramLinkResult>>
{
    private readonly ITelegramAccountRepository _telegramAccountRepository;
    private readonly ITelegramLinkRepository _telegramLinkRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TelegramLinkOptions _telegramOptions;
    public CreateTelegramLinkHandler(
        ITelegramAccountRepository telegramAccountRepository,
        ITelegramLinkRepository telegramLinkRepository,
        IUnitOfWork unitOfWork,
        IOptions<TelegramLinkOptions> telegramOptions)
    {
        _telegramAccountRepository = telegramAccountRepository;
        _telegramLinkRepository = telegramLinkRepository;
        _unitOfWork = unitOfWork;
        _telegramOptions = telegramOptions.Value;
    }

    public async Task<Result<CreateTelegramLinkResult>> Handle(
        CreateTelegramLinkCommand request,
        CancellationToken cancellationToken)
    {
        TelegramAccountOwnerType ownerType = request.Request.OwnerType;
        int ownerId = request.Request.OwnerId;

        if (ownerId <= 0)
            return Result<CreateTelegramLinkResult>.Failure("حساب غير صالح.");

        var existsAccount = await _telegramAccountRepository.GetByOwner(ownerType, ownerId);

        if (existsAccount is not null)
            return Result<CreateTelegramLinkResult>.Failure("هذا الحساب مربوط ب Telegram بالفعل.");

        string token = $"{Guid.NewGuid():N}";

        DateTime expiresAt = DateTime.UtcNow.AddMinutes(_telegramOptions.LinkExpirationMinutes);

        TelegramLink telegramLink = new TelegramLink(
            ownerType,
            ownerId,
            token,
            expiresAt);

        _telegramLinkRepository.Add(telegramLink);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        string deepLink = $"https://t.me/{_telegramOptions.BotUsername}?start={token}";

        return Result<CreateTelegramLinkResult>.Success(new CreateTelegramLinkResult(
            deepLink,
            expiresAt));
    }
}
