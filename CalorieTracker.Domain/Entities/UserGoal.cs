namespace CalorieTracker.Domain.Entities;

/// <summary>
/// Enum tipado que reemplaza las magic strings "Perder"/"Ganar"/"Mantener"
/// para representar el objetivo de peso del usuario.
/// </summary>
public enum UserGoal
{
    Lose,
    Maintain,
    Gain
}
