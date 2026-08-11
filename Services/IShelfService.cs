using Project_Nook.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Project_Nook.Services
{
    public interface IShelfService
    {
        Task InitializeAsync();
        Task<List<ShelfItem>> GetAllItemsAsync();
        Task SaveItemAsync(ShelfItem item);
        Task DeleteItemAsync(ShelfItem item);
    }
}
