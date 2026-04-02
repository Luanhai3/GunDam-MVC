using GunDammvc.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GunDammvc.Models
{
    public interface IOrderRepository
    {
        Task CreateOrderAsync(ClaimsPrincipal user, Order order);
    }
}
