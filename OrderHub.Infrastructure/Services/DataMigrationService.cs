using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal class DataMigrationService : IDataMigrationService
{
    private readonly AppDbContextFactory _appDbContextFactory;
    private IApplicationDirectoriesService _directoriesService;

    public DataMigrationService(AppDbContextFactory appDbContextFactory, IApplicationDirectoriesService directoriesService)
    {
        _appDbContextFactory = appDbContextFactory;
        _directoriesService = directoriesService;
    }

    public async Task MigrateLegacyAttachmentsAsync()
    {
        using AppDbContext _context = _appDbContextFactory.CreateDbContext();
        // 1. التأكد من إنشاء المجلدات الجديدة (بما فيها مجلد AttachmentsPath)
        _directoriesService.EnsureDirectoriesCreated();

        // 2. جلب الرسائل التي تحتوي على مسارات قديمة ولم يتم تحويلها بعد
        var messagesToMigrate = await _context.Set<OutboxMessage>()
            .Where(m => m.LegacyAttachments != null && m.LegacyAttachments.Count > 0 && m.LegacyAttachments.Any())
            .ToListAsync();

        if (!messagesToMigrate.Any()) return;

        foreach (var message in messagesToMigrate)
        {

            foreach (string oldPath in message.LegacyAttachments)
            {
                if (string.IsNullOrWhiteSpace(oldPath)) continue;

                try
                {
                    // 4. التحقق من أن الملف لا يزال موجوداً في جهازه الحالي
                    if (File.Exists(oldPath))
                    {
                        string originalFileName = Path.GetFileName(oldPath);
                        string extension = Path.GetExtension(oldPath);
                        string storedFileName = $"{Guid.NewGuid()}{extension}";

                        // مسار الحفظ الجديد في AppData/Local/OrderHub/Storage/Attachments
                        string destinationPath = Path.Combine(_directoriesService.AttachmentsDirectory, storedFileName);

                        // نسخ الملف فيزيائياً
                        File.Copy(oldPath, destinationPath, overwrite: true);

                        // 5. إنشاء سجل المرفق الجديد وربطه بالرسالة
                        var newAttachment = new OutboxMessageAttachment(
                            originalFileName,
                            extension,
                            storedFileName,
                             new FileInfo(destinationPath).Length,
                             message.Id);

                        _context.Set<OutboxMessageAttachment>().Add(newAttachment);
                    }
                }
                catch (Exception)
                {
                    // تسجيل الخطأ في الـ Logs الخاص ببرنامجك لكي لا يتوقف البرنامج لو تلف ملف واحد
                    // _logger.LogError(ex, "Failed to copy file");
                }
            }

            // 6. تفريغ الحقل القديم حتى لا تتكرر العملية في المرة القادمة
            message.LegacyAttachments = [];
        }

        // حفظ جميع السجلات والتعديلات في قاعدة البيانات دفعة واحدة
        await _context.SaveChangesAsync();
    }

    private static string GetMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",

            ".pdf" => "application/pdf",

            ".doc" => "application/msword",

            ".docx" =>
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

            ".xls" => "application/vnd.ms-excel",

            ".xlsx" =>
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

            ".txt" => "text/plain",

            ".zip" => "application/zip",

            _ => "application/octet-stream"
        };
    }
}
