"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import {
  MAX_ITEMS_PER_REQUEST,
  PHOTO_ALLOWED_TYPES,
  PHOTO_MAX_BYTES,
  confirmUpload,
  putToStorage,
  startUploads,
} from "@/lib/guestApi";

type ItemStatus = "queued" | "uploading" | "confirming" | "done" | "failed";

type QueueItem = {
  id: string; // local id; mediaId once presigned
  file: File;
  contentType: string;
  status: ItemStatus;
  progress: number; // 0..1
  errorKey: string | null; // i18n key under guest.errors
};

/** Fired the moment an item is confirmed, so the gallery can show it instantly. */
export type ConfirmedUpload = {
  mediaId: string;
  file: File;
  contentType: string;
};

const CONCURRENCY = 3;
const NAME_KEY = "wediframe.guestName";
const ACK_KEY_PREFIX = "wediframe.privacyAck:";

/**
 * A local queue id. crypto.randomUUID() exists only in secure contexts
 * (HTTPS/localhost), so it's undefined when testing over plain http on a LAN
 * IP. crypto.getRandomValues() has no such restriction; Math.random() is the
 * last-ditch fallback. This id never leaves the browser — the real mediaId
 * comes from the server — so any unique string is fine.
 */
function newLocalId(): string {
  const c = globalThis.crypto;
  if (c?.randomUUID) return c.randomUUID();
  if (c?.getRandomValues) {
    const b = c.getRandomValues(new Uint8Array(16));
    b[6] = (b[6] & 0x0f) | 0x40;
    b[8] = (b[8] & 0x3f) | 0x80;
    const h = Array.from(b, (x) => x.toString(16).padStart(2, "0"));
    return `${h[0]}${h[1]}${h[2]}${h[3]}-${h[4]}${h[5]}-${h[6]}${h[7]}-${h[8]}${h[9]}-${h[10]}${h[11]}${h[12]}${h[13]}${h[14]}${h[15]}`;
  }
  return `id-${Date.now()}-${Math.random().toString(36).slice(2)}`;
}

/** Some Androids report an empty file.type — fall back to the extension. */
function resolveContentType(file: File): string | null {
  if (file.type && PHOTO_ALLOWED_TYPES.has(file.type.toLowerCase())) {
    return file.type.toLowerCase();
  }
  const ext = file.name.split(".").pop()?.toLowerCase();
  const byExt: Record<string, string> = {
    jpg: "image/jpeg",
    jpeg: "image/jpeg",
    png: "image/png",
    webp: "image/webp",
    heic: "image/heic",
    heif: "image/heif",
    gif: "image/gif",
  };
  return ext && byExt[ext] ? byExt[ext] : null;
}

