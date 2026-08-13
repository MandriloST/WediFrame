"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import { getPackages, type PublicPackage } from "@/lib/guestApi";
import { ApiError, createEvent, isAuthed } from "@/lib/hostApi";

/** Today as yyyy-MM-dd for the date input's default/min. */
function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

const GB = 1024 ** 3;
const MB = 1024 ** 2;

function formatSize(bytes: number): string {
  return bytes >= GB
    ? `${Math.round(bytes / GB)} GB`
    : `${Math.round(bytes / MB)} MB`;
}

export default function NewEventPage() {
  const t = useTranslations("newEvent");
  const tp = useTranslations("packages");
  const locale = useLocale();
  const router = useRouter();

  const [title, setTitle] = useState("");
  const [date, setDate] = useState(todayIso());
  const [packages, setPackages] = useState<PublicPackage[] | null>(null);
  const [slug, setSlug] = useState<string>("free");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!isAuthed()) {
      router.replace("/login");
      return;
    }
    getPackages()
      .then((list) => setPackages(list))
      .catch(() => setPackages([]));
  }, [router]);

  const money = (cents: number, currency: string) =>
    new Intl.NumberFormat(locale, { style: "currency", currency }).format(
      cents / 100,
    );

  const submit = async () => {
    if (busy) return;
    const trimmed = title.trim();
    if (trimmed.length < 1) {
      setError(t("errors.titleRequired"));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await createEvent(trimmed, date, slug);
      router.replace("/dashboard");
    } catch (e) {
      if (e instanceof ApiError && e.code === "events.package_invalid") {
        setError(t("errors.packageInvalid"));
      } else if (e instanceof ApiError && e.status === 401) {
        setError(t("errors.session"));
      } else {
        setError(t("errors.generic"));
      }
      setBusy(false);
    }
  };

  const selected = packages?.find((p) => p.slug === slug) ?? null;
  const paidSelected = !!selected && selected.priceCents > 0;

  return (
    <main className="mx-auto min-h-dvh w-full max-w-md bg-[#FFFDF9] px-5 py-8">
      <Link href="/dashboard" className="text-sm text-[#57534E]">
        ‹ {t("back")}
      </Link>

      <h1 className="mt-4 text-xl font-semibold tracking-tight text-[#1C1917]">
        {t("title")}
      </h1>

      <div className="mt-6 rounded-2xl border border-[#E7E0D8] bg-white p-6 shadow-sm">
        <label className="block text-sm font-medium text-[#44403C]">
          {t("titleLabel")}
          <input
            type="text"
            value={title}
            maxLength={200}
            placeholder={t("titlePlaceholder")}
            onChange={(e) => setTitle(e.target.value)}
            className="mt-1 w-full rounded-lg border border-[#E7E0D8] bg-[#FFFDF9] px-3 py-2.5 text-[#1C1917] outline-none focus:border-[#7C2D3E]"
          />
        </label>

        <label className="mt-4 block text-sm font-medium text-[#44403C]">
          {t("dateLabel")}
          <input
            type="date"
            value={date}
            onChange={(e) => setDate(e.target.value)}
            className="mt-1 w-full rounded-lg border border-[#E7E0D8] bg-[#FFFDF9] px-3 py-2.5 text-[#1C1917] outline-none focus:border-[#7C2D3E]"
          />
        </label>
        <p className="mt-1 text-xs text-[#A8A29E]">{t("dateHint")}</p>

        {/* Package picker */}
        <p className="mt-5 text-sm font-medium text-[#44403C]">
          {t("packageLabel")}
        </p>
        {packages === null ? (
          <p className="mt-2 text-sm text-[#A8A29E]">{t("packagesLoading")}</p>
        ) : (
          <div className="mt-2 space-y-2">
            {packages.map((p) => {
              const active = p.slug === slug;
              return (
                <button
                  key={p.slug}
                  type="button"
                  onClick={() => setSlug(p.slug)}
                  className={`flex w-full items-start justify-between gap-3 rounded-xl border px-4 py-3 text-left transition ${
                    active
                      ? "border-[#7C2D3E] bg-[#FBF3F0] ring-1 ring-[#7C2D3E]"
                      : "border-[#E7E0D8] bg-[#FFFDF9]"
                  }`}
                >
                  <span className="min-w-0">
                    <span className="block text-sm font-semibold text-[#1C1917]">
                      {tp(`${p.slug}.name`)}
                    </span>
                    <span className="mt-0.5 block text-xs text-[#78716C]">
                      {t("specPhotos", { count: p.maxPhotoCount })} ·{" "}
                      {t("specVideo", { size: formatSize(p.maxVideoTotalBytes) })}
                    </span>
                    <span className="mt-0.5 block text-xs text-[#A8A29E]">
                      {t("specUpload", { days: p.uploadPeriodDays })} ·{" "}
                      {t("specStorage", { days: p.retentionDays })}
                    </span>
                  </span>
                  <span className="shrink-0 text-sm font-semibold text-[#7C2D3E]">
                    {p.priceCents === 0 ? t("free") : money(p.priceCents, p.currency)}
                  </span>
                </button>
              );
            })}
          </div>
        )}

        {paidSelected && (
          <p className="mt-3 rounded-lg bg-[#FBF3F0] px-3 py-2 text-xs text-[#7C2D3E]">
            {t("paidNote")}
          </p>
        )}

        {error && <p className="mt-3 text-sm text-[#B4432F]">{error}</p>}

        <button
          type="button"
          onClick={submit}
          disabled={busy || packages === null}
          className="mt-5 w-full rounded-xl bg-[#7C2D3E] px-4 py-3 font-medium text-white transition active:scale-[0.99] disabled:opacity-60"
        >
          {busy ? t("creating") : t("createButton")}
        </button>
      </div>
    </main>
  );
}
