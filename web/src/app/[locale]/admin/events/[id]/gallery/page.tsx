"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { type AdminEventDetail, getEvent } from "@/lib/adminApi";
import { AdminGallery } from "@/components/admin/AdminGallery";

export default function AdminEventGalleryPage() {
  const t = useTranslations("admin.gallery");
  const params = useParams<{ id: string }>();
  const eventId = params.id;

  const [event, setEvent] = useState<AdminEventDetail | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        const e = await getEvent(eventId);
        if (alive) setEvent(e);
      } catch {
        if (alive) setError(true);
      }
    })();
    return () => {
      alive = false;
    };
  }, [eventId]);

  return (
    <div className="space-y-4">
      <Link
        href={`/admin/events/${eventId}`}
        className="text-sm text-stone-500 transition hover:text-stone-800"
      >
        ← {t("back")}
      </Link>

      <div>
        <h1 className="text-2xl font-semibold text-stone-900">{t("title")}</h1>
        {event && (
          <p className="mt-1 truncate text-sm text-stone-500">{event.title}</p>
        )}
      </div>

      {error ? (
        <p className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {t("loadError")}
        </p>
      ) : (
        <AdminGallery eventId={eventId} />
      )}
    </div>
  );
}
