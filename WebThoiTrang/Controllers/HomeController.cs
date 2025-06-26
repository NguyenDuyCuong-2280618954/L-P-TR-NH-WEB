using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebThoiTrang.Models;
using WebThoiTrang.Repositories.Interface;

namespace WebThoiTrang.Controllers
{

    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _dbContext;

        
        
        
        public HomeController(IProductRepository productRepository,ApplicationDbContext dbContext )
        {
            _productRepository = productRepository;
            _dbContext = dbContext;
        }

        // Hi?n th? danh sách s?n ph?m
        public async Task<IActionResult> Index()
        {
            // L?y 4 s?n ph?m m?i nh?t t? database
            // OrderByDescending(p => p.CreatedDate) ?? l?y s?n ph?m m?i nh?t
            // Take(4) ?? gi?i h?n s? l??ng s?n ph?m hi?n th?
            var latestProducts = await _dbContext.Products
                                                .OrderByDescending(p => p.CreatedDate)
                                                .Take(4)
                                                .ToListAsync(); // Use ToListAsync for async operation

            // Truy?n danh sách s?n ph?m qua ViewData ho?c ViewModel
            // ViewData là cách nhanh chóng, ViewModel s? t?t h?n cho ?ng d?ng l?n h?n
            ViewData["LatestProducts"] = latestProducts;

            return View();
        }
    }
}
