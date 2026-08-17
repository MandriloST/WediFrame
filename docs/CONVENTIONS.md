# WediFrame — Konvencije rada

> **Svrha:** stabilni obrasci koji se rijetko mijenjaju, da se na početku sesije **ne grepaju iznova** (module-graf, kako dodati entitet/migraciju, audit, frontend obrasci, delivery ritual, Definition-of-Done). Ako se obrazac promijeni — ažurirati OVDJE. Volatilne stvari (verzije paketa, konkretne brojke) NE idu ovamo; one žive u kodu/BACKLOG-u.

---

## 1. Ritual sesije

1. **Klonirati `develop`** i raditi nad stvarnim kodom (ne iz sjećanja):
   `git clone --depth 1 --branch develop https://github.com/MandriloST/WediFrame.git`
2. Pročitati **`BACKLOG.md`** (stanje + sljedeći korak + Decision Log + otvorena pitanja) i **ovaj file**. Povijest po potrebi: `HISTORY.md`.
3. Pročitati **stvarne fileove** koje mijenjaš prije prijedloga (PROJECT.md pravilo).
4. Napraviti izmjenu → provjere (dolje) → **isporuka** (dolje) → ažurirati BACKLOG (+ ovaj file ako se obrazac promijenio).

## 2. Isporuka koda (OBAVEZNO)

- **Puni fileovi, nikad isječci** koje vlasnik ručno lijepi. Nema ručnih patcheva na fileove kojih nemam u cijelosti — zato se repo klonira.
- Isporuka = **(a) ZIP** s točnom strukturom foldera (raspakira se u root) **i (b) `git` patch** (`git apply`). Oba se generiraju iz kloniranog repoa (`git add -A` → `git diff --cached`).
- Oba se prikažu kroz `present_files`.
- **Oba jezika uvijek** (`web/src/messages/hr.json` + `en.json`) — nikad samo jedan.

## 3. Module-graf i pravilo referenci

Modularni monolit. Moduli: `Identity`, `Events`, `Media`, `Billing`, `Partners`, `Retention`, `Admin`, + `Shared`, `Infrastructure`, `Api`.

**Pravilo:** modul referencira **samo `Shared`**; cross-module komunikacija ide isključivo kroz **port (interface) definiran u `Shared`**, a implementiran u modulu-vlasniku. Nema ciklusa. Nijedan modul ne ovisi o `Infrastructure`.

Postojeće direktne reference (iznimke, jednosmjerne, bez ciklusa): `Events→Billing`, `Media→Events`, `Retention→Events`.

**`Admin` je leaf** — referencira samo `Shared`; sve čita/piše preko Shared portova (`IAdmin*`, `IUserDirectory`, `IPackageCatalog`, `IPartnerAdmin`…). Nikad ne referencira druge module. Admin-facing HTTP za druge domene (npr. partneri) svejedno živi u `Admin` modulu i zove Shared port.

**Obrazac Shared porta:** interface + DTO recordi u `src/WediFrame.Shared/<Area>/`; impl u `src/Modules/<Modul>/Services/`; registracija u `<Modul>Module.RegisterServices` (`AddScoped<IPort, Impl>()`). Konzument dobiva port kroz DI.

## 4. Recept: dodavanje entiteta + migracija

1. Entitet u `src/Modules/<Modul>/Domain/`. Cross-module reference = **plain `Guid`** (bez FK/navigacije).
2. EF config `IEntityTypeConfiguration<T>` u `src/Modules/<Modul>/Persistence/` — `ToTable("<tablica>", "<shema>")`, ključ, indeksi, `HasConversion<string>()` za enume.
3. Ako modul **prvi put** dobiva entitete:
   - modul csproj: `<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />`
   - `src/WediFrame.Infrastructure/WediFrame.Infrastructure.csproj`: dodati `ProjectReference` na modul
   - `src/WediFrame.Infrastructure/Persistence/AppDbContext.cs`: dodati `modelBuilder.ApplyConfigurationsFromAssembly(typeof(<Modul>Module).Assembly);` (+ `using`)
4. **Migracija** (radi je vlasnik lokalno):
   `dotnet ef migrations add <Naziv> --project src/WediFrame.Infrastructure --startup-project src/WediFrame.Api`
   → `dotnet ef database update`
5. Entiteti se čitaju kroz `DbContext` (`db.Set<T>()`); modul bez vlastitih entiteta koji samo čita Shared entitet (npr. `AuditLogEntry`) treba samo bazni `Microsoft.EntityFrameworkCore` u csproj-u (ne Npgsql).

Moduli su registrirani u petlji u `src/WediFrame.Api/Program.cs` (`IModule[] modules = { … }`) — novi modul koji već postoji u petlji ne treba dodatno ožičenje ondje.

## 5. Audit log

Shared entitet `WediFrame.Shared.Audit.AuditLogEntry` (tablica `shared.audit_log`) — dostupan svim modulima kroz `db.Set<AuditLogEntry>()`.

Zapis:
```csharp
db.Set<AuditLogEntry>().Add(new AuditLogEntry {
    Id = Guid.NewGuid(),
    OccurredAt = timeProvider.GetUtcNow(),
    ActorUserId = userId,          // null = sustav/background job
    Action = "event.retention_extended",
    EntityType = nameof(Event),
    EntityId = e.Id.ToString(),
    Details = JsonSerializer.Serialize(new { from, to }), // opcionalno, jsonb
});
await db.SaveChangesAsync(ct);
```
Konvencija naziva akcije: `entitet.radnja` (npr. `event.expired`, `media.deleted`). Admin-inicirane akcije nose `_by_admin` sufiks (`media.deleted_by_admin`), host `_by_host`. Actor = admin/host userId; background job = `null`.

