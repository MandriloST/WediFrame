# WediFrame — web (Next.js)

Mobile-first PWA frontend. Kontekst projekta: [`/docs`](../docs/) u rootu repoa.

## Pokretanje

```bash
cd web
npm install
npm run dev     # http://localhost:3000
```

## i18n (next-intl)

- **HR je default** — bez URL prefiksa (`/`). Engleski na `/en`.
- Svi stringovi žive u `src/messages/{hr,en}.json` — **nikad hardkodirani u komponentama**.
- Novi jezik kasnije (npr. `sr`): dodaj u `src/i18n/routing.ts` + kreiraj `src/messages/sr.json`.
- Link/redirect/useRouter uvijek importati iz `@/i18n/navigation`, ne iz `next/*`.

## PWA

- Manifest: `src/app/manifest.ts` → `/manifest.webmanifest`.
- Ikone u `public/icons/` su **placeholderi** — zamijeniti pravim logotipom (M5).
- Service worker / offline fallback dolazi u M5 (backlog).

## Struktura

```
src/
  app/
    [locale]/         — sve stranice (layout + landing placeholder)
    manifest.ts       — PWA manifest
    globals.css
  i18n/               — routing, request config, navigacija
  messages/           — hr.json, en.json
  middleware.ts       — locale detekcija
```

Guest stranica `/e/{token}` dolazi u M1 (srce proizvoda).
