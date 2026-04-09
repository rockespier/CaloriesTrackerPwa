using CalorieTracker.Domain.Entities;
using CalorieTracker.Domain.Services;
using Xunit;

namespace CalorieTracker.Tests.Domain
{
    public class CaloricCalculatorServiceTests
    {
        // ── CalculateBMR ────────────────────────────────────────────────────────

        [Fact]
        public void CalculateBMR_Male_ReturnsCorrectValue()
        {
            // Mifflin-St Jeor (male): (10*90) + (6.25*180) - (5*30) + 5 = 1880
            var bmr = CaloricCalculatorService.CalculateBMR(90, 180, 30, 'M');
            Assert.Equal(1880.0, bmr, precision: 5);
        }

        [Fact]
        public void CalculateBMR_Female_ReturnsCorrectValue()
        {
            // Mifflin-St Jeor (female): (10*60) + (6.25*165) - (5*25) - 161 = 1345.25
            var bmr = CaloricCalculatorService.CalculateBMR(60, 165, 25, 'F');
            Assert.Equal(1345.25, bmr, precision: 5);
        }

        // ── CalculateTDEE ────────────────────────────────────────────────────────

        [Theory]
        [InlineData(ActivityLevel.Sedentary, 1.2)]
        [InlineData(ActivityLevel.LightlyActive, 1.375)]
        [InlineData(ActivityLevel.ModeratelyActive, 1.55)]
        [InlineData(ActivityLevel.VeryActive, 1.725)]
        [InlineData(ActivityLevel.ExtraActive, 1.9)]
        public void CalculateTDEE_AppliesCorrectMultiplier(ActivityLevel level, double multiplier)
        {
            var bmr = 2000.0;
            var tdee = CaloricCalculatorService.CalculateTDEE(bmr, level);
            Assert.Equal(bmr * multiplier, tdee, precision: 5);
        }

        // ── CalculateDailyTarget ─────────────────────────────────────────────────

        [Fact]
        public void CalculateDailyTarget_Perder_AppliesDeficit()
        {
            var target = CaloricCalculatorService.CalculateDailyTarget(2256.0, CaloricCalculatorService.GoalLose);
            Assert.Equal(1756.0, target, precision: 5);
        }

        [Fact]
        public void CalculateDailyTarget_Ganar_AppliesSurplus()
        {
            var target = CaloricCalculatorService.CalculateDailyTarget(2256.0, CaloricCalculatorService.GoalGain);
            Assert.Equal(2556.0, target, precision: 5);
        }

        [Theory]
        [InlineData("Mantener")]
        [InlineData(null)]
        [InlineData("")]
        public void CalculateDailyTarget_Maintenance_ReturnsTDEE(string? goal)
        {
            var target = CaloricCalculatorService.CalculateDailyTarget(2256.0, goal);
            Assert.Equal(2256.0, target, precision: 5);
        }
    }
}
