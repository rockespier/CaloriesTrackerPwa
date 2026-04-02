using Microsoft.AspNetCore.Mvc;

namespace CalorieTracker.Api.Controllers
{
    public class NutritionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }


    }
}
