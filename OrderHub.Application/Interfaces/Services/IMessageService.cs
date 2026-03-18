using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IMessageService
{
    void QueueMessage(OutboxMessage message);
    void QueueMessages(IEnumerable<OutboxMessage> messages);
    Task StartAsync();
    Task StopAsync();
}
