using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includeProperties: "Category");
            return View(productList);
        }

        public IActionResult Details(int productId)
        {
            ShopingCart cart = new()
            {
                Product = _unitOfWork.Product.Get(u => u.ProductId == productId, includeProperties: "Category"),
                Count = 1,
                ProductId = productId

            };       
                return View(cart);
        }
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShopingCart shopingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shopingCart.ApplicationUserId = userId;

            ShopingCart cartFromDb = _unitOfWork.ShopingCart.Get(u => u.ApplicationUserId == userId &&
            u.ProductId == shopingCart.ProductId);

            if(cartFromDb != null) 
            {
                cartFromDb.Count += shopingCart.Count;
                _unitOfWork.ShopingCart.Update(cartFromDb);
            }
            else
            {
                _unitOfWork.ShopingCart.Add(shopingCart);
            }
            TempData["success"] = "Cart updated successfully";

            
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}