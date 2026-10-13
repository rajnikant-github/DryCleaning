# EDry Cleaners

On-demand dry cleaning and laundry marketplace for South Africa (prices in ZAR).
Customers book pickups, service providers clean the items, and shared marketplace drivers collect and deliver.

| Folder | What it is |
|---|---|
| [`DryCleaning/`](DryCleaning/) | .NET MAUI app (Android + iOS, MVVM) – Customer, Service Provider and Driver roles in one app |
| [`DryCleaning.Api/`](DryCleaning.Api/) | ASP.NET Core Web API (.NET 10) – SQL Server, EF Core, JWT, Swagger, Paystack. [API docs →](DryCleaning.Api/README.md) |
| `run-api.sh` / `run-api.ps1` | One-command local API runner – macOS (bash) / Windows (PowerShell) |
| `DryCleaning.sln` | Solution with both projects |

## Contents

- [Quick start](#quick-start)
  - [API + Swagger](#api--swagger)
  - [Mobile app](#mobile-app)
- [Testing the app against a deployed API](#testing-the-app-against-a-deployed-api)
  - [1. Point the app at the server](#1-point-the-app-at-the-server)
  - [2. Server checklist](#2-server-checklist)
  - [3. Smoke test after each deploy](#3-smoke-test-after-each-deploy)
  - [Troubleshooting](#troubleshooting)
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

| Role | Demo account | Try |
|---|---|---|
| Customer | `sipho@example.co.za` | Browse → provider → items → cart → pickup address & slot → pay (Paystack test card) → track |
| Service Provider | `thandi@sparkledrycleaners.co.za` | Accept → receive → inspect → process → quality check → request driver |
| Driver | `bongani@drivers.edrycleaners.co.za` | Go online → accept a job → navigate → verify items → pickup code → hand over / deliver with OTP |

**Demo data instead of the API:** set `"UseMockData": true` – no API needed, any email/phone + 4-character password,
OTP `123456`, and the "simulate driver" / show-customer-code shortcuts (`Features:ShowDemoActions`).

**Address search** uses Google (`GoogleMaps:ApiKey`): Places API (New) for suggestions and Geocoding API for
"Use my current location". Enable both APIs for the key in Google Cloud Console and restrict the key to them.
Leave `ApiKey` empty to use the sample Cape Town addresses.

**Payments:** the app opens Paystack's checkout page and Paystack returns to the app via
`edrycleaners://payment-complete` (must match `Paystack:CallbackUrl` in the API). The API then verifies the payment.
Use Paystack test keys and a [test card](https://paystack.com/docs/payments/test-payments/) while developing.

## Testing the app against a deployed API

Use this when the API runs on a server (staging or production) instead of your machine.

### 1. Point the app at the server

In `DryCleaning/appsettings.json`:

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
| `Otp__ExposeCodeInResponse` | `false`. No SMS/email provider is connected yet, so on a server sign in with **password**. For OTP testing on a test server only, set `true` – the OTP screen then shows the code. |
| `Push__FirebaseCredentialsPath` | Path to the Firebase service-account JSON (optional – without it pushes are only logged). |
| `Swagger__Enabled` | `true` on staging; your choice in production. |
| Uploads | Order photos are saved under `wwwroot/uploads` – use persistent disk (not a container's temporary file system). |

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
| Photos missing after a restart/redeploy | `wwwroot/uploads` isn't on persistent storage. |
| "Address search isn't working" | Enable Places API (New) and Geocoding API for `GoogleMaps:ApiKey` in Google Cloud Console, and check the key's restrictions. |

## Configuration

- **App** – `DryCleaning/appsettings.json`: currency (ZAR / R / en-ZA), VAT, delivery fee, driver earning per job,
  mock data and demo switches, API URLs (production + local Debug), Google Maps key, payment callback.
- **API** – `DryCleaning.Api/appsettings.json`: connection string, marketplace rules, JWT, Paystack, OTP, storage.
  Keep real secrets in `dotnet user-secrets` or environment variables.

## Status (V1)

- ✅ Mobile screens and workflows for Customer, Service Provider and Driver (mock data)
- ✅ API for all three roles plus admin endpoints, Paystack payments, Swagger
- ✅ API: refresh tokens, forgot/reset password, push notifications (Firebase – needs your service-account file)
- ✅ App connected to the API (sign-in + saved session with token refresh, all screens, photo upload, Paystack checkout, Google address search)
- ⏳ Push notification registration in the app (needs your Firebase project files)
- ⏳ Registration and forgot-password screens, Admin web portal, SMS/email provider for codes
