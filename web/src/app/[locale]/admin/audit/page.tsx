"use client";

import { useCallback, useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import {
  type AuditLogItem,
  type Paged,
  getAuditLog,
} from "@/lib/adminApi";

const PAGE_SIZE = 50;

// Known entity types in the audit trail (extend as modules add entries).
const ENTITY_TYPES = ["Event", "MediaItem"];

type Filters = {
  entityType: string;
  action: string;
  entityId: string;
  from: string; // yyyy-MM-dd (date input)
  to: string;
};

const EMPTY: Filters = {
  entityType: "",
  action: "",
  entityId: "",
  from: "",
  to: "",
};

/** date input (yyyy-MM-dd) → ISO at day start/end, or undefined. */
function dayStartIso(d: string): string | undefined {
  return d ? new Date(`${d}T00:00:00`).toISOString() : undefined;
}
function dayEndIso(d: string): string | undefined {
  return d ? new Date(`${d}T23:59:59.999`).toISOString() : undefined;
}

function prettyDetails(raw: string | null): string | null {
  if (!raw) return null;
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

export default function AdminAuditPage() {
  const t = useTranslations("admin.audit");
  const locale = useLocale();

  const [draft, setDraft] = useState<Filters>(EMPTY);
  const [applied, setApplied] = useState<Filters>(EMPTY);
  const [page, setPage] = useState(1);

  const [data, setData] = useState<Paged<AuditLogItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(false);
    try {
      const res = await getAuditLog({
        page,
        pageSize: PAGE_SIZE,
        entityType: applied.entityType || undefined,
        action: applied.action.trim() || undefined,
        entityId: applied.entityId.trim() || undefined,
        from: dayStartIso(applied.from),
        to: dayEndIso(applied.to),
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

  const fmt = (iso: string) =>
    new Date(iso).toLocaleString(locale, {
      dateStyle: "medium",
      timeStyle: "short",
    });

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold text-stone-900">{t("title")}</h1>
        <p className="mt-1 text-stone-500">{t("subtitle")}</p>
      </div>

      {/* Filters */}
      <div className="rounded-2xl border border-stone-200 bg-white p-4 shadow-sm">
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
          <label className="flex flex-col gap-1 text-sm">
            <span className="font-medium text-stone-600">
              {t("filters.entityType")}
            </span>
            <select
              value={draft.entityType}
              onChange={(e) =>
                setDraft({ ...draft, entityType: e.target.value })
              }
              className="rounded-lg border border-stone-300 px-3 py-2"
            >
              <option value="">{t("filters.entityTypeAny")}</option>
              {ENTITY_TYPES.map((x) => (
                <option key={x} value={x}>
                  {x}
                </option>
              ))}
            </select>
          </label>

          <label className="flex flex-col gap-1 text-sm">
            <span className="font-medium text-stone-600">
              {t("filters.action")}
            </span>
            <input
              value={draft.action}
              onChange={(e) => setDraft({ ...draft, action: e.target.value })}
              placeholder={t("filters.actionPlaceholder")}
              className="rounded-lg border border-stone-300 px-3 py-2"
            />
          </label>

          <label className="flex flex-col gap-1 text-sm">
            <span className="font-medium text-stone-600">
              {t("filters.entityId")}
            </span>
            <input
              value={draft.entityId}
              onChange={(e) => setDraft({ ...draft, entityId: e.target.value })}
              className="rounded-lg border border-stone-300 px-3 py-2"
            />
          </label>

          <label className="flex flex-col gap-1 text-sm">
            <span className="font-medium text-stone-600">
              {t("filters.from")}
            </span>
            <input
              type="date"
              value={draft.from}
              onChange={(e) => setDraft({ ...draft, from: e.target.value })}
              className="rounded-lg border border-stone-300 px-3 py-2"
            />
          </label>

          <label className="flex flex-col gap-1 text-sm">
            <span className="font-medium text-stone-600">{t("filters.to")}</span>
            <input
              type="date"
              value={draft.to}
              onChange={(e) => setDraft({ ...draft, to: e.target.value })}
              className="rounded-lg border border-stone-300 px-3 py-2"
            />
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
            <table className="w-full min-w-[720px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-stone-200 text-left text-stone-500">
                  <th className="px-4 py-3 font-medium">{t("col.time")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.action")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.entity")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.actor")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.details")}</th>
                </tr>
              </thead>
              <tbody>
                {data!.items.map((item) => {
                  const details = prettyDetails(item.details);
                  return (
                    <tr
                      key={item.id}
                      className="border-b border-stone-100 align-top last:border-0"
                    >
                      <td className="whitespace-nowrap px-4 py-3 text-stone-600">
                        {fmt(item.occurredAt)}
                      </td>
                      <td className="px-4 py-3">
                        <code className="rounded bg-stone-100 px-1.5 py-0.5 text-xs text-stone-800">
                          {item.action}
                        </code>
                      </td>
                      <td className="px-4 py-3 text-stone-600">
                        {item.entityType ? (
                          <span>
                            {item.entityType}
                            {item.entityId ? (
                              <span className="block text-xs text-stone-400">
                                {item.entityId}
                              </span>
                            ) : null}
                          </span>
                        ) : (
                          <span className="text-stone-300">—</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-stone-600">
                        {item.actorUserId ? (
                          <span className="text-xs">{item.actorUserId}</span>
                        ) : (
                          <span className="rounded bg-stone-100 px-1.5 py-0.5 text-xs text-stone-500">
                            {t("system")}
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-3">
                        {details ? (
                          <details>
                            <summary className="cursor-pointer text-xs text-rose-700">
                              {t("col.details")}
                            </summary>
                            <pre className="mt-2 max-w-md overflow-x-auto rounded-lg bg-stone-50 p-2 text-xs text-stone-700">
                              {details}
                            </pre>
                          </details>
                        ) : (
                          <span className="text-stone-300">—</span>
                        )}
                      </td>
                    </tr>
                  );
                })}
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
