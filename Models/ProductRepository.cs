using Microsoft.EntityFrameworkCore;
using GunDammvc.Data;
using GunDammvc.Models;

namespace GunDammvc.Models
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync() => 
            await _context.Products.ToListAsync();

        public async Task<IEnumerable<Product>> GetByGradeAsync(string grade) => 
            await _context.Products.Where(p => p.Grade == grade).ToListAsync();

        public async Task<Product?> GetByIdAsync(int id) => 
            await _context.Products.FindAsync(id);

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }
    }
}