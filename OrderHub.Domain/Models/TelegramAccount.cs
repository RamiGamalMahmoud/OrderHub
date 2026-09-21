using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Domain.Models;

public class TelegramAccount : ModelBase
{
    private TelegramAccount() { }

    public TelegramAccount(
        TelegramAccountOwnerType ownerType,
        int ownerId,
        long telegramUserId,
        long chatId,
        string username,
        string firstName,
        string lastName)
    {
        OwnerType = ownerType;
        OwnerId = ownerId;

        TelegramUserId = telegramUserId;
        ChatId = chatId;

        Username = username;
        FirstName = firstName;
        LastName = lastName;

        LinkedAt = DateTime.UtcNow;
    }

    public TelegramAccountOwnerType OwnerType { get; private set; }

    public int OwnerId { get; private set; }

    public long TelegramUserId { get; private set; }

    public long ChatId { get; private set; }

    public string Username { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public DateTime LinkedAt { get; private set; }
}