namespace ActivityMonitor.Api.Models;

public record ActivityEventDto(
    int Id,
    string UserEmail,
    string ProductName,
    string Action,
    DateTime OccurredAt);

public record ProductUsage(
    string ProductName,
    int EventCount,
    int SharePercent);
