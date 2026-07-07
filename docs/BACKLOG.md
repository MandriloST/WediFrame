# WediFrame — Backlog

> **Pravila:** Ažurira se na kraju svake radne sesije. Statusi: `[ ]` todo, `[~]` u tijeku, `[x]` gotovo, `[!]` blokirano.
> Redoslijed unutar milestonea = prioritet. Ništa se ne briše (gotovo ostaje radi povijesti).
> **Zadnje ažurirano:** 2026-07-07 (v4)

---

## Otvorena pitanja (čekaju korisnika)

- **Link na GitHub repo** — podijeliti u chatu da Claude može čitati stvarno stanje koda prije prijedloga (spomenut push + develop branch, ali link još nije dostavljen).

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

---

## M0 — Temelji projekta (prije koda)

- [x] Definiran koncept, MVP opseg, arhitektura (PROJECT.md, ARCHITECTURE.md) — 2026-07-04
- [x] Razriješena ključna otvorena pitanja (stack, baza, paketi, retencija, hosting, brend) — 2026-07-06
- [x] Plan paketa unesen u dokumentaciju — 2026-07-06
- [ ] Korisnik: registrirati wediframe.hr — **odluka korisnika 2026-07-07: registracija pri kraju projekta** (Claude preporučio odmah zbog rizika zauzeća; korisnik svjestan, ostaje njegova odluka)
- [x] Kreirati Git repo, dodati /docs — 2026-07-07 (push napravljen, `develop` branch za razvoj; link repoa još nije podijeljen u chatu)
- [x] Postaviti solution strukturu (.NET modularni monolit skeleton, EF Core Code First + prva migracija) — 2026-07-07: build prolazi, migracija `InitialCreate` primijenjena na lokalni PG (`shared.audit_log`); riješeni NU1903 (pin ranjivih transitivnih paketa + NoWarn NU1902/NU1903) i NU1605 (EF paketi na 10.0.4)
- [x] Postaviti Next.js projekt (mobile-first, i18n skeleton, PWA manifest) — 2026-07-07: `web/` u monorepou; Next 16 + next-intl (HR default bez prefiksa, /en), manifest + placeholder ikone, landing placeholder; build/lint/smoke test prošli — **čeka commit korisnika**
- [ ] Cloudflare račun + R2 bucket (EU jurisdiction), Stripe test račun, Railway + Neon (EU) projekti — korisnik (može i tijekom M1; R2 treba prije Media flowa)

## M1 — Event + guest upload (srce proizvoda)

- [ ] Identity: registracija/prijava hosta (minimalno)
- [ ] Events: kreiranje eventa (naslov, T0, draft), guest token, QR generiranje
- [ ] Cover fotografija: upload (host) + prikaz na guest stranici
- [ ] Media: presigned upload flow — single PUT za fotke
- [ ] Media: multipart upload za video (chunk retry, resume, cleanup nedovršenih)
- [ ] Guest stranica `/e/{token}`: cover + naslov → upload button → upload UI s napretkom i statusima
- [ ] Privacy notice + opcionalni unos imena gosta (HR/EN)
- [ ] Backend enforcement: tip, veličina po datoteci, upload period aktivan
- [ ] Test na stvarnim mobitelima: iOS Safari, Android Chrome, Instagram/WhatsApp webview
- [ ] **Milestone test:** 20 datoteka (uklj. 1 velik video) s mobitela na lošoj mreži — sve stigne, status jasan

## M2 — Galerija

- [ ] Guest galerija na `/e/{token}` (ispod upload buttona): grid thumbnailova, lazy load, lightbox
- [ ] Thumbnail generiranje za fotke (background job)
- [ ] Video: poster frame ako izvedivo bez transcodinga, inače generička pločica; reprodukcija originala (range requests) — provjeriti HEVC/H.264 pokrivenost u browserima
- [ ] Host galerija: isto + hide/delete
- [ ] Download pojedinačne datoteke; ZIP export kao background job
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

## Dnevnik sesija

- **2026-07-04** — Inicijalna analiza, kreirani PROJECT.md / ARCHITECTURE.md / BACKLOG.md.
- **2026-07-06** — Korisnik odgovorio na svih 9 pitanja; ažurirane sve tri datoteke. **Sljedeći korak:** Git repo + /docs, zatim .NET i Next.js skeleton (M0).
- **2026-07-06 (v2)** — Zatvorena preostala 4 pitanja. Backlog bez otvorenih pitanja.
- **2026-07-06 (v3)** — Isporučen .NET skeleton (slnx, Api host, Shared kernel, Infrastructure, 7 modula).
- **2026-07-07 (v4)** — .NET skeleton **potvrđen kod korisnika**: riješeni NU1903 (pin + NoWarn) i NU1605 (EF 10.0.4), build prolazi, `InitialCreate` migracija primijenjena na lokalni PG. Repo pushan, `develop` branch kreiran (link još nije podijeljen). Isporučen **Next.js skeleton** u `web/`: Next 16 + TS + Tailwind 4, next-intl (HR default bez prefiksa, /en), PWA manifest + placeholder ikone, landing placeholder sa svim stringovima kroz i18n ključeve; build, lint i smoke test (HR/EN/manifest) verificirani u sesiji. **Sljedeći korak:** korisnik commita `web/` na develop + podijeli repo link; zatim M1 — Identity (registracija/prijava hosta) ili Events (kreiranje eventa + token + QR), preporuka: Identity prvi jer Events ovisi o njemu.
