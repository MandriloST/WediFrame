# WediFrame — Backlog

> **Pravila:** Ažurira se na kraju svake radne sesije. Statusi: `[ ]` todo, `[~]` u tijeku, `[x]` gotovo, `[!]` blokirano.
> Redoslijed unutar milestonea = prioritet. Ništa se ne briše (gotovo ostaje radi povijesti).
> **Zadnje ažurirano:** 2026-08-12 (v19)

> **⚠️ ISPORUKA KODA (OBAVEZNO, svaki put):** Sve izmjene korisniku se daju kao **(a) ZIP s točnom strukturom foldera** (raspakiraš u root projekta → sve legne na svoje mjesto, čisti copy/paste) **i/ili (b) `git` patch** (`git apply`). NIKAD samo isječci u chatu bez foldera. Svaki isporučeni file mora biti na svojoj stvarnoj putanji unutar zipa. Ovo je izričit i ponovljen zahtjev vlasnika.

---

## Otvorena pitanja (čekaju korisnika)

*(trenutno nema otvorenih pitanja)*

## Riješena pitanja (2026-07-07)

- GitHub repo podijeljen: `https://github.com/MandriloST/WediFrame` — razvoj na `develop`, merge u `main` radi korisnik ✔
- Domena wediframe.hr: korisnik potvrdio registraciju pri kraju projekta ✔

## Riješena pitanja (2026-07-06, v2)

- Free/Trial limiti su **ukupni** (250 MB total, video do 50 MB ukupno) ✔
- Max video datoteka: **2 GB** (svi paketi) ✔
- wediframe.com **trenutno nedostupan** — pratiti dostupnost / razmotriti backorder ✔
- R1 flow potvrđen: checkbox "Trebam R1" u checkoutu → naziv firme, OIB, adresa ✔
- Lokalizacija: kasnije dodatni jezici (srpski i dr.) — i18n disciplina od prvog dana ✔

## Riješena pitanja (2026-07-06)

- EF Core: **Code First + migracije** ✔ · Baza: **PostgreSQL** ✔ · Galerija: **svi gosti vide sve, upload bez računa** ✔
- Paketi i cijene: dostavljeni, u `PROJECT.md` ✔ · Retencija: **od T0 = datum početka uploada koji bira host** ✔
- Plaćanje: **Stripe + R1/fiskalizacija za HR**, multi-currency spremnost ✔ · Hosting: **Railway (EU) + Neon (EU) + Vercel** ✔
- Domena/brend: **wediframe.hr**, "Powered by EverFrame" ✔ · Video: **bez limita trajanja, limit veličine po datoteci** ✔

## Pretpostavke (vrijede dok korisnik ne kaže drukčije)

- Gosti nemaju račune; identitet = samoprijavljeno ime (opcionalno).
- MVP bez video transcodinga; reprodukcija originala (R2 range requests).
- Provizije partnerima ručno u MVP-u. HR + EN jezici.
- Kod pitanja 5 korisnik je napisao "gost odabire datum" — protumačeno kao **host** odabire T0.
- Cover fotografija: max **20 MB**, tipovi **JPEG/PNG/WebP** (v7 — pretpostavka, lako promjenjivo u `CoverPhotoRules`).
- Guest stranica vidi event samo u statusu **Active/UploadClosed**; Draft/Expired/Deleted → 404 (v7 — pretpostavka).
- `uploadOpen` na guest endpointu je provizoran do M3 (kraj upload perioda ovisi o paketu): Active + danas ≥ T0.
- Guest fotke: max **50 MB po datoteci**, tipovi JPEG/PNG/WebP/HEIC/HEIF/GIF, max 30 datoteka po presign requestu (v8 — pretpostavka; HEIC prihvaćamo jer ga iPhone generira, prikaz rješava thumbnail job u M2).
- Galerija: stranica **24 stavke**, dohvat kroz "Učitaj još"; do thumbnail joba grid lazy-loada **originale** (na svadbenom wifiju skupo — thumbnaili su sljedeći korak M2). HEIC/HEIF bez thumbnaila → **placeholder pločica** (browser ih ne renderira) (v10 — pretpostavka).
- Thumbnaili: **JPEG, najduži rub 640 px, kvaliteta 80**, generira ih background worker (libvips); poll 15 s, batch 10, sekvencijalno (RAM). Neuspjeli fileovi → `ThumbnailStatus=Failed` (ne ponavljaju se) (v11 — pretpostavka, sve promjenjivo kroz `Media:Thumbnails`).

---

## M0 — Temelji projekta (prije koda)

- [x] Definiran koncept, MVP opseg, arhitektura (PROJECT.md, ARCHITECTURE.md) — 2026-07-04
- [x] Razriješena ključna otvorena pitanja (stack, baza, paketi, retencija, hosting, brend) — 2026-07-06
- [x] Plan paketa unesen u dokumentaciju — 2026-07-06
- [ ] Korisnik: registrirati wediframe.hr — **odluka korisnika 2026-07-07: registracija pri kraju projekta** (Claude preporučio odmah zbog rizika zauzeća; korisnik svjestan, ostaje njegova odluka)
- [x] Kreirati Git repo, dodati /docs — 2026-07-07 (push napravljen, `develop` branch za razvoj; link repoa još nije podijeljen u chatu)
- [x] Postaviti solution strukturu (.NET modularni monolit skeleton, EF Core Code First + prva migracija) — 2026-07-07: build prolazi, migracija `InitialCreate` primijenjena na lokalni PG (`shared.audit_log`); riješeni NU1903 (pin ranjivih transitivnih paketa + NoWarn NU1902/NU1903) i NU1605 (EF paketi na 10.0.4)
- [x] Postaviti Next.js projekt (mobile-first, i18n skeleton, PWA manifest) — 2026-07-07: `web/` u monorepou; Next 16 + next-intl (HR default bez prefiksa, /en), manifest + ikone, landing placeholder ✔ (commitano)
- [ ] Cloudflare račun + R2 bucket (EU jurisdiction), Stripe test račun, Railway + Neon (EU) projekti — korisnik; **R2 bucket + API token sada blokira smoke test cover flowa** (upute korak-po-korak u README, sekcija "Cloudflare R2")

## M1 — Event + guest upload (srce proizvoda)

