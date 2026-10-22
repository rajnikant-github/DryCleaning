# File storage: MinIO only

Every file – order photos, provider logos and verification documents – is stored in **MinIO** (S3-compatible), the same
store as the E Dry Cleaners reference project. There is no other store and no switch between debug, release or machines:
a laptop and the live server behave the same.

## Settings (`Storage:Minio`)

| Setting | Where it lives | Value |
|---|---|---|
| `Url` | `appsettings.json` (shared) | `77.237.233.114:9000` (no scheme) |
| `BucketName` | `appsettings.json` | `edrycleaners-api` – created automatically |
| `UseSsl` | `appsettings.json` | `false` today (plain http, see below) |
| `SignedLinkMinutes` | `appsettings.json` | `15` – lifetime of an order-photo link |
| `PublicUrl` | optional | CDN/https address for logos |
| `AccessKey` / `SecretKey` | **`secrets.json`** locally, **environment variables** on the server | never in git |

The API refuses to start when the keys are missing (it never falls back to another store).

Inside the bucket: `public/` (logos – anyone can read), `private/` (order photos – 15-minute signed links), `protected/`
(verification documents – no link ever, the API streams them after a permission check). The API creates the bucket and sets
this policy itself on first use.

**Do not use the reference project's bucket (`edrycleaners`).** Its policy lets anyone read every file in it.

## Set up a machine (local or live) – one time

Add the keys (ask for a MinIO user limited to the `edrycleaners-api` bucket; do not use the admin account):

- Local: in the git-ignored `secrets.json` → `local` → `Api` → `Storage` → `Minio` → `AccessKey` / `SecretKey`
  (see `secrets.example.json`).
- Server: environment variables `Storage__Minio__AccessKey` and `Storage__Minio__SecretKey`.

Start the API, upload one photo, open it. Done. No database change, no app update.

## Plain http to the file server

Phones refuse plain `http://` addresses by default, so the app has an explicit allowance for the MinIO address and nothing else:
- iOS: `DryCleaning/Platforms/iOS/Info.plist` (`NSExceptionDomains`)
- Android: `DryCleaning/Platforms/Android/Resources/xml/network_security_config.xml`

If the MinIO address changes, update both. Logos and order photos then travel unencrypted from MinIO to the phone
(verification documents do not: the API streams them). To remove the allowance later, give MinIO an `https://` address, set
`Url` and `UseSsl=true` in `appsettings.json` and delete the two entries above.
