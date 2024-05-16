using Microsoft.AspNetCore.Mvc;
using BulkyWeb.Repository.IRepository;
using BulkyWeb.Models;
using System.Security.Claims;
using BulkyWeb.Utils;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IConfiguration _configuration;
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
    public async Task<IActionResult> SummaryPOST()
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
            var url = "https://a.khalti.com/api/v2/epayment/initiate/";
            var payload = new
            {
                return_url = $"{domain}/customer/cart/OrderConfirmation",
                website_url = domain,
                amount = ShoppingCartVm.OrderHeader.OrderTotal * 100,
                purchase_order_id = ShoppingCartVm.OrderHeader.Id.ToString(),
                purchase_order_name = "testtt",
                customer_info = new
                {
                    name = applicationUser.Name,
                    email = applicationUser.Email,
                    phone = applicationUser.PhoneNumber
                },
            };
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "key " + Environment.GetEnvironmentVariable("KHALTI_KEY"));

            try
            {
                var response = await client.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseContent);
                var jsonResponse = JObject.Parse(responseContent);
                if (!jsonResponse.ContainsKey("pidx"))
                {
                    return RedirectToAction(nameof(Summary));
                }
                else
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVm.OrderHeader.Id, jsonResponse["pidx"].ToString(), null);
                    _unitOfWork.Save();

                    Response.Headers.Add("Location", jsonResponse["payment_url"].ToString());
                    return new StatusCodeResult(303);
                }
            }
            catch (HttpRequestException err)
            {
                return RedirectToAction(nameof(Summary));
            }
        }

        return RedirectToAction(nameof(OrderConfirmation), new
        {
            id = ShoppingCartVm.OrderHeader.SessionId
        });
    }


    public async Task<IActionResult> OrderConfirmation(string pidx)
    {
        var orderHeader = _unitOfWork.OrderHeader.Get(u => u.SessionId == pidx, includeProperties: "ApplicationUser");

        if (orderHeader.PaymentStatus != PAYMENT_STATUS.PaymentStatusDealyedPayment)
        {
            var domain = Request.Scheme + "://" + Request.Host.Value;
            var url = "https://a.khalti.com/api/v2/epayment/lookup/";
            var payload = new
            {
                pidx = orderHeader.SessionId
            };
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "key " + Environment.GetEnvironmentVariable("KHALTI_KEY"));
            try
            {
                var response = await client.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseContent);
                Console.WriteLine("HELOOOOOOOO");
                var jsonResponse = JObject.Parse(responseContent);
                if (jsonResponse.ContainsKey("status"))
                {
                    if (jsonResponse["status"].ToString().ToLower() == "completed")
                    {
                        _unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeader.Id, jsonResponse["pidx"].ToString(), jsonResponse["transaction_id"].ToString());
                        _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, ORDER_STATUS.OrderStatusApproved, PAYMENT_STATUS.PaymentStatusApproved);
                        _unitOfWork.Save();
                    }
                }
                else
                {
                    TempData["error"] = "Internal server error.";
                    return RedirectToAction(nameof(Summary));
                }
            }
            catch (HttpRequestException)
            {
                return RedirectToAction(nameof(Summary));
            }
        }
        List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

        _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
        _unitOfWork.Save();

        return View(orderHeader.Id);
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
