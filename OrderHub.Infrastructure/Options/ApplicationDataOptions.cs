namespace OrderHub.Infrastructure.Options;

public sealed class ApplicationDataOptions
{
    public const string SECTION_NAME = "ApplicationData";

    public required string RootPath { get; init; }
}