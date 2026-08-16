"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useParams } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import { getPackages, putToStorage, type PublicPackage } from "@/lib/guestApi";
import {
  COVER_ALLOWED_TYPES,
  COVER_MAX_BYTES,
  ApiError,
  type CheckoutR1,
  type EventStats,
  type HostEvent,
  activateEvent,
  closeUpload,
  confirmCover,
  getEvent,
  getEventStats,
  getQrPng,
  isAuthed,
  reopenUpload,
  startCheckout,
  startCoverUpload,
} from "@/lib/hostApi";

async function copyText(text: string): Promise<boolean> {
  try {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(text);
      return true;
    }
  } catch {
    // fall through
  }
  try {
    const ta = document.createElement("textarea");
    ta.value = text;
    ta.style.position = "fixed";
    ta.style.opacity = "0";
    document.body.appendChild(ta);
    ta.select();
    const ok = document.execCommand("copy");
    document.body.removeChild(ta);
    return ok;
  } catch {
    return false;
  }
}

export default function EventDetailPage() {
  const t = useTranslations("eventDetail");
  const tp = useTranslations("packages");
  const tStatus = useTranslations("dashboard.status");
  const router = useRouter();
  const { id } = useParams<{ id: string }>();

  const [event, setEvent] = useState<HostEvent | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [qrUrl, setQrUrl] = useState<string | null>(null);
  const [checkoutReturn, setCheckoutReturn] = useState<"success" | "cancel" | null>(null);

  const load = useCallback(async () => {
    try {
      setEvent(await getEvent(id));
    } catch {
      setNotFound(true);
    }
  }, [id]);

  useEffect(() => {
    if (!isAuthed()) {
      router.replace("/login");
      return;
    }
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [router, load]);

  // Returned from Stripe (?checkout=success|cancel). On success re-load — the
  // webhook has (very likely) already flipped the event to Active.
  useEffect(() => {
    const v = new URLSearchParams(window.location.search).get("checkout");
    if (v === "success" || v === "cancel") {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setCheckoutReturn(v);
      if (v === "success") void load();
    }
  }, [load]);

  const shareable =
    event?.status === "Active" || event?.status === "UploadClosed";

  // Load the QR (authed → blob) once the event is shareable.
  useEffect(() => {
    if (!shareable) return;
    let revoke: string | null = null;
    getQrPng(id, 20)
      .then((blob) => {
        const url = URL.createObjectURL(blob);
        revoke = url;
        setQrUrl(url);
      })
      .catch(() => setQrUrl(null));
    return () => {
      if (revoke) URL.revokeObjectURL(revoke);
    };
  }, [id, shareable]);

  if (notFound) {
    return (
      <main className="mx-auto min-h-dvh w-full max-w-md bg-[#FFFDF9] px-5 py-8">
        <Link href="/dashboard" className="text-sm text-[#57534E]">
          ‹ {t("back")}
        </Link>
        <p className="mt-10 text-center text-sm text-[#A8A29E]">
          {t("notFound")}
        </p>
      </main>
    );
  }

  return (
    <main className="mx-auto min-h-dvh w-full max-w-md bg-[#FFFDF9] px-5 py-8">
      <Link href="/dashboard" className="text-sm text-[#57534E]">
        ‹ {t("back")}
      </Link>

      {event === null ? (
        <p className="mt-10 text-center text-sm text-[#A8A29E]">{t("loading")}</p>
      ) : (
        <>
          {checkoutReturn && (
            <div
              className={`mt-4 rounded-xl px-4 py-3 text-sm ${
                checkoutReturn === "success"
                  ? "bg-[#EAF3EC] text-[#2F6B3A]"
                  : "bg-[#FBF3F0] text-[#7C2D3E]"
              }`}
            >
              {checkoutReturn === "success" ? t("checkoutSuccess") : t("checkoutCancel")}
            </div>
          )}
          <div className="mt-4 flex items-start justify-between gap-3">
            <div className="min-w-0">
              <h1 className="truncate text-xl font-semibold tracking-tight text-[#1C1917]">
                {event.title}
              </h1>
              <p className="mt-0.5 text-xs text-[#A8A29E]">
                {t("uploadStart")}: {event.uploadStartDate}
              </p>
              {event.packageName && (
                <p className="mt-0.5 text-xs text-[#A8A29E]">
                  {t("package")}:{" "}
                  {event.packageSlug ? tp(`${event.packageSlug}.name`) : event.packageName}
                </p>
              )}
              {event.uploadEndsAt && (
                <p className="mt-0.5 text-xs text-[#A8A29E]">
                  {t("uploadEnds")}: {event.uploadEndsAt}
                </p>
              )}
              {event.expiresAt && (
                <p className="mt-0.5 text-xs text-[#A8A29E]">
                  {t("expires")}: {event.expiresAt}
                </p>
              )}
            </div>
            <StatusBadge label={tStatus(event.status)} status={event.status} />
          </div>

          <CoverSection event={event} onUpdated={setEvent} />

          {event.status === "Draft" && (
            <ActivateSection event={event} onUpdated={setEvent} />
          )}

          {shareable && (
            <>
              <StatsSection eventId={event.id} />
              <ShareSection event={event} qrUrl={qrUrl} />
              <UploadPeriodSection event={event} onUpdated={setEvent} />
            </>
          )}
        </>
      )}
    </main>
  );
}

const GB = 1024 ** 3;
const MB = 1024 ** 2;

function formatBytes(bytes: number): string {
  if (bytes >= GB) return `${(bytes / GB).toFixed(bytes >= 10 * GB ? 0 : 1)} GB`;
  if (bytes >= MB) return `${Math.round(bytes / MB)} MB`;
  return `${Math.max(0, Math.round(bytes / 1024))} KB`;
}

function UsageBar({
  label,
  used,
  max,
  format,
}: {
  label: string;
  used: number;
  max: number | null;
  format: (n: number) => string;
}) {
  const t = useTranslations("eventDetail");
  const pct = max && max > 0 ? Math.min(100, Math.round((used / max) * 100)) : 0;
  const over = max !== null && used > max;
  return (
    <div>
      <div className="flex items-baseline justify-between text-xs">
        <span className="text-[#57534E]">{label}</span>
        <span className="text-[#A8A29E]">
          {max === null
            ? t("usageNoLimit", { used: format(used) })
            : `${format(used)} / ${format(max)}`}
        </span>
      </div>
      {max !== null && (
        <div className="mt-1 h-1.5 w-full overflow-hidden rounded-full bg-[#EFE9E2]">
          <div
            className={`h-full rounded-full ${over ? "bg-[#B4432F]" : "bg-[#7C2D3E]"}`}
            style={{ width: `${Math.max(2, pct)}%` }}
          />
        </div>
      )}
    </div>
  );
}

function StatsSection({ eventId }: { eventId: string }) {
  const t = useTranslations("eventDetail");
  const [stats, setStats] = useState<EventStats | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    let alive = true;
    getEventStats(eventId)
      .then((s) => alive && setStats(s))
      .catch(() => alive && setFailed(true));
    return () => {
      alive = false;
    };
  }, [eventId]);

  if (failed) return null;

  return (
    <section className="mt-5 rounded-2xl border border-[#E7E0D8] bg-white p-5">
      <h2 className="text-sm font-medium text-[#44403C]">{t("usage")}</h2>
      {stats === null ? (
        <p className="mt-2 text-xs text-[#A8A29E]">{t("usageLoading")}</p>
      ) : (
        <div className="mt-3 space-y-3">
          <UsageBar
            label={t("usagePhotos")}
            used={stats.photoCount}
            max={stats.maxPhotoCount}
            format={(n) => `${n}`}
          />
          <UsageBar
            label={t("usageVideo")}
            used={stats.videoBytes}
            max={stats.maxVideoTotalBytes}
            format={formatBytes}
          />
          <UsageBar
            label={t("usageTotal")}
            used={stats.totalBytes}
            max={stats.maxTotalBytes}
            format={formatBytes}
          />
        </div>
      )}
    </section>
  );
}

