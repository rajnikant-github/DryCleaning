# EDry Cleaners

On-demand dry cleaning and laundry marketplace for South Africa (prices in ZAR).
Customers book pickups, service providers clean the items, and shared marketplace drivers collect and deliver.

| Folder | What it is |
|---|---|
| [`DryCleaning/`](DryCleaning/) | .NET MAUI app (Android + iOS, MVVM) – Customer, Service Provider and Driver roles in one app |
| [`DryCleaning.Api/`](DryCleaning.Api/) | ASP.NET Core Web API (.NET 10) – SQL Server, EF Core, JWT, Swagger, Paystack. [API docs →](DryCleaning.Api/README.md) |
| `run-api.sh` / `run-api.ps1` | One-command local API runner – macOS (bash) / Windows (PowerShell) |
| `DryCleaning.sln` | Solution with both projects |

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
The app currently runs on **mock data** (`Features:UseMockData` in `DryCleaning/appsettings.json`) – no API needed.

On the role screen pick **Customer**, **Service Provider** or **Driver**, then sign in with any email/phone and a
password of 4+ characters, or use OTP `123456`.

| Role | Demo account | Try |
|---|---|---|
| Customer | Sipho Ndlovu | Browse → provider → items → cart → pickup address & slot → pay → track |
| Service Provider | Sparkle Dry Cleaners | Accept → receive → inspect → process → quality check → request driver |
| Driver | Bongani Zulu | Go online → accept a job → navigate → verify items → pickup code → hand over / deliver with OTP |

While `Features:ShowDemoActions` is on, the driver's code screens show the customer's code so a whole job can be
tested on one phone.

## Configuration

- **App** – `DryCleaning/appsettings.json`: currency (ZAR / R / en-ZA), VAT, delivery fee, driver earning per job,
  mock data and demo switches, API base URL.
- **API** – `DryCleaning.Api/appsettings.json`: connection string, marketplace rules, JWT, Paystack, OTP, storage.
  Keep real secrets in `dotnet user-secrets` or environment variables.

## Status (V1)

- ✅ Mobile screens and workflows for Customer, Service Provider and Driver (mock data)
- ✅ API for all three roles plus admin endpoints, Paystack payments, Swagger
- ✅ API: refresh tokens, forgot/reset password, push notifications (Firebase – needs your service-account file)
- ⏳ Connect the app to the API (replace mock services, token refresh, push registration, Paystack checkout)
- ⏳ Admin web portal, Google Places key, SMS/email provider for codes
