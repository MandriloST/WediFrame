"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import {
  type HostEvent,
  isAuthed,
  listEvents,
  logout,
} from "@/lib/hostApi";

export default function DashboardPage() {
  const t = useTranslations("dashboard");
  const router = useRouter();
  const [events, setEvents] = useState<HostEvent[] | null>(null);
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    try {
      const list = await listEvents();
      setEvents(list);
      setError(false);
    } catch {
      setError(true);
    }
  }, []);

  useEffect(() => {
    if (!isAuthed()) {
      router.replace("/login");
      return;
    }
    // Data fetch on mount; setState happens after the await, not synchronously.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [router, load]);

  const signOut = () => {
    logout();
    router.replace("/login");
  };

  return (
    <main className="mx-auto min-h-dvh w-full max-w-2xl bg-[#FFFDF9] px-5 py-8">
      <header className="flex items-center justify-between">
        <h1 className="text-xl font-semibold tracking-tight text-[#1C1917]">
          {t("title")}
        </h1>
        <button
          type="button"
          onClick={signOut}
          className="text-sm text-[#57534E] underline-offset-2 hover:underline"
        >
          {t("logout")}
        </button>
      </header>

      <Link
        href="/dashboard/events/new"
        className="mt-6 flex items-center justify-center rounded-xl bg-[#7C2D3E] px-4 py-3 font-medium text-white transition active:scale-[0.99]"
      >
        + {t("newEvent")}
      </Link>

      <div className="mt-6">
        {error && (
          <div className="rounded-xl border border-[#E7E0D8] bg-white p-5 text-center">
            <p className="text-sm text-[#B4432F]">{t("loadError")}</p>
            <button
              type="button"
              onClick={load}
              className="mt-2 text-sm font-medium text-[#7C2D3E]"
            >
              {t("retry")}
            </button>
          </div>
        )}

        {!error && events === null && (
          <p className="py-10 text-center text-sm text-[#A8A29E]">
            {t("loading")}
          </p>
        )}

        {!error && events?.length === 0 && (
          <p className="py-10 text-center text-sm text-[#A8A29E]">{t("empty")}</p>
        )}

        <ul className="space-y-3">
          {events?.map((ev) => (
            <EventCard key={ev.id} event={ev} />
          ))}
        </ul>
      </div>
    </main>
  );
}

function EventCard({ event }: { event: HostEvent }) {
  const t = useTranslations("dashboard");
  const [copied, setCopied] = useState(false);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(event.guestUrl);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      // clipboard blocked — the link is visible below anyway
    }
  };

  return (
    <li className="rounded-2xl border border-[#E7E0D8] bg-white p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate font-medium text-[#1C1917]">{event.title}</p>
          <p className="mt-0.5 text-xs text-[#A8A29E]">
            {t("uploadStart")}: {event.uploadStartDate}
          </p>
        </div>
        <StatusBadge status={event.status} />
      </div>

      <div className="mt-3 flex items-center gap-2">
        <input
          readOnly
          value={event.guestUrl}
          onFocus={(e) => e.currentTarget.select()}
          className="min-w-0 flex-1 truncate rounded-lg border border-[#E7E0D8] bg-[#FBF8F4] px-2.5 py-1.5 text-xs text-[#57534E]"
        />
        <button
          type="button"
          onClick={copy}
          className="shrink-0 rounded-lg bg-[#EFE7DC] px-3 py-1.5 text-xs font-medium text-[#7C2D3E]"
        >
          {copied ? t("copied") : t("copyLink")}
        </button>
      </div>
    </li>
  );
}

function StatusBadge({ status }: { status: string }) {
  const t = useTranslations("dashboard.status");
  const known = ["Draft", "Active", "UploadClosed", "Expired", "Deleted"];
  const label = known.includes(status) ? t(status) : status;
  const tone =
    status === "Active"
      ? "bg-[#E7F0E9] text-[#4D7C5F]"
      : status === "Draft"
        ? "bg-[#EFE7DC] text-[#8A6D3B]"
        : "bg-[#F0E7E7] text-[#8A5A5A]";
  return (
    <span
      className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-medium ${tone}`}
    >
      {label}
    </span>
  );
}
