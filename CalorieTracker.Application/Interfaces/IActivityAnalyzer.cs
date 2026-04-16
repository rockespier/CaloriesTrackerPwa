using System.Threading.Tasks;

namespace CalorieTracker.Application.Interfaces
{
    public interface IActivityAnalyzer
    {
        /// <summary>
        /// Estima las calorías quemadas para una actividad física dada.
        /// </summary>
        /// <param name="activityDescription">Descripción de la actividad (ej: "correr", "nadar", "ciclismo").</param>
        /// <param name="durationMinutes">Duración en minutos.</param>
        /// <param name="weightKg">Peso del usuario en kilogramos para mayor precisión.</param>
        Task<int> AnalyzeCaloriesBurnedAsync(string activityDescription, int durationMinutes, double weightKg);
    }
}
