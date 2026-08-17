"use client";

import { useCallback, useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { type AdminEvent, type Paged, getEvents } from "@/lib/adminApi";

const PAGE_SIZE = 50;
const STATUSES = ["Draft", "Active", "UploadClosed", "Expired", "Deleted"];

type Filters = { q: string; status: string };
const EMPTY: Filters = { q: "", status: "" };

export default function AdminEventsPage() {
  const t = useTranslations("admin.events");
  const locale = useLocale();

  const [draft, setDraft] = useState<Filters>(EMPTY);
  const [applied, setApplied] = useState<Filters>(EMPTY);
  const [page, setPage] = useState(1);

  const [data, setData] = useState<Paged<AdminEvent> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(false);
    try {
      const res = await getEvents({
        page,
        pageSize: PAGE_SIZE,
        q: applied.q.trim() || undefined,
        status: applied.status || undefined,
      });
      setData(res);
    } catch {
      setError(true);
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [page, applied]);

  useEffect(() => {
    void load();
  }, [load]);

  const apply = () => {
    setPage(1);
    setApplied(draft);
  };
  const reset = () => {
    setDraft(EMPTY);
    setApplied(EMPTY);
    setPage(1);
  };

  const total = data?.total ?? 0;
  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  const fmtDate = (iso: string) => new Date(iso).toLocaleDateString(locale);
  const statusLabel = (s: string) => t(`status.${s}`);
  const statusBadge = (s: string) => {
    switch (s) {
      case "Active":
        return "bg-emerald-100 text-emerald-700";
      case "UploadClosed":
        return "bg-amber-100 text-amber-700";
      case "Draft":
        return "bg-stone-100 text-stone-600";
      case "Expired":
        return "bg-stone-200 text-stone-600";
      case "Deleted":
        return "bg-red-100 text-red-700";
      default:
        return "bg-stone-100 text-stone-600";
    }
  };

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold text-stone-900">{t("title")}</h1>
        <p className="mt-1 text-stone-500">{t("subtitle")}</p>
      </div>

      {/* Filters */}
      <div className="rounded-2xl border border-stone-200 bg-white p-4 shadow-sm">
        <div className="grid gap-3 sm:grid-cols-3">
          <label className="flex flex-col gap-1 text-sm sm:col-span-2">
            <span className="font-medium text-stone-600">
              {t("filters.search")}
            </span>
            <input
              value={draft.q}
              onChange={(e) => setDraft({ ...draft, q: e.target.value })}
              placeholder={t("filters.searchPlaceholder")}
              className="rounded-lg border border-stone-300 px-3 py-2"
              onKeyDown={(e) => {
                if (e.key === "Enter") apply();
              }}
            />
          </label>

          <label className="flex flex-col gap-1 text-sm">
            <span className="font-medium text-stone-600">
              {t("filters.status")}
            </span>
            <select
              value={draft.status}
              onChange={(e) => setDraft({ ...draft, status: e.target.value })}
              className="rounded-lg border border-stone-300 px-3 py-2"
            >
              <option value="">{t("filters.statusAny")}</option>
              {STATUSES.map((s) => (
                <option key={s} value={s}>
                  {statusLabel(s)}
                </option>
              ))}
            </select>
          </label>
        </div>

        <div className="mt-3 flex gap-2">
          <button
            type="button"
            onClick={apply}
            className="rounded-lg bg-rose-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-rose-700"
          >
            {t("filters.apply")}
          </button>
          <button
            type="button"
            onClick={reset}
            className="rounded-lg border border-stone-300 px-4 py-2 text-sm font-medium text-stone-700 transition hover:bg-stone-100"
          >
            {t("filters.reset")}
          </button>
        </div>
      </div>

      {/* Results */}
      {error ? (
        <p className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {t("error")}
        </p>
      ) : loading && !data ? (
        <p className="text-sm text-stone-500">{t("loading")}</p>
      ) : total === 0 ? (
        <p className="rounded-xl border border-stone-200 bg-white px-4 py-8 text-center text-sm text-stone-500">
          {t("empty")}
        </p>
      ) : (
        <>
          <div className="overflow-x-auto rounded-2xl border border-stone-200 bg-white shadow-sm">
            <table className="w-full min-w-[760px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-stone-200 text-left text-stone-500">
                  <th className="px-4 py-3 font-medium">{t("col.title")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.status")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.owner")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.package")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.expires")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.created")}</th>
                </tr>
              </thead>
              <tbody>
                {data!.items.map((e) => (
                  <tr
                    key={e.id}
                    className="border-b border-stone-100 last:border-0"
                  >
                    <td className="px-4 py-3">
                      <Link
                        href={`/admin/events/${e.id}`}
                        className="font-medium text-rose-700 hover:underline"
                      >
                        {e.title}
                      </Link>
                    </td>
                    <td className="px-4 py-3">
                      <span
                        className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusBadge(e.status)}`}
                      >
                        {statusLabel(e.status)}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-stone-600">
                      {e.ownerEmail ?? (
                        <span className="text-xs text-stone-400">
                          {e.ownerUserId}
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-stone-600">
                      {e.packageName ?? (
                        <span className="text-stone-300">—</span>
                      )}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-stone-600">
                      {e.expiresAt ? fmtDate(e.expiresAt) : "—"}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-stone-600">
                      {fmtDate(e.createdAt)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          <div className="flex items-center justify-between text-sm text-stone-500">
            <span>{t("totalCount", { total })}</span>
            <div className="flex items-center gap-3">
              <button
                type="button"
                disabled={page <= 1 || loading}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                className="rounded-lg border border-stone-300 px-3 py-1.5 font-medium text-stone-700 transition enabled:hover:bg-stone-100 disabled:opacity-40"
              >
                {t("prev")}
              </button>
              <span>{t("pageOf", { page, pages })}</span>
              <button
                type="button"
                disabled={page >= pages || loading}
                onClick={() => setPage((p) => Math.min(pages, p + 1))}
                className="rounded-lg border border-stone-300 px-3 py-1.5 font-medium text-stone-700 transition enabled:hover:bg-stone-100 disabled:opacity-40"
              >
                {t("next")}
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
