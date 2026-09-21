using OrderHub.Api.Features.Telegram.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Api.Features.Telegram.Receiving;

public interface ITelegramUpdateHandler
{
    bool CanHandle(TelegramUpdate update);

    Task HandleAsync(
        TelegramUpdate update,
        CancellationToken cancellationToken = default);
}