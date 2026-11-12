using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Application.Features.Inventory;
using OrderHub.Application.Features.Inventory.DeleteStockAttachment;
using OrderHub.Application.Features.Inventory.GetStockAttachmentFile;
using OrderHub.Application.Features.Inventory.UploadStockAttachment;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Web.Infrastructure.Authorization;

namespace OrderHub.Web.Controllers.Files;

/// <summary>
/// Dedicated inventory attachment endpoint (D14): PDF/JPG/PNG up to 5 MB,
/// separate folder and metadata entity. The legacy attachments endpoint is untouched.
/// </summary>
[ApiController]
[Route("files/stock-attachments")]
public sealed class StockAttachmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILocalFileAccessService _localAccessService;

    public StockAttachmentsController(
        IMediator mediator,
        ILocalFileAccessService localAccessService)
    {
        _mediator = mediator;
        _localAccessService = localAccessService;
    }

    [PermissionAuthorize(PermissionFeature.Inventory, PermissionLevel.Write)]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        [FromForm] int warehouseId,
        [FromForm] string description,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
            return BadRequest("الملف فارغ.");

        if (file.Length > StockAttachmentRules.MaxSizeBytes)
            return BadRequest("حجم الملف يتجاوز 5 ميجابايت.");

        byte[] content;
        await using (Stream stream = file.OpenReadStream())
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            content = buffer.ToArray();
        }

        var result = await _mediator.Send(
            new UploadStockAttachmentCommand(
                warehouseId,
                file.FileName,
                description,
                content),
            cancellationToken);

        if (!result.IsSuccess)
            return Failure(result);

        return Ok(result.Value);
    }

    [PermissionAuthorize(PermissionFeature.Inventory, PermissionLevel.Read)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetStockAttachmentFileQuery(id),
            cancellationToken);

        if (!result.IsSuccess)
            return Failure(result);

        string filePath = await _localAccessService.GetStockAttachmentAsync(
            result.Value.StoredFileName);

        if (!_localAccessService.FileExists(filePath))
            return NotFound();

        return PhysicalFile(
            filePath,
            result.Value.ContentType,
            result.Value.OriginalFileName);
    }

    [PermissionAuthorize(PermissionFeature.Inventory, PermissionLevel.Write)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeleteStockAttachmentCommand(id),
            cancellationToken);

        if (!result.IsSuccess)
            return Failure(result);

        return NoContent();
    }

    private IActionResult Failure(Result result) =>
        result.ErrorCode switch
        {
            "Unauthorized" => Unauthorized(result.ErrorMessage),
            "Forbidden" => StatusCode(StatusCodes.Status403Forbidden, result.ErrorMessage),
            "NotFound" => NotFound(result.ErrorMessage),
            "Conflict" => Conflict(result.ErrorMessage),
            _ => BadRequest(result.ErrorMessage)
        };
}
