using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Infrastructure;
using System;
using System.IO;

namespace OrderHub.Tests;

internal class TestDbContextFactory : AppDbContextFactory
{
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDbContextFactory(DbContextOptions<AppDbContext> options, IApplicationDirectoriesService applicationDirectoriesService) : base(applicationDirectoriesService)
    {
        _options = options;
    }

    public new AppDbContext CreateDbContext()
    {
        return new AppDbContext(_options);
    }

    private AppDbContextFactory CreateFactory()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");

        var directoriesService = new TestApplicationDirectoriesService(dbPath);

        var factory = new AppDbContextFactory(directoriesService);

        using var context = factory.CreateDbContext();
        context.Database.EnsureCreated();

        return factory;
    }
}