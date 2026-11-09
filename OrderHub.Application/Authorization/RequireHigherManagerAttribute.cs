using System;

namespace OrderHub.Application.Authorization;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequireHigherManagerAttribute : Attribute;
