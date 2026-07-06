# WediFrame (by EverFrame) — Kontekst projekta

> **Namjena datoteke:** Ovo je glavni kontekst za AI asistenta (Claude/Sonnet) i podsjetnik za vlasnika projekta.
> Pročitaj ovu datoteku na početku svake sesije. Uz nju idu `ARCHITECTURE.md` i `BACKLOG.md`.
> **Zadnje ažurirano:** 2026-07-06 (v2)

---

## 1. Što gradimo

**WediFrame** (wediframe.hr) je web/PWA platforma za prikupljanje i dijeljenje fotografija i videa s vjenčanja.
**EverFrame** je brend/firma vlasnik — aplikacija nosi oznaku "Powered by EverFrame", ali se prema korisnicima u potpunosti brendira kao WediFrame.

**Core flow:**
1. Mladenci (host) kreiraju event, odaberu **datum početka uploada** i kupe paket (ili uzmu Free/Trial).
2. Dobiju privatni link + QR kod (za tisak na stolove, pozivnice...).
3. Gosti skeniraju QR → otvara se PWA u browseru → **bez instalacije i bez registracije** uploadaju slike/video.
4. **Svi gosti vide cijelu galeriju** — upload nije vezan za račun; pristupni link je jedini ključ.
5. Sadržaj ide u Cloudflare R2, prikazuje se u privatnoj galeriji.
6. Upload je moguć samo unutar **upload perioda** paketa; pohrana traje do isteka **retencije** paketa, zatim automatsko brisanje.

**Guest landing stranica (definiran layout):** naslovna (cover) fotografija koju biraju mladenci, preko/ispod nje naslov eventa (npr. "Iva i Ivan, 23.6.2027."), ispod toga button za upload, ispod buttona galerija s thumbnailovima (foto i video).

**Dugoročna vizija:** EverFrame platforma za rođendane, krštenja, team buildinge, ture, cruise/sailing evente. Arhitektura event-type agnostična (WediFrame = tema/branding + defaulti, ne poseban kod). Fokus do MVP-a: isključivo vjenčanja i HR tržište, uz spremnost za širenje (i18n, multi-currency).

## 2. Tech stack (odlučeno)

| Sloj | Izbor | Napomena |
|---|---|---|
| Backend | .NET 10 / ASP.NET Core Web API | Modularni monolit |
| ORM | EF Core — **Code First + migracije** | Odlučeno 2026-07-06 |
| Baza | **PostgreSQL** | |
| Frontend | Next.js, mobile-first, PWA | Guest stranica je najvažniji ekran u proizvodu |
| Storage | Cloudflare R2, **EU jurisdiction** | Nula egress naknada |
| Upload | Presigned URL, direktno browser → R2; **multipart za velike videe** | API NIKAD ne streama datoteke |
| Plaćanje | Stripe + **HR fiskalizacija i R1 račun** (Fiskalizacija 2.0 — detalji u M3). R1 flow: checkbox "Trebam R1" u checkoutu → naziv firme, OIB, adresa | Multi-currency spremnost za kasnije |
| Hosting | API: Railway (EU) · Baza: Neon/Railway PostgreSQL (EU) · Frontend: Vercel | Kasnije po potrebi: Render/Fly/Azure, managed PG, Cloudflare Pages |
| Lokalizacija | HR + EN u MVP-u; **kasnije dodatni jezici (srpski i dr.)** — svi stringovi kroz i18n ključeve od prvog dana, nikad hardkodirani; datumi/valute kroz locale API-je | HR default |
| Domena | **wediframe.hr** | wediframe.com trenutno nedostupan — pratiti/backorder |

## 3. Paketi i monetizacija

Naplata **po eventu**, jednokratno. Službeni paketi (izvor: vlasnik, 2026-07-06):

| Paket | Cijena | Foto limit | Video (ukupno) | Ukupni upload | Upload period | Pohrana (retencija) |
|---|---|---|---|---|---|---|
| Free / Trial | 0 € | 50 fotki | do 50 MB (ukupno) | 250 MB | 2 dana | 7 dana |
| Essential / Osnovni | 25 € | 500 fotki | do 5 GB | 10 GB | 1 mjesec | 3 mjeseca |
| Classic / Klasični | 40 € | 1.500 fotki | do 15 GB | 20 GB | 2 mjeseca | 12 mjeseci |
| Premium | 80 € | 5.000 fotki | do 40 GB | 50 GB | 4 mjeseca | 12 mjeseci |
| Brzi i žestoki | 50 € | 5.000 fotki | do 40 GB | 50 GB | 2 tjedna | 2 mjeseca |

Svi limiti su **ukupni** po eventu. Uz to: **max 2 GB po pojedinačnoj video datoteci** (potvrđeno 2026-07-06).

**Vremenska semantika (odlučeno):** T0 = datum početka uploada, bira ga host pri kreiranju eventa.
- Upload moguć: od T0 do T0 + upload period paketa.
- Galerija dostupna (pregled/download): do T0 + retencija paketa.
- Nakon isteka retencije: soft delete → grace period (~7 dana) → fizičko brisanje.

