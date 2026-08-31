using System;
using System.Collections.Generic;
using System.IO;

namespace OrderHub.UI.Features.Messages;

public class MessageAttachmentItem
{
    public MessageAttachmentItem(string filePath, string fileName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        var fileInfo = new FileInfo(filePath);

        FilePath = fileInfo.FullName;
        Name = string.IsNullOrEmpty(fileName) ? fileInfo.Name : fileName;
        Extension = fileInfo.Extension.TrimStart('.').ToLowerInvariant();
        Size = Math.Round(fileInfo.Length / 1024d / 1024d, 2);

        var visual = GetVisual(Extension);

        Icon = visual.Icon;
        IconColor = visual.Color;
        IconBackgroundColor = AddTransparency(visual.Color);
    }

    public string FilePath { get; }

    public string Name { get; }

    public string Extension { get; }

    public double Size { get; }

    public string Icon { get; }

    public string IconColor { get; }

    public string IconBackgroundColor { get; }

    private static (string Icon, string Color) GetVisual(string extension)
    {
        return _fileVisuals.TryGetValue(extension, out var visual)
            ? visual
            : ("File", "#757575");
    }

    private static string AddTransparency(string color)
    {
        return $"#26{color.TrimStart('#')}";
    }

    private static readonly Dictionary<string, (string Icon, string Color)> _fileVisuals =
        new Dictionary<string, (string Icon, string Color)>(StringComparer.OrdinalIgnoreCase)
        {
            ["pdf"] = ("FilePdfBox", "#D32F2F"),
            ["doc"] = ("FileWordBox", "#2B579A"),
            ["docx"] = ("FileWordBox", "#2B579A"),

            ["xls"] = ("FileExcelBox", "#217346"),
            ["xlsx"] = ("FileExcelBox", "#217346"),
            ["xlsm"] = ("FileExcelBox", "#217346"),
            ["csv"] = ("FileExcelBox", "#217346"),

            ["ppt"] = ("FilePowerpointBox", "#B7472A"),
            ["pptx"] = ("FilePowerpointBox", "#B7472A"),

            ["txt"] = ("FileDocument", "#607D8B"),
            ["md"] = ("FileDocument", "#607D8B"),
            ["rtf"] = ("FileDocument", "#607D8B"),

            ["jpg"] = ("FileImage", "#E91E63"),
            ["jpeg"] = ("FileImage", "#E91E63"),
            ["png"] = ("FileImage", "#E91E63"),
            ["gif"] = ("FileImage", "#E91E63"),
            ["bmp"] = ("FileImage", "#E91E63"),
            ["svg"] = ("FileImage", "#E91E63"),

            ["mp4"] = ("FileVideo", "#673AB7"),
            ["avi"] = ("FileVideo", "#673AB7"),
            ["mkv"] = ("FileVideo", "#673AB7"),
            ["mov"] = ("FileVideo", "#673AB7"),
            ["wmv"] = ("FileVideo", "#673AB7"),

            ["mp3"] = ("FileMusic", "#00BCD4"),
            ["wav"] = ("FileMusic", "#00BCD4"),
            ["flac"] = ("FileMusic", "#00BCD4"),
            ["aac"] = ("FileMusic", "#00BCD4"),

            ["zip"] = ("ZipBox", "#FFB300"),
            ["rar"] = ("ZipBox", "#FFB300"),
            ["7z"] = ("ZipBox", "#FFB300"),
            ["tar"] = ("ZipBox", "#FFB300"),
            ["gz"] = ("ZipBox", "#FFB300"),

            ["cs"] = ("FileCode", "#007ACC"),
            ["json"] = ("FileCode", "#007ACC"),
            ["xml"] = ("FileCode", "#007ACC"),
            ["html"] = ("FileCode", "#007ACC"),
            ["css"] = ("FileCode", "#007ACC"),
            ["js"] = ("FileCode", "#007ACC")
        };

}