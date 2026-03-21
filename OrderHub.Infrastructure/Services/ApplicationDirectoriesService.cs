using OrderHub.Application.Interfaces.Services;
using System;
using System.IO;

namespace OrderHub.Infrastructure.Services
{
    internal class ApplicationDirectoriesService : IApplicationDirectoriesService
    {
        public string AppPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OrderHub");
        public string CredentialsFilePath => Path.Combine(StoragePath, "creditals.bin");
        public string StoragePath => Path.Combine(AppPath, "Storage");
        public string TokenFilePath => Path.Combine(StoragePath, "token.bin");
        public string DataPath => Path.Combine(AppPath, "Data");
        public string DatabaseFilePath => Path.Combine(DataPath, "order_hub.db");
        public string WhatAppProfilesPath => Path.Combine(StoragePath, "WhatAppProfiles");
        public string DefaultWhatAppProfilePath => Path.Combine(WhatAppProfilesPath, "Default");
        public string LogsPath => Path.Combine(StoragePath, "Logs");

        public void EnsureDatabaseFileCreated()
        {
            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }
            if (!File.Exists(DatabaseFilePath))
            {
                File.Copy("./Data/order_hub.db", DatabaseFilePath);
            }
        }

        public void EnsureAppDirectoryCreated()
        {
            if (!Directory.Exists(AppPath)) Directory.CreateDirectory(AppPath);
        }

        public void EnsureStorageDirectoryCreated()
        {
            if (!Directory.Exists(StoragePath)) Directory.CreateDirectory(StoragePath);
        }

        public void EnsureWhatsAppProfilesDirectoryCreated()
        {
            if (!Directory.Exists(WhatAppProfilesPath)) Directory.CreateDirectory(WhatAppProfilesPath);
        }

        public void EnsureLogsDirectoryCreated()
        {
            if (!Directory.Exists(LogsPath)) Directory.CreateDirectory(LogsPath);
        }

        public void EnsureDefaultWhatAppProfileDirectoryCreated()
        {
            if (!Directory.Exists(DefaultWhatAppProfilePath)) Directory.CreateDirectory(DefaultWhatAppProfilePath);
        }
    }
}
