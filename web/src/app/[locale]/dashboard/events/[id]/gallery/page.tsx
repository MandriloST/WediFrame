"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import { ApiError, getEvent, isAuthed, type HostEvent } from "@/lib/hostApi";
import { HostGallery } from "@/components/host/HostGallery";

export default function HostGalleryPage() {
  const t = useTranslations("hostGallery");
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const eventId = params.id;

  const [event, setEvent] = useState<HostEvent | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [error, setError] = useState(false);

  useEffect(() => {
    if (!isAuthed()) {
      router.replace("/login");
      return;
    }
    let alive = true;
    void (async () => {
      try {
        const ev = await getEvent(eventId);
        if (alive) setEvent(ev);
      } catch (e) {
        if (!alive) return;
        if (e instanceof ApiError && e.status === 401) {
          router.replace("/login");
        } else if (e instanceof ApiError && e.status === 404) {
          setNotFound(true);
        } else {
          setError(true);
        }
      }
    })();
    return () => {
      alive = false;
    };
  }, [router, eventId]);

  return (
    <main className="mx-auto min-h-dvh w-full max-w-2xl bg-[#FFFDF9] px-5 py-8">
      <Link href="/dashboard" className="text-sm text-[#57534E]">
        ‹ {t("back")}
      </Link>

      <header className="mt-4">
        <h1 className="text-xl font-semibold tracking-tight text-[#1C1917]">
          {t("title")}
        </h1>
        {event && (
          <p className="mt-0.5 truncate text-sm text-[#A8A29E]">{event.title}</p>
        )}
      </header>

      {notFound && (
        <p className="mt-10 rounded-2xl border border-[#E7E0D8] bg-white p-6 text-center text-sm text-[#A8A29E]">
          {t("notFound")}
        </p>
      )}

      {error && (
        <p className="mt-10 rounded-2xl border border-[#E7E0D8] bg-white p-6 text-center text-sm text-[#B4432F]">
          {t("loadError")}
        </p>
      )}

      {event && !notFound && !error && <HostGallery eventId={eventId} />}
    </main>
  );
}
