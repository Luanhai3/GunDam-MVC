using GunDammvc.Models;

namespace GunDammvc.Models
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IEnumerable<Product>> GetProductsAsync(string? gradeFilter)
        {
            if (!string.IsNullOrEmpty(gradeFilter))
                return await _productRepository.GetByGradeAsync(gradeFilter);
            return await _productRepository.GetAllAsync();
        }

        public async Task<Product?> GetProductDetailsAsync(int id) => await _productRepository.GetByIdAsync(id);
    }
}