using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BulkyWeb.Models;
using BulkyWeb.Data;
using Microsoft.EntityFrameworkCore;
using BulkyWeb.Repository.IRepository;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace BulkyWeb.Areas.Customer.Controllers;

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

    [HttpGet]
    public IActionResult Details(int id)
    {
        var product = _unitOfWork.Product.Get(u => u.Id == id, includeProperties: "Category");

        if (product == null)
        {
            return NotFound();
        }

        return View(new ShoppingCart
        {
            Product = product,
            ProductId = id,
            Count = 1,
        });
    }

    [HttpPost]
    [Authorize]
    public IActionResult Details(ShoppingCart shoppingCart)
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
        shoppingCart.ApplicationUserId = userId;

        ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.ApplicationUserId == userId && u.ProductId == shoppingCart.ProductId);

        if (cartFromDb != null)
        {
            // shopping cart exists
            cartFromDb.Count += shoppingCart.Count;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
        }
        else
        {
            shoppingCart.Id = 0;
            _unitOfWork.ShoppingCart.Add(shoppingCart);
        }
        _unitOfWork.Save();

        TempData["success"] = "Cart updated successfully!";

        return RedirectToAction(nameof(Index));
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
