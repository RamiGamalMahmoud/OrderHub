using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Domain.Models;

public class TelegramLink : ModelBase
{
    private TelegramLink() { }

    public TelegramLink(
        TelegramAccountOwnerType ownerType,
        int ownerId,
        string token,
        DateTime expiresAt)
    {
        OwnerType = ownerType;
        OwnerId = ownerId;
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public TelegramAccountOwnerType OwnerType { get; private set; }

    public int OwnerId { get; private set; }

    public string Token { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? UsedAt { get; private set; }

    public bool IsUsed => UsedAt.HasValue;

    public bool IsExpired(DateTime utcNow)
    {
        return utcNow >= ExpiresAt;
    }

    public bool IsActive(DateTime utcNow)
    {
        return !IsUsed && !IsExpired(utcNow);
    }

    public void MarkAsUsed()
    {
        if (IsUsed)
            return;

        UsedAt = DateTime.UtcNow;
    }
}