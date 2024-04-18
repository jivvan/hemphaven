using System.Security.Claims;
using BulkyWeb.Models;
using BulkyWeb.Repository.IRepository;
using BulkyWeb.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("admin")]
[Authorize]
public class OrderController : Controller
{
    public readonly IUnitOfWork _unitOfWork;
    [BindProperty]
    public OrderVM OrderVM { get; set; }
    public OrderController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(int orderId)
    {
        OrderVM = new()
        {
            OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
            OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
        };
        return View(OrderVM);
    }

    [HttpPost]
    [Authorize(Roles = USER_ROLE.Role_Admin + "," + USER_ROLE.Role_Employee)]
    public IActionResult UpdateOrderDetail()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
        orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
        orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
        orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
        orderHeaderFromDb.City = OrderVM.OrderHeader.City;
        orderHeaderFromDb.State = OrderVM.OrderHeader.State;
        orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
        if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
        {
            orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
        }
        if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
        {
            orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
        }
        _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
        _unitOfWork.Save();

        TempData["success"] = "Order details updated successfully";

        return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
    }

    [HttpPost]
    [Authorize(Roles = USER_ROLE.Role_Admin + "," + USER_ROLE.Role_Employee)]
    public IActionResult StartProcessing()
    {
        _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, ORDER_STATUS.StatusInProcess);
        _unitOfWork.Save();

        TempData["success"] = "Order details updated successfully";
        return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
    }

    [HttpPost]
    [Authorize(Roles = USER_ROLE.Role_Admin + "," + USER_ROLE.Role_Employee)]
    public IActionResult ShipOrder()
    {
        var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
        orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
        orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
        orderHeader.OrderStatus = ORDER_STATUS.StatusShipped;
        orderHeader.ShippingDate = DateTime.Now.ToUniversalTime();
        if (orderHeader.PaymentStatus == PAYMENT_STATUS.PaymentStatusDealyedPayment)
        {
            orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30).ToUniversalTime());
        }

        _unitOfWork.OrderHeader.Update(orderHeader);
        _unitOfWork.Save();

        TempData["success"] = "Order shipped successfully";
        return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
    }

    [HttpPost]
    [Authorize(Roles = USER_ROLE.Role_Admin + "," + USER_ROLE.Role_Employee)]
    public IActionResult CancelOrder()
    {
        var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

        if (orderHeader.PaymentStatus == PAYMENT_STATUS.PaymentStatusApproved)
        {
            var options = new RefundCreateOptions
            {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = orderHeader.PaymentIntentId
            };
            var service = new RefundService();
            Refund refund = service.Create(options);

            _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, ORDER_STATUS.StatusCancelled, ORDER_STATUS.StatusRefunded);
        }
        else
        {
            _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, ORDER_STATUS.StatusCancelled, ORDER_STATUS.StatusCancelled);
        }
        _unitOfWork.Save();
        TempData["success"] = "Order cancelled successfully";
        return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
    }

    [ActionName("Details")]
    [HttpPost]
    public IActionResult Details_Pay_Now()
    {
        OrderVM.OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
        OrderVM.OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

        var domain = "http://localhost:5080";
        var options = new SessionCreateOptions
        {
            SuccessUrl = $"{domain}/admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
            CancelUrl = $"{domain}/admin/order/details?orderId={OrderVM.OrderHeader.Id}",
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment"
        };
        foreach (var item in OrderVM.OrderDetail)
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
                        Description = item.Product.Description,
                        Images = [item.Product.ImageUrl]
                    }
                },
                Quantity = item.Count
            };
            options.LineItems.Add(sessionLineItem);
        }

        var service = new SessionService();
        Session session = service.Create(options);
        _unitOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
        _unitOfWork.Save();

        Response.Headers.Add("Location", session.Url);
        return new StatusCodeResult(303);
    }


    public IActionResult PaymentConfirmation(int orderHeaderId)
    {
        var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);

        if (orderHeader != null && orderHeader.OrderStatus != null && orderHeader.PaymentStatus == PAYMENT_STATUS.PaymentStatusDealyedPayment)
        {
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.ToLower() == "paid")
            {
                _unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, PAYMENT_STATUS.PaymentStatusApproved);
                _unitOfWork.Save();
            }
        }

        return View(orderHeaderId);
    }


    #region API CALLS
    [HttpGet]
    public IActionResult GetAll(string status)
    {
        IEnumerable<OrderHeader> objOrderHeaders;

        if (User.IsInRole(USER_ROLE.Role_Admin) || User.IsInRole(USER_ROLE.Role_Employee))
        {
            objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
        }
        else
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            objOrderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser").ToList();
        }

        switch (status)
        {
            case "pending":
                objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == PAYMENT_STATUS.PaymentStatusDealyedPayment);
                break;
            case "processing":
                objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == ORDER_STATUS.StatusInProcess);
                break;
            case "completed":
                objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == ORDER_STATUS.StatusShipped);
                break;
            case "approved":
                objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == PAYMENT_STATUS.PaymentStatusApproved);
                break;
            default:
                break;
        }
        return Json(new { data = objOrderHeaders });
    }

    #endregion
}