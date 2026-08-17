"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import {
  type AdminEventDetail,
  extendRetention,
  getEvent,
} from "@/lib/adminApi";
import { ApiError } from "@/lib/hostApi";

export default function AdminEventDetailPage() {
  const params = useParams<{ id: string }>();
  const id = params.id;
  const t = useTranslations("admin.events");
  const locale = useLocale();

  const [event, setEvent] = useState<AdminEventDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        const e = await getEvent(id);
        if (alive) setEvent(e);
      } catch {
        if (alive) setError(true);
      } finally {
        if (alive) setLoading(false);
      }
    })();
    return () => {
      alive = false;
    };
  }, [id]);

  const fmtDate = (iso: string | null) =>
    iso ? new Date(iso).toLocaleDateString(locale) : "—";
  const fmtDateTime = (iso: string) =>
    new Date(iso).toLocaleString(locale, {
      dateStyle: "medium",
      timeStyle: "short",
    });

  return (
    <div className="space-y-5">
      <Link
        href="/admin/events"
        className="text-sm text-stone-500 transition hover:text-stone-800"
      >
        ← {t("detail.back")}
      </Link>

      {error ? (
        <p className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {t("detail.error")}
        </p>
      ) : loading || !event ? (
        <p className="text-sm text-stone-500">{t("loading")}</p>
      ) : (
        <>
          <div>
            <h1 className="text-2xl font-semibold text-stone-900">
              {event.title}
            </h1>
            <p className="mt-1 text-sm text-stone-500">
              {t(`status.${event.status}`)} · {event.type}
            </p>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <Field label={t("detail.owner")} value={event.ownerEmail ?? event.ownerUserId} />
            <Field
              label={t("detail.package")}
              value={event.packageName ?? "—"}
            />
            <Field
              label={t("detail.uploadStart")}
              value={fmtDate(event.uploadStartDate)}
            />
            <Field
              label={t("detail.uploadEnds")}
              value={fmtDate(event.uploadEndsAt)}
            />
            <Field
              label={t("detail.expires")}
              value={fmtDate(event.expiresAt)}
            />
            <Field
              label={t("detail.cover")}
              value={event.hasCover ? t("detail.coverYes") : t("detail.coverNo")}
            />
            <Field
              label={t("detail.created")}
              value={fmtDateTime(event.createdAt)}
            />
            <Field label={t("detail.eventId")} value={event.id} mono />
          </div>

          <div className="rounded-2xl border border-stone-200 bg-white p-5 shadow-sm">
            <h2 className="font-semibold text-stone-900">
              {t("detail.guestLink")}
            </h2>
            <p className="mt-1 break-all text-sm text-stone-600">
              {event.guestUrl}
            </p>
            <div className="mt-3 flex flex-wrap gap-2">
              <a
                href={event.guestUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="rounded-lg border border-stone-300 px-3 py-1.5 text-sm font-medium text-stone-700 transition hover:bg-stone-100"
              >
                {t("detail.openGuest")}
              </a>
            </div>
            <p className="mt-3 text-xs text-stone-400">
              {t("detail.guestLinkHint")}
            </p>
          </div>

          <div className="rounded-2xl border border-stone-200 bg-white p-5 shadow-sm">
            <h2 className="font-semibold text-stone-900">
              {t("detail.moderation")}
            </h2>
            <p className="mt-1 text-sm text-stone-500">
              {t("detail.moderationHint")}
            </p>
            <Link
              href={`/admin/events/${event.id}/gallery`}
              className="mt-3 inline-block rounded-lg bg-rose-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-rose-700"
            >
              {t("detail.moderateGallery")}
            </Link>
          </div>

          {event.expiresAt && event.status !== "Deleted" && (
            <RetentionSection
              event={event}
              onExtended={(expiresAt, status) =>
                setEvent((prev) =>
                  prev ? { ...prev, expiresAt, status } : prev,
                )
              }
            />
          )}
        </>
      )}
    </div>
  );
}

function RetentionSection({
  event,
  onExtended,
}: {
  event: AdminEventDetail;
  onExtended: (expiresAt: string, status: string) => void;
}) {
  const t = useTranslations("admin.events");
  const [value, setValue] = useState(event.expiresAt ?? "");
  const [saving, setSaving] = useState(false);
  const [errorKey, setErrorKey] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const errorFor = (code: string | null): string => {
    switch (code) {
      case "events.retention_not_later":
        return t("retention.errorNotLater");
      case "events.retention_not_activated":
        return t("retention.errorNotActivated");
      case "events.retention_date_invalid":
        return t("retention.errorDateInvalid");
      default:
        return t("retention.error");
    }
  };

  const submit = async () => {
    if (saving || !value) return;
    setSaving(true);
    setErrorKey(null);
    setDone(false);
    try {
      const res = await extendRetention(event.id, value);
      onExtended(res.expiresAt, res.status);
      setValue(res.expiresAt);
      setDone(true);
    } catch (e) {
      setErrorKey(e instanceof ApiError ? errorFor(e.code) : t("retention.error"));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="rounded-2xl border border-stone-200 bg-white p-5 shadow-sm">
      <h2 className="font-semibold text-stone-900">{t("retention.title")}</h2>
      <p className="mt-1 text-sm text-stone-500">{t("retention.hint")}</p>

      <div className="mt-3 flex flex-wrap items-end gap-3">
        <label className="flex flex-col gap-1 text-sm">
          <span className="font-medium text-stone-600">
            {t("retention.newDate")}
          </span>
          <input
            type="date"
            value={value}
            min={event.expiresAt ?? undefined}
            onChange={(e) => {
              setValue(e.target.value);
              setDone(false);
              setErrorKey(null);
            }}
            className="rounded-lg border border-stone-300 px-3 py-2"
          />
        </label>
        <button
          type="button"
          onClick={() => void submit()}
          disabled={saving || !value || value === event.expiresAt}
          className="rounded-lg bg-rose-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-rose-700 disabled:opacity-50"
        >
          {saving ? t("retention.saving") : t("retention.extend")}
        </button>
      </div>

      {done && (
        <p className="mt-2 text-sm text-emerald-700">{t("retention.done")}</p>
      )}
      {errorKey && <p className="mt-2 text-sm text-red-600">{errorKey}</p>}
    </div>
  );
}

function Field({
  label,
  value,
  mono,
}: {
  label: string;
  value: string;
  mono?: boolean;
}) {
  return (
    <div className="rounded-xl border border-stone-200 bg-white p-4">
      <div className="text-xs font-medium uppercase tracking-wide text-stone-400">
        {label}
      </div>
      <div
        className={`mt-1 break-all text-stone-800 ${mono ? "font-mono text-xs" : ""}`}
      >
        {value}
      </div>
    </div>
  );
}
