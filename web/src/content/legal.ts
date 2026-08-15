// Legal content for /privacy and /terms, per PROJECT.md §4 (GDPR).
//
// NACRT / DRAFT: this is a starting draft grounded in the product spec, NOT legal
// advice. It must be reviewed by a lawyer before launch, and the legal-entity
// placeholders ([Naziv obrta] / OIB / contact e-mail) filled in once the obrt is
// registered. Kept as a TS module (not i18n JSON) because the prose is long.

export type LegalSection = { heading: string; paragraphs: string[] };

export type LegalDoc = {
  title: string;
  updatedLabel: string;
  updated: string;
  intro: string;
  sections: LegalSection[];
  disclaimer: string;
  backLabel: string;
};

const UPDATED = "2026-08-15";

// Placeholders to replace once the business entity exists.
const ENTITY_HR = "[Naziv obrta / EverFrame]";
const OIB_HR = "[OIB]";
const EMAIL = "[kontakt e-mail]";

export const legal: Record<"hr" | "en", { privacy: LegalDoc; terms: LegalDoc }> = {
  hr: {
    privacy: {
      title: "Pravila privatnosti",
      updatedLabel: "Zadnje ažurirano",
      updated: UPDATED,
      backLabel: "Natrag na početnu",
      intro:
        `WediFrame (platformu vodi ${ENTITY_HR}, OIB ${OIB_HR}) omogućuje mladencima ` +
        "prikupljanje i dijeljenje fotografija i videa s vjenčanja. Ova pravila objašnjavaju " +
        "koje podatke obrađujemo, zašto, koliko dugo i koja su vaša prava.",
      sections: [
        {
          heading: "1. Uloge u obradi podataka",
          paragraphs: [
            "Za podatke mladenaca (host / kupac) nastupamo kao voditelj obrade.",
            "Za sadržaj koji gosti uploadaju (fotografije i video) nastupamo kao izvršitelj obrade — obrađujemo ga u ime i po uputi hosta koji je organizirao event.",
          ],
        },
        {
          heading: "2. Koje podatke prikupljamo",
          paragraphs: [
            "Host: e-mail, lozinka (spremljena kao kriptografski sažetak), odabrani jezik, podaci o kupnji te podaci za R1 račun (naziv firme, OIB, adresa) ako ih host zatraži.",
            "Gost: samoprijavljeno ime (neobavezno) i sadržaj koji uploada. Gosti nemaju korisnički račun i ne tražimo njihov e-mail.",
          ],
        },
        {
          heading: "3. Svrha i pravna osnova",
          paragraphs: [
            "Podatke obrađujemo radi pružanja usluge (izvršenje ugovora), naplate te ispunjenja zakonskih obveza (izdavanje i fiskalizacija računa).",
          ],
        },
        {
          heading: "4. Koliko dugo čuvamo podatke",
          paragraphs: [
            "Sadržaj eventa (fotografije/video) čuva se do isteka razdoblja pohrane (retencije) odabranog paketa, koje teče od datuma početka uploada. Nakon isteka sadržaj se automatski briše: prvo postaje nedostupan, slijedi kratko razdoblje počeka (~7 dana), a zatim se trajno briše.",
            "Podatke o kupnji i izdanim računima čuvamo onoliko koliko nalažu porezni i računovodstveni propisi.",
          ],
        },
        {
          heading: "5. Gdje se podaci obrađuju",
          paragraphs: [
            "Svi podaci obrađuju se unutar EU: mediji na Cloudflare R2 (EU), aplikacija na Railway (EU), baza podataka na Neon (EU, Frankfurt).",
          ],
        },
        {
          heading: "6. Što NE radimo s vašim podacima",
          paragraphs: [
            "Ne koristimo vaše fotografije, video ni druge podatke za treniranje umjetne inteligencije.",
            "Ne prodajemo vaše podatke i ne dijelimo ih javno.",
          ],
        },
        {
          heading: "7. Vaša prava",
          paragraphs: [
            "Imate pravo na pristup, ispravak i brisanje svojih podataka. Host u svakom trenutku može obrisati pojedini sadržaj ili cijeli event.",
            `Za ostvarivanje prava obratite se na ${EMAIL}.`,
          ],
        },
        {
          heading: "8. Pristupni link eventa",
          paragraphs: [
            "Svatko tko ima pristupni link (ili QR kod) eventa može vidjeti cijelu galeriju i uploadati sadržaj. Host je odgovoran za to s kim dijeli link te ga u svakom trenutku može zamijeniti novim (rotacija tokena).",
          ],
        },
      ],
      disclaimer:
        "Ovo je nacrt dokumenta i ne predstavlja pravni savjet. Prije javnog korištenja dati na pregled pravnom stručnjaku; podaci o pravnom subjektu bit će upisani nakon registracije obrta.",
    },
    terms: {
      title: "Uvjeti korištenja",
      updatedLabel: "Zadnje ažurirano",
      updated: UPDATED,
      backLabel: "Natrag na početnu",
      intro:
        `Korištenjem WediFrame platforme (vodi je ${ENTITY_HR}, OIB ${OIB_HR}) prihvaćate ove uvjete.`,
      sections: [
        {
          heading: "1. O usluzi",
          paragraphs: [
            "WediFrame omogućuje prikupljanje i dijeljenje fotografija i videa s vjenčanja putem privatnog linka i QR koda, bez instalacije aplikacije i bez registracije gostiju.",
          ],
        },
        {
          heading: "2. Račun hosta",
          paragraphs: [
            "Host je odgovoran za točnost podataka pri registraciji i za čuvanje pristupnih podataka svog računa.",
          ],
        },
        {
          heading: "3. Paketi i naplata",
          paragraphs: [
            "Naplata je jednokratna, po eventu. Svaki paket ima svoje limite (broj fotografija, količina videa, ukupni prostor) te trajanje uploada i pohrane.",
            "Plaćanje se obavlja putem Stripe-a. Računi se izdaju i fiskaliziraju u skladu s hrvatskim propisima; R1 račun izdaje se na zahtjev pri plaćanju.",
          ],
        },
        {
          heading: "4. Dopušteni sadržaj",
          paragraphs: [
            "Host i gosti ne smiju uploadati nezakonit sadržaj niti sadržaj koji krši prava trećih osoba. Host je odgovoran za event i sadržaj prikupljen putem svog linka.",
          ],
        },
        {
          heading: "5. Pristupni link",
          paragraphs: [
            "Pristupni link i QR kod omogućuju pristup galeriji svakome tko ih ima. Host upravlja dijeljenjem linka i može ga u svakom trenutku zamijeniti novim.",
          ],
        },
        {
          heading: "6. Pohrana i brisanje",
          paragraphs: [
            "Sadržaj se automatski briše po isteku razdoblja pohrane odabranog paketa. Host može i ranije obrisati pojedini sadržaj ili cijeli event.",
          ],
        },
        {
          heading: "7. Odgovornost",
          paragraphs: [
            "Usluga se pruža „kakva jest”. U mjeri dopuštenoj zakonom, ne odgovaramo za neizravnu štetu nastalu korištenjem usluge.",
          ],
        },
        {
          heading: "8. Izmjene uvjeta",
          paragraphs: [
            "Uvjete možemo povremeno ažurirati. Datum posljednje izmjene naveden je na vrhu.",
          ],
        },
        {
          heading: "9. Kontakt",
          paragraphs: [`Za pitanja o uvjetima korištenja obratite se na ${EMAIL}.`],
        },
      ],
      disclaimer:
        "Ovo je nacrt dokumenta i ne predstavlja pravni savjet. Prije javnog korištenja dati na pregled pravnom stručnjaku.",
    },
  },

  en: {
    privacy: {
      title: "Privacy Policy",
      updatedLabel: "Last updated",
      updated: UPDATED,
      backLabel: "Back to home",
      intro:
        `WediFrame (operated by ${ENTITY_HR}, VAT/OIB ${OIB_HR}) lets couples collect and ` +
        "share wedding photos and videos. This policy explains what data we process, why, for " +
        "how long, and what your rights are.",
      sections: [
        {
          heading: "1. Data processing roles",
          paragraphs: [
            "For the couple's (host / customer) data we act as the data controller.",
            "For content guests upload (photos and videos) we act as the data processor — processing it on behalf of, and on the instructions of, the host who organised the event.",
          ],
        },
        {
          heading: "2. What data we collect",
          paragraphs: [
            "Host: e-mail, password (stored as a cryptographic hash), chosen language, purchase data, and R1 invoice details (company name, VAT/OIB, address) if requested.",
            "Guest: a self-reported name (optional) and the content they upload. Guests have no account and we do not ask for their e-mail.",
          ],
        },
        {
          heading: "3. Purpose and legal basis",
          paragraphs: [
            "We process data to provide the service (performance of a contract), to take payment, and to meet legal obligations (issuing and fiscalising invoices).",
          ],
        },
        {
          heading: "4. How long we keep data",
          paragraphs: [
            "Event content (photos/videos) is kept until the storage (retention) period of the chosen package expires, counted from the upload start date. After that it is deleted automatically: first it becomes inaccessible, then a short grace period (~7 days) follows, and then it is permanently deleted.",
            "Purchase and invoice data is kept for as long as tax and accounting rules require.",
          ],
        },
        {
          heading: "5. Where data is processed",
          paragraphs: [
            "All data is processed within the EU: media on Cloudflare R2 (EU), the application on Railway (EU), the database on Neon (EU, Frankfurt).",
          ],
        },
        {
          heading: "6. What we do NOT do with your data",
          paragraphs: [
            "We do not use your photos, videos or other data to train artificial intelligence.",
            "We do not sell your data and we do not share it publicly.",
          ],
        },
        {
          heading: "7. Your rights",
          paragraphs: [
            "You have the right to access, correct and delete your data. The host can delete individual content or the entire event at any time.",
            `To exercise your rights, contact us at ${EMAIL}.`,
          ],
        },
        {
          heading: "8. The event access link",
          paragraphs: [
            "Anyone with the event's access link (or QR code) can see the whole gallery and upload content. The host is responsible for who they share the link with and can replace it with a new one at any time (token rotation).",
          ],
        },
      ],
      disclaimer:
        "This is a draft document and does not constitute legal advice. Have it reviewed by a lawyer before public use; the legal-entity details will be filled in once the business is registered.",
    },
    terms: {
      title: "Terms of Service",
      updatedLabel: "Last updated",
      updated: UPDATED,
      backLabel: "Back to home",
      intro: `By using the WediFrame platform (operated by ${ENTITY_HR}, VAT/OIB ${OIB_HR}) you accept these terms.`,
      sections: [
        {
          heading: "1. About the service",
          paragraphs: [
            "WediFrame lets you collect and share wedding photos and videos via a private link and QR code, with no app install and no guest sign-up.",
          ],
        },
        {
          heading: "2. Host account",
          paragraphs: [
            "The host is responsible for the accuracy of registration data and for keeping their account credentials safe.",
          ],
        },
        {
          heading: "3. Packages and payment",
          paragraphs: [
            "Payment is one-off, per event. Each package has its own limits (number of photos, amount of video, total storage) and upload/storage durations.",
            "Payment is handled via Stripe. Invoices are issued and fiscalised in line with Croatian rules; an R1 invoice is issued on request at checkout.",
          ],
        },
        {
          heading: "4. Permitted content",
          paragraphs: [
            "Hosts and guests must not upload unlawful content or content that infringes third-party rights. The host is responsible for the event and the content collected through their link.",
          ],
        },
        {
          heading: "5. Access link",
          paragraphs: [
            "The access link and QR code grant gallery access to anyone who has them. The host manages link sharing and can replace it with a new one at any time.",
          ],
        },
        {
          heading: "6. Storage and deletion",
          paragraphs: [
            "Content is deleted automatically when the chosen package's storage period expires. The host may also delete individual content or the whole event earlier.",
          ],
        },
        {
          heading: "7. Liability",
          paragraphs: [
            'The service is provided "as is". To the extent permitted by law, we are not liable for indirect damage arising from use of the service.',
          ],
        },
        {
          heading: "8. Changes to the terms",
          paragraphs: [
            "We may update these terms from time to time. The date of the last change is shown at the top.",
          ],
        },
        {
          heading: "9. Contact",
          paragraphs: [`For questions about these terms, contact us at ${EMAIL}.`],
        },
      ],
      disclaimer:
        "This is a draft document and does not constitute legal advice. Have it reviewed by a lawyer before public use.",
    },
  },
};
