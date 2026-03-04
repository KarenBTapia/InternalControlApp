using Microsoft.AspNetCore.Mvc;

namespace InternalControlApp.Controllers
{
    public class SuperadminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
