namespace CalorieTracker.Application.DTOs;

/// <summary>
/// DTO que representa el resumen de calorías consumidas en un día específico.
/// </summary>
public record DailyCaloriesSummaryDto(DateTime Date, int TotalCalories);
