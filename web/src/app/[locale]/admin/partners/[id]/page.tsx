"use client";

import { useCallback, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import {
  type BonusCode,
  type PartnerDetail,
  createCode,
  getPartner,
  toggleCode,
} from "@/lib/adminApi";
import { ApiError } from "@/lib/hostApi";

export default function AdminPartnerDetailPage() {
  const t = useTranslations("admin.partners");
  const locale = useLocale();
  const params = useParams<{ id: string }>();
  const id = params.id;

  const [partner, setPartner] = useState<PartnerDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  // create-code form
  const [code, setCode] = useState("");
  const [discountType, setDiscountType] = useState("Percentage");
  const [discountValue, setDiscountValue] = useState("");
  const [maxRedemptions, setMaxRedemptions] = useState("");
  const [expiresAt, setExpiresAt] = useState("");
  const [saving, setSaving] = useState(false);
  const [codeError, setCodeError] = useState<string | null>(null);
  const [busyCodeId, setBusyCodeId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(false);
    try {
      setPartner(await getPartner(id));
    } catch {
      setError(true);
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    void load();
  }, [load]);

  const fmtDate = (iso: string | null) =>
    iso ? new Date(iso).toLocaleDateString(locale) : "—";

  const discountLabel = (c: BonusCode) =>
    c.discountType === "Percentage"
      ? `${c.discountValue}%`
      : new Intl.NumberFormat(locale, { style: "currency", currency: "EUR" }).format(
          c.discountValue / 100,
        );

  const submitCode = async () => {
    const value = parseInt(discountValue, 10);
    if (saving || !code.trim() || Number.isNaN(value)) return;
    setSaving(true);
    setCodeError(null);
    try {
      await createCode(id, {
        code: code.trim(),
        discountType,
        discountValue: value,
        maxRedemptions: maxRedemptions ? parseInt(maxRedemptions, 10) : null,
        expiresAt: expiresAt || null,
      });
      setCode("");
      setDiscountValue("");
      setMaxRedemptions("");
      setExpiresAt("");
      await load();
    } catch (e) {
      const c = e instanceof ApiError ? e.code : null;
      setCodeError(
        c === "partners.code_duplicate"
          ? t("codes.errorDuplicate")
          : c === "partners.code_invalid"
            ? t("codes.errorInvalid")
            : t("codes.error"),
      );
    } finally {
      setSaving(false);
    }
  };

  const flip = async (c: BonusCode) => {
    if (busyCodeId) return;
    setBusyCodeId(c.id);
    try {
      const updated = await toggleCode(id, c.id, !c.isActive);
      setPartner((prev) =>
        prev
          ? { ...prev, codes: prev.codes.map((x) => (x.id === c.id ? updated : x)) }
          : prev,
      );
    } catch {
      // ignore; row stays as-is
    } finally {
      setBusyCodeId(null);
    }
  };

  const totalRedemptions = partner?.codes.reduce((s, c) => s + c.redemptionCount, 0) ?? 0;

  return (
    <div className="space-y-5">
      <Link
        href="/admin/partners"
        className="text-sm text-stone-500 transition hover:text-stone-800"
      >
        ← {t("detail.back")}
      </Link>

      {error ? (
        <p className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {t("error")}
        </p>
      ) : loading || !partner ? (
        <p className="text-sm text-stone-500">{t("loading")}</p>
      ) : (
        <>
          <div>
            <h1 className="text-2xl font-semibold text-stone-900">{partner.name}</h1>
            <p className="mt-1 text-sm text-stone-500">
              {t(`type.${partner.type}`)}
              {partner.contactEmail ? ` · ${partner.contactEmail}` : ""}
              {partner.contactPhone ? ` · ${partner.contactPhone}` : ""}
            </p>
            {partner.notes && (
              <p className="mt-2 text-sm text-stone-600">{partner.notes}</p>
            )}
          </div>

          {/* Report summary */}
          <div className="grid gap-4 sm:grid-cols-3">
            <Stat label={t("report.codes")} value={String(partner.codes.length)} />
            <Stat label={t("report.redemptions")} value={String(totalRedemptions)} />
            <Stat
              label={t("report.active")}
              value={String(partner.codes.filter((c) => c.isActive).length)}
            />
          </div>

          {/* Create code */}
          <div className="rounded-2xl border border-stone-200 bg-white p-4 shadow-sm">
            <h2 className="font-semibold text-stone-900">{t("codes.createTitle")}</h2>
            <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
              <input
                value={code}
                onChange={(e) => setCode(e.target.value.toUpperCase())}
                placeholder={t("codes.code")}
                className="rounded-lg border border-stone-300 px-3 py-2 text-sm uppercase"
              />
              <select
                value={discountType}
                onChange={(e) => setDiscountType(e.target.value)}
                className="rounded-lg border border-stone-300 px-3 py-2 text-sm"
              >
                <option value="Percentage">{t("codes.percentage")}</option>
                <option value="FixedAmount">{t("codes.fixed")}</option>
              </select>
              <input
                value={discountValue}
                onChange={(e) => setDiscountValue(e.target.value)}
                inputMode="numeric"
                placeholder={
                  discountType === "Percentage"
                    ? t("codes.valuePercent")
                    : t("codes.valueCents")
                }
                className="rounded-lg border border-stone-300 px-3 py-2 text-sm"
              />
              <input
                value={maxRedemptions}
                onChange={(e) => setMaxRedemptions(e.target.value)}
                inputMode="numeric"
                placeholder={t("codes.maxRedemptions")}
                className="rounded-lg border border-stone-300 px-3 py-2 text-sm"
              />
              <input
                type="date"
                value={expiresAt}
                onChange={(e) => setExpiresAt(e.target.value)}
                className="rounded-lg border border-stone-300 px-3 py-2 text-sm"
              />
            </div>
            <div className="mt-3 flex items-center gap-3">
              <button
                type="button"
                onClick={() => void submitCode()}
                disabled={saving || !code.trim() || !discountValue}
                className="rounded-lg bg-rose-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-rose-700 disabled:opacity-50"
              >
                {saving ? t("codes.saving") : t("codes.add")}
              </button>
              {codeError && <span className="text-sm text-red-600">{codeError}</span>}
            </div>
            <p className="mt-2 text-xs text-stone-400">{t("codes.hint")}</p>
          </div>

          {/* Codes table */}
          {partner.codes.length === 0 ? (
            <p className="rounded-xl border border-stone-200 bg-white px-4 py-8 text-center text-sm text-stone-500">
              {t("codes.empty")}
            </p>
          ) : (
            <div className="overflow-x-auto rounded-2xl border border-stone-200 bg-white shadow-sm">
              <table className="w-full min-w-[640px] border-collapse text-sm">
                <thead>
                  <tr className="border-b border-stone-200 text-left text-stone-500">
                    <th className="px-4 py-3 font-medium">{t("codes.code")}</th>
                    <th className="px-4 py-3 font-medium">{t("codes.discount")}</th>
                    <th className="px-4 py-3 font-medium">{t("codes.redemptions")}</th>
                    <th className="px-4 py-3 font-medium">{t("codes.expires")}</th>
                    <th className="px-4 py-3 font-medium">{t("codes.status")}</th>
                    <th className="px-4 py-3 font-medium"></th>
                  </tr>
                </thead>
                <tbody>
                  {partner.codes.map((c) => (
                    <tr key={c.id} className="border-b border-stone-100 last:border-0">
                      <td className="px-4 py-3">
                        <code className="rounded bg-stone-100 px-1.5 py-0.5 text-xs font-semibold text-stone-800">
                          {c.code}
                        </code>
                      </td>
                      <td className="px-4 py-3 text-stone-600">{discountLabel(c)}</td>
                      <td className="px-4 py-3 text-stone-600">
                        {c.redemptionCount}
                        {c.maxRedemptions ? ` / ${c.maxRedemptions}` : ""}
                      </td>
                      <td className="px-4 py-3 text-stone-600">{fmtDate(c.expiresAt)}</td>
                      <td className="px-4 py-3">
                        <span
                          className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                            c.isActive
                              ? "bg-emerald-100 text-emerald-700"
                              : "bg-stone-200 text-stone-600"
                          }`}
                        >
                          {c.isActive ? t("codes.activeYes") : t("codes.activeNo")}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-right">
                        <button
                          type="button"
                          disabled={busyCodeId === c.id}
                          onClick={() => void flip(c)}
                          className="rounded-lg border border-stone-300 px-3 py-1.5 text-xs font-medium text-stone-700 transition hover:bg-stone-100 disabled:opacity-50"
                        >
                          {c.isActive ? t("codes.disable") : t("codes.enable")}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          <p className="text-xs text-stone-400">{t("report.redemptionsHint")}</p>
        </>
      )}
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-stone-200 bg-white p-4">
      <div className="text-xs font-medium uppercase tracking-wide text-stone-400">
        {label}
      </div>
      <div className="mt-1 text-2xl font-semibold text-stone-900">{value}</div>
    </div>
  );
}
