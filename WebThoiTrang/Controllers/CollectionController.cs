using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Thêm using để sử dụng ILogger
using WebThoiTrang.Models;
using WebThoiTrang.Models.Entity;

public class CollectionController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CollectionController> _logger; // Thêm ILogger để log

    // Constructor đã được cập nhật để nhận ILogger
    public CollectionController(ApplicationDbContext context, ILogger<CollectionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Đang truy vấn danh sách các bộ sưu tập...");

        // 1. Dùng .Include() để nạp dữ liệu Products
        var collections = await _context.Collections
                                        .Include(c => c.Products)
                                        .ToListAsync();

        // 2. Log ra console để kiểm tra dữ liệu đã nạp thành công chưa
        if (collections.Any())
        {
            _logger.LogInformation($"Đã tìm thấy {collections.Count} bộ sưu tập.");

            // Duyệt qua từng collection để log các Product Id
            foreach (var collection in collections)
            {
                _logger.LogInformation($"--- Bộ sưu tập: {collection.Name} (Id: {collection.Id}) ---");

                if (collection.Products != null && collection.Products.Any())
                {
                    // Lấy danh sách Id của các sản phẩm
                    var productIds = collection.Products.Select(p => p.Id).ToList();
                    _logger.LogInformation($"  - Số lượng sản phẩm: {productIds.Count}");
                    _logger.LogInformation($"  - Danh sách Product Id: [{string.Join(", ", productIds)}]");
                }
                else
                {
                    _logger.LogWarning("  - Bộ sưu tập này không có sản phẩm nào (danh sách rỗng hoặc null).");
                }
            }
        }
        else
        {
            _logger.LogWarning("Không tìm thấy bộ sưu tập nào trong database.");
        }

        // 3. Tạo ViewModel và truyền vào View
        var viewModel = new CollectionListVm
        {
            Collections = collections
        };

        return View(viewModel);
    }
}