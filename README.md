# EDry Cleaners

On-demand dry cleaning and laundry marketplace for South Africa (prices in ZAR).
Customers book pickups, service providers clean the items, and shared marketplace drivers collect and deliver.

| Folder | What it is |
|---|---|
| [`DryCleaning/`](DryCleaning/) | .NET MAUI app (Android + iOS, MVVM) – Customer, Service Provider and Driver roles in one app |
| [`DryCleaning.Api/`](DryCleaning.Api/) | ASP.NET Core Web API (.NET 10) – SQL Server, EF Core, JWT, Swagger, Paystack. [API docs →](DryCleaning.Api/README.md) |
| [`docs/API-GUIDE.md`](docs/API-GUIDE.md) | API & workflow guide – every endpoint, order/payment/driver/dispute flows, request rules, enums and all database tables |
| [`DryCleaning.Admin/`](DryCleaning.Admin/) | Admin web portal (Blazor, .NET 10) – dashboard, orders, driver jobs, payments, disputes, provider approvals, drivers, customers, categories, delivery pricing, extra order charges and push announcements to any user or everyone. Talks to the API only |
| `run-api.sh` / `run-api.ps1` | One-command local API runner – macOS (bash) / Windows (PowerShell) |
| `set-app-env.sh` / `set-app-env.ps1` | Switch the app between environments (mock, local, staging, production) |
| `DryCleaning.sln` | Solution with both projects |

## Secrets: one file for everything

Every secret of the repo lives in **one git-ignored file at the repository root: `secrets.json`**. Create it by copying the
committed template and filling it in:

```bash
cp secrets.example.json secrets.json     # then edit secrets.json (chmod 600 is a good idea)
```

It has one block per environment (`local`, `staging`, `production`) and, in each, one section per project:

| Section | Read by | Holds |
|---|---|---|
| `Api` | the API (`DryCleaning.Api`) | Paystack keys and callback URL, Routes API key (`Routes.ApiKey`, for driving distance), the Firebase service-account JSON (`Push.FirebaseCredentialsJson`, pasted in as an object), the Apple APNs `.p8` text (`ApnsSettings.PrivateKey`), and any other API setting you want kept out of git |
| `Admin` | the admin portal | nothing secret yet (it only needs the API address) |
| `App` | `./set-app-env.sh <env>` → the mobile app build | keys that are restricted in Google's console, e.g. `GoogleMaps.ApiKey`. **Never put server secrets here** – anything in the app can be extracted |

- **Which block:** the API and portal pick the block from `ASPNETCORE_ENVIRONMENT` (`Development` → `local`). Override with
  `EDRY_ENVIRONMENT=staging`. Override the file location with `EDRY_SECRETS_FILE=/path/secrets.json`.
- **Order of precedence:** `appsettings*.json` < user-secrets < `secrets.json` < real environment variables. So on a server you can skip the
  file and set variables like `Paystack__SecretKey`; they win over the file.
- **Production:** don't copy `secrets.json` to the server or into an image. Keep the `production` block empty on developer
  machines and supply production values as environment variables or from a secret manager (e.g. Azure Key Vault).
- **Property names ending in `Json`** keep their object as raw JSON text (that is how the Firebase file is pasted in unchanged).
- **Rotating a key** = edit one line in `secrets.json` and restart the API.

## Admin portal

```bash
cd DryCleaning.Admin && dotnet run --launch-profile http   # http://localhost:5190
```

