using DryCleaning.Configuration;
using DryCleaning.Helpers;
using DryCleaning.Services;
using DryCleaning.Services.Api;
using DryCleaning.Services.Mock;
using DryCleaning.Services.Places;
using DryCleaning.ViewModels;
using DryCleaning.ViewModels.Auth;
using DryCleaning.ViewModels.Customer;
using DryCleaning.ViewModels.Driver;
using DryCleaning.ViewModels.Provider;
using DryCleaning.Views.Auth;
using DryCleaning.Views.Customer;
using DryCleaning.Views.Driver;
using DryCleaning.Views.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DryCleaning;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // Swap these files for Outfit (Figma) by dropping the .ttf files into Resources/Fonts
                // and changing only the file names below – styles use the aliases.
                fonts.AddFont("OpenSans-Regular.ttf", "AppRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "AppSemibold");
            });

        ConfigureHandlers();

        // Configuration (appsettings.json – currency, VAT, feature flags)
        var settings = AppSettings.Load();
        var currency = new CurrencyFormatter(settings.Currency);
        CurrencyFormatter.Initialize(currency);
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton<ICurrencyFormatter>(currency);

        // Platform & app services
        builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<IPhotoService, PhotoService>();
        builder.Services.AddSingleton<ProfilePhotoChanger>();
        builder.Services.AddSingleton<ISessionService, SessionService>();
        builder.Services.AddSingleton<ViewModelServices>();

        // Domain services – Features.UseMockData in appsettings.json switches between demo data and the real API.
        if (settings.Features.UseMockData)
            AddMockServices(builder.Services);
        else
            AddApiServices(builder.Services, settings);

        // Address search: Google Places when GoogleMaps.ApiKey is set (and UseSampleAddresses isn't), otherwise sample Cape Town addresses.
        if (settings.GoogleMaps.UseGoogle)
            builder.Services.AddSingleton<IPlacesService, GooglePlacesService>();
        else
            builder.Services.AddSingleton<IPlacesService, MockPlacesService>();

        builder.Services.AddSingleton<ICartService, CartService>();
        builder.Services.AddSingleton<DriverPickupDraft>();

        builder.Services.AddSingleton<AppShell>();

        // Pages & ViewModels
        builder.Services
            .AddPage<SplashPage, SplashViewModel>()
            .AddPage<RoleSelectionPage, RoleSelectionViewModel>()
            .AddPage<LoginPage, LoginViewModel>()
            .AddPage<OtpPage, OtpViewModel>()
            .AddPage<SignUpPage, SignUpViewModel>()
            .AddPage<ForgotPasswordPage, ForgotPasswordViewModel>()
            .AddPage<ChangePasswordPage, ChangePasswordViewModel>()
            .AddPage<EditBusinessProfilePage, EditBusinessProfileViewModel>()
            .AddPage<ProviderCategoriesPage, ProviderCategoriesViewModel>()
            .AddPage<DashboardPage, DashboardViewModel>()
            .AddPage<OrdersPage, OrdersViewModel>()
            .AddPage<OrderHistoryPage, OrderHistoryViewModel>()
            .AddPage<OrderDetailPage, OrderDetailViewModel>()
            .AddPage<ReceiveOrderPage, ReceiveOrderViewModel>()
            .AddPage<InspectionPage, InspectionViewModel>()
            .AddPage<ProcessingPage, ProcessingViewModel>()
            .AddPage<QualityCheckPage, QualityCheckViewModel>()
            .AddPage<ReadyForDeliveryPage, ReadyForDeliveryViewModel>()
            .AddPage<ServicesPage, CatalogViewModel>()
            .AddPage<ProductDetailPage, ProductDetailViewModel>()
            .AddPage<ProductEditPage, ProductEditViewModel>()
            .AddPage<ServiceItemEditPage, ServiceItemEditViewModel>()
            .AddPage<ProfilePage, ProfileViewModel>()
            .AddPage<NotificationsPage, NotificationsViewModel>()
            // Customer
            .AddPage<CustomerHomePage, CustomerHomeViewModel>()
            .AddPage<CategoriesPage, CategoriesViewModel>()
            .AddPage<ProvidersPage, ProvidersViewModel>()
            .AddPage<ProductsPage, ProductsViewModel>()
            .AddPage<ServiceItemsPage, ServiceItemsViewModel>()
            .AddPage<ItemDetailsPage, ItemDetailsViewModel>()
            .AddPage<CartPage, CartViewModel>()
            .AddPage<PickupAddressPage, PickupAddressViewModel>()
            .AddPage<PickupTimePage, PickupTimeViewModel>()
            .AddPage<OrderSummaryPage, OrderSummaryViewModel>()
            .AddPage<PaymentPage, PaymentViewModel>()
            .AddPage<OrderConfirmedPage, OrderConfirmedViewModel>()
            .AddPage<OrderTrackingPage, OrderTrackingViewModel>()
            .AddPage<CustomerOrdersPage, CustomerOrdersViewModel>()
            .AddPage<CustomerOrderDetailPage, CustomerOrderDetailViewModel>()
            .AddPage<CustomerProfilePage, CustomerProfileViewModel>()
            .AddPage<EditProfilePage, EditProfileViewModel>()
            // Driver
            .AddPage<DriverDashboardPage, DriverDashboardViewModel>()
            .AddPage<DriverJobsPage, DriverJobsViewModel>()
            .AddPage<DriverJobDetailPage, DriverJobDetailViewModel>()
            .AddPage<DriverNavigatePage, DriverNavigateViewModel>()
            .AddPage<DriverVerifyItemsPage, DriverVerifyItemsViewModel>()
            .AddPage<DriverConfirmPickupPage, DriverConfirmPickupViewModel>()
            .AddPage<DriverPickupCompletePage, DriverPickupCompleteViewModel>()
            .AddPage<DriverHandoverPage, DriverHandoverViewModel>()
            .AddPage<DriverCollectPage, DriverCollectViewModel>()
            .AddPage<DriverDeliverPage, DriverDeliverViewModel>()
            .AddPage<DriverJobCompletedPage, DriverJobCompletedViewModel>()
            .AddPage<DriverEarningsPage, DriverEarningsViewModel>()
            .AddPage<DriverProfilePage, DriverProfileViewModel>()
            .AddPage<DriverEditProfilePage, DriverEditProfileViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void AddMockServices(IServiceCollection services)
    {
        services.AddSingleton<MockDataStore>();
        services.AddSingleton<IAuthService, MockAuthService>();
        services.AddSingleton<IProviderOrderService, MockProviderOrderService>();
        services.AddSingleton<IProviderCatalogService, MockProviderCatalogService>();
        services.AddSingleton<IProviderProfileService, MockProviderProfileService>();
        services.AddSingleton<INotificationService, MockNotificationService>();
        services.AddSingleton<ICustomerCatalogService, MockCustomerCatalogService>();
        services.AddSingleton<ICustomerOrderService, MockCustomerOrderService>();
        services.AddSingleton<ICustomerProfileService, MockCustomerProfileService>();
        services.AddSingleton<IPaymentService, MockPaymentService>();
        services.AddSingleton<IDriverService, MockDriverService>();
        services.AddSingleton<IRealtimeService, MockRealtimeService>();
        services.AddSingleton<IUserPhotoService, MockUserPhotoService>();
    }

    private static void AddApiServices(IServiceCollection services, AppSettings settings)
    {
        // Demo shortcuts (simulate driver, show customer codes) only make sense with mock data.
        settings.Features.ShowDemoActions = false;

        services.AddSingleton<TokenStore>();
        services.AddSingleton<ApiClient>();
        services.AddSingleton<IAuthService, ApiAuthService>();
        services.AddSingleton<IProviderOrderService, ApiProviderOrderService>();
        services.AddSingleton<IProviderCatalogService, ApiProviderCatalogService>();
        services.AddSingleton<IProviderProfileService, ApiProviderProfileService>();
        services.AddSingleton<INotificationService, ApiNotificationService>();
        services.AddSingleton<ICustomerCatalogService, ApiCustomerCatalogService>();
        services.AddSingleton<ICustomerOrderService, ApiCustomerOrderService>();
        services.AddSingleton<ICustomerProfileService, ApiCustomerProfileService>();
        services.AddSingleton<IPaymentService, ApiPaymentService>();
        services.AddSingleton<IDriverService, ApiDriverService>();
        services.AddSingleton<IRealtimeService, ApiRealtimeService>();
        services.AddSingleton<IUserPhotoService, ApiUserPhotoService>();
    }

    private static IServiceCollection AddPage<TPage, TViewModel>(this IServiceCollection services)
        where TPage : Page
        where TViewModel : BaseViewModel
    {
        services.AddTransient<TPage>();
        services.AddTransient<TViewModel>();
        return services;
    }

    /// <summary>Removes native underlines/borders so Entry/Editor/Picker blend into the InputField style.</summary>
    private static void ConfigureHandlers()
    {
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoBorder", (handler, _) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
        });

        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("NoBorder", (handler, _) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
        });

        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoBorder", (handler, _) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
        });
    }
}
