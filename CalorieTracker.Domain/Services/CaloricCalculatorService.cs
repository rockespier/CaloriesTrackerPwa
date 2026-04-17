using CalorieTracker.Domain.Entities;

namespace CalorieTracker.Domain.Services;

/// <summary>
/// Domain service responsable de todos los cálculos calóricos usando la fórmula Mifflin-St Jeor.
/// Centraliza la lógica BMR/TDEE/objetivo-diario para eliminar duplicación y garantizar consistencia.
/// </summary>
public static class CaloricCalculatorService
{
    private const double MaleConstant    =  5.0;
    private const double FemaleConstant  = -161.0;
    private const double DeficitCalories =  500.0;

    /// <summary>
    /// Superávit para ganancia de peso (300 kcal ≈ ~0.3 kg/semana, favoreciendo masa magra).
    /// </summary>
    private const double SurplusCalories = 300.0;

    // Constantes de objetivo — evitan magic strings en los callers.
    public const string GoalLose    = "Perder";
    public const string GoalGain    = "Ganar";
    public const string GoalMaintain = "Mantener";

    /// <summary>
    /// Calcula la Tasa Metabólica Basal (BMR) usando la ecuación Mifflin-St Jeor.
    /// </summary>
    public static double CalculateBMR(double weightKg, double heightCm, int age, char sex)
    {
        double bmr = (10 * weightKg) + (6.25 * heightCm) - (5 * age);
        return sex == 'M' ? bmr + MaleConstant : bmr + FemaleConstant;
    }

    /// <summary>
    /// Calcula el Gasto Energético Total Diario (TDEE) aplicando el multiplicador de actividad al BMR.
    /// </summary>
    public static double CalculateTDEE(double bmr, ActivityLevel level) =>
        bmr * GetActivityMultiplier(level);

    /// <summary>
    /// Calcula el objetivo calórico neto diario según la meta del usuario.
    /// "Perder" aplica déficit de 500 kcal; "Ganar" aplica superávit de 300 kcal; de lo contrario, mantenimiento.
    /// </summary>
    public static double CalculateDailyTarget(double tdee, string? goal) => goal switch
    {
        GoalLose    => tdee - DeficitCalories,
        GoalGain    => tdee + SurplusCalories,
        _           => tdee
    };

    private static double GetActivityMultiplier(ActivityLevel level) => level switch
    {
        ActivityLevel.Sedentary        => 1.2,
        ActivityLevel.LightlyActive    => 1.375,
        ActivityLevel.ModeratelyActive => 1.55,
        ActivityLevel.VeryActive       => 1.725,
        ActivityLevel.ExtraActive      => 1.9,
        _                              => 1.2
    };
}
