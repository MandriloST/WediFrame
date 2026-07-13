# WediFrame

Web/PWA platforma za prikupljanje i dijeljenje fotografija i videa s vjenčanja.
Powered by EverFrame. Kontekst projekta: [`/docs`](docs/) (PROJECT.md, ARCHITECTURE.md, BACKLOG.md).

## Struktura

```
WediFrame.slnx                  — solution (.NET 10, novi XML format)
src/
  WediFrame.Api/                — ASP.NET Core host: kompozicija modula, /health, /api/v1
  WediFrame.Shared/             — kernel: IModule ugovor, AuditLogEntry, zajednički tipovi
  WediFrame.Infrastructure/     — EF Core (AppDbContext, konfiguracije, design-time factory)
  Modules/
    WediFrame.Modules.Identity/     — host računi, JWT (M1)
    WediFrame.Modules.Events/       — eventi, guest token, QR (M1)
    WediFrame.Modules.Media/        — presigned upload, galerija, limiti (M1–M3)
    WediFrame.Modules.Billing/      — paketi, Stripe, fiskalizacija/R1 (M3)
    WediFrame.Modules.Partners/     — bonus kodovi, atribucija (M3)
    WediFrame.Modules.Retention/    — istek uploada/retencije, brisanje (M4)
    WediFrame.Modules.Admin/        — interni admin (M5)
docs/                           — PROJECT.md, ARCHITECTURE.md, BACKLOG.md
```

Jedan `AppDbContext`, jedna PostgreSQL baza, **shema po modulu** (`identity`, `events`, `media`, ...).
Sve datoteke (fotke/video) idu direktno browser → Cloudflare R2 presigned URL-ovima; API nikad ne streama datoteke.

## Preduvjeti

- .NET 10 SDK
- PostgreSQL 16+ lokalno (najlakše Docker):

```bash
docker run -d --name wediframe-pg -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=wediframe -p 5432:5432 postgres:16
```

## Prvi start

```bash
dotnet restore
dotnet build

# EF alat (jednom po stroju)
dotnet tool install --global dotnet-ef

# Inicijalna migracija (audit_log u shemi "shared")
dotnet ef migrations add InitialCreate \
  --project src/WediFrame.Infrastructure \
  --startup-project src/WediFrame.Api

dotnet ef database update \
  --project src/WediFrame.Infrastructure \
  --startup-project src/WediFrame.Api

# API
dotnet run --project src/WediFrame.Api
# → http://localhost:5080/health
# → http://localhost:5080/openapi/v1.json (Development)
```

Connection string: `appsettings.Development.json` lokalno; na Railwayu env varijabla
`ConnectionStrings__Database`. `dotnet ef` tooling koristi i env `WEDIFRAME_DB` (vidi `AppDbContextFactory`).

## Cloudflare R2 (storage za medije)

1. Cloudflare dashboard → R2 → Create bucket → ime `wediframe-dev` (za dev), **Location: Specify jurisdiction → European Union** (GDPR obveza, ne mijenjati).
2. R2 → Manage API Tokens → Create API Token → permission **Object Read & Write**, scope samo taj bucket. Zapiši Access Key ID i Secret Access Key.
3. CORS na bucketu (R2 → bucket → Settings → CORS policy) — bez ovoga browser ne može PUT-ati:

```json
[
  {
    "AllowedOrigins": ["http://localhost:3000"],
    "AllowedMethods": ["GET", "PUT"],
    "AllowedHeaders": ["Content-Type"],
    "MaxAgeSeconds": 3600
  }
]
```

4. Tajne lokalno kroz user-secrets (nikad u appsettings u repou):

```bash
cd src/WediFrame.Api
dotnet user-secrets init
dotnet user-secrets set "R2:AccountId" "<cloudflare-account-id>"
dotnet user-secrets set "R2:AccessKeyId" "<access-key-id>"
dotnet user-secrets set "R2:SecretAccessKey" "<secret>"
```

Bucket ime je u `appsettings.Development.json` (`R2:Bucket`). Na Railwayu: env varijable
`R2__AccountId`, `R2__AccessKeyId`, `R2__SecretAccessKey`, `R2__Bucket`.
API se diže i bez R2 konfiguracije (health, auth, eventi rade) — prvi storage poziv baci jasnu grešku.

## Konvencije

- Kod, komentari, imena u bazi i API-ju: engleski. Dokumentacija i komunikacija: hrvatski.
- Backlog i odluke: `docs/BACKLOG.md` (Decision Log). Ne mijenjati limite paketa u kodu — oni su podaci u bazi (M3).
- Nikad secrets u repo (`.gitignore` već pokriva `.env*`, `appsettings.Local.json`).