export function UploadSection({
  token,
  uploadOpen,
  onConfirmed,
}: {
  token: string;
  uploadOpen: boolean;
  onConfirmed?: (item: ConfirmedUpload) => void;
}) {
  const t = useTranslations("guest");
  const inputRef = useRef<HTMLInputElement>(null);
  const [items, setItems] = useState<QueueItem[]>([]);
  const [guestName, setGuestName] = useState("");
  const [acknowledged, setAcknowledged] = useState(false);
  const [showNotice, setShowNotice] = useState(false);
  const activeCount = useRef(0);

  // Keep the latest callback in a ref so uploadOne stays stable across renders.
  const onConfirmedRef = useRef(onConfirmed);
  useEffect(() => {
    onConfirmedRef.current = onConfirmed;
  }, [onConfirmed]);

  useEffect(() => {
    setGuestName(localStorage.getItem(NAME_KEY) ?? "");
    setAcknowledged(localStorage.getItem(ACK_KEY_PREFIX + token) === "1");
  }, [token]);

  const patch = useCallback((id: string, changes: Partial<QueueItem>) => {
    setItems((prev) =>
      prev.map((it) => (it.id === id ? { ...it, ...changes } : it)),
    );
  }, []);

  /** Presign + PUT + confirm for one item. Runs with limited concurrency. */
  const uploadOne = useCallback(
    async (item: QueueItem, name: string | null) => {
      try {
        patch(item.id, { status: "uploading", progress: 0, errorKey: null });
        const [presigned] = await startUploads(
          token,
          [
            {
              contentType: item.contentType,
              sizeBytes: item.file.size,
              fileName: item.file.name || null,
            },
          ],
          name,
        );
        await putToStorage(
          presigned.uploadUrl,
          item.file,
          presigned.contentType,
          (fraction) => patch(item.id, { progress: fraction }),
        );
        patch(item.id, { status: "confirming", progress: 1 });
        const confirmed = await confirmUpload(token, presigned.mediaId);
        patch(item.id, { status: "done" });
        // Instant gallery preview from the local file — no round-trip download.
        onConfirmedRef.current?.({
          mediaId: confirmed.mediaId,
          file: item.file,
          contentType: item.contentType,
        });
      } catch {
        patch(item.id, { status: "failed", errorKey: "uploadFailed" });
      }
    },
    [token, patch],
  );

  /** Simple pump: keeps up to CONCURRENCY uploads in flight. */
  const pump = useCallback(
    (queue: QueueItem[], name: string | null) => {
      const next = () => {
        if (activeCount.current >= CONCURRENCY) return;
        const candidate = queue.find((q) => q.status === "queued");
        if (!candidate) return;
        candidate.status = "uploading"; // reserve synchronously
        activeCount.current += 1;
        void uploadOne(candidate, name).finally(() => {
          activeCount.current -= 1;
          next();
        });
        next();
      };
      next();
    },
    [uploadOne],
  );

  const handleFiles = useCallback(
    (fileList: FileList | null) => {
      if (!fileList || fileList.length === 0) return;
      const files = Array.from(fileList).slice(0, MAX_ITEMS_PER_REQUEST);
      const fresh: QueueItem[] = files.map((file) => {
        const contentType = resolveContentType(file);
        const tooBig = file.size <= 0 || file.size > PHOTO_MAX_BYTES;
        return {
          id: newLocalId(),
          file,
          contentType: contentType ?? "",
          status: contentType && !tooBig ? "queued" : "failed",
          progress: 0,
          errorKey: !contentType
            ? "typeUnsupported"
            : tooBig
              ? "fileTooLarge"
              : null,
        };
      });
      setItems((prev) => [...prev, ...fresh]);
      const name = guestName.trim() || null;
      if (name) localStorage.setItem(NAME_KEY, name);
      pump(fresh, name);
    },
    [guestName, pump],
  );

  const retry = useCallback(
    (id: string) => {
      const item = items.find((it) => it.id === id);
      if (!item || item.status !== "failed" || !item.contentType) return;
      const revived: QueueItem = { ...item, status: "queued", progress: 0 };
      setItems((prev) => prev.map((it) => (it.id === id ? revived : it)));
      pump([revived], guestName.trim() || null);
    },
    [items, guestName, pump],
  );

  const openPicker = () => {
    if (!acknowledged) {
      setShowNotice(true);
      return;
    }
    inputRef.current?.click();
  };

  const acceptNotice = () => {
    localStorage.setItem(ACK_KEY_PREFIX + token, "1");
    if (guestName.trim()) localStorage.setItem(NAME_KEY, guestName.trim());
    setAcknowledged(true);
    setShowNotice(false);
    inputRef.current?.click();
  };

  const done = items.filter((i) => i.status === "done").length;
  const failed = items.filter((i) => i.status === "failed").length;
  const busy = items.length - done - failed;

  if (!uploadOpen) {
    return (
      <section className="px-5 pt-8 text-center">
        <p className="rounded-2xl border border-[#E7E0D8] bg-[#FFFDF9] px-6 py-5 text-sm text-[#57534E]">
          {t("uploadClosed")}
        </p>
      </section>
    );
  }

  return (
    <section className="px-5 pt-8">
      <input
        ref={inputRef}
        type="file"
        accept="image/*"
        multiple
        className="sr-only"
        onChange={(e) => {
          handleFiles(e.target.files);
          e.target.value = ""; // allow picking the same files again
        }}
      />

      <button
        type="button"
        onClick={openPicker}
        className="block w-full rounded-full bg-[#7C2D3E] px-6 py-4 text-center text-base font-semibold text-[#FFFDF9] shadow-[0_8px_20px_-8px_rgba(124,45,62,0.55)] transition active:scale-[0.98]"
      >
        {t("addPhotos")}
      </button>
      <p className="mt-2 text-center text-xs text-[#A8A29E]">
        {t("videoComingSoon")}
      </p>

      {/* First-visit privacy notice + optional name (PROJECT.md §4). */}
      {showNotice && (
        <div className="mt-4 rounded-2xl border border-[#E7E0D8] bg-[#FFFDF9] p-5">
          <p className="text-sm leading-relaxed text-[#44403C]">
            {t("privacyNotice")}{" "}
            <a href="/privacy" className="underline decoration-[#7C2D3E]/40">
              {t("privacyLink")}
            </a>
          </p>
          <input
            type="text"
            value={guestName}
            onChange={(e) => setGuestName(e.target.value)}
            placeholder={t("namePlaceholder")}
            maxLength={100}
            className="mt-4 w-full rounded-xl border border-[#E7E0D8] bg-white px-4 py-3 text-base outline-none focus:border-[#7C2D3E]"
          />
          <button
            type="button"
            onClick={acceptNotice}
            className="mt-3 w-full rounded-full bg-[#1C1917] px-6 py-3 text-sm font-semibold text-[#FFFDF9]"
          >
            {t("continue")}
          </button>
        </div>
      )}

      {/* Aggregate status: "N poslano · M čeka · K nije uspjelo". */}
      {items.length > 0 && (
        <div className="mt-6">
          <p
            className="text-center text-sm font-medium text-[#44403C]"
            aria-live="polite"
          >
            {t("statusLine", { done, busy, failed })}
          </p>

          <ul className="mt-3 space-y-2">
            {items.map((item) => (
              <li
                key={item.id}
                className="flex items-center gap-3 rounded-xl border border-[#E7E0D8] bg-[#FFFDF9] px-4 py-3"
              >
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm text-[#1C1917]">
                    {item.file.name}
                  </p>
                  {item.status === "uploading" && (
                    <div className="mt-1.5 h-1.5 overflow-hidden rounded-full bg-[#EFE7DC]">
                      <div
                        className="h-full rounded-full bg-[#7C2D3E] transition-[width]"
                        style={{ width: `${Math.round(item.progress * 100)}%` }}
                      />
                    </div>
                  )}
                  {item.status === "failed" && item.errorKey && (
                    <p className="mt-0.5 text-xs text-[#B4432F]">
                      {t(`errors.${item.errorKey}`)}
                    </p>
                  )}
                </div>
                {item.status === "done" && (
                  <span className="text-sm font-semibold text-[#4D7C5F]">
                    {t("itemDone")}
                  </span>
                )}
                {(item.status === "confirming" || item.status === "queued") && (
                  <span className="text-xs text-[#A8A29E]">
                    {t("itemWaiting")}
                  </span>
                )}
                {item.status === "failed" && item.contentType && (
                  <button
                    type="button"
                    onClick={() => retry(item.id)}
                    className="rounded-full border border-[#7C2D3E] px-3 py-1 text-xs font-semibold text-[#7C2D3E]"
                  >
                    {t("retry")}
                  </button>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}
    </section>
  );
}
