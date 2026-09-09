using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Domain.Models;

public class UserPermission : ModelBase
{
    public int UserId { get; private set; }
    public PermissionFeature Feature { get; private set; }
    public bool CanRead { get; private set; }
    public bool CanWrite { get; private set; }

    private UserPermission()
    {
    }

    public static UserPermission Create(
        int userId,
        PermissionFeature feature,
        bool canRead,
        bool canWrite)
    {
        ValidateUserId(userId);

        if (canWrite)
            canRead = true;

        return new UserPermission
        {
            UserId = userId,
            Feature = feature,
            CanRead = canRead,
            CanWrite = canWrite
        };
    }

    public void SetRead(bool canRead)
    {
        if (CanWrite && !canRead)
            throw new InvalidOperationException(
                "A user cannot have write permission without read permission.");

        CanRead = canRead;
    }

    public void SetWrite(bool canWrite)
    {
        CanWrite = canWrite;

        if (canWrite)
            CanRead = true;
    }

    public void SetPermissions(bool canRead, bool canWrite)
    {
        if (canWrite)
            canRead = true;

        CanRead = canRead;
        CanWrite = canWrite;
    }

    private static void ValidateUserId(int userId)
    {
        if (userId <= 0)
            throw new ArgumentException(
                "User ID must be greater than zero.",
                nameof(userId));
    }
}