function UploadPeriodSection({
  event,
  onUpdated,
}: {
  event: HostEvent;
  onUpdated: (e: HostEvent) => void;
}) {
  const t = useTranslations("eventDetail");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(false);
  const closed = event.status === "UploadClosed";

  const toggle = async () => {
    if (busy) return;
    setBusy(true);
    setError(false);
    try {
      onUpdated(closed ? await reopenUpload(event.id) : await closeUpload(event.id));
    } catch {
      setError(true);
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="mt-5 rounded-2xl border border-[#E7E0D8] bg-white p-5">
      <h2 className="text-sm font-medium text-[#44403C]">{t("uploadPeriod")}</h2>
      <p className="mt-1 text-xs text-[#A8A29E]">
        {closed ? t("uploadClosedHint") : t("uploadOpenHint")}
      </p>
      <button
        type="button"
        onClick={toggle}
        disabled={busy}
        className={`mt-3 w-full rounded-xl px-4 py-2.5 text-sm font-medium transition active:scale-[0.99] disabled:opacity-60 ${
          closed
            ? "bg-[#7C2D3E] text-white"
            : "border border-[#7C2D3E] text-[#7C2D3E]"
        }`}
      >
        {busy
          ? closed
            ? t("reopening")
            : t("closing")
          : closed
            ? t("reopenUpload")
            : t("closeUpload")}
      </button>
      {error && (
        <p className="mt-2 text-sm text-[#B4432F]">{t("uploadPeriodError")}</p>
      )}
    </section>
  );
}

function ActivateSection({
  event,
  onUpdated,
}: {
  event: HostEvent;
  onUpdated: (e: HostEvent) => void;
}) {
  const t = useTranslations("eventDetail");
  const locale = useLocale();
  const [pkg, setPkg] = useState<PublicPackage | null | undefined>(undefined);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // R1 (company invoice) — only relevant for paid packages.
  const [needsR1, setNeedsR1] = useState(false);
  const [companyName, setCompanyName] = useState("");
  const [companyOib, setCompanyOib] = useState("");
  const [companyAddress, setCompanyAddress] = useState("");

  useEffect(() => {
    let alive = true;
    getPackages()
      .then((list) => alive && setPkg(list.find((p) => p.slug === event.packageSlug) ?? null))
      .catch(() => alive && setPkg(null));
    return () => {
      alive = false;
    };
  }, [event.packageSlug]);

  const isPaid = !!pkg && pkg.priceCents > 0;
  const money =
    pkg && pkg.priceCents > 0
      ? new Intl.NumberFormat(locale, { style: "currency", currency: pkg.currency }).format(
          pkg.priceCents / 100,
        )
      : "";

  const mapError = (e: unknown): string => {
    if (e instanceof ApiError && e.code === "events.cannot_checkout") return t("cannotCheckout");
    if (e instanceof ApiError && e.status === 401) return t("activateError");
    return t("activateError");
  };

  const activateFree = async () => {
    if (busy) return;
    setBusy(true);
    setError(null);
    try {
      onUpdated(await activateEvent(event.id));
    } catch (e) {
      setError(
        e instanceof ApiError && e.code === "events.free_limit_reached"
          ? t("freeLimitReached")
          : t("activateError"),
      );
      setBusy(false);
    }
  };

  const pay = async () => {
    if (busy) return;
    setBusy(true);
    setError(null);
    try {
      const r1: CheckoutR1 = {
        needsR1,
        companyName: needsR1 ? companyName.trim() || null : null,
        companyOib: needsR1 ? companyOib.trim() || null : null,
        companyAddress: needsR1 ? companyAddress.trim() || null : null,
      };
      const { url } = await startCheckout(event.id, r1);
      window.location.href = url; // hand off to Stripe's hosted checkout
    } catch (e) {
      setError(mapError(e));
      setBusy(false);
    }
  };

  // Still resolving which package this event uses.
  if (pkg === undefined) {
    return (
      <section className="mt-5 rounded-2xl border border-[#E7E0D8] bg-white p-5">
        <p className="text-sm text-[#A8A29E]">{t("draftHint")}</p>
      </section>
    );
  }

  const inputClass =
    "mt-1 w-full rounded-lg border border-[#E7E0D8] bg-[#FFFDF9] px-3 py-2.5 text-sm text-[#1C1917] outline-none focus:border-[#7C2D3E]";

  return (
    <section className="mt-5 rounded-2xl border border-[#E7E0D8] bg-white p-5">
      <p className="text-sm text-[#57534E]">{t("draftHint")}</p>

      {isPaid ? (
        <>
          <label className="mt-3 flex items-center gap-2 text-sm text-[#44403C]">
            <input
              type="checkbox"
              checked={needsR1}
              onChange={(e) => setNeedsR1(e.target.checked)}
              className="h-4 w-4 accent-[#7C2D3E]"
            />
            {t("needR1")}
          </label>

          {needsR1 && (
            <div className="mt-3 space-y-2">
              <input
                type="text"
                value={companyName}
                placeholder={t("companyName")}
                onChange={(e) => setCompanyName(e.target.value)}
                className={inputClass}
              />
              <input
                type="text"
                value={companyOib}
                placeholder={t("companyOib")}
                onChange={(e) => setCompanyOib(e.target.value)}
                className={inputClass}
              />
              <input
                type="text"
                value={companyAddress}
                placeholder={t("companyAddress")}
                onChange={(e) => setCompanyAddress(e.target.value)}
                className={inputClass}
              />
            </div>
          )}

          <button
            type="button"
            onClick={pay}
            disabled={busy}
            className="mt-3 w-full rounded-xl bg-[#7C2D3E] px-4 py-3 font-medium text-white transition active:scale-[0.99] disabled:opacity-60"
          >
            {busy ? t("redirecting") : t("payAndActivate", { price: money })}
          </button>
        </>
      ) : (
        <button
          type="button"
          onClick={activateFree}
          disabled={busy}
          className="mt-3 w-full rounded-xl bg-[#7C2D3E] px-4 py-3 font-medium text-white transition active:scale-[0.99] disabled:opacity-60"
        >
          {busy ? t("activating") : t("activate")}
        </button>
      )}

      {error && <p className="mt-2 text-sm text-[#B4432F]">{error}</p>}
    </section>
  );
}

function ShareSection({
  event,
  qrUrl,
}: {
  event: HostEvent;
  qrUrl: string | null;
}) {
  const t = useTranslations("eventDetail");
  const [copied, setCopied] = useState(false);

  const copy = async () => {
    if (await copyText(event.guestUrl)) {
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    }
  };

  return (
    <section className="mt-5 rounded-2xl border border-[#E7E0D8] bg-white p-5">
      <h2 className="text-sm font-medium text-[#44403C]">{t("guestLink")}</h2>
      <div className="mt-2 flex items-center gap-2">
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
          {copied ? t("copied") : t("copy")}
        </button>
      </div>
      <a
        href={event.guestUrl}
        target="_blank"
        rel="noopener noreferrer"
        className="mt-2 inline-block text-xs font-medium text-[#7C2D3E]"
      >
        {t("open")} ↗
      </a>

      <h2 className="mt-6 text-sm font-medium text-[#44403C]">{t("qrTitle")}</h2>
      <p className="mt-1 text-xs text-[#A8A29E]">{t("qrHint")}</p>
      <div className="mt-3 flex flex-col items-center">
        {qrUrl ? (
          <>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={qrUrl}
              alt="QR"
              className="h-48 w-48 rounded-lg border border-[#E7E0D8] bg-white p-2"
            />
            <a
              href={qrUrl}
              download={`wediframe-qr-${event.id}.png`}
              className="mt-3 rounded-lg bg-[#EFE7DC] px-4 py-2 text-sm font-medium text-[#7C2D3E]"
            >
              {t("qrDownload")}
            </a>
          </>
        ) : (
          <p className="py-8 text-xs text-[#A8A29E]">{t("qrLoading")}</p>
        )}
      </div>

      <Link
        href={`/dashboard/events/${event.id}/gallery`}
        className="mt-6 flex items-center justify-center rounded-xl bg-[#7C2D3E] px-4 py-3 font-medium text-white transition active:scale-[0.99]"
      >
        {t("manageGallery")}
      </Link>
    </section>
  );
}

function CoverSection({
  event,
  onUpdated,
}: {
  event: HostEvent;
  onUpdated: (e: HostEvent) => void;
}) {
  const t = useTranslations("eventDetail");
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onPick = async (file: File | undefined) => {
    if (!file || busy) return;
    const type = file.type.toLowerCase();
    if (!COVER_ALLOWED_TYPES.has(type)) {
      setError(t("coverTypeError"));
      return;
    }
    if (file.size <= 0 || file.size > COVER_MAX_BYTES) {
      setError(t("coverSizeError"));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const init = await startCoverUpload(event.id, type, file.size);
      await putToStorage(init.uploadUrl, file, init.contentType, () => {});
      onUpdated(await confirmCover(event.id, init.key));
    } catch {
      setError(t("coverError"));
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="mt-5 rounded-2xl border border-[#E7E0D8] bg-white p-5">
      <h2 className="text-sm font-medium text-[#44403C]">{t("cover")}</h2>

      <div className="mt-3 overflow-hidden rounded-xl border border-[#E7E0D8] bg-[#FBF8F4]">
        {event.coverPhotoUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={event.coverPhotoUrl}
            alt=""
            className="aspect-[3/2] w-full object-cover"
          />
        ) : (
          <div className="flex aspect-[3/2] w-full items-center justify-center text-xs text-[#A8A29E]">
            {t("coverNone")}
          </div>
        )}
      </div>

      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        className="sr-only"
        onChange={(e) => {
          void onPick(e.target.files?.[0]);
          e.target.value = "";
        }}
      />
      <button
        type="button"
        onClick={() => inputRef.current?.click()}
        disabled={busy}
        className="mt-3 w-full rounded-xl border border-[#7C2D3E] px-4 py-2.5 text-sm font-medium text-[#7C2D3E] transition active:scale-[0.99] disabled:opacity-60"
      >
        {busy
          ? t("coverUploading")
          : event.coverPhotoUrl
            ? t("coverReplace")
            : t("coverUpload")}
      </button>
      {error && <p className="mt-2 text-sm text-[#B4432F]">{error}</p>}
    </section>
  );
}

function StatusBadge({ label, status }: { label: string; status: string }) {
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