- [x] Identity: registracija/prijava hosta (minimalno) — 2026-07-13: migracija `AddIdentityAndEvents` (20260707110645) commitana i na `main` i na `develop` ✔
- [x] Events: kreiranje eventa (naslov, T0, draft), guest token, QR generiranje — 2026-07-13: kod + migracija na `main`/`develop` ✔
- [x] Cover fotografija: upload (host) + prikaz na guest stranici — smoke test prošao 2026-07-15 (uz tri R2 lekcije: `AuthenticationRegion="auto"`, EU jurisdiction endpoint `.eu.`, PUT datoteka curlom jer VS .http ne podržava file body); dodano odbijanje praznih objekata na confirmu (`events.cover_empty`)
- [~] Media: presigned upload flow — single PUT za fotke — **kod isporučen 2026-07-15 (v8)**: `MediaItem` entitet (shema `media`), `POST /guest/{token}/uploads` (batch presign, all-or-nothing validacija s per-item error kodovima), `POST /guest/{token}/uploads/{mediaId}/confirm` (HEAD verifikacija, stvarna veličina, Failed za prazne/prevelike), `IGuestEventAccess` javni ugovor Events modula; ostaje korisnik lokalno: `dotnet ef migrations add AddMediaItems` → `database update` → smoke test (koraci 9–12 u tools/smoke-test.http) → commit
- [x] Media: multipart upload za video (chunk retry) — **v14**: R2 multipart (init/complete/abort), presigned PUT po dijelu (16 MB), retry po dijelu, 2 GB/file. *Resume nakon zatvaranja taba i cleanup nedovršenih = TODO (cleanup job, backlog)*
- [~] Guest stranica `/e/{token}`: cover + naslov → upload button → upload UI s napretkom i statusima — **kod isporučen 2026-07-18 (v9)**: server stranica (`app/[locale]/e/[token]/page.tsx`, no-store fetch, notFound za 404, noindex), `UploadSection` client komponenta (picker, queue s konkurentnošću 3, XHR progress, retry, agregatni status "N poslano · M čeka · K nije uspjelo"), `guestApi.ts` klijent, dizajn (Fraunces display samo za naslov, kartica-potpis preko covera, bordo CTA); **API dobio CORS** (Frontend:AllowedOrigins). Ostaje korisnik: `.env.local` iz `.env.example`, test na mobitelu (iOS Safari, Android Chrome, IG/WhatsApp webview)
- [~] Privacy notice + opcionalni unos imena gosta (HR/EN) — u v9 na guest stranici (prvi klik na upload → notice + ime, localStorage pamćenje); finalne pravne rečenice u M4 s ToS/Privacy
- [~] Backend enforcement: tip, veličina po datoteci, upload period aktivan — za FOTKE gotovo u v8 (tip, ≤50 MB, `media.upload_closed` izvan perioda); za video stiže s multipart blokom
- [ ] Test na stvarnim mobitelima: iOS Safari, Android Chrome, Instagram/WhatsApp webview
- [~] **Milestone test:** s mobitela — picker/upload rade (v12, potvrđeno na LAN-u); preostaje odraditi checklist (više fotki odjednom, HEIC→thumbnail, loša mreža/retry, IG/WhatsApp webview). Dio s velikim videom čeka multipart (M1)

## M2 — Galerija

