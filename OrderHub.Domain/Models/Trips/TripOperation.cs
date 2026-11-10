using System;

namespace OrderHub.Domain.Models.Trips;

public record TripOperation(
    int UserId,
    string UserName,
    DateTime AtUtc,
    string Source = "Web");
