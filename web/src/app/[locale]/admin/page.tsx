"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { type AdminOverview, getOverview } from "@/lib/adminApi";

const STATUS_ORDER = ["Draft", "Active", "UploadClosed", "Expired", "Deleted"];

function formatBytes(bytes: number): string {
  if (bytes <= 0) return "0 B";
  const units = ["B", "KB", "MB", "GB", "TB"];
  const i = Math.min(
    units.length - 1,
    Math.floor(Math.log(bytes) / Math.log(1024)),
  );
  const value = bytes / Math.pow(1024, i);
  return `${value.toFixed(i === 0 ? 0 : 1)} ${units[i]}`;
}

export default function AdminHomePage() {
  const t = useTranslations("admin");
  const [data, setData] = useState<AdminOverview | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        const res = await getOverview();
        if (alive) setData(res);
      } catch {
        if (alive) setError(true);
      } finally {
        if (alive) setLoading(false);
      }
    })();
    return () => {
      alive = false;
    };
  }, []);

  const statusLabel = (s: string) => t(`events.status.${s}`);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-stone-900">
          {t("home.title")}
        </h1>
        <p className="mt-1 text-stone-500">{t("home.subtitle")}</p>
      </div>

      {error ? (
        <p className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {t("overview.error")}
        </p>
      ) : loading || !data ? (
        <p className="text-sm text-stone-500">{t("overview.loading")}</p>
      ) : (
        <>
          {/* Stat cards */}
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard
              label={t("overview.users")}
              value={data.users.total.toLocaleString()}
              href="/admin/users"
            />
            <StatCard
              label={t("overview.events")}
              value={data.events.total.toLocaleString()}
              href="/admin/events"
            />
            <StatCard
              label={t("overview.storage")}
              value={formatBytes(data.storage.totalBytes)}
            />
            <StatCard
              label={t("overview.mediaItems")}
              value={data.storage.itemCount.toLocaleString()}
              sub={t("overview.photoVideo", {
                photos: data.storage.photoCount,
                videos: data.storage.videoCount,
              })}
            />
          </div>

          {/* Events by status */}
          <section className="rounded-2xl border border-stone-200 bg-white p-5 shadow-sm">
            <h2 className="font-semibold text-stone-900">
              {t("overview.eventsByStatus")}
            </h2>
            <div className="mt-3 flex flex-wrap gap-2">
              {STATUS_ORDER.map((s) => (
                <div
                  key={s}
                  className="flex items-center gap-2 rounded-full border border-stone-200 px-3 py-1.5 text-sm"
                >
                  <span className="text-stone-500">{statusLabel(s)}</span>
                  <span className="font-semibold text-stone-900">
                    {(data.events.byStatus[s] ?? 0).toLocaleString()}
                  </span>
                </div>
              ))}
            </div>
          </section>

          {/* Storage report: top events */}
          <section className="rounded-2xl border border-stone-200 bg-white p-5 shadow-sm">
            <h2 className="font-semibold text-stone-900">
              {t("overview.topEvents")}
            </h2>
            <p className="mt-1 text-sm text-stone-500">
              {t("overview.topEventsHint")}
            </p>
            {data.storage.topEvents.length === 0 ? (
              <p className="mt-3 text-sm text-stone-400">
                {t("overview.noStorage")}
              </p>
            ) : (
              <div className="mt-3 overflow-x-auto">
                <table className="w-full min-w-[420px] border-collapse text-sm">
                  <thead>
                    <tr className="border-b border-stone-200 text-left text-stone-500">
                      <th className="px-2 py-2 font-medium">
                        {t("overview.colEvent")}
                      </th>
                      <th className="px-2 py-2 font-medium">
                        {t("overview.colItems")}
                      </th>
                      <th className="px-2 py-2 font-medium">
                        {t("overview.colSize")}
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.storage.topEvents.map((e) => (
                      <tr
                        key={e.eventId}
                        className="border-b border-stone-100 last:border-0"
                      >
                        <td className="px-2 py-2">
                          <Link
                            href={`/admin/events/${e.eventId}`}
                            className="font-medium text-rose-700 hover:underline"
                          >
                            {e.title ?? e.eventId}
                          </Link>
                        </td>
                        <td className="px-2 py-2 text-stone-600">
                          {e.itemCount.toLocaleString()}
                        </td>
                        <td className="whitespace-nowrap px-2 py-2 text-stone-600">
                          {formatBytes(e.bytes)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </div>
  );
}

function StatCard({
  label,
  value,
  sub,
  href,
}: {
  label: string;
  value: string;
  sub?: string;
  href?: string;
}) {
  const inner = (
    <>
      <div className="text-xs font-medium uppercase tracking-wide text-stone-400">
        {label}
      </div>
      <div className="mt-1 text-2xl font-semibold text-stone-900">{value}</div>
      {sub && <div className="mt-0.5 text-xs text-stone-400">{sub}</div>}
    </>
  );

  const className =
    "block rounded-2xl border border-stone-200 bg-white p-5 shadow-sm";

  return href ? (
    <Link
      href={href}
      className={`${className} transition hover:border-rose-300 hover:shadow`}
    >
      {inner}
    </Link>
  ) : (
    <div className={className}>{inner}</div>
  );
}