- [x] Guest galerija na `/e/{token}` (ispod upload buttona): grid thumbnailova, lazy load, lightbox — **isporučeno v10, commitano (a8eb292) i mergeano na `main` (PR #5)**: backend `GET /guest/{token}/media` (offset paginacija, deterministički order `(CreatedAt desc, ObjectKey desc)`, presigned GET po stranici, samo Confirmed+Visible+ne-obrisano); frontend `Gallery` (grid 3 stupca, lazy `<img>`, lightbox s prev/next/Esc, "Učitaj još"), `GuestExperience` wrapper (instant preview iz lokalnog blob-a na confirm, dedupe po mediaId), HR/EN poruke, `.env.example` dodan. **Bez nove migracije** (MediaItem već postoji). Ostaje: thumbnaili (dolje) i mobilni test. Do tada grid lazy-loada originale.
- [x] Thumbnail generiranje za fotke (background job) — isporučeno v11, **radi end-to-end od v12** (R2 `PutObject` fix): `ThumbnailWorker` (poll DB Confirmed+Pending), libvips/NetVips (JPEG 640px, HEIC podržan), `ThumbnailStatus` (Pending/Ready/Failed), frontend HEIC lightbox fallback. Migracija `AddThumbnailStatus` primijenjena
- [~] Video: reprodukcija originala u lightboxu (`<video>`, R2 range requests) — **v14**; grid pločica je generička (play badge). Poster frame bez transcodinga = TODO
- [x] Host galerija: isto + hide/delete — **v18**: backend host media endpointi (`GET /events/{id}/media` uklj. skrivene, `PATCH …/{mediaId}` visibility, `DELETE …` soft-delete) + `IHostEventAccess` (ownership); frontend `/dashboard/events/{id}/gallery` koji reusa zajednički grid/lightbox (izdvojen u `components/media/MediaGallery.tsx`), hide/unhide + delete u lightboxu, skrivene stavke zatamnjene. Ulaz s detalja eventa. **Bez migracije** (`Visibility`/`SoftDeletedAt` postoje od `AddMediaItems`)
- [~] Download: **pojedinačne datoteke gotovo (v19)** — presigned attachment URL (`Content-Disposition`), gumb u lightboxu; host preuzima bilo koju vidljivost, gost samo vidljive. ZIP export (background job) — još TODO
- [ ] Ponašanje nakon isteka upload perioda (button → "Upload je završen", galerija ostaje)

## M3 — Paketi, plaćanje, limiti

- [ ] Package entiteti + seed 5 paketa iz PROJECT.md
- [ ] Free/Trial aktivacija bez plaćanja (+ zaštita od zloupotrebe: 1 aktivan free event po korisniku?)
- [ ] **Istražiti Fiskalizacija 2.0 obveze za online naplatu (na snazi od 1.1.2026.)** i odabrati rješenje/servis za fiskalizaciju + R1 račune; Stripe to sam ne rješava
- [ ] Stripe checkout + webhook; event aktivan tek nakon plaćanja i fiskalizacije
- [ ] R1 flow u checkoutu: checkbox "Trebam R1" → naziv firme, OIB, adresa
- [ ] Enforcement limita: foto count, video kvota, ukupna kvota, upload period — razumljive poruke gostu
- [ ] Bonus kodovi: entiteti, validacija, popust u checkoutu, atribucija partneru
- [ ] Pricing stranica (5 paketa)

## M4 — Retencija, GDPR, pravni sadržaj

- [ ] Retention job: kraj upload perioda → podsjetnik prije isteka retencije → soft delete → grace ~7d → fizičko brisanje (R2 + metadata)
- [ ] Brisanje cijelog eventa na zahtjev hosta; audit log brisanja i admin akcija
- [ ] ToS + Privacy Policy (HR/EN): uloge obrade, retencija, što se NE radi s podacima
- [ ] Email notifikacije (potvrda kupnje + račun, podsjetnik isteka uploada/retencije)

## M5 — Admin, poliranje, launch

- [ ] Interni admin: eventi, korisnici, storage report, ručno produljenje retencije
- [ ] Token rotacija (host UI) + upozorenje "link = puni pristup"
- [ ] Rate limiting guest ruta
- [ ] Partner izvještaj (admin): iskorištenja kodova po partneru
- [ ] Landing stranica (WediFrame brending, "Powered by EverFrame") — placeholder postoji od M0; pravi dizajn ovdje
- [ ] PWA fino: manifest ✔ (M0), prave ikone/logo umjesto placeholdera, service worker, offline fallback poruka
- [ ] Load test upload flowa (~100 istovremenih gostiju)
- [ ] Pilot: 1–2 stvarna eventa (preko partnera, možda besplatno) prije naplate

## Post-MVP (parkirano, ne raditi bez odluke)

- Live slideshow za projektor · Video transcoding · Partner self-service portal + automatske provizije
- Komentari/lajkovi, guest knjiga (audio/video poruke) · AI moderacija
- Ostale vertikale (rođendani, krštenja, team building, cruise/sailing) · Upsell produljenja retencije
- Širenje van HR (multi-currency, dodatni jezici — infrastruktura spremna od početka)

---

## Decision Log

| Datum | Odluka | Obrazloženje |
|---|---|---|
| 2026-07-04 | Modularni monolit, .NET 10, Next.js PWA, R2 | Solo dev, iterativni razvoj; R2 zbog nula egress troška |
| 2026-07-04 | Upload direktno na R2 presigned URL-ovima | API ne smije biti usko grlo na dan eventa |
| 2026-07-04 | Bez video transcodinga u MVP-u | Trošak/kompleksnost nerazmjerni MVP-u |
| 2026-07-04 | Gosti bez računa, pristup event tokenom | Nula trenja = core value proposition |
| 2026-07-04 | Soft delete + grace period prije fizičkog brisanja | Zaštita od greške, GDPR-kompatibilno |
| 2026-07-06 | EF Core **Code First** umjesto Database First | Greenfield; shema u Gitu, lakše iteracije i AI suradnja |
| 2026-07-06 | **PostgreSQL** (Neon EU) | Jeftin hosting, odličan EF Core support |
| 2026-07-06 | Galerija vidljiva **svim gostima**; upload bez računa | Odluka vlasnika; jednostavnost za goste |
| 2026-07-06 | **T0 = datum početka uploada** (bira host); upload period i retencija računaju se od T0 | Odluka vlasnika |
| 2026-07-06 | 5 paketa s cijenama i limitima (vidi PROJECT.md) | Plan vlasnika |
| 2026-07-06 | Video: **bez limita trajanja**, limit veličine po datoteci (prijedlog 2 GB) | Trajanje nebitno ako je veličina ograničena; jednostavnije za gosta |
| 2026-07-06 | Hosting: Railway EU (API), Neon EU (PG), Vercel (FE) | Plan vlasnika; GDPR-ovski čisto, mediji u R2 EU |
| 2026-07-06 | Stripe + HR fiskalizacija/R1 (Fiskalizacija 2.0) | HR tržište; obveza od 1.1.2026., detalji u M3 |
| 2026-07-06 | Brend: **WediFrame** (wediframe.hr), "Powered by EverFrame" | Odluka vlasnika |
| 2026-07-06 | Guest stranica: cover + naslov → upload button → galerija | Definiran layout od vlasnika |
| 2026-07-06 | Svi limiti paketa su ukupni; max video datoteka **2 GB** | Potvrda vlasnika |
| 2026-07-06 | R1 flow: checkbox u checkoutu → firma, OIB, adresa | Potvrda vlasnika |
| 2026-07-06 | Dodatni jezici (srpski...) post-MVP; i18n ključevi, bez hardkodiranih stringova, layout tolerantan na duže prijevode | Zahtjev vlasnika — spremnost bez sadašnjeg troška |
| 2026-07-06 (v3) | **Jedan `AppDbContext`** za cijeli monolit; granice modula preko **PostgreSQL shema po modulu** | Jedna povijest migracija i jedan deploy — najmanje trenja za solo dev; lako kasnije razdvojiti |
| 2026-07-06 (v3) | Moduli se registriraju **eksplicitnom listom** u `Program.cs` (`IModule` ugovor), bez reflectiona | Čitljivost i predvidljivost > magija |
| 2026-07-06 (v3) | `.slnx` solution format + Central Package Management | Moderni .NET 10 standard; verzije na jednom mjestu |
| 2026-07-06 (v3) | Inicijalna migracija sadrži samo `shared.audit_log` | Skeleton bez feature koda; audit log infrastrukturni (GDPR/M4) |
| 2026-07-06 (v3) | Default EF naming (PascalCase) u PG, bez snake_case paketa | Jedna ovisnost manje |
| 2026-07-07 (v4) | NU1903 fix: pin ranjivih transitivnih paketa (`System.Security.Cryptography.Xml` 10.0.0, `Microsoft.OpenApi` 1.6.22) + `NoWarn NU1902;NU1903` | TreatWarningsAsErrors ostaje za C# warninge; vulnerability warningi na transitivama ne smiju blokirati build |
| 2026-07-07 (v4) | EF Core paketi na **10.0.4** (Npgsql.EFCore.PG 10.0.1 zahtijeva >= 10.0.4) | NU1605 downgrade konflikt |
| 2026-07-07 (v4) | **Monorepo**: frontend u `web/` folderu istog repoa | Solo dev: jedan repo = jedan izvor istine, /docs vrijedi za oboje |
| 2026-07-07 (v4) | **`develop` branch** za razvoj (korisnik) | Workflow korisnika |
| 2026-07-07 (v4) | i18n: **next-intl**, HR default **bez URL prefiksa** (`/`), EN na `/en`; localePrefix "as-needed" | wediframe.hr na hrvatskom bez /hr u URL-u; novi jezik = 1 linija + messages file |
| 2026-07-07 (v4) | Skeleton **bez webfonta** (system font stack) | Tipografija je dizajnerska odluka koja dolazi s guest stranicom (M1); build neovisan o mreži |
| 2026-07-07 (v4) | Domena wediframe.hr: registracija **pri kraju projekta** | Odluka korisnika (uz zabilježenu preporuku Claudea da se registrira ranije) |

| 2026-07-07 (v5) | **Smjer ovisnosti**: Infrastructure → Moduli (radi EF konfiguracija u `AppDbContext`); moduli NIKAD ne referenciraju Infrastructure — podatke koriste kroz bazni `DbContext` iz DI-ja (`AddScoped<DbContext>` alias na `AppDbContext` u `Program.cs`) | Bez kružnih referenci; entiteti i konfiguracije ostaju u modulu (self-contained), jedna migracijska povijest ostaje u Infrastructure |
| 2026-07-07 (v5) | Auth: **JWT access (30 min) + opaque refresh token (30 dana, rotacija na svaku upotrebu, u bazi samo SHA-256 hash)** | Standard, jednostavno, revocable; bez vanjskog identity providera u MVP-u |
| 2026-07-07 (v5) | Lozinke: `PasswordHasher<User>` iz ASP.NET Core Identity (PBKDF2), bez cijelog Identity frameworka | Provjeren hasher iz shared frameworka, nula dodatnih ovisnosti; framework bi bio overkill za 4 endpointa |
| 2026-07-07 (v5) | API greške = **strojni kodovi** (`auth.email_taken`, `auth.invalid_credentials`...) u ProblemDetails; prijevod radi frontend kroz i18n | Backend ne zna jezik gosta/hosta pouzdano; jedan izvor prijevoda (messages/*.json) |
| 2026-07-07 (v5) | Login vraća istu grešku za nepostojeći email i krivu lozinku | Sprječava account enumeration |
| 2026-07-07 (v5) | `Jwt:SigningKey` obavezno izvan repoa (user-secrets / `Jwt__SigningKey` env); u `appsettings.Development.json` samo očiti dev-only key; validacija na startu (min 32 znaka) | Secrets disciplina od prvog auth koda |

| 2026-07-07 (v6) | Moduli s entitetima referenciraju **`Npgsql.EntityFrameworkCore.PostgreSQL`** (ne bazni `Microsoft.EntityFrameworkCore`) | Bazni paket NE sadrži relacijske ekstenzije (`ToTable` i dr. su u `.Relational` koji dolazi kroz provider); potvrđeno CS1061 greškom kod korisnika; ujedno svi moduli na istom provideru |
| 2026-07-07 (v6) | QR: **QRCoder 1.8.0** (MIT, aktivno održavan), PNG + SVG, ECC level Q | Nula ovisnosti o System.Drawing (cross-platform, Railway Linux); SVG idealan za tisak; Q (25%) redundancija za tiskane kartice |
| 2026-07-07 (v6) | Guest token: **32 random bajta → Base64Url (43 znaka)**, unique index | ~256 bita entropije, URL-safe, neprebrojiv — zadovoljava sigurnosni zahtjev iz ARCHITECTURE.md |
| 2026-07-07 (v6) | `Event.OwnerUserId` = **plain Guid bez cross-module FK/navigacije** na `identity.users` | Granice modula: Identity vlasnik korisnika, Events samo drži referencu; JOIN-ovi po potrebi kroz eksplicitne upite |
| 2026-07-07 (v6) | **T0 kao `DateOnly`** (PG `date`) — host bira datum, ne trenutak | Odgovara semantici "datum početka uploada"; izračun perioda (uploadEndsAt/expiresAt) dolazi s paketima u M3 |
| 2026-07-07 (v6) | Guest URL kroz **`Frontend:GuestBaseUrl`** config (`FrontendOptions` u Shared, validacija na startu) | Backend gradi apsolutne linkove za QR (i kasnije emailove u M4) bez hardkodiranja domene |
| 2026-07-07 (v6) | Tuđi/nepostojeći event ID → **404, nikad 403** | Ne otkriva postojanje tuđih evenata (information disclosure) |

| 2026-07-13 (v7) | Storage apstrakcija `IObjectStorage` u Shared, R2 implementacija u Infrastructure; **AWSSDK.S3 v4** s `RequestChecksumCalculation/ResponseChecksumValidation = WHEN_REQUIRED` i path-style endpointom | R2 ne podržava defaultne CRC32 checksume SDK-a v4 (Cloudflare docs); apstrakcija drži module neovisnima o R2 |
| 2026-07-13 (v7) | R2 klijent **lazy** — API se diže bez R2 konfiguracije, prvi storage poziv baca jasnu grešku | Dev bez buketa i dalje može raditi auth/events; nema skrivenih startup failova na Railwayu |
| 2026-07-13 (v7) | Cover confirm je **stateless**: key putuje u requestu, vlasništvo dokazuje obavezni prefiks `events/{id}/cover/`; stari cover se briše NAKON DB commita | Nema pending-state tablice za jednu fotku; idempotentno; fail-safe redoslijed |
| 2026-07-13 (v7) | Cover pravila: 20 MB max, JPEG/PNG/WebP (`CoverPhotoRules`) | Pretpostavka Claudea — dovoljno za mobilne fotke; vlasnik može korigirati |
| 2026-07-13 (v7) | Guest vidljivost: samo Active/UploadClosed; token duljina sanity-check prije DB upita | Draft/istekli eventi ne smiju biti javno dohvatljivi; jeftin early-reject |

| 2026-07-15 (v8) | R2 lekcije (potvrđene smoke testom): SigV4 regija mora biti `auto` (`AuthenticationRegion`), EU jurisdiction bucket koristi endpoint `{accountId}.eu.r2.cloudflarestorage.com` (novo polje `R2Options.Jurisdiction`, default `eu`), VS .http ne šalje file body → PUT curlom | AccessDenied debugging 2026-07-14; oboje bi ponovno ugrizlo na produkcijskom bucketu |
| 2026-07-15 (v8) | Cross-module pristup: Media koristi **`IGuestEventAccess`** — javni ugovor Events modula (token → `GuestEventContext`), jednosmjerna referenca Media→Events | ARCHITECTURE §2: komunikacija kroz jasne interfejse; jedna istina za vidljivost i upload-open pravila |
| 2026-07-15 (v8) | Guest fotke: 50 MB/datoteka, JPEG/PNG/WebP/HEIC/HEIF/GIF, ≤30 datoteka po requestu; batch validacija **all-or-nothing** s per-item ključevima (`items[3].sizeBytes`) | Pretpostavka Claudea (paketske kvote su M3); all-or-nothing = jednostavan ugovor, frontend pre-filtrira |
| 2026-07-15 (v8) | Confirm sprema **stvarnu** veličinu s R2 (HEAD), prazan/prevelik objekt → briši + status Failed; kvote u M3 broje samo Confirmed | Deklarirana veličina je nepovjerljiva; 0-byte rupa nađena smoke testom na coveru |

| 2026-07-18 (v9) | CORS na API-ju kroz `Frontend:AllowedOrigins` (dev: localhost:3000) | Browser (guest stranica) ne može zvati API bez CORS-a; origin liste po okolini |
| 2026-07-18 (v9) | Guest stranica: cover kao `<img>` (ne next/image) — presigned URL-ovi su jedinstveni po requestu pa optimizer cache ionako uvijek promašuje; Fraunces (next/font) SAMO za naslov, UI na system stacku | Performanse u IG/WhatsApp webviewu na lošem wifiju; jedan mali font = cijeli webfont budžet |
| 2026-07-18 (v9) | Upload UX: konkurentnost 3, XHR (fetch nema upload progress), retry = novi presign po datoteci; ime gosta + privacy ack u localStorage | Progress je cijeli UX na svadbenom wifiju; retry sa svježim URL-om izbjegava istek potpisa |

| 2026-08-10 (v10) | Galerija: **offset paginacija** (`?offset&limit`, 24/str.) uz order `(CreatedAt desc, ObjectKey desc)`, klijent dedupira po `mediaId` | Cijeli batch dijeli isti `CreatedAt` (jedan `now` u presignu) pa keyset samo po vremenu lomi; `ObjectKey` je unique tie-break; izbjegnuta `Guid` nejednakost (Npgsql je ne prevodi); dedupe na klijentu upija pomak granice zbog paralelnih uploada |
| 2026-08-10 (v10) | Thumbnaili **odgođeni**: galerija do joba lazy-loada originale; `thumbnailUrl` null → grid koristi original; HEIC/HEIF bez thumbnaila → placeholder pločica | Mali korak: read-side galerije radi odmah, bez slikovne obrade/background workera; thumbnail job je idući M2 komad i samo popuni `ThumbnailKey` |
| 2026-08-10 (v10) | **Instant preview**: na `confirm` fotka odmah uđe u grid iz lokalnog `objectURL`-a (revoke na unmount), server kopija se dedupira po `mediaId` | Nula latencije i nula ponovnog downloada svježe poslane fotke; osjećaj "odmah je tu" na dan eventa |
| 2026-08-10 (v10) | Lightbox **bez biblioteke** (vlastiti overlay: prev/next, Esc, klik na pozadinu) | Jedan mali ovisnostima-čist ekran; webfont/JS budžet ostaje nizak za webviewove |
| 2026-08-10 (v10) | `web/.env.example` dodan (nedostajao u repou iako ga je v9 dnevnik naveo) | Bez njega novi klon nema uzorak za `NEXT_PUBLIC_API_URL`; `.gitignore` ga već propušta (`!.env.example`) |

| 2026-08-10 (v11) | Thumbnaili kroz **libvips (NetVips)**, apstraktno iza `IThumbnailGenerator` (Shared), impl u Infrastructure | libvips shrinka-na-load (nizak RAM za 48 MP HEIC), native lib nosi HEIF/HEIC loader; modul ne vidi native ovisnost |
| 2026-08-10 (v11) | Thumbnail worker = **BackgroundService u API procesu**, "red" je DB stanje (Confirmed foto + `ThumbnailStatus=Pending`), poll svakih 15 s | Nula dodatne infrastrukture (bez queue/Redis); jedan Railway servis; idempotentno i self-healing (crash → ostaje Pending → reprocess) |
| 2026-08-10 (v11) | Novo polje **`MediaItem.ThumbnailStatus`** (Pending/Ready/Failed), default Pending (migracija backfilla) | Neuspjeli/korumpirani fileovi ne blokiraju i ne ponavljaju se u redu; galerija i dalje čita `ThumbnailKey` |
| 2026-08-10 (v11) | `IObjectStorage` dobio **`DownloadAsync`/`UploadAsync`** (server-side, samo background) | Worker mora čitati original i pisati thumb kroz SDK; guest upload i dalje ide isključivo presigned URL-om (pravilo "datoteke ne kroz API" vrijedi za request path) |
| 2026-08-10 (v11) | Thumb spec: **JPEG 640px/q80**, key `events/{id}/thumbs/{mediaId}.jpg`; alpha se flatta na bijelo | JPEG = maksimalna kompatibilnost u IG/WhatsApp webviewima; 640px pokriva 3-col grid na retini; ~30–80 KB umjesto originala |
| 2026-08-10 (v11) | Greške: `ThumbnailFormatException` (loš/nepodržan file) → Failed; infra greške (R2/DB) → bubble + retry idući poll; ako sve pada zbog infre: `UPDATE media.media_items SET "ThumbnailStatus"='Pending' WHERE "ThumbnailStatus"='Failed';` | Loš file ne zaglavljuje red; sistemska greška se ne "spali" trajno — dokumentiran escape hatch (pravi admin je M5) |
| 2026-08-10 (v11) | Frontend: HEIC/HEIF u lightboxu pokazuje **thumbnail** (original browser ne renderira) | Bez toga grid radi ali lightbox HEIC-a je prazan; puni HEIC pregled ionako traži konverziju |
| 2026-08-10 (v11) | `NetVips.Native` (meta, svi RID-ovi) umjesto `NetVips.Native.linux-x64` | Jedan reference radi lokalno (bilo koji OS) i na Railwayu; slimanje na linux-x64 kasnije ako deploy veličina zasmeta |
| 2026-08-10 (v12) | R2 server-side `PutObject` mora imati **`DisablePayloadSigning = true`** | R2 ne implementira SDK-ov `STREAMING-AWS4-HMAC-SHA256-PAYLOAD` (chunked signing); bez toga thumbnail upload puca. Sigurno jer je transport HTTPS |
| 2026-08-10 (v12) | NetVips 8.15: `JpegsaveBuffer(strip: true)` → **`keep: Enums.ForeignKeep.None`** | Parametar `strip` maknut u novoj grani; `keep: None` = ne zadrži metapodatke |
| 2026-08-10 (v12) | `<html suppressHydrationWarning>` u locale layoutu | Ekstenzije (Dark Reader, Grammarly) ubace atribute na `<html>` prije hidracije → inače React zna odustati od hidracije cijelog dokumenta i stranica ostane bez JS-a |
| 2026-08-10 (v12) | File input: `hidden` → **`className="sr-only"`** | Programski `.click()` na `display:none` input u nekim webviewima ne otvara picker; sr-only je pouzdan cross-browser |
| 2026-08-10 (v12) | Dev na LAN-u (mobilni test): `allowedDevOrigins` + API na `0.0.0.0` + CORS/`NEXT_PUBLIC_API_URL` na LAN IP — **lokalno, ne na `main`** | Next 16 blokira `/_next/*` preko LAN IP-a → bez klijentskog JS-a; ovo su dev-only postavke vezane uz kućnu mrežu |
| 2026-08-11 (v13) | HEIC/HEIF se **konvertira u JPEG u browseru prije uploada** (`heic-to`, libheif WASM), dynamic import | Prebuilt libvips (NetVips.Native) nema HEVC/HEIF (licence) pa je server-side thumbnail HEIC-a padao (`Failed`); browser ionako ne prikazuje HEIC. Ovako server nikad ne vidi HEIC, original u galeriji je odmah prikaziv JPEG, thumbnail worker radi normalno. WASM (~2.9 MB) je inline i lazy — ne ulazi u glavni bundle |
| 2026-08-11 (v13) | Lokalni queue id: `crypto.randomUUID()` → **fallback** (`newLocalId`: getRandomValues → Math.random) | `randomUUID` postoji samo u secure contextu (HTTPS/localhost); na LAN http-u je `undefined` i rušio je odabir datoteke |
| 2026-08-11 (v13) | `Gallery` `setItems` updater mora biti **pure** (dedup prema `prev`, bez mutiranja ref-a) | Stari updater je mutirao `loadedIds` ref unutar sebe; React Strict Mode zove updater dvaput pa je drugi prolaz filtrirao cijelu stranicu → galerija se učita i nestane. Bilo skriveno optimistic previewima; otkrilo se na reloadu |
| 2026-08-11 (v14) | Video: **multipart upload browser → R2** (init/complete/abort endpointi), presigned PUT po dijelu | Pravilo "datoteke ne kroz API" vrijedi i za video; multipart daje retry po dijelu na lošoj mreži i podržava velike fajlove |
| 2026-08-11 (v14) | Part size **16 MB**, do 2 GB/file (≈128 dijelova), tipovi mp4/quicktime(mov)/webm | 16 MB = jeftin retry na wedding wifiju, daleko ispod 10 000-part limita; mov jer iPhone snima quicktime |
| 2026-08-11 (v14) | `MediaItem.MultipartUploadId` (nullable), čisti se na complete/abort | Treba za complete/abort i za budući cleanup job koji gasi nedovršene multipartove |
| 2026-08-11 (v14) | **R2 CORS mora exposati `ETag`** (`ExposeHeaders: ["ETag"]`) | Browser mora pročitati ETag svakog dijela (`xhr.getResponseHeader`) da bi complete uspio; bez toga upload puca na kraju |
| 2026-08-11 (v14) | Retry: neuspjeli video = restart cijelog uploada (ne resume) | Resume traži perzistiranje uploadId+ETagova; za MVP restart je jednostavniji. Resume je backlog |
| 2026-08-11 (v15) | Pokrenut **host frontend** (dosad ga uopće nije bilo — samo landing + guest): `hostApi` (JWT u localStorage, refresh na 401), stranice `/login`, `/register`, `/dashboard` (lista evenata + copy-link + logout), `/dashboard/events/new` | Bez host UI-ja mladenci nisu mogli sami raditi evente (radilo se kroz API); ovo je temelj za detalj eventa i upravljanje galerijom |
| 2026-08-11 (v15) | Auth guard client-side (provjera tokena na mountu → redirect na /login); i18n error kodovi (`auth.*`) mapiraju se u `auth.errors.*` | Jednostavno za MVP; SSR-zaštita rute nije nužna jer API ionako traži Bearer |
| 2026-08-11 (v16) | **Free aktivacija** eventa: `POST /events/{id}/activate` (Draft→Active), gumb "Aktiviraj" na dashboard kartici | Novokreirani event je Draft, a gosti vide samo Active/UploadClosed → guest link je vraćao 404. Paketi/plaćanje (M3) će kasnije gatati aktivaciju; Free ostaje trenutna |
| 2026-08-11 (v16) | Copy-to-clipboard fallback (`execCommand`) kad `navigator.clipboard` ne postoji | Clipboard API (kao i `crypto.randomUUID`) radi samo u secure contextu; na LAN http-u je `undefined` |
| 2026-08-11 (v17) | **Detalj eventa** `/dashboard/events/{id}`: cover upload (presigned), QR (authed→blob) + download, guest link + copy/open, aktivacija za Draft | Zaokružuje host self-service: mladenci pripreme cover, aktiviraju, uzmu QR/link za tisak — sve iz UI-ja. Backend nepromijenjen (postojeći event/qr/cover endpointi) |
| 2026-08-12 (v18) | **Host media endpointi** pod `/events/{id}/media` (Media modul) + `IHostEventAccess` (Events modul) za ownership: `GET` (uklj. skrivene), `PATCH` visibility, `DELETE` soft-delete | Zrcalo `IGuestEventAccess`; granica modula ostaje čista (Media već referencira Events jednosmjerno). **Bez migracije** — `Visibility`/`SoftDeletedAt` postoje od `AddMediaItems` |
| 2026-08-12 (v18) | Host **delete = soft-delete** (`SoftDeletedAt`); R2 objekt ostaje do retention grace perioda (M4). Hide/unhide/delete se upisuju u `shared.audit_log` | GDPR erasure trail + oporavak od slučajne greške; fizičko brisanje centralizirano u Retention jobu |
| 2026-08-12 (v18) | **Izdvojen zajednički grid** u `web/src/components/media/MediaGallery.tsx` (tile, thumb, placeholder, play badge, lightbox); guest `Gallery` i host galerija ga dijele (lightbox dobio opcionalni `actions` slot) | DRY — jedan lightbox/tile; host samo dodaje kontrole. Guest ostaje vizualno identičan |
| 2026-08-12 (v18) | Ulaz u galeriju je link **na detaljnoj stranici** eventa (ne na dashboard kartici) | v17 je uveo detalj kao "dom" eventa; tok je dashboard → detalj → "Upravljaj galerijom" |
| 2026-08-12 (v19) | **Download pojedinačne datoteke** preko presigned GET-a s `response-content-disposition=attachment` (novi `IObjectStorage.PresignDownloadAsync`); host `GET /events/{id}/media/{mediaId}/download`, guest `GET /guest/{token}/media/{mediaId}/download` | Bytovi i dalje idu direktno browser→R2 (API ne proxya); filename se potpisuje u URL (`wediframe-<id8>.<ext>`). Gost preuzima samo Visible; host bilo koju vidljivost |
| 2026-08-12 (v19) | **Pravilo isporuke** zapisano u BACKLOG (vrh) i PROJECT §7: uvijek ZIP sa strukturom foldera i/ili `git` patch | Ponavljan zahtjev vlasnika; da svaki novi chat odmah zna |

## Dnevnik sesija

- **2026-07-04** — Inicijalna analiza, kreirani PROJECT.md / ARCHITECTURE.md / BACKLOG.md.
- **2026-07-06** — Korisnik odgovorio na svih 9 pitanja; ažurirane sve tri datoteke. **Sljedeći korak:** Git repo + /docs, zatim .NET i Next.js skeleton (M0).
- **2026-07-06 (v2)** — Zatvorena preostala 4 pitanja. Backlog bez otvorenih pitanja.
- **2026-07-06 (v3)** — Isporučen .NET skeleton (slnx, Api host, Shared kernel, Infrastructure, 7 modula).
- **2026-07-07 (v4)** — .NET skeleton **potvrđen kod korisnika**: riješeni NU1903 (pin + NoWarn) i NU1605 (EF 10.0.4), build prolazi, `InitialCreate` migracija primijenjena na lokalni PG. Repo pushan, `develop` branch kreiran (link još nije podijeljen). Isporučen **Next.js skeleton** u `web/`: Next 16 + TS + Tailwind 4, next-intl (HR default bez prefiksa, /en), PWA manifest + placeholder ikone, landing placeholder sa svim stringovima kroz i18n ključeve; build, lint i smoke test (HR/EN/manifest) verificirani u sesiji. **Sljedeći korak:** korisnik commita `web/` na develop + podijeli repo link; zatim M1 — Identity (registracija/prijava hosta) ili Events (kreiranje eventa + token + QR), preporuka: Identity prvi jer Events ovisi o njemu.
- **2026-07-07 (v5)** — Repo link dostavljen; pročitano stvarno stanje `develop` brancha (M0 potvrđen: .NET skeleton + `web/` Next.js skeleton). Isporučen **M1 Identity** (registracija/prijava hosta): entiteti `User` + `RefreshToken` (shema `identity`), `TokenService` (JWT HS256 + rotirajući refresh), endpointi `POST /api/v1/auth/register|login|refresh`, `GET /api/v1/auth/me`; JWT bearer validacija u API hostu; `DbContext` alias pattern za module. Kod NIJE kompajliran u sesiji (nema .NET SDK) — korisnik lokalno: build → `dotnet ef migrations add AddIdentityAuth` → `database update` → smoke test → commit. **Sljedeći korak:** potvrda builda + migracije; zatim **Events: kreiranje eventa (naslov, T0, draft) + guest token + QR**.
- **2026-07-07 (v6)** — Provjereno stanje developa (Identity kod + csproj fix commitani; migracija AddIdentityAuth JOŠ NE postoji). Isporučen **M1 Events** (kreiranje eventa + guest token + QR): `Event` entitet (shema `events`, status lifecycle, DateOnly T0), endpointi create/list/detail/QR (PNG download + SVG za tisak), `GuestTokenGenerator`, `QrCodeService` (QRCoder 1.8.0), `ClaimsPrincipalExtensions` + `FrontendOptions` u Shared, `Frontend:GuestBaseUrl` config. Korisnik lokalno: build → **JEDNA migracija `AddIdentityAndEvents`** (pokriva identity + events tablice) → `database update` → smoke test (register → create event → QR) → commit na develop. **Sljedeći korak:** potvrda; zatim Cloudflare R2 setup (korisnik) + **Media: presigned upload flow — single PUT za fotke** ili **Cover fotografija** (oboje treba R2).
- **2026-07-13 (v7)** — Provjereno stanje repoa: `main` = `develop`, Identity + Events + migracija `AddIdentityAndEvents` commitani (M1 stavke 1–2 označene [x]). Isporučen **cover fotografija flow + R2 infrastruktura**: `IObjectStorage` (Shared), `R2Options`, `R2ObjectStorage` + DI ekstenzija (Infrastructure, AWSSDK.S3 4.0.7.12), `CoverPhotoRules`, cover endpointi (presigned PUT + confirm s HEAD verifikacijom i zamjenom starog covera), `GET /guest/{token}` javni event info (naslov, cover URL, uploadOpen), R2 config sekcije + README upute (bucket EU, API token, CORS, user-secrets). Bez nove migracije. Kod NIJE kompajliran u sesiji — korisnik lokalno: R2 setup → build → smoke test → commit na develop. **Sljedeći korak:** potvrda cover flowa; zatim **Media: presigned upload flow — single PUT za fotke** (guest upload, enforcement tipa/veličine/perioda).
- **2026-07-15 (v8)** — Cover flow potvrđen end-to-end (nakon tri R2 fixa: auto regija, `.eu.` endpoint, curl za PUT — sve u Decision Logu). Isporučen **guest photo upload (single PUT)**: Media modul dobio `MediaItem` + `PhotoRules` + EF konfiguraciju + guest endpointe (batch presign + confirm), Events dobio `IGuestEventAccess` (GuestEndpoints refaktoriran na njega), cover confirm odbija prazne objekte, Infrastructure/AppDbContext prošireni za Media, novi `tools/smoke-test.http` (12 koraka, uklj. negativne). Korisnik lokalno: `dotnet build` → `dotnet ef migrations add AddMediaItems --project src/WediFrame.Infrastructure --startup-project src/WediFrame.Api` → `database update` → smoke test 9–12 → commit na develop. **Sljedeći korak:** multipart upload za video (chunk retry, resume, cleanup nedovršenih) ILI početak guest stranice u Next.js — korisnik bira.
- **2026-07-18 (v9)** — Isporučena **guest stranica** (srce proizvoda, prva verzija bez galerije): hero s coverom + kartica-potpis s naslovom (Fraunces), upload flow fotki end-to-end iz browsera (presign → XHR PUT s progressom → confirm), privacy notice + opcionalno ime na prvi klik, HR/EN poruke, `.env.example`; API dobio CORS. TypeScript provjera čista; `next build` u Claude okruženju pada samo na dohvatu Google Fonts (mrežna blokada sandboxa) — kod korisnika radi. ⚠️ **Migracija `AddMediaItems` još nije u repou** — obavezno prije testa. Korisnik: migracija → `web/.env.local` → `npm run dev` → otvoriti `/e/{token}` aktivnog eventa → upload s mobitela. **Sljedeći korak:** M2 guest galerija thumbnailova na istoj stranici ILI multipart video — nakon što stranica proradi na stvarnom mobitelu.
- **2026-08-10 (v10)** — Pročitano stvarno stanje repoa: `develop` je 2 commita ispred `main` (`MediaItem`, `wedifram v9`); migracija `AddMediaItems` **jest** u repou (commit `wedifram v9`, ⚠️ iz v9 riješen). Isporučena **guest galerija** (M2, prvi komad): backend `GET /guest/{token}/media` (offset paginacija, order `(CreatedAt desc, ObjectKey desc)`, presigned GET po stranici) + gallery contracts; frontend `Gallery` (grid 3 stupca, lazy `<img>`, lightbox prev/next/Esc, "Učitaj još"), `GuestExperience` wrapper (instant preview iz blob-a + dedupe po mediaId), `UploadSection` dobio `onConfirmed` callback, `page.tsx` renderira `GuestExperience`, HR/EN `gallery` poruke, dodan `web/.env.example`. **Bez nove migracije.** TypeScript (`tsc --noEmit`) i ESLint na novim datotekama **čisti** u sesiji; `next build` opet pada samo na Google Fonts (sandbox) — kod korisnika radi. ⚠️ Zatečene lint greške u `UploadSection.tsx` (set-state-in-effect + `<a href="/privacy">`) **postoje od v9** i ne blokiraju `next build` — čišćenje u zasebnom prolazu. Korisnik: `git pull` (develop) → `dotnet build` → `dotnet run --project src/WediFrame.Api` → `web/.env.local` iz `.env.example` → `npm run dev` → otvoriti `/e/{token}` aktivnog eventa, poslati par fotki, provjeriti da uđu u galeriju i lightbox → commit na develop; kad želiš, merge develop → main (zaostaje 2 commita + ovaj). **Sljedeći korak:** thumbnail background job (M2) — da grid prestane vući originale i da HEIC dobije prikaz; zatim host galerija (hide/delete) ili multipart video.
- **2026-08-10 (v11)** — Potvrđeno: v10 commitan (`a8eb292`) i mergean na `main` (PR #5), `develop`==`main`. Isporučen **thumbnail background job** (M2): `ThumbnailWorker` (BackgroundService, poll DB stanja Confirmed+Pending, batch 10 sekvencijalno, drain kad je pun batch), libvips/NetVips generator iza `IThumbnailGenerator` (Shared) + `NetVipsThumbnailGenerator` (Infrastructure, JPEG 640px/q80, HEIC podržan, alpha→bijelo), `IObjectStorage` proširen `DownloadAsync`/`UploadAsync` (+R2 impl), `MediaItem.ThumbnailStatus` (Pending/Ready/Failed), `PhotoRules.ThumbnailKey`, `ThumbnailOptions` (`Media:Thumbnails`), registracija (`AddImaging` u Program.cs + `AddHostedService` u MediaModule), NetVips paketi (CPM). Frontend: `Gallery` lightbox HEIC fallback (pokazuje thumbnail). Galerija automatski troši `thumbnailUrl` čim job odradi — bez izmjene endpointa. Frontend `tsc`+ESLint čisti u sesiji; **backend NIJE kompajliran** (nema .NET SDK). Korisnik lokalno: `dotnet restore` (povuče NetVips — ako verzija prigovori, `dotnet add src/WediFrame.Infrastructure package NetVips` + `NetVips.Native`) → `dotnet build` → `dotnet ef migrations add AddThumbnailStatus --project src/WediFrame.Infrastructure --startup-project src/WediFrame.Api` → `database update` → `dotnet run` → poslati fotku na `/e/{token}` → za ~15 s grid pokaže thumbnail; HEIC s iPhonea dobije prikaz → commit na develop. **Sljedeći korak:** mobilni test guest stranice na stvarnom uređaju (iOS Safari, Android Chrome, IG/WhatsApp webview) — zadnja stavka prije zatvaranja M1/M2; zatim host galerija (hide/delete) ili multipart video.
- **2026-08-10 (v12)** — v11 (thumbnaili) commitan (na gitu nazvan "WEDIFRAME V12"). **Runda mobilnog testa** na stvarnom uređaju otkrila i riješila lanac problema: (1) thumbnail worker padao na R2 `STREAMING-...-PAYLOAD not implemented` → `DisablePayloadSigning=true` na `PutObject`; (2) NetVips build error `strip` → `keep: Enums.ForeignKeep.None`; (3) "Add Photos" mrtav jer Next 16 blokira `/_next/*` preko LAN IP-a (klijentski JS se ne učita) → `allowedDevOrigins` (dev-only) + API na `0.0.0.0`; (4) hydration noise od Dark Readera → `suppressHydrationWarning`; (5) `hidden`→`sr-only` na file inputu; (6) stari guest link vraćao 404 jer je token zastario nakon DB promjena → koristiti svjež event. **Rezultat:** guest upload radi na mobitelu, thumbnaili se generiraju. Sve pod (1)–(5) commitano na `develop`. **Sljedeći korak:** dovršiti mobilni checklist (više fotki, HEIC→thumbnail, loša mreža/retry, IG/WhatsApp webview), zatim odabrati sljedeću frontu — **host galerija (hide/delete)** ili **multipart video**.
- **2026-08-11 (v13)** — Nastavak mobilnog testa: (a) `crypto.randomUUID` rušio odabir datoteke na LAN http-u → `newLocalId` fallback; (b) galerija se učita pa nestane → `Gallery` `setItems` updater bio nečist (Strict Mode dvostruki poziv) → pure updater (dedup prema `prev`), commitano kao `fix Gallery`. Zatim isporučena **HEIC→JPEG konverzija u browseru** (`heic-to`/libheif WASM, dynamic import): u `uploadOne` se HEIC/HEIF prije presigna pretvori u JPEG (`heicToJpegFile`), status **"preparing"**, pa normalan upload; optimistic preview sad renderira jer je JPEG. i18n: `itemPreparing` + `errors.prepareFailed` (HR/EN); `heic-to@^1.5.2` u `web/package.json`. Frontend `tsc` čist; ESLint bez novih grešaka (ostaju 2 zatečena v9 warninga: setState-in-effect, `<a href="/privacy">`). **Sljedeći korak:** korisnik `npm install` + test HEIC uploada na mobitelu (mora se prikazati kao JPEG, s thumbnailom), pa dovršiti mobilni checklist (webview) i odabrati sljedeću frontu — **host galerija (hide/delete)** ili **multipart video**.
- **2026-08-11 (v14)** — Isporučen **multipart video upload** (zadnji veliki dio M1). Backend: `IObjectStorage` + R2 dobili multipart (Create/PresignUploadPart/Complete/Abort), `VideoRules` (2 GB, 16 MB dijelovi, mp4/mov/webm), `MediaItem.MultipartUploadId` (+config, **traži migraciju `AddMultipartUploadId`**), DTO-ovi, tri guest endpointa (`POST /guest/{token}/videos`, `.../complete`, `.../abort`). Frontend: `guestApi` (init/completu/abort + `putPartToStorage` koji čita ETag), `UploadSection` grananje na `uploadVideo` (init → PUT dijelovi s progressom i retryjem → complete; abort na grešci), `accept="image/*,video/*"`, limit 2 GB za video; `Gallery` lightbox pušta `<video>` (grid = play badge). i18n `mediaHint`. Frontend `tsc` čist, nema novih ESLint grešaka. **Backend NIJE kompajliran** (nema .NET SDK). Korisnik lokalno: `dotnet build` → `dotnet ef migrations add AddMultipartUploadId ...` → `database update`; **u R2 CORS dodati `ExposeHeaders: ["ETag"]`** (uz PUT/GET i LAN origin za test). Test: poslati kraći video s mobitela → progress po dijelovima → pojavi se u galeriji, klik pušta video. **Sljedeći korak:** dovršiti mobilni checklist (webview, veći video na lošoj mreži), pa **host galerija (hide/delete)** ili zatvaranje M1/M2 i prelazak na M3 (paketi/limiti/plaćanje).
- **2026-08-11 (v15)** — Odabir korisnika: graditi host dashboard od nule. Isporučen **temelj host frontenda**: `web/src/lib/hostApi.ts` (register/login/refresh/logout, `authFetch` s Bearer + tihi refresh na 401, `listEvents`/`createEvent`), stranice `/login`, `/register`, `/dashboard` (lista evenata s copy guest-linka i statusom, logout), `/dashboard/events/new` (naslov + datum T0). i18n namespace-ovi `auth`/`dashboard`/`newEvent` (HR/EN). Brend boje usklađene s guest stranicom. Frontend `tsc` + ESLint čisti (novi fajlovi). `next build` u sandboxu pao samo na dohvatu Google Fonta (nema neta) — nije greška koda. **Nema backend promjena** (koristi postojeće Identity/Events endpointe). **Sljedeći korak:** detalj eventa (`/dashboard/events/{id}`: QR + link + cover + postavke), zatim **galerija s upravljanjem (hide/delete)** — za to trebaju i backend host media endpointi (`GET /events/{id}/media`, `PATCH …/{mediaId}`, `DELETE …`).
- **2026-08-11 (v16)** — Nakon v15 testa: novokreirani event davao guest 404 (bio Draft, negostovidljiv) i "Kopiraj link" nije radio (clipboard u non-secure kontekstu). Dodano: backend `POST /events/{id}/activate` (Free, Draft→Active, idempotentno; **bez migracije**), `hostApi.activateEvent`, dashboard kartica sad za Draft prikazuje "Aktiviraj" (pa se link pojavi kad postane Active), copy s `execCommand` fallbackom. i18n `dashboard.activate/activating/draftHint/activateError`. TSC+ESLint čisti. **Sljedeći korak:** detalj eventa (QR/cover/postavke) pa galerija s upravljanjem (hide/delete) — uz backend host media endpointe.
- **2026-08-11 (v17)** — Isporučen **detalj eventa** (`/dashboard/events/[id]`): naslov+status, **cover fotografija** (upload/replace kroz presigned PUT + confirm, JPEG/PNG/WebP ≤20 MB), **QR kod** (dohvat s Bearerom → blob → prikaz + download PNG), **guest link** (copy s fallbackom + otvori), te aktivacija za Draft. Dashboard kartica sad linka naslov na detalj. `hostApi`: `getEvent`, `getQrPng`, `startCoverUpload`/`confirmCover`. i18n `eventDetail` (HR/EN). Frontend-only, bez migracije. TSC+ESLint čisti. **Sljedeći korak:** galerija s upravljanjem (hide/delete) — backend host media endpointi (`GET /events/{id}/media`, `PATCH …/{mediaId}` visibility, `DELETE …` soft-delete) + `IHostEventAccess` + stranica `/dashboard/events/{id}/gallery` koja reusa grid.
- **2026-08-12 (v18)** — Isporučena **host galerija s upravljanjem** (zatvara zadnji dio host trake u M2). **Backend:** `IHostEventAccess`+`HostEventAccess` (Events modul, provjera vlasništva — zrcalo `IGuestEventAccess`), host media endpointi (Media modul, `/events/{id}/media`, JWT+ownership): `GET` (potvrđene stavke uklj. **skrivene**, paginacija/order kao guest), `PATCH …/{mediaId}` (hide/unhide, idempotentno, audit), `DELETE …/{mediaId}` (soft-delete, audit; R2 ostaje do M4 grace), `HostMediaContracts`. **Bez migracije** (`Visibility`/`SoftDeletedAt` postoje). **Frontend:** izdvojen zajednički grid/lightbox u `components/media/MediaGallery.tsx` (guest `Gallery` refaktoriran da ga koristi — vizualno identičan, −175 linija duplikata; lightbox dobio opcionalni `actions` slot), nova `components/host/HostGallery.tsx` (grid + lightbox + hide/unhide/delete s inline potvrdom, optimistički update, skrivene zatamnjene + eye-off badge), stranica `/dashboard/events/[id]/gallery`, `hostApi` (`getHostMedia`/`setMediaVisibility`/`deleteMedia`), link "Upravljaj galerijom" na detalju eventa. i18n `hostGallery` + `eventDetail.manageGallery` (HR/EN). Frontend `tsc`+ESLint čisti na svim mojim fajlovima (zatečene 4 ESLint greške u `UploadSection.tsx` iz v17 nisu dirane — pre-postojeće). **Backend NIJE kompajliran** (nema .NET SDK). Korisnik lokalno: `dotnet build` → `dotnet run --project src/WediFrame.Api` (nema migracije) + `npm run dev` u `web/`; smoke test: aktivan event → gost uploada par fotki → dashboard → klik naslova → "Upravljaj galerijom" → sakrij/prikaži/obriši. **Sljedeći korak:** dovršiti M2 (download pojedinačne datoteke + ZIP export kao background job; ponašanje nakon isteka upload perioda) ili prijelaz na M3 (paketi/limiti/plaćanje).
- **2026-08-12 (v19)** — Isporučen **download pojedinačne datoteke** (M2). **Backend:** `IObjectStorage.PresignDownloadAsync` (+R2 impl s `ResponseHeaderOverrides.ContentDisposition`, sanitizacija imena), helper `MediaDownloadName` (ekstenzija iz content-typea → `wediframe-<id8>.<ext>`), kontrakt `MediaDownloadResponse`, host endpoint `GET /events/{id}/media/{mediaId}/download` (ownership; bilo koja vidljivost, ne soft-deleted) i guest `GET /guest/{token}/media/{mediaId}/download` (samo Visible/Confirmed). **Bez migracije.** **Frontend:** `MediaLightbox` dobio download gumb u gornjoj traci (`onDownload`/`downloading` + `triggerBrowserDownload` helper), `guestApi.getGuestMediaDownloadUrl` + `hostApi.getMediaDownloadUrl`, wiring u guest `Gallery` i `HostGallery`, i18n `download` u `hostGallery` i `guest.gallery` (HR/EN). Frontend `tsc`+ESLint čisti na svim mojim fajlovima. **Backend NIJE kompajliran** (nema .NET SDK). Uz to: **pravilo isporuke** (ZIP sa strukturom / patch) trajno zapisano u BACKLOG (vrh) i PROJECT §7. Korisnik lokalno: `dotnet build` → `dotnet run` (nema migracije) + `npm run dev`; test: otvori sliku/video u lightboxu (gost i host) → klik download → datoteka se spremi s urednim imenom. **Sljedeći korak:** ZIP export cijele galerije kao background job (M2), pa ponašanje nakon isteka upload perioda (button → "Upload je završen", galerija ostaje), ili prijelaz na M3 (paketi/limiti/plaćanje).
