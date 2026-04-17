namespace CalorieTracker.Application.DTOs;

/// <summary>
/// DTO que representa el estado semanal del seguimiento calórico del usuario.
/// </summary>
public record WeeklyStatusDto(
    int DailyTarget,
    double WeeklyConsumed,
    int WeeklyTarget,
    string ComplianceStatus,
    int DaysActive
);
