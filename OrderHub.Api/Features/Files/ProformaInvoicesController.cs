using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Api.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace OrderHub.Api.Features.Files;

[ApiController]
[Route("api/files/proforma-invoices")]
public sealed class ProformaInvoicesController : ControllerBase
{
    private readonly ILocalFileAccessService _localAccessService;

    public ProformaInvoicesController(
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

        await _localAccessService.SaveProformaInvoiceAsync(
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
            await _localAccessService.GetProformaInvoiceAsync(
                fileName);

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

        await _localAccessService.DeleteProformaInvoiceAsync(
            fileName);

        return NoContent();
    }

    private static bool IsValidFileName(string fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName) &&
               fileName == Path.GetFileName(fileName);
    }
}