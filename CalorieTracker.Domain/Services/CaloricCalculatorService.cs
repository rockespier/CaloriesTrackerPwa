using CalorieTracker.Domain.Entities;

namespace CalorieTracker.Domain.Services
{
    /// <summary>
    /// Domain service responsible for all caloric calculations using the Mifflin-St Jeor formula.
    /// Centralizes BMR/TDEE/daily-target logic to eliminate duplication and ensure consistency.
    /// </summary>
    public static class CaloricCalculatorService
    {
        private const double MaleConstant = 5.0;
        private const double FemaleConstant = -161.0;
        private const double DeficitCalories = 500.0;

        /// <summary>
        /// Surplus for weight gain (300 kcal ≈ ~0.3 kg/week, favoring lean mass gain).
        /// Previously User.cs applied +500 kcal; this value aligns with UserService and clinical guidelines.
        /// </summary>
        private const double SurplusCalories = 300.0;

        // Goal string constants — keeps callers free of magic strings.
        public const string GoalLose = "Perder";
        public const string GoalGain = "Ganar";

        /// <summary>
        /// Calculates the Basal Metabolic Rate using the Mifflin-St Jeor equation.
        /// </summary>
        public static double CalculateBMR(double weightKg, double heightCm, int age, char sex)
        {
            double bmr = (10 * weightKg) + (6.25 * heightCm) - (5 * age);
            return sex == 'M' ? bmr + MaleConstant : bmr + FemaleConstant;
        }

        /// <summary>
        /// Calculates the Total Daily Energy Expenditure (TDEE) by applying an activity multiplier to the BMR.
        /// </summary>
        public static double CalculateTDEE(double bmr, ActivityLevel level) =>
            bmr * GetActivityMultiplier(level);

        /// <summary>
        /// Calculates the net daily caloric target based on the user's goal.
        /// "Perder" applies a 500 kcal deficit; "Ganar" applies a 300 kcal surplus; otherwise maintenance.
        /// </summary>
        public static double CalculateDailyTarget(double tdee, string? goal) => goal switch
        {
            GoalLose => tdee - DeficitCalories,
            GoalGain => tdee + SurplusCalories,
            _ => tdee
        };

        private static double GetActivityMultiplier(ActivityLevel level) => level switch
        {
            ActivityLevel.Sedentary => 1.2,
            ActivityLevel.LightlyActive => 1.375,
            ActivityLevel.ModeratelyActive => 1.55,
            ActivityLevel.VeryActive => 1.725,
            ActivityLevel.ExtraActive => 1.9,
            _ => 1.2
        };
    }
}
