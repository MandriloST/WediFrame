"use client";

import { useCallback, useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import {
  type PartnerListItem,
  type Paged,
  createPartner,
  getPartners,
} from "@/lib/adminApi";
import { ApiError } from "@/lib/hostApi";

const PAGE_SIZE = 50;
const TYPES = ["Photographer", "Videographer", "Venue", "Planner", "Organizer", "Other"];

export default function AdminPartnersPage() {
  const t = useTranslations("admin.partners");
  const locale = useLocale();

  const [q, setQ] = useState("");
  const [applied, setApplied] = useState("");
  const [page, setPage] = useState(1);
  const [data, setData] = useState<Paged<PartnerListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  // create form
  const [name, setName] = useState("");
  const [type, setType] = useState("Photographer");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(false);
    try {
      const res = await getPartners({ page, pageSize: PAGE_SIZE, q: applied || undefined });
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

  const submit = async () => {
    if (creating || !name.trim()) return;
    setCreating(true);
    setCreateError(false);
    try {
      await createPartner({
        name: name.trim(),
        type,
        contactEmail: email.trim() || undefined,
        contactPhone: phone.trim() || undefined,
      });
      setName("");
      setEmail("");
      setPhone("");
      setPage(1);
      setApplied("");
      setQ("");
      await load();
    } catch (e) {
      setCreateError(!(e instanceof ApiError) || true);
    } finally {
      setCreating(false);
    }
  };

  const total = data?.total ?? 0;
  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE));
  const fmt = (iso: string) => new Date(iso).toLocaleDateString(locale);

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold text-stone-900">{t("title")}</h1>
        <p className="mt-1 text-stone-500">{t("subtitle")}</p>
      </div>

      {/* Create partner */}
      <div className="rounded-2xl border border-stone-200 bg-white p-4 shadow-sm">
        <h2 className="font-semibold text-stone-900">{t("create.title")}</h2>
        <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder={t("create.name")}
            className="rounded-lg border border-stone-300 px-3 py-2 text-sm"
          />
          <select
            value={type}
            onChange={(e) => setType(e.target.value)}
            className="rounded-lg border border-stone-300 px-3 py-2 text-sm"
          >
            {TYPES.map((x) => (
              <option key={x} value={x}>
                {t(`type.${x}`)}
              </option>
            ))}
          </select>
          <input
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder={t("create.email")}
            className="rounded-lg border border-stone-300 px-3 py-2 text-sm"
          />
          <input
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            placeholder={t("create.phone")}
            className="rounded-lg border border-stone-300 px-3 py-2 text-sm"
          />
        </div>
        <div className="mt-3 flex items-center gap-3">
          <button
            type="button"
            onClick={() => void submit()}
            disabled={creating || !name.trim()}
            className="rounded-lg bg-rose-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-rose-700 disabled:opacity-50"
          >
            {creating ? t("create.saving") : t("create.add")}
          </button>
          {createError && (
            <span className="text-sm text-red-600">{t("create.error")}</span>
          )}
        </div>
      </div>

      {/* Search */}
      <div className="flex gap-2">
        <input
          value={q}
          onChange={(e) => setQ(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              setPage(1);
              setApplied(q.trim());
            }
          }}
          placeholder={t("searchPlaceholder")}
          className="w-full max-w-sm rounded-lg border border-stone-300 px-3 py-2 text-sm"
        />
        <button
          type="button"
          onClick={() => {
            setPage(1);
            setApplied(q.trim());
          }}
          className="rounded-lg border border-stone-300 px-4 py-2 text-sm font-medium text-stone-700 transition hover:bg-stone-100"
        >
          {t("search")}
        </button>
      </div>

      {/* List */}
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
                  <th className="px-4 py-3 font-medium">{t("col.name")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.type")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.contact")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.codes")}</th>
                  <th className="px-4 py-3 font-medium">{t("col.created")}</th>
                </tr>
              </thead>
              <tbody>
                {data!.items.map((p) => (
                  <tr key={p.id} className="border-b border-stone-100 last:border-0">
                    <td className="px-4 py-3">
                      <Link
                        href={`/admin/partners/${p.id}`}
                        className="font-medium text-rose-700 hover:underline"
                      >
                        {p.name}
                      </Link>
                    </td>
                    <td className="px-4 py-3 text-stone-600">{t(`type.${p.type}`)}</td>
                    <td className="px-4 py-3 text-stone-600">
                      {p.contactEmail ?? p.contactPhone ?? "—"}
                    </td>
                    <td className="px-4 py-3 text-stone-600">{p.codeCount}</td>
                    <td className="whitespace-nowrap px-4 py-3 text-stone-600">
                      {fmt(p.createdAt)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="flex items-center justify-between text-sm text-stone-500">
            <span>{t("totalCount", { total })}</span>
            <div className="flex items-center gap-3">
              <button
                type="button"
                disabled={page <= 1 || loading}
                onClick={() => setPage((x) => Math.max(1, x - 1))}
                className="rounded-lg border border-stone-300 px-3 py-1.5 font-medium text-stone-700 transition enabled:hover:bg-stone-100 disabled:opacity-40"
              >
                {t("prev")}
              </button>
              <span>{t("pageOf", { page, pages })}</span>
              <button
                type="button"
                disabled={page >= pages || loading}
                onClick={() => setPage((x) => Math.min(pages, x + 1))}
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
