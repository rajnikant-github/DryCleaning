# EDry Cleaners API (.NET 10)

ASP.NET Core Web API for the EDry Cleaners marketplace (Customer, Service Provider, shared marketplace Driver, Admin).
SQL Server + EF Core 10, JWT auth, Swagger UI, Paystack payments. Tables follow the V1 schema.

## Run locally

1. **SQL Server** – e.g. Docker on macOS (Apple Silicon):
   ```bash
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
   ```
   Update `ConnectionStrings:DefaultConnection` in `appsettings.json` if your server/password differs.
2. **Create the first migration** (recommended):
   ```bash
   dotnet tool install --global dotnet-ef
   cd DryCleaning.Api
   dotnet ef migrations add InitialCreate -o Data/Migrations
   ```
   On startup the API applies migrations automatically. If no migrations exist it falls back to `EnsureCreated`.

   **Schema additions for the Customer flow** (`Orders.PickupDate`, `Orders.PickupSlot`, `OrderItems.Notes`). If you already created `InitialCreate`, add a second migration:

   ```bash
   dotnet ef migrations add AddPickupSlotAndItemNotes -o Data/Migrations
   ```
3. **Run** – `dotnet run` (or Rider), then open **https://localhost:7180/swagger**.
   In Development, demo data is seeded (Cape Town). Password for all demo users: `Demo@12345`

   | Role | Login |
   |---|---|
   | Admin | admin@edrycleaners.co.za |
   | Provider | thandi@sparkledrycleaners.co.za |
   | Driver | bongani@drivers.edrycleaners.co.za |
   | Customer | sipho@example.co.za |

   In Swagger: `POST /api/auth/login` → copy `accessToken` → **Authorize** → paste.

## Configuration (`appsettings.json`)

| Section | Purpose |
|---|---|
| `Marketplace` | Currency (`ZAR`), VAT rate, delivery fee, driver earning per job, `RequirePaymentBeforeAccept` |
| `Jwt` | Issuer, audience, **SigningKey (≥ 32 chars – keep it in user-secrets / env vars)** |
| `Paystack` | `SecretKey`, `PublicKey`, `CallbackUrl` (app deep link) |
| `Otp` | Length/expiry. `ExposeCodeInResponse` returns the code in dev (no SMS provider wired yet) |
| `Storage` | Upload folder under `wwwroot` and max photo size |
| `Database` | `InitializeOnStartup`, `SeedDemoData` |

Secrets (don't commit real keys):
```bash
dotnet user-secrets set "Jwt:SigningKey" "<long random string>"
dotnet user-secrets set "Paystack:SecretKey" "sk_live_..."
```

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
| Driver verifies customer pickup code (QR) | `POST /api/driver/jobs/{jobId}/pickup` |
| Driver hands over to provider | `POST /api/driver/jobs/{jobId}/handover` |
| Provider confirms receipt | `POST /api/provider/orders/{id}/receive` |
| Inspection → Processing | `POST /api/provider/orders/{id}/inspection` |
| Quality check | `POST /api/provider/orders/{id}/quality-check` |
| Ready for delivery | `POST /api/provider/orders/{id}/ready` |
| Request driver (publishes **Delivery** job) | `POST /api/provider/orders/{id}/request-driver` |
| Driver collects from provider | `POST /api/driver/jobs/{jobId}/start-delivery` |
| Driver delivers with customer OTP → Completed | `POST /api/driver/jobs/{jobId}/deliver` |

Every status change is written to `OrderStatusHistory`.

## Schema notes

- All 18 tables from the V1 diagram; enums stored as strings; money `decimal(10,2)`; lat/lng `decimal(10,7)`.
- **Drivers have no `ServiceProviderId`** – `DriverJobs` links a marketplace driver to an order.
- `DriverJobs.DriverId` is nullable: a job is `Pending` on the marketplace until a driver accepts it.
  `DriverJobs` also has a `RowVersion` concurrency column so two drivers can't take the same job.
- `AddressId` on Customers/ServiceProviders/Drivers is nullable so users can register before adding an address.
- `OrderVerifications`: `QR` = customer pickup code, `OTP` = delivery code, `Item` = provider receipt check.
- Order numbers come from a SQL sequence: `EDC-10001`, `EDC-10002`, …
- Login OTPs are held in memory (no table in the schema) – plug an SMS provider into `MemoryOtpService`.
