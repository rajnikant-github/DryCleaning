namespace DryCleaning.Configuration;

/// <summary>Where the app finds the E Dry Cleaners API (used when Features.UseMockData is false).</summary>
public sealed class ApiSettings
{
    /// <summary>Production API root (no trailing "api/" – request paths include it).</summary>
    public string BaseUrl { get; set; } = "https://edrycleaners.co.za/";

    /// <summary>Debug builds talk to the API running on your machine (./run-api.sh) instead of production.</summary>
    public bool UseLocalApiInDebug { get; set; } = true;

    /// <summary>Android emulator: 10.0.2.2 is the host machine. Use your Mac's LAN IP for a physical phone.</summary>
    public string LocalBaseUrlAndroid { get; set; } = "http://10.0.2.2:5180/";

    /// <summary>iOS simulator / Mac Catalyst. Use your Mac's LAN IP for a physical phone.</summary>
    public string LocalBaseUrlApple { get; set; } = "http://localhost:5180/";

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Must match Paystack:CallbackUrl in the API's appsettings.json.</summary>
    public string PaymentCallbackUrl { get; set; } = "edrycleaners://payment-complete";

    /// <summary>The base URL for this build and platform.</summary>
    public string ResolveBaseUrl()
    {
        var url = BaseUrl;
#if DEBUG
        if (UseLocalApiInDebug)
            url = DeviceInfo.Current.Platform == DevicePlatform.Android ? LocalBaseUrlAndroid : LocalBaseUrlApple;
#endif
        return url.EndsWith('/') ? url : url + "/";
    }
}
