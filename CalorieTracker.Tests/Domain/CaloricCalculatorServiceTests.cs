using CalorieTracker.Domain.Entities;
using CalorieTracker.Domain.Services;

namespace CalorieTracker.Tests.Domain;

public class CaloricCalculatorServiceTests
{
    [Theory]
    [InlineData('M', 80, 180, 30, true)]   // Hombre: BMR > 0
    [InlineData('F', 60, 165, 25, true)]   // Mujer:  BMR > 0
    public void CalculateBMR_HappyPath_ReturnsPositiveValue(
        char sex, double weight, double height, int age, bool shouldBePositive)
    {
        var bmr = CaloricCalculatorService.CalculateBMR(weight, height, age, sex);
        Assert.Equal(shouldBePositive, bmr > 0);
    }

    [Fact]
    public void CalculateBMR_MaleVsFemale_MaleIsHigher()
    {
        double maleBmr   = CaloricCalculatorService.CalculateBMR(80, 180, 30, 'M');
        double femaleBmr = CaloricCalculatorService.CalculateBMR(80, 180, 30, 'F');
        Assert.True(maleBmr > femaleBmr);
    }

    [Fact]
    public void CalculateTDEE_SedentaryMultiplier_IsCorrect()
    {
        double bmr  = 1800;
        double tdee = CaloricCalculatorService.CalculateTDEE(bmr, ActivityLevel.Sedentary);
        Assert.Equal(1800 * 1.2, tdee, precision: 1);
    }

    [Fact]
    public void CalculateDailyTarget_GoalLose_AppliesDeficit()
    {
        double tdee   = 2000;
        double target = CaloricCalculatorService.CalculateDailyTarget(tdee, CaloricCalculatorService.GoalLose);
        Assert.Equal(1500, target, precision: 1);
    }

    [Fact]
    public void CalculateDailyTarget_GoalGain_AppliesSurplus()
    {
        double tdee   = 2000;
        double target = CaloricCalculatorService.CalculateDailyTarget(tdee, CaloricCalculatorService.GoalGain);
        Assert.Equal(2300, target, precision: 1);
    }

    [Fact]
    public void CalculateDailyTarget_GoalMaintain_ReturnsTdee()
    {
        double tdee   = 2000;
        double target = CaloricCalculatorService.CalculateDailyTarget(tdee, CaloricCalculatorService.GoalMaintain);
        Assert.Equal(2000, target, precision: 1);
    }

    [Fact]
    public void CalculateDailyTarget_NullGoal_ReturnsTdee()
    {
        double tdee   = 2000;
        double target = CaloricCalculatorService.CalculateDailyTarget(tdee, null);
        Assert.Equal(2000, target, precision: 1);
    }
}
