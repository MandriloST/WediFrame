# WediFrame — Arhitektura (high-level, bez koda)

> Živi dokument. Mijenja se kroz Decision Log u `BACKLOG.md`.
> **Zadnje ažurirano:** 2026-07-06 (v2)

## 1. Pregled sustava

```
Gost (mobitel, browser/PWA)          Host (mladenci)              Admin (interno)
        │                                  │                            │
        ▼                                  ▼                            ▼
┌────────────────── Next.js frontend (PWA, Vercel) ─────────────────────────────┐[BACKLOG.md](https://github.com/user-attachments/files/29706488/BACKLOG.md)

│  guest stranica (cover+upload+galerija)   host dashboard      interni admin   │
└───────────────┬────────────────────────────────────────────────────────────────┘
                │ REST (JSON)
                ▼
┌───── ASP.NET Core Web API (.NET 10, modularni monolit, Railway EU) ───────────┐
│ Identity │ Events │ Media │ Billing │ Partners │ Retention │ Admin │ Shared   │
└─────┬──────────────────────────┬──────────────────────────────┬───────────────┘
      │                          │ presigned URL (PUT/multipart)│ background jobs
      ▼                          ▼                              ▼
 PostgreSQL (Neon EU)     Cloudflare R2 (EU jurisdiction)  Retention/ZIP/thumb workeri
```

**Ključni princip:** datoteke NIKAD ne prolaze kroz API. Browser uploada direktno na R2 presigned URL-om (multipart za velike videe); API upravlja samo metadata i autorizacijom. Download/galerija idu preko potpisanih, kratkotrajnih R2 URL-ova (video uz range requeste za streaming).

## 2. Moduli backenda (modularni monolit)

Svaki modul = vlastiti folder/projekt, vlastiti entiteti, komunikacija kroz jasne interfejse ili domenske evente; zajednička PostgreSQL baza s odvojenim shemama po modulu. **EF Core Code First s migracijama** — shema verzionirana u Gitu.

| Modul | Odgovornost |
|---|---|
| **Identity** | Registracija/prijava hosta, JWT/session, uloge (Host, Admin). Gosti NEMAJU račun — autorizacija event tokenom. |
| **Events** | Životni ciklus eventa (draft → active → upload-closed → expired → deleted), event token/QR, postavke (naslov, datum početka uploada, cover fotografija), event-type (wedding sada). |
| **Media** | Presigned upload flow (single PUT za fotke, multipart za video), metadata, thumbnaili (background), galerija, hide/delete, enforcement limita paketa (foto count, video bytes, total bytes, upload period). |
| **Billing** | Paketi (limiti kao podaci u bazi), Free/Trial aktivacija bez plaćanja, Stripe checkout + webhook, bonus kod u checkoutu, **HR fiskalizacija + R1 podaci na računu** (integracija se definira u M3), status plaćanja. |
| **Partners** | Partneri, bonus kodovi, atribucija iskorištenja, izvještaj po partneru. |
| **Retention** | Job: kraj upload perioda (zatvori upload) → podsjetnik hostu prije isteka retencije → soft delete → grace period → fizičko brisanje R2 + metadata. |
| **Admin** | Interni pregled: eventi, korisnici, storage potrošnja, ručne intervencije (produljenje retencije...). |
| **Shared/Kernel** | Lokalizacija, audit log, R2 klijent, email, zajednički tipovi. |

## 3. Model podataka (glavni entiteti, high-level)

