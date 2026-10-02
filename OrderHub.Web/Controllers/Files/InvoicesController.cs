using Microsoft.AspNetCore.Mvc;
using OrderHub.Application.Interfaces.Services;

namespace OrderHub.Web.Controllers.Files;

[ApiController]
[Route("api/files/invoices")]
public sealed class InvoicesController : ControllerBase
{
    private readonly ILocalFileAccessService _localAccessService;

    public InvoicesController(
        ILocalFileAccessService localAccessService)
    {
        _localAccessService = localAccessService;
    }

    [HttpPost("{fileName}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        string fileName,
        IFormFile file)
    {
        if (!IsValidFileName(fileName))
            return BadRequest("Invalid file name.");

        if (file is null || file.Length == 0)
            return BadRequest("File is empty.");

        await using Stream stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();

        await stream.CopyToAsync(memoryStream);

        await _localAccessService.SaveInvoiceAsync(
            fileName,
            memoryStream.ToArray());

        return Ok();
    }

    [HttpGet("{fileName}")]
    public async Task<IActionResult> Get(string fileName)
    {
        if (!IsValidFileName(fileName))
            return BadRequest("Invalid file name.");

        string filePath =
            await _localAccessService.GetInvoiceAsync(fileName);

        if (!_localAccessService.FileExists(filePath))
            return NotFound();

        return PhysicalFile(
            filePath,
            "application/pdf");
    }

    [HttpDelete("{fileName}")]
    public async Task<IActionResult> Delete(string fileName)
    {
        if (!IsValidFileName(fileName))
            return BadRequest("Invalid file name.");

        await _localAccessService.DeleteInvoiceAsync(fileName);

        return NoContent();
    }

    private static bool IsValidFileName(string fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName) &&
               fileName == Path.GetFileName(fileName);
    }
}