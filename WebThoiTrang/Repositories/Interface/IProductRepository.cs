using WebThoiTrang.Models.Entity;

namespace WebThoiTrang.Repositories.Interface
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        IQueryable<Product> GetQueryable();
        Task<Product> GetByIdAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
        Product? GetById(int id);
    }
}
