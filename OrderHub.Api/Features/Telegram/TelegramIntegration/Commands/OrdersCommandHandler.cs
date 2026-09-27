using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace OrderHub.Api.Features.Telegram.TelegramIntegration.Commands;

public class OrdersCommandHandler : CommandHandlerBase, ITelegramCommandHandler
{
    private readonly IMediator _mediator;
    public OrdersCommandHandler(
        ITelegramBotClient botClient,
        IMediator mediator) : base(botClient)
    {
        _mediator = mediator;
    }

    public string Command => "orders";

    public bool RequiresAuthentication => true;

    public async Task HandleAsync(TelegramUpdateContext context, string arguments, CancellationToken cancellationToken = default)
    {
        var orders = await _mediator.Send(new Application.Features.Orders.Queries.GetOrderPaged.Query());

        foreach (var order in orders.Items)
        {
            InlineKeyboardButton[] inlineKeyboardButtons =
                [
                    InlineKeyboardButton.WithCallbackData("📄 تفاصيل", $"order:details{order.Id}"),
                    InlineKeyboardButton.WithCallbackData("✅ تأكيد", $"order:confirm{order.Id}"),
                    InlineKeyboardButton.WithCallbackData("❌ إلغاء", $"order:cancel{order.Id}")
                ];

            InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(inlineKeyboardButtons);

            await _botClient.SendMessage(
                chatId: context.Update.Message.Chat.Id,
                text: $"📦 الطلب {order.OrderNumber}\nالعميل: {order.ClientName}",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }


    }
}
