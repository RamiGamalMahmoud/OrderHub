using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Api.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OrderHub.Api.Features.Files;

[ApiController]
[Route("api/files/attachments")]
public class AttachmentsController : ControllerBase
{
    private readonly ILocalFileAccessService _localAccessService;

    public AttachmentsController(ILocalFileAccessService localAccessService)
    {
        _localAccessService = localAccessService;
    }

    [HttpGet("{fileName}")]
    public async Task<IActionResult> Get(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName != Path.GetFileName(fileName))
        {
            return BadRequest("Invalid file name.");
        }

        string filePath = await _localAccessService.GetAttachmentAsync(fileName);

        if (!_localAccessService.FileExists(filePath))
            return NotFound();

        return PhysicalFile(
            filePath,
            GetContentType(fileName));
    }

    [HttpPost("{fileName}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StoredFile>> Upload(
        string fileName,
        IFormFile file,
        [FromForm] string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName != Path.GetFileName(fileName))
        {
            return BadRequest("Invalid file name.");
        }

        if (file is null || file.Length == 0)
            return BadRequest("File is empty.");

        await using Stream stream = file.OpenReadStream();

        await _localAccessService.SaveAttachmentAsync(
            fileName,
            stream);

        string url = Url.ActionLink(
            nameof(Get),
            values: new { fileName })!;

        var storedFile = new StoredFile(
            OriginalFileName: originalFileName,
            StoredFileName: fileName,
            Extension: Path.GetExtension(fileName),
            Url: url,
            Size: file.Length);

        return Ok(storedFile);
    }

    [HttpDelete("{fileName}")]
    public async Task<IActionResult> Delete(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName != Path.GetFileName(fileName))
        {
            return BadRequest("Invalid file name.");
        }

        try
        {
            await _localAccessService.DeleteAttachmentAsync(fileName);
            return Ok();
        }
        catch (Exception)
        {
            return NoContent();
        }
    }

    private static string GetContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}

public sealed record StoredFile(
    string OriginalFileName,
    string StoredFileName,
    string Url,
    string Extension,
    double Size);