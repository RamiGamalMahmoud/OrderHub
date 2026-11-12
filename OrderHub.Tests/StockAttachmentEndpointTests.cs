using OrderHub.Application.Features.Inventory;
using OrderHub.Application.Features.Inventory.DeleteStockAttachment;
using OrderHub.Application.Features.Inventory.GetStockAttachmentFile;
using OrderHub.Application.Features.Inventory.UploadStockAttachment;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrderHub.Tests;

public sealed class StockAttachmentEndpointTests
{
    private static readonly DateTime Now = new(2026, 11, 12, 9, 0, 0, DateTimeKind.Utc);

    private static readonly byte[] PdfContent = "%PDF-1.4\nfixture".Select(character => (byte)character).ToArray();
    private static readonly byte[] PngContent = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
    private static readonly byte[] JpegContent = [0xFF, 0xD8, 0xFF, 0xE0, 0x00];

    private static UploadStockAttachmentCommandHandler UploadHandler(StockInTestDb db, User user) =>
        new(
            db.Warehouses,
            db.Receipts,
            db.Files,
            new FixedCurrentUser(user),
            db.UnitOfWork,
            new FixedClock(Now));

    private static DeleteStockAttachmentCommandHandler DeleteHandler(StockInTestDb db, User user) =>
        new(
            db.Warehouses,
            db.Receipts,
            db.Files,
            new FixedCurrentUser(user),
            db.UnitOfWork);

    private static GetStockAttachmentFileQueryHandler FileQueryHandler(StockInTestDb db, User user) =>
        new(
            db.Warehouses,
            db.Receipts,
            db.AuthorizationFor(user),
            new FixedCurrentUser(user));

    [Fact]
    public void Rules_AcceptSupportedTypesWithMatchingMagicBytes()
    {
        Assert.True(StockAttachmentRules.TryResolve(PdfContent, "invoice.PDF", out string pdfType, out string pdfExtension, out _));
        Assert.Equal("application/pdf", pdfType);
        Assert.Equal(".pdf", pdfExtension);

        Assert.True(StockAttachmentRules.TryResolve(PngContent, "scan.png", out string pngType, out string pngExtension, out _));
        Assert.Equal("image/png", pngType);
        Assert.Equal(".png", pngExtension);

        Assert.True(StockAttachmentRules.TryResolve(JpegContent, "photo.JPEG", out string jpegType, out string jpegExtension, out _));
        Assert.Equal("image/jpeg", jpegType);
        Assert.Equal(".jpg", jpegExtension);
    }

    [Fact]
    public void Rules_RejectMismatchedMagicBytesOrUnsupportedExtensions()
    {
        Assert.False(StockAttachmentRules.TryResolve(PngContent, "invoice.pdf", out _, out _, out string mismatch));
        Assert.Contains("لا يطابق", mismatch);

        Assert.False(StockAttachmentRules.TryResolve(PdfContent, "invoice.docx", out _, out _, out string unsupported));
        Assert.Contains("PDF أو JPG أو PNG", unsupported);

        Assert.False(StockAttachmentRules.TryResolve([], "invoice.pdf", out _, out _, out string empty));
        Assert.Equal("الملف فارغ.", empty);
    }

    [Fact]
    public void Rules_RejectFilesLargerThanFiveMegabytes()
    {
        byte[] oversized = new byte[StockAttachmentRules.MaxSizeBytes + 1];
        PdfContent.CopyTo(oversized, 0);

        Assert.False(StockAttachmentRules.TryResolve(oversized, "invoice.pdf", out _, out _, out string error));
        Assert.Contains("5 ميجابايت", error);
    }

    [Fact]
    public void Rules_RejectDescriptionLongerThan200()
    {
        StockAttachmentRules.NormalizeDescription("  وصف  ", out string trimmedError);
        Assert.Null(trimmedError);
        Assert.Equal("وصف", StockAttachmentRules.NormalizeDescription("  وصف  ", out _));

        string tooLong = new('و', StockReceiptAttachment.DescriptionMaxLength + 1);
        Assert.Null(StockAttachmentRules.NormalizeDescription(tooLong, out string error));
        Assert.Contains("200", error);
    }

