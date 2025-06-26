using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace WebThoiTrang.Controllers
{
    public class ImageController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public ImageController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        public IActionResult GetResultImage(string path)
        {
            if (string.IsNullOrEmpty(path))
                return NotFound();

            var fullPath = Path.Combine(_env.ContentRootPath, path.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var contentType = "image/jpeg"; // hoặc image/png tùy file
            var fileBytes = System.IO.File.ReadAllBytes(fullPath);
            return File(fileBytes, contentType);
        }
    }
}
