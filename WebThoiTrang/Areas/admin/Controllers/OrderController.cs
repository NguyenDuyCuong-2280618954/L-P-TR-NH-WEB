using Microsoft.AspNetCore.Mvc;

namespace WebThoiTrang.Areas.admin.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
