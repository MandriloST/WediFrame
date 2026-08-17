"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import {
  ADMIN_MEDIA_PAGE_SIZE,
  type AdminMediaItem,
  deleteAdminMedia,
  getAdminMedia,
  setAdminMediaVisibility,
} from "@/lib/adminApi";
import {
  MediaLightbox,
  MediaThumb,
  tileFromServer,
  type MediaTile,
} from "@/components/media/MediaGallery";

/**
 * Admin moderation gallery. Reuses the shared grid tile + lightbox; lets an admin
 * hide/unhide or (soft) delete any item on any event. No download button — this is
 * moderation, not export. Updates are optimistic; a failure surfaces an error and
 * the next load reconciles with the server.
 */
export function AdminGallery({ eventId }: { eventId: string }) {
  const t = useTranslations("admin.gallery");
  const [items, setItems] = useState<AdminMediaItem[]>([]);
  const [nextOffset, setNextOffset] = useState<number | null>(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);
  const [lightbox, setLightbox] = useState<number | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [actionError, setActionError] = useState(false);
  const started = useRef(false);

  const loadMore = useCallback(async () => {
    setLoading(true);
    setError(false);
    try {
      const offset = nextOffset ?? 0;
      const page = await getAdminMedia(eventId, offset, ADMIN_MEDIA_PAGE_SIZE);
      setItems((prev) => {
        const seen = new Set(prev.map((i) => i.mediaId));
        const fresh = page.items.filter((i) => !seen.has(i.mediaId));
        return fresh.length === 0 ? prev : [...prev, ...fresh];
      });
      setNextOffset(page.nextOffset);
    } catch {
      setError(true);
    } finally {
      setLoading(false);
    }
  }, [eventId, nextOffset]);

  useEffect(() => {
    if (started.current) return;
    started.current = true;
    void loadMore();
  }, [loadMore]);

  const tiles: MediaTile[] = useMemo(() => items.map(tileFromServer), [items]);

  const closeLightbox = useCallback(() => {
    setLightbox(null);
    setConfirmingDelete(false);
    setActionError(false);
  }, []);

  const step = useCallback(
    (dir: 1 | -1) => {
      setConfirmingDelete(false);
      setActionError(false);
      setLightbox((cur) => {
        if (cur === null) return cur;
        const next = cur + dir;
        return next < 0 || next >= tiles.length ? cur : next;
      });
    },
    [tiles.length],
  );

  const openLightbox = useCallback((index: number) => {
    setConfirmingDelete(false);
    setActionError(false);
    setLightbox(index);
  }, []);

  useEffect(() => {
    if (lightbox === null) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") closeLightbox();
      else if (e.key === "ArrowRight") step(1);
      else if (e.key === "ArrowLeft") step(-1);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [lightbox, closeLightbox, step]);

  const toggleVisibility = useCallback(
    async (item: AdminMediaItem) => {
      if (busyId) return;
      const target = item.visibility === "Hidden" ? "Visible" : "Hidden";
      setBusyId(item.mediaId);
      setActionError(false);
      try {
        const res = await setAdminMediaVisibility(eventId, item.mediaId, target);
        setItems((prev) =>
          prev.map((i) =>
            i.mediaId === item.mediaId ? { ...i, visibility: res.visibility } : i,
          ),
        );
      } catch {
        setActionError(true);
      } finally {
        setBusyId(null);
      }
    },
    [eventId, busyId],
  );

  const removeItem = useCallback(
    async (item: AdminMediaItem) => {
      if (busyId) return;
      setBusyId(item.mediaId);
      setActionError(false);
      try {
        await deleteAdminMedia(eventId, item.mediaId);
        setItems((prev) => {
          const next = prev.filter((i) => i.mediaId !== item.mediaId);
          setLightbox((cur) => {
            if (cur === null) return cur;
            if (next.length === 0) return null;
            return Math.min(cur, next.length - 1);
          });
          return next;
        });
        setConfirmingDelete(false);
      } catch {
        setActionError(true);
      } finally {
        setBusyId(null);
      }
    },
    [eventId, busyId],
  );

  const initialLoading = loading && tiles.length === 0;
  const isEmpty = !loading && !error && tiles.length === 0 && nextOffset === null;

  const current = lightbox !== null ? items[lightbox] : null;
  const isBusy = current !== null && busyId === current.mediaId;

  return (
    <section className="mt-2">
      {tiles.length > 0 && (
        <div className="mb-3 flex items-baseline justify-between">
          <span className="text-xs text-stone-500">
            {t("count", { count: tiles.length })}
          </span>
        </div>
      )}

      {tiles.length > 0 && (
        <p className="mb-3 text-xs text-stone-400">{t("hiddenHint")}</p>
      )}

      {isEmpty && (
        <p className="rounded-2xl border border-dashed border-stone-200 px-6 py-10 text-center text-sm text-stone-400">
          {t("empty")}
        </p>
      )}

      {initialLoading && (
        <div className="grid grid-cols-3 gap-1.5 sm:grid-cols-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <div
              key={i}
              className="aspect-square animate-pulse rounded-md bg-stone-100"
            />
          ))}
        </div>
      )}

      {tiles.length > 0 && (
        <ul className="grid grid-cols-3 gap-1.5 sm:grid-cols-4">
          {tiles.map((tile, index) => {
            const hidden = items[index].visibility === "Hidden";
            return (
              <li key={tile.mediaId}>
                <button
                  type="button"
                  onClick={() => openLightbox(index)}
                  className="group relative block aspect-square w-full overflow-hidden rounded-md bg-stone-100"
                >
                  <MediaThumb tile={tile} />
                  {hidden && (
                    <>
                      <span className="absolute inset-0 bg-white/55" />
                      <span className="absolute left-1 top-1 flex h-6 w-6 items-center justify-center rounded-full bg-black/60 text-white">
                        <EyeOffIcon />
                      </span>
                    </>
                  )}
                </button>
              </li>
            );
          })}
        </ul>
      )}

      {error && (
        <div className="mt-4 text-center">
          <p className="text-sm text-red-600">{t("loadError")}</p>
          <button
            type="button"
            onClick={() => void loadMore()}
            className="mt-2 rounded-full border border-rose-600 px-4 py-1.5 text-xs font-semibold text-rose-700"
          >
            {t("retry")}
          </button>
        </div>
      )}

      {!error && nextOffset !== null && tiles.length > 0 && (
        <div className="mt-4 text-center">
          <button
            type="button"
            onClick={() => void loadMore()}
            disabled={loading}
            className="rounded-full border border-stone-300 bg-white px-6 py-2.5 text-sm font-semibold text-stone-700 disabled:opacity-60"
          >
            {t("loadMore")}
          </button>
        </div>
      )}

      {lightbox !== null && tiles[lightbox] && current && (
        <MediaLightbox
          tile={tiles[lightbox]}
          hasPrev={lightbox > 0}
          hasNext={lightbox < tiles.length - 1}
          onPrev={() => step(-1)}
          onNext={() => step(1)}
          onClose={closeLightbox}
          labels={{
            close: t("close"),
            prev: t("prev"),
            next: t("next"),
            unsupported: t("unsupported"),
            download: t("download"),
          }}
          actions={
            <div className="flex w-full max-w-md flex-col items-center gap-2">
              {actionError && (
                <p className="text-xs text-red-300">{t("actionError")}</p>
              )}
              <div className="flex items-center justify-center gap-2">
                <button
                  type="button"
                  disabled={isBusy}
                  onClick={() => void toggleVisibility(current)}
                  className="rounded-full bg-white/15 px-5 py-2.5 text-sm font-semibold text-white backdrop-blur disabled:opacity-50"
                >
                  {current.visibility === "Hidden" ? t("unhide") : t("hide")}
                </button>

                {confirmingDelete ? (
                  <>
                    <button
                      type="button"
                      disabled={isBusy}
                      onClick={() => void removeItem(current)}
                      className="rounded-full bg-red-600 px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
                    >
                      {isBusy ? t("deleting") : t("confirmDelete")}
                    </button>
                    <button
                      type="button"
                      disabled={isBusy}
                      onClick={() => setConfirmingDelete(false)}
                      className="rounded-full bg-white/15 px-4 py-2.5 text-sm font-semibold text-white backdrop-blur disabled:opacity-50"
                    >
                      {t("cancel")}
                    </button>
                  </>
                ) : (
                  <button
                    type="button"
                    disabled={isBusy}
                    onClick={() => setConfirmingDelete(true)}
                    className="rounded-full bg-white/15 px-5 py-2.5 text-sm font-semibold text-red-300 backdrop-blur disabled:opacity-50"
                  >
                    {t("delete")}
                  </button>
                )}
              </div>
            </div>
          }
        />
      )}
    </section>
  );
}

function EyeOffIcon() {
  return (
    <svg
      width="14"
      height="14"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden
    >
      <path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 10 8 10 8a18.5 18.5 0 0 1-2.16 3.19M6.61 6.61A18.45 18.45 0 0 0 2 12s3 8 10 8a9.12 9.12 0 0 0 5.39-1.61" />
      <line x1="2" y1="2" x2="22" y2="22" />
    </svg>
  );
}
