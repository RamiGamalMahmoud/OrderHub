namespace OrderHub.Application.Interfaces.Services
{
    public interface IApplicationDirectoriesService
    {
        string AppPath { get; }
        string StoragePath { get; }
        string CredentialsFilePath { get; }
        string TokenFilePath { get; }
        string DatabaseFilePath { get; }
        string WhatAppProfilesPath { get; }
        string DefaultWhatAppProfilePath { get; }

        void EnsureAppDirectoryCreated();
        void EnsureDatabaseFileCreated();
        void EnsureStorageDirectoryCreated();
        void EnsureWhatAppProfilesDirectoryCreated();
    }
}
