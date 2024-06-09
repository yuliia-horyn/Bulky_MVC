using Bulky.Models;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IShopingCartRepository: IRepository<ShopingCart>
    {
        void Update(ShopingCart obj);
    }
}
