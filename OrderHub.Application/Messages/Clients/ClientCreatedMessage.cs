namespace OrderHub.Application.Messages.Clients;

public record ClientCreatedMessage;
public record ClientUpdatedMessage;
public record ClientDeletedMessage(int Id);
