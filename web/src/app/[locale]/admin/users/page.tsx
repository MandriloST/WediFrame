"use client";

import { useCallback, useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { type AdminUser, type Paged, getUsers } from "@/lib/adminApi";

const PAGE_SIZE = 50;

type Filters = { q: string; role: string };
const EMPTY: Filters = { q: "", role: "" };

export default function AdminUsersPage() {
  const t = useTranslations("admin.users");
  const locale = useLocale();

  const [draft, setDraft] = useState<Filters>(EMPTY);
  const [applied, setApplied] = useState<Filters>(EMPTY);
  const [page, setPage] = useState(1);

  const [data, setData] = useState<Paged<AdminUser> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(false);
    try {
      const res = await getUsers({
        page,
        pageSize: PAGE_SIZE,
        q: applied.q.trim() || undefined,
        role: applied.role || undefined,
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

  const roleBadge = (role: string) =>
    role === "Admin"
      ? "bg-rose-100 text-rose-700"
      : "bg-stone-100 text-stone-600";

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
              {t("filters.role")}
            </span>
            <select
              value={draft.role}
              onChange={(e) => setDraft({ ...draft, role: e.target.value })}
              className="rounded-lg border border-stone-300 px-3 py-2"
            >
              <option value="">{t("filters.roleAny")}</option>
              <option value="Host">{t("filters.roleHost")}</option>
              <option value="Admin">{t("filters.roleAdmin")}</option>
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
            <table className="w-full min-w-[560px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-stone-200 text-left text-stone-500">
                  <th className="px-4 py-3 font-medium">{t("col.email")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.role")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.language")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.created")}</th>
                </tr>
              </thead>
              <tbody>
                {data!.items.map((u) => (
                  <tr
                    key={u.id}
                    className="border-b border-stone-100 last:border-0"
                  >
                    <td className="px-4 py-3 text-stone-800">{u.email}</td>
                    <td className="px-4 py-3">
                      <span
                        className={`rounded-full px-2 py-0.5 text-xs font-medium ${roleBadge(u.role)}`}
                      >
                        {u.role}
                      </span>
                    </td>
                    <td className="px-4 py-3 uppercase text-stone-500">
                      {u.preferredLanguage}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-stone-600">
                      {fmt(u.createdAt)}
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
