using System;

namespace OrderHub.Application.Authorization;

public sealed class AuthorizationDeniedException() : Exception("غير مصرح لك بتنفيذ هذه العملية.");
