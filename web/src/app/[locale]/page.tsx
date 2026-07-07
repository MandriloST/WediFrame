import { setRequestLocale, getTranslations } from "next-intl/server";

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

      <span className="rounded-full border border-neutral-300 px-4 py-1.5 text-sm text-neutral-500">
        {t("cta")}
      </span>

      <footer className="fixed inset-x-0 bottom-4 text-center text-xs text-neutral-400">
        {tCommon("poweredBy")}
      </footer>
    </main>
  );
}
