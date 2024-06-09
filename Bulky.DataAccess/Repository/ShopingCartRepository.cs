using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository
{
    public class ShopingCartRepository : Repository<ShopingCart>, IShopingCartRepository
    {
        private ApplicationDbContext _db;
        public ShopingCartRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ShopingCart obj)
        {
            _db.ShopingCarts.Update(obj);
        }
    }
}
