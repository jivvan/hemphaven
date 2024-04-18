namespace BulkyWeb.Models;

public class ShoppingCartVm
{
    public IEnumerable<ShoppingCart> ShoppingCartList { get; set; }
    public OrderHeader OrderHeader { get; set; }
}
