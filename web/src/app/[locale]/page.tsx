import { setRequestLocale, getTranslations } from "next-intl/server";
import { Fraunces } from "next/font/google";
import { Link } from "@/i18n/navigation";

const fraunces = Fraunces({ subsets: ["latin"], weight: ["400", "600"] });

const BORDO = "#7C2D3E";
const CREAM = "#FFFDF9";

export default async function LandingPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("landing");
  const tCommon = await getTranslations("common");

  const steps = [1, 2, 3].map((n) => ({
    title: t(`how.step${n}Title`),
    desc: t(`how.step${n}Desc`),
  }));

  const features = [1, 2, 3, 4, 5, 6].map((n) => ({
    title: t(`features.f${n}Title`),
    desc: t(`features.f${n}Desc`),
    icon: n,
  }));

  return (
    <div className="min-h-dvh bg-[#FFFDF9] text-[#1C1917]">
      {/* Top bar */}
      <header className="sticky top-0 z-20 border-b border-[#EFE7DE] bg-[#FFFDF9]/85 backdrop-blur">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-5 py-3">
          <span className={`${fraunces.className} text-lg font-semibold text-[#7C2D3E]`}>
            WediFrame
          </span>
          <nav className="flex items-center gap-4 text-sm">
            <Link href="/pricing" className="text-[#57534E] transition hover:text-[#1C1917]">
              {t("nav.pricing")}
            </Link>
            <Link href="/login" className="text-[#57534E] transition hover:text-[#1C1917]">
              {t("nav.login")}
            </Link>
            <Link
              href="/register"
              className="rounded-full bg-[#7C2D3E] px-4 py-1.5 font-medium text-white transition hover:opacity-90"
            >
              {t("nav.createEvent")}
            </Link>
          </nav>
        </div>
      </header>

      <main>
        {/* Hero */}
        <section className="relative overflow-hidden">
          <div
            aria-hidden
            className="pointer-events-none absolute inset-0"
            style={{
              background:
                "radial-gradient(900px 380px at 78% -8%, rgba(124,45,62,0.10), transparent 60%)",
            }}
          />
          <div className="mx-auto grid max-w-5xl items-center gap-10 px-5 py-16 sm:py-20 lg:grid-cols-2">
            <div>
              <span className="inline-flex items-center gap-2 rounded-full border border-[#E7D9C9] bg-white px-3 py-1 text-xs font-medium text-[#8A6D3B]">
                <span className="size-1.5 rounded-full bg-[#7C2D3E]" />
                {tCommon("appName")} · {t("hero.trust")}
              </span>
              <h1
                className={`${fraunces.className} mt-5 text-4xl leading-[1.08] font-semibold text-balance sm:text-5xl`}
              >
                {t("hero.title")}
              </h1>
              <p className="mt-4 max-w-md text-lg text-[#57534E] text-pretty">
                {t("hero.subtitle")}
              </p>
              <div className="mt-7 flex flex-wrap gap-3">
                <Link
                  href="/register"
                  className="rounded-xl bg-[#7C2D3E] px-6 py-3 font-medium text-white transition active:scale-[0.99] hover:opacity-90"
                >
                  {t("hero.ctaPrimary")}
                </Link>
                <Link
                  href="/pricing"
                  className="rounded-xl border border-[#7C2D3E] px-6 py-3 font-medium text-[#7C2D3E] transition hover:bg-[#7C2D3E] hover:text-white"
                >
                  {t("hero.ctaSecondary")}
                </Link>
              </div>
            </div>

            {/* Decorative visual: tilted photo cards + a QR glyph */}
            <HeroArt />
          </div>
        </section>

        {/* How it works */}
        <section className="border-t border-[#EFE7DE] bg-white">
          <div className="mx-auto max-w-5xl px-5 py-16">
            <h2 className={`${fraunces.className} text-center text-3xl font-semibold`}>
              {t("how.title")}
            </h2>
            <ol className="mt-10 grid gap-6 sm:grid-cols-3">
              {steps.map((s, i) => (
                <li key={s.title} className="rounded-2xl border border-[#EFE7DE] bg-[#FFFDF9] p-6">
                  <span className="flex size-9 items-center justify-center rounded-full bg-[#7C2D3E] font-mono text-sm text-white">
                    {i + 1}
                  </span>
                  <h3 className="mt-4 font-semibold text-[#1C1917]">{s.title}</h3>
                  <p className="mt-1 text-sm text-[#57534E]">{s.desc}</p>
                </li>
              ))}
            </ol>
          </div>
        </section>

        {/* Features */}
        <section className="border-t border-[#EFE7DE]">
          <div className="mx-auto max-w-5xl px-5 py-16">
            <h2 className={`${fraunces.className} text-center text-3xl font-semibold`}>
              {t("features.title")}
            </h2>
            <div className="mt-10 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
              {features.map((f) => (
                <div key={f.title} className="rounded-2xl border border-[#EFE7DE] bg-white p-6">
                  <div className="flex size-10 items-center justify-center rounded-xl bg-[#F6ECEE] text-[#7C2D3E]">
                    <FeatureIcon n={f.icon} />
                  </div>
                  <h3 className="mt-4 font-semibold text-[#1C1917]">{f.title}</h3>
                  <p className="mt-1 text-sm text-[#57534E]">{f.desc}</p>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* Privacy / trust */}
        <section className="border-t border-[#EFE7DE] bg-[#2A1418] text-[#FCEEF0]">
          <div className="mx-auto max-w-5xl px-5 py-16">
            <h2 className={`${fraunces.className} text-3xl font-semibold`}>
              {t("privacy.title")}
            </h2>
            <p className="mt-2 text-[#E6C9CF]">{t("privacy.subtitle")}</p>
            <ul className="mt-6 grid gap-3 sm:grid-cols-3">
              {["p1", "p2", "p3"].map((k) => (
                <li
                  key={k}
                  className="flex items-start gap-3 rounded-2xl border border-white/10 bg-white/5 p-4 text-sm"
                >
                  <CheckIcon />
                  <span>{t(`privacy.${k}`)}</span>
                </li>
              ))}
            </ul>
          </div>
        </section>

        {/* Packages CTA */}
        <section className="border-t border-[#EFE7DE] bg-white">
          <div className="mx-auto flex max-w-5xl flex-col items-center gap-4 px-5 py-16 text-center">
            <h2 className={`${fraunces.className} text-3xl font-semibold`}>
              {t("packagesCta.title")}
            </h2>
            <p className="max-w-md text-[#57534E]">{t("packagesCta.subtitle")}</p>
            <Link
              href="/pricing"
              className="mt-2 rounded-xl border border-[#7C2D3E] px-6 py-3 font-medium text-[#7C2D3E] transition hover:bg-[#7C2D3E] hover:text-white"
            >
              {t("packagesCta.button")}
            </Link>
          </div>
        </section>

        {/* Final CTA */}
        <section className="border-t border-[#EFE7DE]">
          <div className="mx-auto max-w-5xl px-5 py-16">
            <div className="rounded-3xl bg-[#7C2D3E] px-8 py-12 text-center text-white">
              <h2 className={`${fraunces.className} text-3xl font-semibold`}>
                {t("finalCta.title")}
              </h2>
              <p className="mx-auto mt-2 max-w-md text-[#F3D9DE]">{t("finalCta.subtitle")}</p>
              <Link
                href="/register"
                className="mt-6 inline-block rounded-xl bg-white px-6 py-3 font-medium text-[#7C2D3E] transition hover:opacity-90"
              >
                {t("finalCta.button")}
              </Link>
            </div>
          </div>
        </section>
      </main>

      {/* Footer */}
      <footer className="border-t border-[#EFE7DE] bg-[#FFFDF9]">
        <div className="mx-auto flex max-w-5xl flex-col items-center gap-2 px-5 py-8 text-center text-xs text-[#A8A29E]">
          <div className="flex items-center gap-4">
            <Link href="/privacy" className="hover:text-[#57534E]">
              {tCommon("privacy")}
            </Link>
            <Link href="/terms" className="hover:text-[#57534E]">
              {tCommon("terms")}
            </Link>
          </div>
          <div>{tCommon("poweredBy")}</div>
        </div>
      </footer>
    </div>
  );
}

/** Decorative hero art — tilted photo cards + a QR glyph. Pure CSS/SVG, no assets. */
function HeroArt() {
  return (
    <div aria-hidden className="relative mx-auto hidden h-72 w-full max-w-sm lg:block">
      <div className="absolute left-6 top-8 h-44 w-36 -rotate-6 rounded-2xl border border-[#EFE7DE] bg-white p-2 shadow-sm">
        <div className="h-full w-full rounded-xl bg-gradient-to-br from-[#F6ECEE] to-[#EADFD3]" />
      </div>
      <div className="absolute right-10 top-2 h-48 w-36 rotate-6 rounded-2xl border border-[#EFE7DE] bg-white p-2 shadow-sm">
        <div className="h-full w-full rounded-xl bg-gradient-to-br from-[#EADFD3] to-[#F6ECEE]" />
      </div>
      <div className="absolute bottom-2 left-1/2 flex -translate-x-1/2 items-center gap-3 rounded-2xl border border-[#EFE7DE] bg-white px-4 py-3 shadow-md">
        <svg width="44" height="44" viewBox="0 0 44 44" fill="none" aria-hidden>
          <rect x="1" y="1" width="42" height="42" rx="6" fill={CREAM} stroke="#EFE7DE" />
          <g fill={BORDO}>
            <rect x="7" y="7" width="10" height="10" rx="2" />
            <rect x="27" y="7" width="10" height="10" rx="2" />
            <rect x="7" y="27" width="10" height="10" rx="2" />
            <rect x="10" y="10" width="4" height="4" fill={CREAM} />
            <rect x="30" y="10" width="4" height="4" fill={CREAM} />
            <rect x="10" y="30" width="4" height="4" fill={CREAM} />
            <rect x="27" y="27" width="4" height="4" />
            <rect x="33" y="27" width="4" height="4" />
            <rect x="27" y="33" width="4" height="4" />
            <rect x="33" y="33" width="4" height="4" />
          </g>
        </svg>
        <div className="text-left">
          <div className="text-[11px] font-medium text-[#7C2D3E]">Scan · Upload</div>
          <div className="text-[11px] text-[#A8A29E]">wediframe.hr</div>
        </div>
      </div>
    </div>
  );
}

function FeatureIcon({ n }: { n: number }) {
  const common = {
    width: 20,
    height: 20,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
  };
  switch (n) {
    case 1: // no app / phone
      return (
        <svg {...common}>
          <rect x="7" y="3" width="10" height="18" rx="2" />
          <path d="M11 18h2" />
        </svg>
      );
    case 2: // QR
      return (
        <svg {...common}>
          <rect x="3" y="3" width="7" height="7" rx="1" />
          <rect x="14" y="3" width="7" height="7" rx="1" />
          <rect x="3" y="14" width="7" height="7" rx="1" />
          <path d="M14 14h3v3M21 14v7h-7" />
        </svg>
      );
    case 3: // photo/video
      return (
        <svg {...common}>
          <rect x="3" y="5" width="18" height="14" rx="2" />
          <path d="M10 9l5 3-5 3V9z" />
        </svg>
      );
    case 4: // people / everyone
      return (
        <svg {...common}>
          <circle cx="9" cy="8" r="3" />
          <path d="M3 20c0-3 3-5 6-5s6 2 6 5" />
          <path d="M16 11a3 3 0 0 0 0-6M21 20c0-2.5-2-4.2-4.5-4.8" />
        </svg>
      );
    case 5: // lock / private
      return (
        <svg {...common}>
          <rect x="4" y="10" width="16" height="10" rx="2" />
          <path d="M8 10V7a4 4 0 0 1 8 0v3" />
        </svg>
      );
    default: // tag / pay per event
      return (
        <svg {...common}>
          <path d="M3 12l9-9 9 9-9 9-9-9z" />
          <circle cx="12" cy="12" r="2" />
        </svg>
      );
  }
}

function CheckIcon() {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 24 24"
      fill="none"
      stroke="#E9A6B0"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className="mt-0.5 shrink-0"
      aria-hidden
    >
      <path d="M20 6L9 17l-5-5" />
    </svg>
  );
}
