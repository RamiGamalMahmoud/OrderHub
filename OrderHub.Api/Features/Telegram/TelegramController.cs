using Microsoft.AspNetCore.Mvc;
using OrderHub.Application.Features.Telegram.Links.Create;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Threading.Tasks;

namespace OrderHub.Api.Features.TelegramIntegration;
[ApiController]
[Route("api/telegram")]
public class TelegramController : Controller
{
    private readonly IRequestExecutor _requestExecutor;

    public TelegramController(
        IRequestExecutor requestExecutor)
    {
        _requestExecutor = requestExecutor;
    }

    [HttpPost("links")]
    public async Task<IActionResult> Links(Request request)
    {
        Result<CreateTelegramLinkResult> result = await _requestExecutor
            .ExecuteAsync(new CreateTelegramLinkCommand(
                new CreateTelegramLinkRequest(
                    request.OwnerType,
                    request.OwnerId)));

        if (result.IsSuccess)
            return Ok(result.Value);
        return BadRequest(result.ErrorMessage);
    }

}

public record Request(TelegramAccountOwnerType OwnerType, int OwnerId);

