using System.Threading.Tasks;

namespace CalorieTracker.Application.Interfaces
{
    public interface INutritionAnalyzer
    {
        Task<int> AnalyzeCaloriesAsync(string foodDescription);
    }
}