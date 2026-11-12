using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using Xunit;

namespace OrderHub.Tests;

public sealed class StockReceiptTests
{
    private static readonly DateTime CreatedAt = new(2026, 11, 12, 8, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_AggregatesDuplicateProductLines()
    {
        StockReceipt receipt = StockReceipt.Create(
            1,
            StockReceiptType.StockIn,
            null,
            null,
            2,
            [
                new StockReceiptLineData(10, 2),
                new StockReceiptLineData(10, 3),
                new StockReceiptLineData(11, 1)
            ],
            CreatedAt);

        Assert.Equal(2, receipt.Lines.Count);
        Assert.Equal(5, receipt.Lines.Single(line => line.ProductId == 10).Quantity);
        Assert.Equal(1, receipt.Lines.Single(line => line.ProductId == 11).Quantity);
        Assert.Equal(CreatedAt, receipt.CreatedAt);
    }

    [Fact]
    public void Create_NormalizesReferenceNote()
    {
        StockReceipt receipt = StockReceipt.Create(
            1,
            StockReceiptType.StockIn,
            null,
            "   فاتورة 42  ",
            2,
            [new StockReceiptLineData(10, 1)]);

        Assert.Equal("فاتورة 42", receipt.ReferenceNote);

        StockReceipt blank = StockReceipt.Create(
            1,
            StockReceiptType.StockIn,
            null,
            "   ",
            2,
            [new StockReceiptLineData(10, 1)]);

        Assert.Null(blank.ReferenceNote);
    }

    [Fact]
    public void Create_RejectsReferenceNoteLongerThanLimit()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            StockReceipt.Create(
                1,
                StockReceiptType.StockIn,
                null,
                new string('م', StockReceipt.ReferenceNoteMaxLength + 1),
                2,
                [new StockReceiptLineData(10, 1)]));

        Assert.Contains("300", exception.Message);
    }

    [Fact]
    public void Create_RejectsEmptyOrInvalidLines()
    {
        Assert.Throws<ArgumentException>(() =>
            StockReceipt.Create(1, StockReceiptType.StockIn, null, null, 2, []));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StockReceipt.Create(
                1,
                StockReceiptType.StockIn,
                null,
                null,
                2,
                [new StockReceiptLineData(10, 0)]));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StockReceipt.Create(
                1,
                StockReceiptType.StockIn,
                null,
                null,
                2,
                [new StockReceiptLineData(0, 1)]));
    }

    [Fact]
    public void Create_RejectsUndefinedReceiptTypeAndInvalidIdentifiers()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StockReceipt.Create(
                1,
                (StockReceiptType)99,
                null,
                null,
                2,
                [new StockReceiptLineData(10, 1)]));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StockReceipt.Create(
                0,
                StockReceiptType.StockIn,
                null,
                null,
                2,
                [new StockReceiptLineData(10, 1)]));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StockReceipt.Create(
                1,
                StockReceiptType.StockIn,
                null,
                null,
                0,
                [new StockReceiptLineData(10, 1)]));
    }

    [Fact]
    public void Attachment_NormalizesDescriptionAndValidatesLimits()
    {
        StockReceiptAttachment attachment = StockReceiptAttachment.CreateDraft(
            1,
            "abc.pdf",
            "invoice.pdf",
            "   فاتورة المورد  ",
            42,
            "application/pdf",
            5);

        Assert.Equal("فاتورة المورد", attachment.Description);
        Assert.True(attachment.IsDraft);
        Assert.Null(attachment.StockReceiptId);

        StockReceiptAttachment blank = StockReceiptAttachment.CreateDraft(
            1,
            "abc.pdf",
            "invoice.pdf",
            "   ",
            42,
            "application/pdf",
            5);

        Assert.Null(blank.Description);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StockReceiptAttachment.CreateDraft(
                1,
                "abc.pdf",
                "invoice.pdf",
                new string('و', StockReceiptAttachment.DescriptionMaxLength + 1),
                42,
                "application/pdf",
                5));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StockReceiptAttachment.CreateDraft(
                1,
                "abc.pdf",
                "invoice.pdf",
                null,
                0,
                "application/pdf",
                5));
    }

    [Fact]
    public void Attachment_CanBindOnlyOnce()
    {
        StockReceiptAttachment attachment = StockReceiptAttachment.CreateDraft(
            1,
            "abc.pdf",
            "invoice.pdf",
            null,
            42,
            "application/pdf",
            5);

        attachment.AttachTo(9);

        Assert.False(attachment.IsDraft);
        Assert.Equal(9, attachment.StockReceiptId);

        Assert.Throws<InvalidOperationException>(() => attachment.AttachTo(10));
    }

    [Fact]
    public void Receipt_RejectsAttachmentFromAnotherWarehouse()
    {
        StockReceipt receipt = StockReceipt.Create(
            1,
            StockReceiptType.StockIn,
            null,
            null,
            2,
            [new StockReceiptLineData(10, 1)]);

        StockReceiptAttachment foreign = StockReceiptAttachment.CreateDraft(
            2,
            "abc.pdf",
            "invoice.pdf",
            null,
            42,
            "application/pdf",
            5);

        Assert.Throws<InvalidOperationException>(() => receipt.Attach(foreign));
    }
}