- **User** — host ili admin; email, hash, uloga, jezik.
- **Event** — vlasnik (User), tip (wedding), **naslov** (npr. "Iva i Ivan, 23.6.2027."), **uploadStartDate (T0, bira host)**, **coverPhoto key**, status, guest access token (dugačak, neprebrojiv), veza na Purchase/Package; izvedeno iz paketa: uploadEndsAt = T0 + upload period, expiresAt = T0 + retencija.
- **Package** — naziv (Free, Essential, Classic, Premium, Brzi i žestoki), cijena, maxPhotoCount, maxVideoTotalBytes, maxTotalBytes, maxFileBytes (po datoteci; video **2 GB**, potvrđeno), uploadPeriodDays, retentionDays, aktivan/arhiviran. **Limiti su podaci u bazi, ne hardkodirani.**
- **Purchase** — event, paket, iznos, valuta (EUR; multi-currency spremno), Stripe referenca, primijenjeni BonusCode, status; podaci za **R1 račun** (checkbox u checkoutu → naziv firme, OIB, adresa) i referenca fiskalizacije.
- **MediaItem** — event, tip (photo/video), R2 object key, veličina, mime, guest ime (samoprijavljeno, opcionalno), upload status (pending/confirmed/failed), visibility (visible/hidden), soft-delete timestamp, thumbnail key.
- **Partner** — naziv, tip (fotograf/sala/planner...), kontakt.
- **BonusCode** — partner, kod, tip popusta, max iskorištenja, rok, iskorištenja (veza na Purchase).
- **AuditLog** — tko, što, kad (brisanja, admin akcije, promjene postavki).

## 4. API površina (skica, REST, /api/v1)

