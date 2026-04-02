using GunDammvc.Models;

namespace GunDammvc.Models
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetProductsAsync(string? gradeFilter);
        Task<Product?> GetProductDetailsAsync(int id);
    }
}