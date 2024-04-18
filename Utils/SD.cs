using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Utils;

public static class USER_ROLE
{
    public const string Role_Customer = "Customer";
    public const string Role_Company = "Company";
    public const string Role_Admin = "Admin";
    public const string Role_Employee = "Employee";
}

public static class PAYMENT_STATUS
{
    public const string PaymentStatusPending = "Pending";
    public const string PaymentStatusApproved = "Approved";
    public const string PaymentStatusDealyedPayment = "DelayedPayment";
}
public static class ORDER_STATUS
{
    public const string OrderStatusPending = "Pending";
    public const string StatusInProcess = "processing";
    public const string StatusShipped = "shipped";
    public const string StatusRefunded = "refunded";
    public const string StatusCancelled = "cancelled";
    public const string OrderStatusApproved = "Approved";
}

public static class SIZES
{
    public const string EXTRA = "Extra Large";
    public const string SMALL = "Small";
    public const string MEDIUM = "Medium";
    public const string LARGE = "Large";

    public static IEnumerable<SelectListItem> Sizes()
    {
        List<SelectListItem> items = new List<SelectListItem>{
            new SelectListItem { Text = SMALL, Value = SMALL },
            new SelectListItem { Text = MEDIUM, Value = MEDIUM },
            new SelectListItem { Text = LARGE, Value = LARGE },
            new SelectListItem { Text = EXTRA, Value = EXTRA }
        };

        IEnumerable<SelectListItem> selectList = items.AsEnumerable();
        return selectList;
    }
}

public static class NepaliFormat
{
    static CultureInfo nepaliCulture = new CultureInfo("ne-NP");

    static NumberFormatInfo nepaliFormat = (NumberFormatInfo)nepaliCulture.NumberFormat.Clone();

    public static NumberFormatInfo GetFormat()
    {
        nepaliFormat.CurrencySymbol = "NPR";
        return nepaliFormat;
    }

}