### Auth (host)
- `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `GET /auth/me`

### Events (host, autenticiran)
- `POST /events` — kreiraj (draft): naslov, T0, tip
- `GET /events`, `GET /events/{id}`, `PATCH /events/{id}`
- `POST /events/{id}/cover` — presigned upload cover fotografije + potvrda
- `POST /events/{id}/activate` — aktivacija (Free odmah; plaćeni nakon potvrde plaćanja)
- `GET /events/{id}/qr` — QR (PNG/SVG) za guest link
- `POST /events/{id}/token/rotate` — novi guest token (ako link procuri)
- `DELETE /events/{id}` — soft delete cijelog eventa
- `GET /events/{id}/stats` — broj datoteka, potrošeni storage/limiti, po danu

### Media — host perspektiva
- `GET /events/{id}/media` — galerija (paginacija, filter tip/gost/status)
- `PATCH /events/{id}/media/{mediaId}` — hide/unhide
- `DELETE /events/{id}/media/{mediaId}`
- `POST /events/{id}/export` — ZIP (background job) → `GET /events/{id}/export/{jobId}` status + link

### Guest (bez računa, sve preko event tokena)
- `GET /guest/{token}` — event info: naslov, cover URL, privacy notice, limiti, status upload perioda
- `POST /guest/{token}/uploads` — zatraži presigned URL(ove); backend validira tip/veličinu/limite/upload period → URL + mediaId (za velike videe: multipart init → part URL-ovi → complete)
- `POST /guest/{token}/uploads/{mediaId}/confirm` — potvrda; backend verificira objekt na R2
- `GET /guest/{token}/media` — galerija (svi gosti vide sve — odluka 2026-07-06)

### Billing
- `GET /packages` — javno, za pricing stranicu
- `POST /events/{id}/checkout` — Stripe checkout session (+ opcionalni bonus kod; + checkbox "Trebam R1" → firma, OIB, adresa)
- `POST /billing/validate-code` — provjera bonus koda prije checkouta
- `POST /webhooks/stripe` — potvrda plaćanja → fiskalizacija računa → aktivacija eventa

### Partners / Admin (interno)
- `POST /admin/partners`, `POST /admin/partners/{id}/codes`, `GET /admin/partners/{id}/report`
- `GET /admin/events`, `GET /admin/users`, `GET /admin/storage-report`
- `POST /admin/events/{id}/extend-retention`

**Napomene:** guest token dugačak random string, rate limiting po tokenu i IP-u, nikad sekvencijalni ID-jevi u guest URL-ovima; lokalizirane poruke; idempotencija na confirm i webhook rutama.

## 5. Ključni flowovi

### Upload (najvažniji u proizvodu)
1. Gost otvori `/e/{token}` → cover + naslov, privacy notice (2 rečenice + link), "Dodaj slike/video" button, ispod galerija thumbnailova.
2. Odabere datoteke → frontend traži presigned URL-ove; backend validira (tip, veličina po datoteci, foto count, video/total kvota, je li upload period aktivan).
3. Fotke: single PUT. Video: multipart (chunkovi ~50–100 MB, retry po chunku, nastavak nakon prekida).
4. Upload direktno na R2 s prikazom napretka; nastavlja u pozadini dok gost bira dalje. Jasan status "N poslano / M čeka / K nije uspjelo (pokušaj ponovno)".
5. `confirm` → backend provjeri objekt, označi confirmed, enqueue thumbnail job (za video: poster frame — izvedivost bez transcodinga provjeriti; fallback generička video pločica).
6. Pending bez confirma stariji od X sati → cleanup job (uklj. nedovršene multipart uploade na R2).

### Vremenska linija eventa
```
kreiranje → (plaćanje) → aktivan
T0 ────────── upload period ──────────▶ upload zatvoren, galerija i dalje dostupna
T0 ──────────────────── retencija ───────────────────▶ soft delete → grace ~7d → brisanje
```
- Prije isteka retencije: email hostu (podsjetnik + ZIP download link).

### Kupnja
- Free/Trial: aktivacija odmah, bez Stripea.
- Plaćeni: draft → paket → (bonus kod → validate → popust) → Stripe checkout → webhook → fiskalizacija/R1 račun → aktivacija.

## 6. Frontend struktura (Next.js, mobile-first PWA)

### Javno
- `/` — landing (WediFrame, "Powered by EverFrame" u footeru), kako radi u 3 koraka
- `/pricing` — 5 paketa
- `/terms`, `/privacy`, `/login`, `/register`

### Guest — `/e/{token}` (srce proizvoda, jedna stranica)
1. **Cover fotografija** (full-width hero) + **naslov eventa**
2. **Upload button** (primarni CTA) → picker + upload panel s napretkom/retry
3. **Galerija thumbnailova** (grid, lazy load, lightbox, video s poster/play oznakom)
- Prvi posjet: privacy notice + opcionalni unos imena. Nakon isteka upload perioda button se mijenja u "Upload je završen" (galerija ostaje).

### Host dashboard
- `/dashboard` — lista evenata
- `/dashboard/events/new` — wizard: naslov + T0 + cover → paket (+ bonus kod) → plaćanje/Free → QR/link
- `/dashboard/events/{id}` — statistika + potrošnja limita, QR/link (download za tisak), postavke, token rotacija
- `/dashboard/events/{id}/gallery` — upravljanje (hide/delete, download, ZIP)
- `/dashboard/account`

### Interni admin
- `/admin/...` — minimalno, ne troši dizajn budžet.

### PWA / UX principi
- Nula registracije i instalacije za goste; testirati rano u Instagram/WhatsApp webviewu.
- Upload otporan na prekide (multipart resume); i18n od prvog commita (HR default, EN u MVP-u; dizajn spreman za dodatne jezike — srpski i dr. — kasnije: svi stringovi kroz ključeve, bez teksta u slikama, layout tolerantan na duže prijevode).
- Cover + naslov = tema eventa u MVP-u.

## 7. Sigurnost (sažetak)

- Guest pristup samo tokenom; rotacija dostupna hostu; host upozoren da link = puni pristup galeriji.
- Presigned URL-ovi kratkog trajanja, vezani na točan key i content-type/length.
- Rate limiting na guest rutama; captcha tek ako se pojavi abuse.
- Privatni R2 bucket, sve preko potpisanih URL-ova; audit log brisanja i admin akcija.
- HTTPS svugdje, secrets van repoa, EU regije za sve (Railway EU, Neon EU, R2 EU).
