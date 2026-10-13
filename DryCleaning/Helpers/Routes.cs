using DryCleaning.Models;

namespace DryCleaning.Helpers;

public static class Routes
{
    // Shell roots (absolute)
    public const string Splash = "//splash";
    public const string RoleSelection = "//roles";
    public const string Login = "//login";
    public const string ProviderDashboard = "//provider/dashboard";
    public const string ProviderOrders = "//provider/orders";

    public const string CustomerHome = "//customer/home";
    public const string CustomerOrders = "//customer/orders";
    public const string CustomerProfile = "//customer/profile";

    public const string DriverDashboard = "//driver/dashboard";
    public const string DriverJobs = "//driver/jobs";
    public const string DriverEarnings = "//driver/earnings";

    // Pushed pages (relative)
    public const string Otp = "otp";
    public const string SignUp = "signup";
    public const string ForgotPassword = "forgotpassword";
    public const string ChangePassword = "changepassword";
    public const string EditBusinessProfile = "editbusinessprofile";
    public const string ProviderCategories = "providercategories";
    public const string Notifications = "notifications";
    public const string OrderDetail = "orderdetail";
    public const string ReceiveOrder = "receiveorder";
    public const string Inspection = "inspection";
    public const string Processing = "processing";
    public const string QualityCheck = "qualitycheck";
    public const string ReadyForDelivery = "readyfordelivery";
    public const string OrderHistory = "orderhistory";
    public const string ProductDetail = "productdetail";
    public const string ProductEdit = "productedit";
    public const string ServiceItemEdit = "serviceitemedit";

    // Customer pushed pages
    public const string Categories = "categories";
    public const string Providers = "providers";
    public const string Products = "products";
    public const string ServiceItems = "serviceitems";
    public const string ItemDetails = "itemdetails";
    public const string Cart = "cart";
    public const string PickupAddress = "pickupaddress";
    public const string PickupTime = "pickuptime";
    public const string OrderSummary = "ordersummary";
    public const string Payment = "payment";
    public const string OrderConfirmed = "orderconfirmed";
    public const string OrderTracking = "ordertracking";
    public const string CustomerOrderDetail = "customerorderdetail";
    public const string EditProfile = "editprofile";

    // Driver pushed pages
    public const string DriverJobDetail = "driverjob";
    public const string DriverNavigate = "drivernavigate";
    public const string DriverVerifyItems = "driververifyitems";
    public const string DriverConfirmPickup = "driverconfirmpickup";
    public const string DriverPickupComplete = "driverpickupcomplete";
    public const string DriverHandover = "driverhandover";
    public const string DriverCollect = "drivercollect";
    public const string DriverDeliver = "driverdeliver";
    public const string DriverJobCompleted = "driverjobcompleted";
    public const string DriverEditProfile = "drivereditprofile";

    // Query parameter keys
    public const string OrderIdKey = "orderId";
    public const string ProductIdKey = "productId";
    public const string CategoryIdKey = "categoryId";
    public const string ServiceItemIdKey = "serviceItemId";
    public const string FilterKey = "filter";
    public const string IdentifierKey = "identifier";
    public const string ProviderIdKey = "providerId";
    public const string ModeKey = "mode";
    public const string JobIdKey = "jobId";

    /// <summary>Home route for a signed-in role.</summary>
    public static string HomeFor(UserRole role) => role switch
    {
        UserRole.Customer => CustomerHome,
        UserRole.Driver => DriverDashboard,
        _ => ProviderDashboard
    };
}