## 6. Admin modul (obrazac)

- Sve rute pod grupom `/admin` s policy `Admin` (`group.RequireAuthorization(AdminPolicy.Name)`), mapirane u `AdminModule.MapEndpoints`. Nova domena = novi `Endpoints/Admin<X>Endpoints.cs` + `group.MapAdmin<X>Endpoints()`.
- JWT nosi ulogu u claimu `role`; policy `Admin` = `RequireRole("Admin")`. Admin uloga se dodjeljuje **isključivo** kroz `Admin:BootstrapEmails` (startup promocija postojećih korisnika; nema self-promote endpointa). Nakon promocije: odjava/prijava (token nosi ulogu iz trenutka izdavanja).
- Actor u write-akcijama: `principal.GetUserId()` (`WediFrame.Shared.Auth`).
- Kontrakti (request/response recordi) u `Admin/Contracts/AdminContracts.cs`; generički `PagedResponse<T>` već postoji.
- Read-only liste: filter + paginacija (`page`/`pageSize`, `PagedResponse`), `AsNoTracking`, newest-first.

## 7. Frontend obrasci

- **API klijenti:** host = `web/src/lib/hostApi.ts`, admin = `adminApi.ts`, guest = `guestApi.ts`. Autentificirani pozivi kroz **`authFetch`** (exportan iz `hostApi`, radi refresh na 401). Greške: `throw new ApiError(status, code)`; kod iz ProblemDetails vadi `parseProblemCode(res)` (u `adminApi`). `API_BASE` već uključuje `/api/v1`.
- **i18n:** poruke u `web/src/messages/{hr,en}.json`; **uvijek oba jezika, parni ključevi**. Izmjene raditi **JSON round-trip u Pythonu** (`json.load` s `OrderedDict` → set ključ → `json.dump(ensure_ascii=False, indent=2)`) da se zajamči valjanost i očuvaju ostali namespace-i. ICU plural/varijable podržani (`{count, plural, …}`, `{page}`).
- **Dinamičke rute:** `const params = useParams<{ id: string }>()` iz `next/navigation` (ne `use(params)`). `Link`/`useRouter`/`usePathname` iz `@/i18n/navigation`.
- **Galerija:** reuse `web/src/components/media/MediaGallery.tsx` (`MediaThumb`, `MediaLightbox`, `tileFromServer`) — ne duplicirati grid/lightbox. `MediaLightbox.onDownload` je opcionalan.
- **Brand:** guest/host stranice = bordo `#7C2D3E` na `#FFFDF9`, display font Fraunces samo za naslove. Admin sučelje = neutralni `stone` + `rose` akcent. „Powered by EverFrame".
- **Artifacts/dev:** bez `localStorage`/`sessionStorage` u artifactima (n/a za repo kod, ali pravilo Anthropic okruženja).
- Konvencija formata: `formatBytes` helper (KB/MB/GB) ponavljan po stranicama; datumi kroz `toLocaleDateString(locale)`.

## 8. Config / tajne

- Tajne lokalno: `appsettings.Development.json` ili `dotnet user-secrets`. Produkcija (Railway): env varijable s `__` kao separatorom sekcija (npr. `Admin__BootstrapEmails__0`, `Jwt__SigningKey`).
- Nove config sekcije: dodati **prazan/siguran default** u `appsettings.json` (bez tajni) + `Options` klasu bindanu u modulu.

## 9. Definition of Done (provjeriti prije isporuke)

- [ ] Radim nad **kloniranim `develop`-om**, pune fileove.
- [ ] **Oba jezika** (`hr.json` + `en.json`) ažurirana; JSON valjan (`python -c json.load`); ostali namespace-i očuvani.
- [ ] C#/TSX **balans zagrada** provjeren; usinzi/DI/registracije na mjestu.
- [ ] **Admin ostaje leaf** (ref samo Shared); nema novih ciklusa; nijedan modul ne ovisi o Infrastructure.
- [ ] Exporti koje drugi fileovi koriste postoje (npr. `authFetch`, `ApiError`).
- [ ] Migracija: naznačena **naredba** ako ima novih/promijenjenih entiteta; „bez migracije" ako nema.
- [ ] **BACKLOG ažuriran** (stanje, checklist status, Decision Log unos, zadnja sesija na vrh Dnevnika + zrcalo u HISTORY po potrebi).
- [ ] Isporuka: **ZIP + git patch** kroz `present_files`.
- [ ] Napomena: kod **nije kompajliran** u okruženju (nema .NET SDK / `node_modules`) — navesti što je ručno provjereno.

## 10. Mapa koda (gdje što živi)

- ** Port/contract (cross-module):** `src/WediFrame.Shared/<Area>/`
- **Entiteti + EF config:** `src/Modules/<Modul>/Domain/`, `…/Persistence/`
- **Servisi (impl portova, workeri):** `src/Modules/<Modul>/Services/`
- **HTTP endpointi:** `src/Modules/<Modul>/Endpoints/`
- **DbContext + migracije:** `src/WediFrame.Infrastructure/Persistence/`, `…/Migrations/`
- **Host bootstrap + module petlja + auth/rate-limit:** `src/WediFrame.Api/Program.cs`
- **Frontend rute:** `web/src/app/[locale]/…`; **komponente:** `web/src/components/…`; **API klijenti:** `web/src/lib/…`; **i18n:** `web/src/messages/…`
