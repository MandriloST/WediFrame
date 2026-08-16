import { setRequestLocale, getTranslations } from "next-intl/server";
import { Link } from "@/i18n/navigation";

export default async function LandingPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("landing");
  const tCommon = await getTranslations("common");

  const steps = [t("steps.one"), t("steps.two"), t("steps.three")];

  return (
    <main className="flex min-h-dvh flex-col items-center justify-center gap-10 px-6 py-16 text-center">
      <header className="flex flex-col items-center gap-4">
        <p className="font-mono text-sm tracking-widest uppercase text-neutral-500">
          {tCommon("appName")}
        </p>
        <h1 className="max-w-md text-3xl font-semibold text-balance sm:text-4xl">
          {t("title")}
        </h1>
        <p className="max-w-sm text-neutral-600 text-pretty">{t("subtitle")}</p>
      </header>

      <ol className="flex max-w-sm flex-col gap-3 text-left">
        {steps.map((step, i) => (
          <li key={step} className="flex items-start gap-3">
            <span
              aria-hidden
              className="mt-0.5 flex size-6 shrink-0 items-center justify-center rounded-full bg-neutral-900 font-mono text-xs text-white"
            >
              {i + 1}
            </span>
            <span className="text-neutral-700">{step}</span>
          </li>
        ))}
      </ol>

      <Link
        href="/pricing"
        className="rounded-full border border-[#7C2D3E] px-5 py-2 text-sm font-medium text-[#7C2D3E] transition hover:bg-[#7C2D3E] hover:text-white"
      >
        {t("cta")}
      </Link>

      <footer className="fixed inset-x-0 bottom-4 text-center text-xs text-neutral-400">
        <div className="flex items-center justify-center gap-4">
          <Link href="/privacy" className="hover:text-neutral-600">
            {tCommon("privacy")}
          </Link>
          <Link href="/terms" className="hover:text-neutral-600">
            {tCommon("terms")}
          </Link>
        </div>
        <div className="mt-1">{tCommon("poweredBy")}</div>
      </footer>
    </main>
  );
}
