# EDry Cleaners

On-demand dry cleaning and laundry marketplace for South Africa (prices in ZAR).
Customers book pickups, service providers clean the items, and shared marketplace drivers collect and deliver.

| Folder | What it is |
|---|---|
| [`DryCleaning/`](DryCleaning/) | .NET MAUI app (Android + iOS, MVVM) – Customer, Service Provider and Driver roles in one app |
| [`DryCleaning.Api/`](DryCleaning.Api/) | ASP.NET Core Web API (.NET 10) – SQL Server, EF Core, JWT, Swagger, Paystack. [API docs →](DryCleaning.Api/README.md) |
| [`docs/API-GUIDE.md`](docs/API-GUIDE.md) | API & workflow guide – every endpoint, order/payment/driver/dispute flows, request rules, enums and all database tables |
| `run-api.sh` / `run-api.ps1` | One-command local API runner – macOS (bash) / Windows (PowerShell) |
| `set-app-env.sh` / `set-app-env.ps1` | Switch the app between environments (mock, local, staging, production) |
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

**Address search** uses Google: Places API (New) for suggestions and Geocoding API for "Use my current location".
The key is **never committed** and is used **only in the `local` environment**. It lives on your machine in
`DryCleaning/environments/local.secrets.json` (git-ignored):

```json
{ "GoogleMaps": { "ApiKey": "<your key>" } }
```

Create that file with your key, then run `./set-app-env.sh local` to copy it into the build.

`set-app-env` copies only the chosen environment's `<name>.secrets.json` into the build (as the git-ignored
`DryCleaning/appsettings.secrets.json`, merged last). `mock`, `staging` and `production` have no secrets file, so their
builds contain no key. To give another environment a key later, create `environments/<name>.secrets.json` the same way.
In Google Cloud Console enable both APIs, restrict the key to them, and (for released apps) to your Android package /
iOS bundle id. Without a key the app uses sample Cape Town addresses. A key built into an app can be extracted
from it – the API restrictions are what protect it.

**Payments:** the app opens Paystack's checkout page and Paystack returns to the app via
`edrycleaners://payment-complete` (must match `Paystack:CallbackUrl` in the API). The API then verifies the payment.
Use Paystack test keys and a [test card](https://paystack.com/docs/payments/test-payments/) while developing.

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
| "Address search isn't working" | Check `DryCleaning/environments/<name>.secrets.json` has your key and re-run `./set-app-env.sh <name>`, enable Places API (New) and Geocoding API for it in Google Cloud Console, and check the key's restrictions. |

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
- Keys: `environments/<name>.secrets.json` (git-ignored) is copied as `appsettings.secrets.json` for that environment
  only; switching to an environment without one removes the key from the build. `--reset` removes both files.
- Warnings: production with demo data, and a server URL that isn't `https://`, doesn't end with `/`, or ends with `api/`.

**Change or add an environment** – edit `DryCleaning/environments/staging.json` to your real test-server address
(`Api:BaseUrl`), or add e.g. `uat.json` with the same shape and run `./set-app-env.sh uat`. Any setting from
`appsettings.json` can go in a profile – but keys go in `environments/<name>.secrets.json` (git-ignored), never in
a profile, because profiles are committed.

**API side** – the API already switches with `ASPNETCORE_ENVIRONMENT` (`Development` locally via `run-api.sh`,
`Staging`/`Production` on a server) plus `appsettings.{Environment}.json` and environment variables – see the
[server checklist](#2-server-checklist).

## Configuration

- **App** – `DryCleaning/appsettings.json`: currency (ZAR / R / en-ZA), VAT, delivery fee, driver earning per job,
  mock data and demo switches, API URLs (production + local Debug), payment callback.
  Overrides written by `set-app-env`: `appsettings.env.json` (environment) and `appsettings.secrets.json` (that
  environment's keys, from `environments/<name>.secrets.json`) – all git-ignored, never committed.
- **API** – `DryCleaning.Api/appsettings.json`: connection string, marketplace rules, JWT, Paystack, OTP, storage.
  Keep real secrets in `dotnet user-secrets` or environment variables.

## Status (V1)

- ✅ Mobile screens and workflows for Customer, Service Provider and Driver (mock data)
- ✅ API for all three roles plus admin endpoints, Paystack payments, Swagger
- ✅ API: refresh tokens, forgot/reset password, push notifications (Firebase – needs your service-account file)
- ✅ App connected to the API (sign-in + saved session with token refresh, all screens, photo upload, Paystack checkout, Google address search)
- ⏳ Push notification registration in the app (needs your Firebase project files)
- ✅ App: sign-up (all roles), forgot/reset and change password, provider business profile (incl. logo) and categories
- ⏳ Admin web portal, SMS/email provider for codes
