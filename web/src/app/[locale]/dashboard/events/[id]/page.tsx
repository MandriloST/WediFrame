"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import { putToStorage } from "@/lib/guestApi";
import {
  COVER_ALLOWED_TYPES,
  COVER_MAX_BYTES,
  type HostEvent,
  activateEvent,
  closeUpload,
  confirmCover,
  getEvent,
  getQrPng,
  isAuthed,
  reopenUpload,
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
  const tStatus = useTranslations("dashboard.status");
  const router = useRouter();
  const { id } = useParams<{ id: string }>();

  const [event, setEvent] = useState<HostEvent | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [qrUrl, setQrUrl] = useState<string | null>(null);

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
          <div className="mt-4 flex items-start justify-between gap-3">
            <div className="min-w-0">
              <h1 className="truncate text-xl font-semibold tracking-tight text-[#1C1917]">
                {event.title}
              </h1>
              <p className="mt-0.5 text-xs text-[#A8A29E]">
                {t("uploadStart")}: {event.uploadStartDate}
              </p>
            </div>
            <StatusBadge label={tStatus(event.status)} status={event.status} />
          </div>

          <CoverSection event={event} onUpdated={setEvent} />

          {event.status === "Draft" && (
            <ActivateSection event={event} onUpdated={setEvent} />
          )}

          {shareable && (
            <>
              <ShareSection event={event} qrUrl={qrUrl} />
              <UploadPeriodSection event={event} onUpdated={setEvent} />
            </>
          )}
        </>
      )}
    </main>
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
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(false);

  const activate = async () => {
    if (busy) return;
    setBusy(true);
    setError(false);
    try {
      onUpdated(await activateEvent(event.id));
    } catch {
      setError(true);
      setBusy(false);
    }
  };

  return (
    <section className="mt-5 rounded-2xl border border-[#E7E0D8] bg-white p-5">
      <p className="text-sm text-[#57534E]">{t("draftHint")}</p>
      <button
        type="button"
        onClick={activate}
        disabled={busy}
        className="mt-3 w-full rounded-xl bg-[#7C2D3E] px-4 py-3 font-medium text-white transition active:scale-[0.99] disabled:opacity-60"
      >
        {busy ? t("activating") : t("activate")}
      </button>
      {error && <p className="mt-2 text-sm text-[#B4432F]">{t("activateError")}</p>}
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