**Video:** bez limita trajanja. Limit veličine **2 GB po datoteci** (potvrđeno) + ukupni video/storage limiti paketa. "Do ~10 min" se može prikazati kao UX preporuka gostu, bez tehničkog enforcementa trajanja.

**Partnerski kanal:** fotografi, snimatelji, sale, wedding planneri, event organizatori.
- Partner dobiva bonus kodove → daje ih mladencima → mladenci dobiju popust → partner dobiva proviziju.
- MVP: kodovi s atribucijom + osnovni izvještaj. Isplate provizija ručno.
- Vlasnik ima razrađen plan partnerstava — detalje tražiti kad zatreba, ne izmišljati.

## 4. GDPR i privatnost (obavezno, ne "nice to have")

- **Uloge:** EverFrame/WediFrame je voditelj obrade za podatke hostova (kupaca); izvršitelj obrade za sadržaj gostiju (u ime hosta). Jasno u ToS/Privacy Policy.
- **Privacy notice pri uploadu:** dvije rečenice + link na policy, vidljivo prije prvog uploada gosta.
- **Retencija po paketu** + automatsko brisanje (background job), grace period prije fizičkog brisanja.
- Host može obrisati pojedini sadržaj ili cijeli event u bilo kojem trenutku (right to erasure).
- Podaci u EU: R2 EU jurisdiction, Railway EU, Neon EU (Frankfurt).
- Minimalni podaci o gostima: samoprijavljeno ime (opcionalno) — bez računa, bez emaila gosta.
- ToS jasno kaže: čemu služi pohrana, kad se briše, i **što se NE radi** s podacima (nema treniranja, nema prodaje, nema javnog dijeljenja).

## 5. MVP opseg

**U MVP-u:**
- Registracija/prijava hosta, kreiranje eventa (naziv, datum početka uploada, cover fotografija), odabir paketa
- Free/Trial aktivacija bez plaćanja; plaćeni paketi kroz Stripe (+ HR fiskalizacija/R1)
- Privatni link + QR kod
- Guest stranica: cover + naslov → upload button → galerija thumbnailova; upload bez registracije, pouzdan uz retry, multipart za video
- Galerija vidljiva svim gostima s linkom; host upravlja (hide/delete)
- Download (pojedinačno; ZIP svega kao background job)
- Limiti po paketu (foto count, video GB, ukupni GB, upload period) — enforcement na backendu
- Retencija + auto-brisanje
- Bonus kodovi za partnere (popust + atribucija)
- Osnovni interni admin
- HR/EN lokalizacija; privacy notice, ToS, Privacy Policy

**Izvan MVP-a (svjesno):** live slideshow za projektor, AI moderacija, komentari/lajkovi, video transcoding, native aplikacije, partner self-service portal, automatske isplate provizija, ostale vertikale, upsell produljenja retencije.

## 6. Ključni rizici (držati na oku)

1. **Pouzdanost uploada na dan eventa** — loš WiFi, stotine istovremenih gostiju, veliki videi. Retry, multipart, background upload, jasan status. Ovo je proizvod.
2. **Trošak i veličina videa** — bez transcodinga; limit po datoteci + kvote paketa.
3. **Vršno opterećenje** — bursty promet (subota navečer); presigned upload direktno na R2 nosi glavninu.
4. **GDPR i fiskalizacija** — riješeno dizajnom od početka, ne naknadno.
5. **Otvoreni link = otvorena galerija** — svatko s linkom vidi sve i može uploadati. Mitigacije: neprebrojiv token, rotacija tokena, rate limiting. Host mora biti svjestan (jedna rečenica u UI-ju).

## 7. Kako raditi na projektu (upute za Claude/Sonnet)

- **Jezik:** komunikacija s korisnikom na hrvatskom; kod, komentari, imena u bazi i API-ju na engleskom. Dokumentacija na hrvatskom osim tehničkih pojmova.
- **Iterativno, mali koraci.** Korisnik nema stabilan raspored. Svaki radni blok malen, samostalan, završiv u jednoj sesiji. Nikad tri fronte odjednom.
- **Backlog je izvor istine.** Na kraju svake sesije ažurirati `BACKLOG.md`: što je napravljeno, što je sljedeće, nove odluke u Decision Log. Kad korisnik pita "što je u backlogu" — pročitati i sažeti datoteku, ne rekonstruirati iz sjećanja.
- **Odluke se bilježe** u Decision Log s datumom i obrazloženjem.
- **Ne izmišljati produktne detalje.** Cijene i limiti su u ovom dokumentu; uvjete partnerstva i ostalo pitati korisnika.
- **Pitati kad je nejasno, ali ne blokirati.** Sitno pitanje → predložiti default, označiti kao pretpostavku, nastaviti.
- **Kad stigne Git repo:** čitati stvarno stanje koda prije prijedloga; ove datoteke žive u repou u `/docs`.
- **Kvaliteta:** moderan, intuitivan, mobile-first UI. Guest flow mora raditi jednom rukom, na suncu, nakon tri gemišta.

## 8. Otvorena pitanja

Aktualna lista se održava u `BACKLOG.md`.
