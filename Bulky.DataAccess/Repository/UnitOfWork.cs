﻿using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;

namespace Bulky.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        public ICategoryRepository Category{ get; private set; }
        public ICompanyRepository Company { get; private set; }
        public IShopingCartRepository ShopingCart { get; private set; }
        public IApplicationUserRepository ApplicationUser { get; private set; }
        public IOrderHeaderRepository OrderHeader { get; private set; } 
        public IOrderDetailRepository OrderDetail { get; private set; }
        public IProductRepository Product { get; private set; }
        private ApplicationDbContext _db;
        public UnitOfWork(ApplicationDbContext db)
        { 
            _db = db;
            ApplicationUser=new ApplicationUserRepository(_db);
            Category= new CategoryRepository(_db);
            Product = new ProductRepository(_db);
            Company = new CompanyRepository(_db);
            ShopingCart= new ShopingCartRepository(_db);
            OrderHeader = new OrderHeaderRepository(_db);
            OrderDetail = new OrderDetailRepository(_db);
        }
        public void Save()
        {
            _db.SaveChanges();
        }
    }
}
