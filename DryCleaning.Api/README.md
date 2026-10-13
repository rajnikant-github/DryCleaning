# EDry Cleaners API (.NET 10)

ASP.NET Core Web API for the EDry Cleaners marketplace (Customer, Service Provider, shared marketplace Driver, Admin).
SQL Server + EF Core 10, JWT auth, Swagger UI, Paystack payments. Tables follow the V1 schema.

## Contents

- [Run locally](#run-locally)
  - [Quick start – one command](#quick-start--one-command)
    - [macOS – `run-api.sh`](#macos--run-apish)
    - [Windows – `run-api.ps1`](#windows--run-apips1)
  - [Swagger](#swagger)
  - [Manual setup (any OS)](#manual-setup-any-os)
  - [Troubleshooting](#troubleshooting)
- [Configuration (`appsettings.json`)](#configuration-appsettingsjson)
- [Sign-in, sessions and passwords](#sign-in-sessions-and-passwords)
- [Push notifications (Firebase Cloud Messaging)](#push-notifications-firebase-cloud-messaging)
- [Paystack flow](#paystack-flow)
- [Order lifecycle](#order-lifecycle)
  - [Screen helpers](#screen-helpers)
- [Schema notes](#schema-notes)

## Run locally

### Quick start – one command

| OS | Script (in the repo root, next to `DryCleaning.sln`) |
|---|---|
| macOS (Intel or Apple Silicon) | `run-api.sh` – bash |
| Windows 10/11 | `run-api.ps1` – PowerShell 5.1 or PowerShell 7 |

Both scripts work the same way, in three phases:

1. **Check** (changes nothing) – Docker Desktop installed and running, the `edry-sql` SQL Server container
   (exists, running, accepts the SA password, port 1433 free), .NET 10 SDK, `dotnet-ef`, HTTPS dev
   certificate, and the named migrations (`AddPickupSlotAndItemNotes`, `AddPushDevicesAndRefreshTokens`). Each line shows OK, *fix* (the script can
   fix it) or *need* (you must install it – the script stops and tells you what).
2. **Fix** – lists what it will do, asks `Continue? [Y/n]`, then fixes only those items: starts Docker,
   creates/starts the container, installs `dotnet-ef`, trusts the certificate, adds missing migrations.
   It then waits until SQL Server accepts logins and builds the API.
3. **Run** – starts the API (migrations are applied and demo data is seeded on startup) and opens Swagger.

Ctrl+C stops the API; SQL Server keeps running in Docker so the next start is quick.

#### macOS – `run-api.sh`

Open Terminal (zsh or bash both work – the script always runs with bash):

| Command | What it does |
|---|---|
| `./run-api.sh` | Check → fix what's missing (asks first) → run → open Swagger |
| `./run-api.sh --check` | Checks only, changes nothing |
| `./run-api.sh --yes` (or `-y`) | Fix without asking |
| `./run-api.sh --reset-db` | Recreate the SQL Server container – **wipes local data**, reseeds demo data |
| `./run-api.sh --reset-db --yes` | Fresh database without the prompt (options can be combined) |
| `./run-api.sh --stop` | Stop the SQL Server container |
| `./run-api.sh --help` (or `-h`) | Show the options |

Other ways to start it:

| Situation | Command |
|---|---|
| `permission denied` (e.g. after a fresh clone) | `chmod +x run-api.sh` once, or run `bash run-api.sh` |
| From another folder | `~/Repos/DryCleaning/run-api.sh` – the script finds `DryCleaning.Api` relative to itself |
| ⚠️ Don't use | `sh run-api.sh` – `sh` isn't bash; use `./run-api.sh` or `bash run-api.sh` |

#### Windows – `run-api.ps1`

Open **PowerShell** (Windows PowerShell or PowerShell 7) in the repo root. Docker Desktop must use the
WSL 2 backend and **Linux containers** (the script checks this).

| Command | What it does |
|---|---|
| `.\run-api.ps1` | Check → fix what's missing (asks first) → run → open Swagger |
| `.\run-api.ps1 -Check` | Checks only, changes nothing |
| `.\run-api.ps1 -Yes` | Fix without asking |
| `.\run-api.ps1 -ResetDb` | Recreate the SQL Server container – **wipes local data**, reseeds demo data |
| `.\run-api.ps1 -ResetDb -Yes` | Fresh database without the prompt |
| `.\run-api.ps1 -Stop` | Stop the SQL Server container |
| `.\run-api.ps1 -Help` | Show the options |

The macOS-style options also work on Windows: `--check`, `--yes` / `-y`, `--reset-db`, `--stop`, `--help` / `-h`.

Other ways to start it:

| Situation | Command |
|---|---|
| *running scripts is disabled on this system* | `powershell -ExecutionPolicy Bypass -File .\run-api.ps1` (add options at the end, e.g. `-Check`) |
| Allow local scripts permanently (current user) | `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` |
| Downloaded as a zip and still blocked | `Unblock-File .\run-api.ps1` |
| PowerShell 7 | `pwsh .\run-api.ps1` |
| From Command Prompt (cmd.exe) | `powershell -ExecutionPolicy Bypass -File run-api.ps1` |
| From another folder | `C:\path\to\DryCleaning\run-api.ps1` |

### Swagger

Open **https://localhost:7180/swagger** (or http://localhost:5180/swagger).

1. `POST /api/auth/login` → **Try it out** →
   `{ "identifier": "bongani@drivers.edrycleaners.co.za", "password": "Demo@12345" }`
2. Copy `accessToken` from the response.
3. **Authorize** (top right) → paste the token (without `Bearer `) → Authorize.

Demo users (seeded in Development, Cape Town data) – password for all: `Demo@12345`

| Role | Login |
|---|---|
| Customer | sipho@example.co.za |
| Provider | thandi@sparkledrycleaners.co.za |
| Driver | bongani@drivers.edrycleaners.co.za |
| Admin | admin@edrycleaners.co.za |

OTP login in Development: the code is returned in the `POST /api/auth/otp/request` response (no SMS needed).

### Manual setup (any OS)

1. **SQL Server** (Docker; on Apple Silicon enable *Docker Desktop → Settings → General → Use Rosetta*):
   ```bash
   docker run -d --name edry-sql --platform linux/amd64 \
     -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" \
     -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
   ```
   Wait for `docker logs edry-sql` to show *SQL Server is now ready for client connections*.
   Change `ConnectionStrings:DefaultConnection` in `appsettings.json` if your server or password differs.
2. **Tools**:
   ```bash
   dotnet tool install --global dotnet-ef
   dotnet dev-certs https --trust
   ```
3. **Migrations** – after changing entities, add one (they're applied automatically on startup):
   ```bash
   cd DryCleaning.Api
   dotnet ef migrations add <Name> -o Data/Migrations
   ```
4. **Run** – `dotnet run --launch-profile https` (or the **https** profile in Rider).

### Troubleshooting

| Symptom | Fix |
|---|---|
| `A network-related … error occurred while establishing a connection to SQL Server` / `Connection refused` | SQL Server isn't running or isn't ready yet: `docker start edry-sql`, wait ~30 s (or run the script). |
| `Login failed for user 'sa'` | Container was created with another password: `./run-api.sh --reset-db` (Windows: `.\run-api.ps1 -ResetDb`) |
| Container exits right after starting | Apple Silicon: enable Rosetta in Docker Desktop and give Docker ≥ 4 GB memory, then `./run-api.sh --reset-db` |
| Log says *EF Core model has pending changes; migrations were not applied* | Add a migration (`dotnet ef migrations add <Name> -o Data/Migrations`) or just run `./run-api.sh`. |
| API starts but there are no demo orders | Seeding only runs on an empty database. If a previous run failed half-way, run `./run-api.sh --reset-db` (Windows: `.\run-api.ps1 -ResetDb`). |
| Windows: *Docker is in Windows containers mode* | Right-click the Docker tray icon → **Switch to Linux containers…** |
| Windows: port 1433 in use | A local SQL Server service is running – stop it in *Services*, or `netstat -ano \| findstr :1433` to find it. |
| Browser certificate warning | `dotnet dev-certs https --trust`, or use http://localhost:5180/swagger |
| `401 Unauthorized` in Swagger | Access token missing or expired (1 day in Development) or wrong role – log in again, or call `POST /api/auth/refresh` with the `refreshToken`. |
| Paystack endpoints fail | Add your test keys (`sk_test_…`, `pk_test_…`) to `appsettings.Development.json`. |

## Configuration (`appsettings.json`)

| Section | Purpose |
|---|---|
| `Marketplace` | Currency (`ZAR`), VAT rate, delivery fee, driver earning per job, `RequirePaymentBeforeAccept` |
| `Jwt` | Issuer, audience, **SigningKey (≥ 32 chars – keep it in user-secrets / env vars)**, `ExpiryMinutes` (access token, 60; 1440 in Development), `RefreshTokenDays` (30) |
| `Paystack` | `SecretKey`, `PublicKey`, `CallbackUrl` (app deep link) |
| `Otp` | Length/expiry, `MaxAttempts` (wrong guesses before a code is invalidated, 5). `ExposeCodeInResponse` returns the code in dev (no SMS provider wired yet) |
| `Push` | `Enabled`, `FirebaseCredentialsPath` (Firebase service-account JSON). Empty path → pushes are only logged |
| `Storage` | Upload folder under `wwwroot` and max photo size |
| `Database` | `InitializeOnStartup`, `SeedDemoData` |

Secrets (don't commit real keys):
```bash
dotnet user-secrets set "Jwt:SigningKey" "<long random string>"
dotnet user-secrets set "Paystack:SecretKey" "sk_live_..."
dotnet user-secrets set "Push:FirebaseCredentialsPath" "/secure/path/firebase-service-account.json"
```

## Sign-in, sessions and passwords

| Action | Endpoint |
|---|---|
| Log in (password) | `POST /api/auth/login` |
| Log in with a code | `POST /api/auth/otp/request` → `POST /api/auth/otp/verify` |
| Renew the session | `POST /api/auth/refresh` `{ refreshToken }` |
| Log out this device | `POST /api/auth/logout` `{ refreshToken }` |
| Forgot password | `POST /api/auth/password/forgot` `{ identifier }` → `POST /api/auth/password/reset` `{ identifier, code, newPassword }` |
| Change password (signed in) | `POST /api/auth/me/change-password` |

- Login, register, OTP verify, refresh and password reset all return `accessToken` + `expiresAt` **and** a
  `refreshToken` + `refreshTokenExpiresAt`. The app keeps the refresh token in secure storage and calls
  `/api/auth/refresh` when the access token is about to expire (or on a 401).
- **Refresh tokens rotate:** every refresh returns a new one and the old one stops working. If an old (already
  used) refresh token is presented again, all of that user's sessions are revoked – it may have been stolen.
  Only a SHA-256 hash of each refresh token is stored.
- Password reset and change password sign the user out on every other device.
- `password/forgot` answers the same way whether or not the account exists, so it can't be used to discover
  accounts. In Development the code is returned as `devCode`.
- Login and reset codes are invalidated after `Otp:MaxAttempts` (5) wrong guesses.

## Push notifications (Firebase Cloud Messaging)

Every in-app notification the API creates (new order, driver assigned, out for delivery, payment, …) is also
sent as a push to the user's phones – Android and iOS through Firebase.

| Action | Endpoint |
|---|---|
| Register this phone (after sign-in, on app start, when Firebase issues a new token) | `POST /api/me/devices` `{ token, platform: "Android" \| "iOS" }` |
| Stop pushes to this phone (on logout) | `DELETE /api/me/devices?token=…` |

Push payload: `notification.title` / `notification.body` plus `data.notificationId`, `data.type` and
`data.orderId` (when the notification is about an order), so the app can open the right screen.

**Setup**

1. Create a Firebase project and add the Android and iOS apps (for iOS, upload the APNs key in
   *Project settings → Cloud Messaging*).
2. *Project settings → Service accounts → Generate new private key* – download the JSON. **Don't commit it.**
3. Set `Push:FirebaseCredentialsPath` to that file (user-secrets or environment variable `Push__FirebaseCredentialsPath`).

Without a credentials file the API runs normally and logs each push instead of sending it. Sending happens on a
background queue, so API calls never wait for Firebase; tokens Firebase reports as expired are removed automatically.

## Paystack flow

1. Customer creates an order → `POST /api/customer/orders`
2. `POST /api/customer/orders/{id}/payments/paystack` → open `authorizationUrl` (WebView/browser)
3. Paystack redirects to `CallbackUrl`; app calls `GET /api/customer/orders/{id}/payments/verify?reference=…`
4. Webhook `POST /api/payments/paystack/webhook` (set it in the Paystack dashboard) also confirms `charge.success`.
   The signature (`x-paystack-signature`, HMAC-SHA512) is validated and the transaction is always re-verified with Paystack.
5. Paid orders appear in the provider's **New** list.

## Order lifecycle

```
Created → Confirmed → PickupAssigned → PickedUp → ReceivedByProvider → Processing
→ QualityCheck → ReadyForDelivery → DeliveryAssigned → OutForDelivery → Delivered → Completed
(+ Cancelled, Disputed)
```

| Step | Endpoint |
|---|---|
| Provider accepts (publishes **Pickup** job) | `POST /api/provider/orders/{id}/accept` |
| Driver accepts job | `POST /api/driver/jobs/{jobId}/accept` |
| Driver checks items + verifies customer pickup code (QR) | `POST /api/driver/jobs/{jobId}/pickup` (`Code`, optional per-item `Items`, `Notes`) |
| Driver hands over to provider | `POST /api/driver/jobs/{jobId}/handover` |
| Provider confirms receipt | `POST /api/provider/orders/{id}/receive` |
| Inspection → Processing | `POST /api/provider/orders/{id}/inspection` |
| Quality check | `POST /api/provider/orders/{id}/quality-check` |
| Ready for delivery | `POST /api/provider/orders/{id}/ready` |
| Request driver (publishes **Delivery** job) | `POST /api/provider/orders/{id}/request-driver` |
| Driver collects from provider | `POST /api/driver/jobs/{jobId}/start-delivery` (optional `PackageCount`, `Notes`) |
| Driver delivers with customer OTP → Completed | `POST /api/driver/jobs/{jobId}/deliver` |
| Driver earnings (last 7 days / 4 weeks) | `GET /api/driver/earnings?period=Week\|Month` |
| Driver job history | `GET /api/driver/jobs/history` |
| Driver pickup / delivery photos | `POST /api/orders/{orderId}/photos` (multipart, `Type=Pickup\|Completion`) |

Every status change is written to `OrderStatusHistory`.

Driver job endpoints (`GET /api/driver/jobs/{jobId}`, accept, release, pickup, handover, start-delivery, deliver)
return a `DriverJobDetailDto`: the job, its `NextStep`, the order's items and – only while the job is the
driver's own and active – the customer's and provider's phone numbers.

### Screen helpers

| Screen | Endpoint |
|---|---|
| Customer home – popular services | `GET /api/catalog/popular?take=6` |
| Providers list (incl. `FastestDays`) | `GET /api/providers?categoryId=…` |
| One provider (products screen header) | `GET /api/providers/{providerId}?categoryId=…` |
| One product with its prices (customer) | `GET /api/catalog/products/{productId}` |
| Edit one service item (provider) | `GET /api/provider/service-items/{serviceItemId}` |
| Upload business logo (provider, multipart field `File`) | `POST /api/provider/profile/logo` |
| Customer order tab counts | `GET /api/customer/orders/counts` |
| Provider order tab counts | `GET /api/provider/orders/counts` |
| Driver dashboard (name, active job, today's jobs, unread) | `GET /api/driver/dashboard` |
| Driver job tab counts | `GET /api/driver/jobs/counts` |
| Driver job for a notification | `GET /api/driver/jobs/by-order/{orderId}` |

## Schema notes

- All 18 tables from the V1 diagram; enums stored as strings; money `decimal(10,2)`; lat/lng `decimal(10,7)`.
- **Drivers have no `ServiceProviderId`** – `DriverJobs` links a marketplace driver to an order.
- `DriverJobs.DriverId` is nullable: a job is `Pending` on the marketplace until a driver accepts it.
  `DriverJobs` also has a `RowVersion` concurrency column so two drivers can't take the same job.
- `AddressId` on Customers/ServiceProviders/Drivers is nullable so users can register before adding an address.
- `OrderVerifications`: `QR` = customer pickup code, `OTP` = delivery code, `Item` = driver item check at pickup / provider receipt check.
- Order numbers come from a SQL sequence: `EDC-10001`, `EDC-10002`, …
- Login OTPs are held in memory (no table in the schema) – plug an SMS provider into `MemoryOtpService`.
- **Schema additions** (not in the V1 diagram):
  - `Orders.PickupDate`, `Orders.PickupSlot`, `OrderItems.Notes` – customer pickup window and item instructions.
  - `DeviceTokens` (UserId, Platform, Token – unique, LastSeenAt) – phones registered for push.
  - `RefreshTokens` (UserId, TokenHash – unique SHA-256, ExpiresAt, RevokedAt, ReplacedByTokenHash) – sessions.

  Migrations: `AddPickupSlotAndItemNotes`, `AddPushDevicesAndRefreshTokens` – the run scripts create them
  automatically when missing, or run `dotnet ef migrations add <Name> -o Data/Migrations`.
