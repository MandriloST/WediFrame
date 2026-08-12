"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import { ApiError, createEvent, isAuthed } from "@/lib/hostApi";

/** Today as yyyy-MM-dd for the date input's default/min. */
function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export default function NewEventPage() {
  const t = useTranslations("newEvent");
  const router = useRouter();
  const [title, setTitle] = useState("");
  const [date, setDate] = useState(todayIso());
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!isAuthed()) router.replace("/login");
  }, [router]);

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
      await createEvent(trimmed, date);
      router.replace("/dashboard");
    } catch (e) {
      setError(
        e instanceof ApiError && e.status === 401
          ? t("errors.session")
          : t("errors.generic"),
      );
      setBusy(false);
    }
  };

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

        {error && <p className="mt-3 text-sm text-[#B4432F]">{error}</p>}

        <button
          type="button"
          onClick={submit}
          disabled={busy}
          className="mt-5 w-full rounded-xl bg-[#7C2D3E] px-4 py-3 font-medium text-white transition active:scale-[0.99] disabled:opacity-60"
        >
          {busy ? t("creating") : t("createButton")}
        </button>
      </div>
    </main>
  );
}
