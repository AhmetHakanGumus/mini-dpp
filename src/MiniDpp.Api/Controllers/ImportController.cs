using Microsoft.AspNetCore.Mvc;
using MiniDpp.Api.Import;

namespace MiniDpp.Api.Controllers;

[ApiController]
[Route("api/import")]
public class ImportController(ProductImportService importService) : ControllerBase
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    /// <summary>CSV yükler. Geçerli satırlar kaydedilir, hatalı satırlar nedenleriyle döner.</summary>
    [HttpPost]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ImportResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Problem("'file' alanında bir CSV dosyası gönderin.", statusCode: StatusCodes.Status400BadRequest);
        }

        await using var stream = file.OpenReadStream();
        try
        {
            return Ok(await importService.ImportAsync(stream, cancellationToken));
        }
        catch (InvalidCsvFileException exception)
        {
            return Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
