using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
	[Area("customer")]
	[Authorize]
	public class CartController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		[BindProperty]
		public ShopingCartVM ShopingCartVM { get; set; }
		public CartController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		public IActionResult Index()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
			ShopingCartVM = new()
			{
				ShopingCartList = _unitOfWork.ShopingCart.GetAll(u => u.ApplicationUserId == userId,
				includeProperties: "Product"),
				OrderHeader = new()
			};
			foreach (var cart in ShopingCartVM.ShopingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShopingCartVM.OrderHeader.OrderTolal += (cart.Price * cart.Count);
			}
			return View(ShopingCartVM);
		}
		public IActionResult Summary()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
			ShopingCartVM = new()
			{
				ShopingCartList = _unitOfWork.ShopingCart.GetAll(u => u.ApplicationUserId == userId,
				includeProperties: "Product"),
				OrderHeader = new()
			};
			ShopingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

			ShopingCartVM.OrderHeader.Name = ShopingCartVM.OrderHeader.ApplicationUser.Name;
			ShopingCartVM.OrderHeader.PhoneNumber = ShopingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			ShopingCartVM.OrderHeader.StreetAddress = ShopingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			ShopingCartVM.OrderHeader.City = ShopingCartVM.OrderHeader.ApplicationUser.City;
			ShopingCartVM.OrderHeader.State = ShopingCartVM.OrderHeader.ApplicationUser.State;
			ShopingCartVM.OrderHeader.PostalCode = ShopingCartVM.OrderHeader.ApplicationUser.PostalCode;
			foreach (var cart in ShopingCartVM.ShopingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShopingCartVM.OrderHeader.OrderTolal += (cart.Price * cart.Count);
			}
			return View(ShopingCartVM);
		}

		[HttpPost]
		[ActionName(nameof(Summary))]
		public IActionResult SummaryPOST()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
			ShopingCartVM.ShopingCartList = _unitOfWork.ShopingCart.GetAll(u => u.ApplicationUserId == userId,
				includeProperties: "Product");

			ShopingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
			ShopingCartVM.OrderHeader.ApplicationUserId = userId;

			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

			foreach (var cart in ShopingCartVM.ShopingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShopingCartVM.OrderHeader.OrderTolal += (cart.Price * cart.Count);
			}
			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				ShopingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				ShopingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
			}
			else
			{
				ShopingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShopingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}
			_unitOfWork.OrderHeader.Add(ShopingCartVM.OrderHeader);
			_unitOfWork.Save();
			foreach (var cart in ShopingCartVM.ShopingCartList)
			{
				OrderDetail orderDetail = new()
				{
					ProductId = cart.ProductId,
					OrderHeaderId = ShopingCartVM.OrderHeader.Id,
					Price = cart.Price,
					Count = cart.Count
				};
				_unitOfWork.OrderDetail.Add(orderDetail);
				_unitOfWork.Save();
			}

			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				//stripe logic
				var domain = "http://localhost:5081/";
				var options = new SessionCreateOptions
				{
					SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShopingCartVM.OrderHeader.Id}",
					CancelUrl = domain + "customer/cart/index",
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",
				};
				foreach (var item in ShopingCartVM.ShopingCartList)
				{
					var sessionLineItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = (long)(item.Price * 100),
							Currency = "usd",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = item.Product.Title
							}
						},
						Quantity = item.Count
					};
					options.LineItems.Add(sessionLineItem);
				}

				var service = new SessionService();
				Session session= service.Create(options);
				_unitOfWork.OrderHeader.UpdateStripePaymentID(ShopingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
				_unitOfWork.Save();
				Response.Headers.Add("Location",session.Url);
				return new StatusCodeResult(303);
			}

			return RedirectToAction(nameof(OrderConfirmation), new { id = ShopingCartVM.OrderHeader.Id });
		}
		public IActionResult OrderConfirmation(int id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
			if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				var service= new SessionService();
				Session session=service.Get(orderHeader.SessionId);

				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
					_unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
			}
			List<ShopingCart> shopingCarts=_unitOfWork.ShopingCart.
				GetAll(u=>u.ApplicationUserId==orderHeader.ApplicationUserId).ToList();

			_unitOfWork.ShopingCart.RemoveRange(shopingCarts);
			_unitOfWork.Save();

			return View(id);
		}

		public IActionResult Plus(int cartId)
		{
			var cartFromDb = _unitOfWork.ShopingCart.Get(u => u.Id == cartId);
			cartFromDb.Count += 1;
			_unitOfWork.ShopingCart.Update(cartFromDb);
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}

		public IActionResult Minus(int cartId)
		{
			var cartFromDb = _unitOfWork.ShopingCart.Get(u => u.Id == cartId);
			if (cartFromDb.Count <= 1)
			{
				_unitOfWork.ShopingCart.Remove(cartFromDb);
			}
			else
			{
				cartFromDb.Count -= 1;
				_unitOfWork.ShopingCart.Update(cartFromDb);
			}
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}
		public IActionResult Remove(int cartId)
		{
			var cartFromDb = _unitOfWork.ShopingCart.Get(u => u.Id == cartId);
			_unitOfWork.ShopingCart.Remove(cartFromDb);

			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}
		private double GetPriceBasedOnQuantity(ShopingCart shopingCart)
		{
			if (shopingCart.Count <= 50)
			{
				return shopingCart.Product.Price;
			}
			else
			{
				if (shopingCart.Count <= 100)
				{
					return shopingCart.Product.Price50;
				}
				else
				{
					return shopingCart.Product.Price100;
				}
			}
		}
	}
}
