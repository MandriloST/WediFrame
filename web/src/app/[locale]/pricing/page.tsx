import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { getPackages, type PublicPackage } from "@/lib/guestApi";

const GB = 1024 ** 3;
const MB = 1024 ** 2;

function formatSize(bytes: number): string {
  return bytes >= GB
    ? `${Math.round(bytes / GB)} GB`
    : `${Math.round(bytes / MB)} MB`;
}

/** Days → clean marketing duration: whole months when ≥ 30 days, else days. */
function duration(days: number): { key: "months" | "days"; n: number } {
  return days >= 30
    ? { key: "months", n: Math.round(days / 30) }
    : { key: "days", n: days };
}

export default async function PricingPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("pricing");
  const tp = await getTranslations("packages");
  const tCommon = await getTranslations("common");

  let packages: PublicPackage[] = [];
  let failed = false;
  try {
    packages = await getPackages();
  } catch {
    failed = true;
  }

  const money = (cents: number, currency: string) =>
    new Intl.NumberFormat(locale, { style: "currency", currency }).format(
      cents / 100,
    );

  const durationLabel = (days: number) => {
    const d = duration(days);
    return t(d.key, { n: d.n });
  };

  return (
    <main className="mx-auto min-h-dvh w-full max-w-4xl bg-[#FFFDF9] px-5 py-12">
      <Link href="/" className="text-sm text-[#57534E]">
        ‹ {t("back")}
      </Link>

      <header className="mt-6 text-center">
        <p className="font-mono text-xs tracking-widest text-[#A8A29E] uppercase">
          {tCommon("appName")}
        </p>
        <h1 className="mt-2 text-2xl font-semibold tracking-tight text-[#1C1917] sm:text-3xl">
          {t("title")}
        </h1>
        <p className="mx-auto mt-2 max-w-md text-sm text-[#57534E]">
          {t("subtitle")}
        </p>
      </header>

      {failed || packages.length === 0 ? (
        <p className="mt-10 text-center text-sm text-[#A8A29E]">
          {t("unavailable")}
        </p>
      ) : (
        <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {packages.map((p) => (
            <div
              key={p.slug}
              className="flex flex-col rounded-2xl border border-[#E7E0D8] bg-white p-6 shadow-sm"
            >
              <h2 className="text-lg font-semibold text-[#1C1917]">
                {tp(`${p.slug}.name`)}
              </h2>

              <p className="mt-1">
                <span className="text-2xl font-bold text-[#7C2D3E]">
                  {p.priceCents === 0 ? t("free") : money(p.priceCents, p.currency)}
                </span>
                {p.priceCents > 0 && (
                  <span className="ml-1 text-xs text-[#A8A29E]">
                    {t("perEvent")}
                  </span>
                )}
              </p>

              <ul className="mt-4 flex-1 space-y-1.5 text-sm text-[#57534E]">
                <li>{t("photos", { count: p.maxPhotoCount })}</li>
                <li>{t("video", { size: formatSize(p.maxVideoTotalBytes) })}</li>
                <li>{t("total", { size: formatSize(p.maxTotalBytes) })}</li>
                <li>{t("uploadFor", { duration: durationLabel(p.uploadPeriodDays) })}</li>
                <li>{t("galleryFor", { duration: durationLabel(p.retentionDays) })}</li>
              </ul>

              <Link
                href="/register"
                className="mt-6 rounded-xl bg-[#7C2D3E] px-4 py-2.5 text-center text-sm font-medium text-white transition active:scale-[0.99]"
              >
                {t("cta")}
              </Link>
            </div>
          ))}
        </div>
      )}

      <footer className="mt-12 text-center text-xs text-[#A8A29E]">
        {tCommon("poweredBy")}
      </footer>
    </main>
  );
}
