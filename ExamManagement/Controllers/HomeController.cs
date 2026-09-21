using Microsoft.AspNetCore.Mvc;

namespace ExamManagement.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}