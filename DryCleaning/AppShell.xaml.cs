using DryCleaning.Helpers;
using DryCleaning.Views.Auth;
using DryCleaning.Views.Customer;
using DryCleaning.Views.Driver;
using DryCleaning.Views.Provider;

namespace DryCleaning;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pushed (non-tab) pages
        Routing.RegisterRoute(Routes.Otp, typeof(OtpPage));
        Routing.RegisterRoute(Routes.Notifications, typeof(NotificationsPage));
        Routing.RegisterRoute(Routes.OrderDetail, typeof(OrderDetailPage));
        Routing.RegisterRoute(Routes.ReceiveOrder, typeof(ReceiveOrderPage));
        Routing.RegisterRoute(Routes.Inspection, typeof(InspectionPage));
        Routing.RegisterRoute(Routes.Processing, typeof(ProcessingPage));
        Routing.RegisterRoute(Routes.QualityCheck, typeof(QualityCheckPage));
        Routing.RegisterRoute(Routes.ReadyForDelivery, typeof(ReadyForDeliveryPage));
        Routing.RegisterRoute(Routes.OrderHistory, typeof(OrderHistoryPage));
        Routing.RegisterRoute(Routes.ProductDetail, typeof(ProductDetailPage));
        Routing.RegisterRoute(Routes.ProductEdit, typeof(ProductEditPage));
        Routing.RegisterRoute(Routes.ServiceItemEdit, typeof(ServiceItemEditPage));

        // Customer pushed pages
        Routing.RegisterRoute(Routes.Categories, typeof(CategoriesPage));
        Routing.RegisterRoute(Routes.Providers, typeof(ProvidersPage));
        Routing.RegisterRoute(Routes.Products, typeof(ProductsPage));
        Routing.RegisterRoute(Routes.ServiceItems, typeof(ServiceItemsPage));
        Routing.RegisterRoute(Routes.ItemDetails, typeof(ItemDetailsPage));
        Routing.RegisterRoute(Routes.Cart, typeof(CartPage));
        Routing.RegisterRoute(Routes.PickupAddress, typeof(PickupAddressPage));
        Routing.RegisterRoute(Routes.PickupTime, typeof(PickupTimePage));
        Routing.RegisterRoute(Routes.OrderSummary, typeof(OrderSummaryPage));
        Routing.RegisterRoute(Routes.Payment, typeof(PaymentPage));
        Routing.RegisterRoute(Routes.OrderConfirmed, typeof(OrderConfirmedPage));
        Routing.RegisterRoute(Routes.OrderTracking, typeof(OrderTrackingPage));
        Routing.RegisterRoute(Routes.CustomerOrderDetail, typeof(CustomerOrderDetailPage));
        Routing.RegisterRoute(Routes.EditProfile, typeof(EditProfilePage));

        // Driver pushed pages
        Routing.RegisterRoute(Routes.DriverJobDetail, typeof(DriverJobDetailPage));
        Routing.RegisterRoute(Routes.DriverNavigate, typeof(DriverNavigatePage));
        Routing.RegisterRoute(Routes.DriverVerifyItems, typeof(DriverVerifyItemsPage));
        Routing.RegisterRoute(Routes.DriverConfirmPickup, typeof(DriverConfirmPickupPage));
        Routing.RegisterRoute(Routes.DriverPickupComplete, typeof(DriverPickupCompletePage));
        Routing.RegisterRoute(Routes.DriverHandover, typeof(DriverHandoverPage));
        Routing.RegisterRoute(Routes.DriverCollect, typeof(DriverCollectPage));
        Routing.RegisterRoute(Routes.DriverDeliver, typeof(DriverDeliverPage));
        Routing.RegisterRoute(Routes.DriverJobCompleted, typeof(DriverJobCompletedPage));
        Routing.RegisterRoute(Routes.DriverEditProfile, typeof(DriverEditProfilePage));
    }
}
