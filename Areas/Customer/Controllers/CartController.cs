using Microsoft.AspNetCore.Mvc;
using BulkyWeb.Repository.IRepository;
using BulkyWeb.Models;
using System.Security.Claims;
using BulkyWeb.Utils;
using Stripe.Checkout;
using Microsoft.AspNetCore.Authorization;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    [BindProperty]
    public ShoppingCartVm ShoppingCartVm { get; set; }

    public CartController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public IActionResult Index()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        ShoppingCartVm = new()
        {
            ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product").ToList(),
            OrderHeader = new()
        };

        foreach (var cartItem in ShoppingCartVm.ShoppingCartList)
        {
            cartItem.Price = GetPriceBasedOnQuantity(cartItem);
            ShoppingCartVm.OrderHeader.OrderTotal += cartItem.Price * cartItem.Count;
        }

        return View(ShoppingCartVm);
    }

    public IActionResult Summary()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        ShoppingCartVm = new ShoppingCartVm
        {
            ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
            OrderHeader = new()
        };

        foreach (var cartItem in ShoppingCartVm.ShoppingCartList)
        {
            cartItem.Price = GetPriceBasedOnQuantity(cartItem);
            ShoppingCartVm.OrderHeader.OrderTotal += cartItem.Price * cartItem.Count;
        }

        ShoppingCartVm.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

        ShoppingCartVm.OrderHeader.Name = ShoppingCartVm.OrderHeader.ApplicationUser.Name;
        ShoppingCartVm.OrderHeader.PhoneNumber = ShoppingCartVm.OrderHeader.ApplicationUser.PhoneNumber;
        ShoppingCartVm.OrderHeader.StreetAddress = ShoppingCartVm.OrderHeader.ApplicationUser.StreetAddress;
        ShoppingCartVm.OrderHeader.City = ShoppingCartVm.OrderHeader.ApplicationUser.City;
        ShoppingCartVm.OrderHeader.State = ShoppingCartVm.OrderHeader.ApplicationUser.State;
        ShoppingCartVm.OrderHeader.PostalCode = ShoppingCartVm.OrderHeader.ApplicationUser.PostalCode;

        return View(ShoppingCartVm);
    }

    [HttpPost]
    [ActionName("Summary")]
    public IActionResult SummaryPOST()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        ShoppingCartVm.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");

        if (ShoppingCartVm.ShoppingCartList.Count() == 0)
        {
            TempData["error"] = "Your cart is empty";
            Response.Headers.Add("Location", "/Customer/Cart");
            return new StatusCodeResult(303);
        }

        ShoppingCartVm.OrderHeader.OrderDate = DateTime.Now.ToUniversalTime();
        ShoppingCartVm.OrderHeader.ApplicationUserId = userId;

        ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

        foreach (var cartItem in ShoppingCartVm.ShoppingCartList)
        {
            cartItem.Price = GetPriceBasedOnQuantity(cartItem);
            ShoppingCartVm.OrderHeader.OrderTotal += cartItem.Price * cartItem.Count;
        }

        if (applicationUser.CompanyId.GetValueOrDefault() == 0)
        {
            //it is a regular customer account and we need to capture account
            ShoppingCartVm.OrderHeader.PaymentStatus = PAYMENT_STATUS.PaymentStatusPending;
            ShoppingCartVm.OrderHeader.OrderStatus = ORDER_STATUS.OrderStatusPending;
        }
        else
        {
            ShoppingCartVm.OrderHeader.PaymentStatus = PAYMENT_STATUS.PaymentStatusDealyedPayment;
            ShoppingCartVm.OrderHeader.OrderStatus = ORDER_STATUS.OrderStatusApproved;
        }

        _unitOfWork.OrderHeader.Add(ShoppingCartVm.OrderHeader);
        _unitOfWork.Save();

        foreach (var cart in ShoppingCartVm.ShoppingCartList)
        {
            OrderDetail orderDetail = new()
            {
                ProductId = cart.ProductId,
                OrderHeaderId = ShoppingCartVm.OrderHeader.Id,
                Price = cart.Price,
                Count = cart.Count
            };
            _unitOfWork.OrderDetail.Add(orderDetail);
            _unitOfWork.Save();
        }


        if (applicationUser.CompanyId.GetValueOrDefault() == 0)
        {
            var domain = Request.Scheme + "://" + Request.Host.Value;
            var options = new SessionCreateOptions
            {
                SuccessUrl = $"{domain}/customer/cart/OrderConfirmation?id={ShoppingCartVm.OrderHeader.Id}",
                CancelUrl = $"{domain}/customer/cart/index",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment"
            };
            foreach (var item in ShoppingCartVm.ShoppingCartList)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "npr",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title,
                            // Description = item.Product.Description,
                            // Images = [item.Product.ImageUrl]
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }


            var service = new SessionService();
            Session session = service.Create(options);
            _unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVm.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVm.OrderHeader.Id });
    }


    public IActionResult OrderConfirmation(int id)
    {
        var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");

        if (orderHeader.PaymentStatus != PAYMENT_STATUS.PaymentStatusDealyedPayment)
        {
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.ToLower() == "paid")
            {
                _unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.OrderHeader.UpdateStatus(id, ORDER_STATUS.OrderStatusApproved, PAYMENT_STATUS.PaymentStatusApproved);
                _unitOfWork.Save();
            }
        }

        List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

        _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
        _unitOfWork.Save();

        return View(id);
    }

    public IActionResult Plus(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingCart.Get(c => c.Id == cartId);
        cartFromDb.Count += 1;
        _unitOfWork.ShoppingCart.Update(cartFromDb);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Minus(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingCart.Get(c => c.Id == cartId);
        if (cartFromDb.Count <= 1)
        {
            _unitOfWork.ShoppingCart.Remove(cartFromDb);
        }
        else
        {
            cartFromDb.Count -= 1;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
        }
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Remove(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingCart.Get(c => c.Id == cartId);
        _unitOfWork.ShoppingCart.Remove(cartFromDb);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
    {
        return shoppingCart.Product.Price;
    }

}
