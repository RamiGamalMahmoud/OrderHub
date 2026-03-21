using OrderHub.Application.Interfaces.Services;

namespace OrderHub.Tests;

public class TestApplicationDirectoriesService : IApplicationDirectoriesService
{
    public string DatabaseFilePath { get; }
    public string AppPath { get; }
    public string StoragePath { get; }
    public string CredentialsFilePath { get; }
    public string TokenFilePath { get; }
    public string WhatAppProfilesPath { get; }
    public string DefaultWhatAppProfilePath { get; }
    public string LogsPath { get; }

    public TestApplicationDirectoriesService(string databasePath)
    {
        DatabaseFilePath = databasePath;
    }

    public void EnsureAppDirectoryCreated()
    {
        throw new System.NotImplementedException();
    }

    public void EnsureDatabaseFileCreated()
    {
        throw new System.NotImplementedException();
    }

    public void EnsureStorageDirectoryCreated()
    {
        throw new System.NotImplementedException();
    }

    public void EnsureWhatsAppProfilesDirectoryCreated()
    {
        throw new System.NotImplementedException();
    }

    public void EnsureLogsDirectoryCreated()
    {
        throw new System.NotImplementedException();
    }
}