Sign in with an **Admin** account (locally `admin@edrycleaners.co.za`, password in the [API guide](docs/API-GUIDE.md#1-quick-start)).
The portal calls the API at `Api:BaseUrl` (`DryCleaning.Admin/appsettings.json`, default `http://localhost:5180/`); in production set
`Api__BaseUrl` to your API address. It keeps no data of its own, so there is nothing to migrate. The sign-in lasts as long as
the API's access token (60 minutes), then you sign in again.

From **Send notification** an admin can notify one user (search by name, email or phone), everyone, or all customers / providers /
drivers. Each recipient gets an in-app notification and a push on their phone.

## Contents

- [Quick start](#quick-start)
  - [API + Swagger](#api--swagger)
  - [Mobile app](#mobile-app)
- [Testing the app against a deployed API](#testing-the-app-against-a-deployed-api)
  - [1. Point the app at the server](#1-point-the-app-at-the-server)
  - [2. Server checklist](#2-server-checklist)
  - [3. Smoke test after each deploy](#3-smoke-test-after-each-deploy)
  - [Troubleshooting](#troubleshooting)
- [Switching environments](#switching-environments)
- [Configuration](#configuration)
- [Status (V1)](#status-v1)

## Quick start

### API + Swagger

```bash
./run-api.sh          # macOS (Terminal)
```
```powershell
.\run-api.ps1         # Windows (PowerShell)
```

Checks your setup first (Docker, SQL Server container, .NET 10, `dotnet-ef`, HTTPS certificate, migrations),
fixes only what's missing after asking, then runs the API and opens **https://localhost:7180/swagger**.
Check only: `--check` / `-Check`. Fresh database: `--reset-db` / `-ResetDb`. All options, other ways to run
the scripts (permissions, execution policy) and troubleshooting:
[DryCleaning.Api/README.md](DryCleaning.Api/README.md#run-locally).

Demo users (password `Demo@12345`): `sipho@example.co.za` (Customer), `thandi@sparkledrycleaners.co.za` (Provider),
`bongani@drivers.edrycleaners.co.za` (Driver), `admin@edrycleaners.co.za` (Admin).

### Mobile app

Open `DryCleaning.sln` in Rider (or Visual Studio) and run the **DryCleaning** project on an Android emulator or iOS simulator.

The app talks to the **real API** (`"UseMockData": false` in `DryCleaning/appsettings.json`):

> Tip: `./set-app-env.sh local` / `mock` / `staging` switches all of this in one command – see
> [Switching environments](#switching-environments).

1. Start the API first: `./run-api.sh` (it listens on `http://localhost:5180`).
2. Run the app in **Debug**. It calls your machine automatically:
   - Android emulator → `http://10.0.2.2:5180/`
   - iOS simulator → `http://localhost:5180/`
   - Physical phone → set `Api:LocalBaseUrlAndroid` / `Api:LocalBaseUrlApple` to `http://<your Mac's LAN IP>:5180/`
     (and on Android add that IP to `Platforms/Android/Resources/xml/network_security_config.xml`). Start the API with the
     `http` profile so the phone can reach it: `cd DryCleaning.Api && dotnet run --launch-profile http`.
3. Release builds use `Api:BaseUrl` (`https://edrycleaners.co.za/`). To test against a server, see
   [Testing the app against a deployed API](#testing-the-app-against-a-deployed-api).

Pick a role, then sign in with a demo account (password `Demo@12345`) or with OTP – in Development the API returns the
code and the OTP screen shows it. Choosing the wrong role for an account shows which role to pick. The session is kept
on the phone (SecureStorage) and refreshed automatically, so you stay signed in after closing the app.

**Accounts and passwords in the app**

| Where | What |
|---|---|
| Login → *Create account* | Sign up for the role picked on the Role screen. Customers and drivers are signed in straight away; a new **Service Provider** (business name, description, categories) is reviewed by an admin first, so the app confirms the registration and returns to Login. Passwords: at least 8 characters with a letter and a number. |
| Login → *Forgot password?* | Enter your mobile number or email → enter the 6-digit code (shown on screen in Development) and a new password → you're signed in; other devices are signed out. |
| Profile → *Change Password* | Customer, Service Provider and Driver profiles: current password + new password. |
| Provider Profile → *Edit* (Business) | Business name, description, phone, email and logo (take or choose a photo – uploaded to `POST api/provider/profile/logo`). |
| Provider Profile → *Manage* (Categories) | Switch the categories you offer on or off; customers only see your products in categories that are on. |

| Role | Demo account | Try |
|---|---|---|
| Customer | `sipho@example.co.za` | Browse → provider → items → cart → pickup address & slot → pay (Paystack test card) → track |
| Service Provider | `thandi@sparkledrycleaners.co.za` | Accept → receive → inspect → process → quality check → request driver |
| Driver | `bongani@drivers.edrycleaners.co.za` | Go online → accept a job → navigate → verify items → pickup code → hand over / deliver with OTP |

**Demo data instead of the API:** set `"UseMockData": true` – no API needed, any email/phone + 4-character password,
OTP `123456`, and the "simulate driver" / show-customer-code shortcuts (`Features:ShowDemoActions`).

**First launch:** someone who isn't signed in and hasn't seen it yet gets the intro screen and three onboarding slides (Get Started /
Next / Skip) before the role picker, built from the Figma "Customer General Onboarding" screens. Finishing, skipping or tapping *Log in*
remembers that on the phone, so later launches go straight to the role picker (or to the home screen when signed in). To see it again,
uninstall the app (Android) or, on an iOS simulator, also log out first, because the iOS keychain keeps the sign-in after an uninstall.

**Address search** uses Google: Places API (New) for suggestions and Geocoding API for "Use my current location".
The key is **never committed**. It lives in the repository's single secrets file (see [Secrets](#secrets-one-file-for-everything)),
under `local` → `App` → `GoogleMaps` → `ApiKey`. Add it there, then run `./set-app-env.sh local` to copy the `App` section into the build.

`set-app-env` copies only the chosen environment's `App` section into the build (as the git-ignored
`DryCleaning/appsettings.secrets.json`, merged last) – the `Api` section never goes into the app. Environments without an `App`
section or key get none. To give another environment a key later, fill its `App` section in `secrets.json` the same way.
In Google Cloud Console enable both APIs, restrict the key to them, and (for released apps) to your Android package /
iOS bundle id. Without a key the app uses sample Cape Town addresses. A key built into an app can be extracted
from it – the API restrictions are what protect it.

**Payments:** the app opens Paystack's checkout page and Paystack returns to the app via
`edrycleaners://payment-complete` (must match `Paystack:CallbackUrl` in the API). The API then verifies the payment.
Use Paystack test keys and a [test card](https://paystack.com/docs/payments/test-payments/) while developing.

**Push notifications in the app** (Android: Firebase, iOS: Apple APNs – server setup in [DryCleaning.Api/README.md](DryCleaning.Api/README.md#push-notifications-android-firebase-ios-apple-apns)):

- The app asks for notification permission after sign-in, registers the phone with the API and unregisters it on sign-out. Tapping a
  notification opens the order. Demo (mock) mode has no push.
- **iOS needs a real iPhone** to receive pushes: the simulator can't get an APNs token because simulator builds aren't signed with the
  push entitlement. In the simulator you can still check how a notification looks with
  `xcrun simctl push <device> com.dev.edrycleaners payload.apns` (payload with `aps.alert` and an `orderId`).
- A real iPhone build needs an App ID with **Push Notifications** enabled and a development provisioning profile that includes it.
- **Android** works on an emulator with Google Play services; the first run asks for the Android 13+ notification permission.
- **Android build tip:** `DryCleaning.csproj` pins `Xamarin.AndroidX.Fragment.Ktx` to 1.9.0 next to the Firebase packages. Without it the
  build fails with "Type androidx.fragment.app… is defined multiple times". After adding or upgrading Android packages, delete
  `DryCleaning/obj` and `DryCleaning/bin` and rebuild – a stale incremental build can crash on start with a missing-resource error.

## Testing the app against a deployed API

Use this when the API runs on a server (staging or production) instead of your machine.

### 1. Point the app at the server

Quickest: put the server address in `DryCleaning/environments/staging.json` and run `./set-app-env.sh staging`
(see [Switching environments](#switching-environments)). The settings it applies, if you prefer to edit
`DryCleaning/appsettings.json` directly:

```json
"Features": { "UseMockData": false },
"Api": {
  "BaseUrl": "https://your-server.example.com/",
  "UseLocalApiInDebug": false
}
```

- `BaseUrl` is the server root – **without** `api/` (request paths already include it) and **with** a trailing `/`.
- `UseLocalApiInDebug: false` makes **Debug** builds use `BaseUrl` too. Leave it `true` and Debug builds keep calling
  your local API, whatever `BaseUrl` says. Release builds always use `BaseUrl`.
- The server must use **HTTPS**. Android only allows plain `http` to `10.0.2.2` / `localhost` / `127.0.0.1`
  (`Platforms/Android/Resources/xml/network_security_config.xml`), and iOS only to local network addresses.
- To hand the app to testers, build a **Release** version (Android: an `.apk`/`.aab` from Rider's *Publish*;
  iOS: TestFlight).

### 2. Server checklist

Set these on the server as environment variables (`__` separates sections, e.g. `Jwt__SigningKey`) or in
`appsettings.Production.json` – never commit real secrets.

| Setting | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` (or `Staging`). Development-only behaviour (OTP code in the response, HTTP allowed, demo data) is then off. |
| `ConnectionStrings__DefaultConnection` | Your SQL Server. With `Database:InitializeOnStartup: true` (default) the API applies migrations on start-up. |
| `Jwt__SigningKey` | A long random secret (32+ characters). Changing it later signs everyone out. |
| `Paystack__SecretKey` / `Paystack__PublicKey` | Test keys (`sk_test_…`/`pk_test_…`) on staging, live keys only in production. |
| `Paystack__CallbackUrl` | `edrycleaners://payment-complete` – must match `Api:PaymentCallbackUrl` in the app. |
| Paystack dashboard → Webhook URL | `https://your-server.example.com/api/payments/paystack/webhook` |
| `Database__SeedDemoData` | `true` on a **test** server to get the demo accounts (only seeds an empty database). Keep `false` in production. |
| `Otp__ExposeCodeInResponse` | `false`. No SMS provider is connected (codes are only emailed), so on a server sign in with **password** unless the account has an email. For OTP testing on a test server only, set `true` – the OTP screen then shows the code. |
| `Push__FirebaseCredentialsJson` | The Firebase service-account JSON (the whole file's text) for Android pushes (optional – without it they are only logged). |
| `ApnsSettings__PrivateKey` (+ `ApnsSettings__UseSandbox=false` on TestFlight/App Store) | The text of the Apple APNs `.p8` key for iPhone pushes (or a file path in `ApnsSettings__PrivateKeyPath`); `TeamId`, `KeyId`, `BundleId` are in `appsettings.json` (optional – without the key they are only logged). |
| `Swagger__Enabled` | `true` on staging; your choice in production. |
| Uploads | **Azure:** set `Storage__Provider=AzureBlob` and `Storage__AzureBlob__AccountUrl=https://<account>.blob.core.windows.net`, and give the App Service's managed identity the **Storage Blob Data Contributor** role on that account. Order photos are then private (signed 15-minute links); logos are public. **Other hosting:** the default `Local` provider saves under `wwwroot/uploads` – use persistent disk, not a container's temporary file system. |

### 3. Smoke test after each deploy

1. Open `https://your-server.example.com/swagger` – it loads and `POST /api/auth/login` works with a demo account.
2. **Customer:** sign in → choose an address (Google search) → add items → pickup slot → pay with a Paystack
   [test card](https://paystack.com/docs/payments/test-payments/) → the order shows as paid.
3. **Service Provider:** the order appears under *New* → accept it.
4. **Driver:** go online → accept the pickup job → enter the customer's pickup code (shown on the customer's order
   screen) → hand over to the provider.
5. **Service Provider:** receive → inspect (add a photo) → processing → quality check → ready → request a driver.
6. **Driver:** accept the delivery → collect → deliver with the customer's delivery OTP → the order is completed.
7. Close and reopen the app – you should still be signed in.

### Troubleshooting

| Problem | Fix |
|---|---|
| App still shows your local data or "Can't reach EDry Cleaners" | Debug build with `UseLocalApiInDebug: true` – set it to `false`, or build Release. Check `BaseUrl` ends with `/` and has no `api/`. |
| "Cleartext HTTP traffic not permitted" (Android) / ATS error (iOS) | The server URL is `http://` – put the API behind HTTPS. |
| Everyone signed out after a deploy, 401 errors | `Jwt:SigningKey` changed between deploys – keep it the same. |
| Login works in Swagger but the app says "This is a … account" | You picked a different role on the role screen than the account has. |
| OTP never arrives | Expected for now – use password login, or `Otp__ExposeCodeInResponse=true` on a test server. |
| Paystack page doesn't return to the app | Check `Paystack:CallbackUrl` is `edrycleaners://payment-complete`. If Paystack rejects that address, the API needs a small https page that forwards to the app. |
| Photos missing after a restart/redeploy | Using `Local` storage without persistent disk – switch to `Storage__Provider=AzureBlob`. |
| Photos not loading after a while (Azure) | Signed links expire after 15 minutes; reload the order to get new ones. |
| "Address search isn't working" | Check `secrets.json` has your key under `<env>` → `App` → `GoogleMaps` and re-run `./set-app-env.sh <name>`, enable Places API (New) and Geocoding API for it in Google Cloud Console, and check the key's restrictions. |

## Switching environments

`set-app-env.sh` (macOS) / `set-app-env.ps1` (Windows) point the app at an environment without editing
`DryCleaning/appsettings.json`:

```bash
./set-app-env.sh local                          # API on this machine (./run-api.sh) – emulator / simulator
./set-app-env.sh local --device-ip 192.168.1.20 # API on this machine – physical phone on your Wi-Fi
./set-app-env.sh mock                           # demo data, no API
./set-app-env.sh staging                        # test server
./set-app-env.sh production                     # https://edrycleaners.co.za/
./set-app-env.sh --show                         # which environment / server the next build uses
./set-app-env.sh --list                         # available environments (current one marked)
./set-app-env.sh --reset                        # back to appsettings.json as committed
```
```powershell
.\set-app-env.ps1 local                        # same commands on Windows
.\set-app-env.ps1 local -DeviceIp 192.168.1.20
.\set-app-env.ps1 -Show                        # also -List, -Reset, -Help (or --show, --list, ...)
```

Then **rebuild and run the app** – settings are built into the app. In Debug builds the role screen shows the
environment and server at the bottom (e.g. `Environment: staging · https://staging.edrycleaners.co.za/`).

| Environment | Data | Debug builds call | Release builds call |
|---|---|---|---|
| `mock` | Demo data on the phone | – | – |
| `local` | Your local API | `http://10.0.2.2:5180/` (Android emulator), `http://localhost:5180/` (iOS simulator) | `Api:BaseUrl` |
| `staging` | Test server | `https://staging.edrycleaners.co.za/` | same |
| `production` | Live server | `https://edrycleaners.co.za/` | same |

**How it works**

- Each environment is a small file in `DryCleaning/environments/<name>.json` that lists only the settings that
  differ from `appsettings.json` (sections are merged key by key).
- The script validates it and copies it to `DryCleaning/appsettings.env.json`. That file is git-ignored, so switching
  never shows up in `git status`; each developer picks their own environment.
- The app embeds `appsettings.env.json` when it exists and `AppSettings.Load()` merges it over `appsettings.json`.
  Without it the app uses `appsettings.json` exactly as committed.
- `--device-ip` / `-DeviceIp` sets both local URLs to `http://<ip>:5180/` and adds the IP to
  `Platforms/Android/Resources/xml/network_security_config.xml` (Android blocks plain `http` otherwise). That file
  is committed – remove the extra IP before a release. Start the API with the `http` profile so the phone can reach
  it: `cd DryCleaning.Api && dotnet run --launch-profile http`.
- Keys: the `App` section of that environment in `secrets.json` (git-ignored) is copied as `appsettings.secrets.json`;
  switching to an environment without one removes the key from the build. `--reset` removes both files.
- Warnings: production with demo data, and a server URL that isn't `https://`, doesn't end with `/`, or ends with `api/`.

**Change or add an environment** – edit `DryCleaning/environments/staging.json` to your real test-server address
(`Api:BaseUrl`), or add e.g. `uat.json` with the same shape and run `./set-app-env.sh uat`. Any setting from
`appsettings.json` can go in a profile – but keys go in `secrets.json` (git-ignored), never in
a profile, because profiles are committed.

**API side** – the API already switches with `ASPNETCORE_ENVIRONMENT` (`Development` locally via `run-api.sh`,
`Staging`/`Production` on a server) plus `appsettings.{Environment}.json` and environment variables – see the
[server checklist](#2-server-checklist).

## Configuration

- **App** – `DryCleaning/appsettings.json`: currency (ZAR / R / en-US, so amounts read R1,234.50 – use en-ZA for R1 234,50), the demo-data VAT, the per-km pickup & delivery price for demo data (with the API the admin portal sets it), driver earning per job,
  mock data and demo switches, API URLs (production + local Debug), payment callback.
  Overrides written by `set-app-env`: `appsettings.env.json` (environment) and `appsettings.secrets.json` (that
  environment's keys, from `secrets.json`) – all git-ignored, never committed.
- **API** – `DryCleaning.Api/appsettings.json`: connection string, marketplace rules, JWT, Paystack, OTP, storage.
  Real secrets live in the single `secrets.json` (see [Secrets](#secrets-one-file-for-everything)) or in server environment variables.

## Status (V1)

- ✅ Mobile screens and workflows for Customer, Service Provider and Driver (mock data)
- ✅ API for all three roles plus admin endpoints, Paystack payments, Swagger
- ✅ API: refresh tokens, forgot/reset password, push notifications (Android through Firebase, iPhone through Apple APNs)
- ✅ App connected to the API (sign-in + saved session with token refresh, all screens, photo upload, Paystack checkout, Google address search)
- ✅ App: push notifications on Android (native Firebase) and iOS (native APNs) – registers on sign-in, opens the order when a notification is tapped
- ✅ App: sign-up (all roles), forgot/reset and change password, provider business profile (incl. logo) and categories
- ✅ Email (SendGrid or SMTP): welcome, sign-in/reset codes, payment receipts, optional announcement emails; admin portal **Email** page sends a test
- ⏳ SMS provider for codes