    [Fact]
    public async Task Upload_StoresFileAndDraftMetadata()
    {
        using var db = new StockInTestDb();

        var result = await UploadHandler(db, db.Keeper).Handle(
            new UploadStockAttachmentCommand(
                db.MainWarehouse.Id,
                "invoice.pdf",
                "فاتورة المورد",
                PdfContent),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("invoice.pdf", result.Value.OriginalFileName);
        Assert.Equal("فاتورة المورد", result.Value.Description);
        Assert.Equal((long)PdfContent.Length, result.Value.SizeBytes);
        Assert.Equal("application/pdf", result.Value.ContentType);

        db.Context.ChangeTracker.Clear();
        StockReceiptAttachment saved = db.Context.StockReceiptAttachments.Single();
        Assert.Equal(result.Value.Id, saved.Id);
        Assert.True(saved.IsDraft);
        Assert.Equal(db.Keeper.Id, saved.UploadedByUserId);
        Assert.True(File.Exists(Path.Combine(db.Directories.StockAttachmentsDirectory, saved.StoredFileName)));
    }

    [Fact]
    public async Task Upload_RejectsSpoofedContent()
    {
        using var db = new StockInTestDb();

        var result = await UploadHandler(db, db.Keeper).Handle(
            new UploadStockAttachmentCommand(
                db.MainWarehouse.Id,
                "invoice.pdf",
                null,
                PngContent),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("لا يطابق", result.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        Assert.Empty(db.Context.StockReceiptAttachments);
        Assert.Empty(Directory.GetFiles(db.Directories.StockAttachmentsDirectory));
    }

    [Fact]
    public async Task Upload_RejectsNonKeeper()
    {
        using var db = new StockInTestDb();

        var result = await UploadHandler(db, db.OtherKeeper).Handle(
            new UploadStockAttachmentCommand(
                db.MainWarehouse.Id,
                "invoice.pdf",
                null,
                PdfContent),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Forbidden", result.ErrorCode);
    }

    [Fact]
    public async Task Delete_RemovesDraftFileAndRow()
    {
        using var db = new StockInTestDb();

        var upload = await UploadHandler(db, db.Keeper).Handle(
            new UploadStockAttachmentCommand(db.MainWarehouse.Id, "invoice.pdf", null, PdfContent),
            CancellationToken.None);
        Assert.True(upload.IsSuccess, upload.ErrorMessage);

        string storedFileName = db.Context.StockReceiptAttachments.Single().StoredFileName;
        string storedPath = Path.Combine(db.Directories.StockAttachmentsDirectory, storedFileName);
        Assert.True(File.Exists(storedPath));

        var result = await DeleteHandler(db, db.Keeper).Handle(
            new DeleteStockAttachmentCommand(upload.Value.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        Assert.Empty(db.Context.StockReceiptAttachments);
        Assert.False(File.Exists(storedPath));
    }

    [Fact]
    public async Task Delete_RejectsSavedAttachment()
    {
        using var db = new StockInTestDb();
        StockReceipt receipt = StockReceipt.Create(
            db.MainWarehouse.Id,
            StockReceiptType.StockIn,
            null,
            null,
            db.Keeper.Id,
            [new StockReceiptLineData(db.LinkedProduct.Id, 1)]);

        db.Context.StockReceipts.Add(receipt);
        db.Context.SaveChanges();

        StockReceiptAttachment draft = db.SeedDraft(db.Keeper, db.MainWarehouse);
        StockReceiptAttachment tracked = db.Context.StockReceiptAttachments
            .Single(attachment => attachment.Id == draft.Id);
        tracked.AttachTo(receipt.Id);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var result = await DeleteHandler(db, db.Keeper).Handle(
            new DeleteStockAttachmentCommand(draft.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("مرتبط بتوريد محفوظ", result.ErrorMessage);
    }

    [Fact]
    public async Task Delete_RejectsAnotherUploader()
    {
        using var db = new StockInTestDb();
        StockReceiptAttachment draft = db.SeedDraft(db.OtherKeeper, db.MainWarehouse);

        var result = await DeleteHandler(db, db.Keeper).Handle(
            new DeleteStockAttachmentCommand(draft.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Forbidden", result.ErrorCode);
    }

    [Fact]
    public async Task FileQuery_AllowsKeeperAndApproverOnly()
    {
        using var db = new StockInTestDb();
        StockReceiptAttachment draft = db.SeedDraft(db.Keeper, db.MainWarehouse);

        var keeper = await FileQueryHandler(db, db.Keeper).Handle(
            new GetStockAttachmentFileQuery(draft.Id),
            CancellationToken.None);
        Assert.True(keeper.IsSuccess, keeper.ErrorMessage);
        Assert.Equal(draft.StoredFileName, keeper.Value.StoredFileName);

        var approver = await FileQueryHandler(db, db.Manager).Handle(
            new GetStockAttachmentFileQuery(draft.Id),
            CancellationToken.None);
        Assert.True(approver.IsSuccess, approver.ErrorMessage);

        var stranger = await FileQueryHandler(db, db.OtherKeeper).Handle(
            new GetStockAttachmentFileQuery(draft.Id),
            CancellationToken.None);
        Assert.False(stranger.IsSuccess);
        Assert.Equal("Forbidden", stranger.ErrorCode);
    }

    [Fact]
    public async Task Upload_SweepsStaleDraftsOlderThan24Hours()
    {
        using var db = new StockInTestDb();
        StockReceiptAttachment stale = db.SeedDraft(
            db.Keeper,
            db.MainWarehouse,
            originalFileName: "old.pdf",
            createdAtUtc: Now.AddHours(-25));
        string stalePath = Path.Combine(db.Directories.StockAttachmentsDirectory, stale.StoredFileName);
        File.WriteAllBytes(stalePath, PdfContent);

        var result = await UploadHandler(db, db.Keeper).Handle(
            new UploadStockAttachmentCommand(db.MainWarehouse.Id, "new.pdf", null, PdfContent),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        StockReceiptAttachment remaining = Assert.Single(db.Context.StockReceiptAttachments);
        Assert.Equal("new.pdf", remaining.OriginalFileName);
        Assert.False(File.Exists(stalePath));
    }
}
