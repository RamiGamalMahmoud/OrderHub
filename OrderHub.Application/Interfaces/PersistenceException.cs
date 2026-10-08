using System;

namespace OrderHub.Application.Interfaces;

// Provider-independent form of a failed persistence commit, retaining the original diagnostics.
public sealed class PersistenceException : Exception
{
    public PersistenceException(string message, Exception innerException) : base(message, innerException) { }
}
