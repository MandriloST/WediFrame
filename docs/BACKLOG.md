# WediFrame — Backlog

> **Pravila:** Ažurira se na kraju svake radne sesije. Statusi: `[ ]` todo, `[~]` u tijeku, `[x]` gotovo, `[!]` blokirano.
> Redoslijed unutar milestonea = prioritet. Ništa se ne briše (gotovo ostaje radi povijesti).
> **Zadnje ažurirano:** 2026-07-06 (v2)

---

## Otvorena pitanja (čekaju korisnika)

*(trenutno nema otvorenih pitanja)*

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
- [ ] Korisnik: registrirati wediframe.hr (.com nedostupan — pratiti/backorder)
- [ ] Kreirati Git repo, dodati /docs s ova tri .md fajla, podijeliti link u chatu
- [ ] Postaviti solution strukturu (.NET modularni monolit skeleton, EF Core Code First + prva migracija) — bez feature koda
- [ ] Postaviti Next.js projekt (mobile-first, i18n skeleton, PWA manifest)
- [ ] Cloudflare račun + R2 bucket (EU jurisdiction), Stripe test račun, Railway + Neon (EU) projekti

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
- [ ] Landing stranica (WediFrame brending, "Powered by EverFrame")
- [ ] PWA fino: manifest, ikone, offline fallback poruka
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

## Dnevnik sesija

- **2026-07-04** — Inicijalna analiza, kreirani PROJECT.md / ARCHITECTURE.md / BACKLOG.md.
- **2026-07-06** — Korisnik odgovorio na svih 9 pitanja; ažurirane sve tri datoteke (Code First, PostgreSQL, paketi, T0 semantika, hosting, fiskalizacija, brend, guest layout). Ostala 4 manja otvorena pitanja. **Sljedeći korak:** Git repo + /docs, zatim .NET i Next.js skeleton (M0).
- **2026-07-06 (v2)** — Zatvorena preostala 4 pitanja (limiti ukupni, video 2 GB/datoteka, .com nedostupan, R1 flow) + napomena o budućoj višejezičnosti. Backlog bez otvorenih pitanja. **Sljedeći korak:** korisnik kreira Git repo i stavlja /docs; zatim skeleton (.NET modularni monolit + Next.js).
