"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import {
  BROWSER_DISPLAYABLE_TYPES,
  GALLERY_PAGE_SIZE,
  getGuestMedia,
  getGuestMediaDownloadUrl,
  type GalleryItem,
} from "@/lib/guestApi";
import {
  MediaLightbox,
  MediaThumb,
  tileFromServer,
  triggerBrowserDownload,
  type MediaTile,
} from "@/components/media/MediaGallery";

/** A locally-uploaded item shown instantly from its blob, before the server lists it. */
export type GalleryPreview = {
  mediaId: string;
  url: string; // object URL of the local file
  contentType: string;
};

function previewTile(preview: GalleryPreview): MediaTile {
  return {
    mediaId: preview.mediaId,
    gridUrl: preview.url,
    fullUrl: preview.url,
    contentType: preview.contentType,
    isVideo: preview.contentType.startsWith("video/"),
    displayable: BROWSER_DISPLAYABLE_TYPES.has(preview.contentType.toLowerCase()),
    guestName: null,
  };
}

export function Gallery({
  token,
  previews,
}: {
  token: string;
  previews: GalleryPreview[];
}) {
  const t = useTranslations("guest.gallery");
  const [items, setItems] = useState<GalleryItem[]>([]);
  const [nextOffset, setNextOffset] = useState<number | null>(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);
  const [lightbox, setLightbox] = useState<number | null>(null);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);
  const started = useRef(false);

  const loadMore = useCallback(async () => {
    setLoading(true);
    setError(false);
    try {
      const offset = nextOffset ?? 0;
      const page = await getGuestMedia(token, offset, GALLERY_PAGE_SIZE);
      // Pure updater: dedupe against what's already in state. React Strict Mode
      // invokes state updaters twice in dev to surface impurities — mutating an
      // external ref here (the old loadedIds Set) made the second pass filter
      // out the whole page, so the gallery loaded then vanished.
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
  }, [token, nextOffset]);

  // Initial load once (guarded against React strict-mode double effect).
  useEffect(() => {
    if (started.current) return;
    started.current = true;
    void loadMore();
  }, [loadMore]);

  // Local previews on top; server items deduped against them.
  const tiles: MediaTile[] = useMemo(() => {
    const previewIds = new Set(previews.map((p) => p.mediaId));
    return [
      ...previews.map(previewTile),
      ...items.filter((i) => !previewIds.has(i.mediaId)).map(tileFromServer),
    ];
  }, [previews, items]);

  const closeLightbox = useCallback(() => setLightbox(null), []);
  const step = useCallback(
    (dir: 1 | -1) =>
      setLightbox((cur) => {
        if (cur === null) return cur;
        const next = cur + dir;
        return next < 0 || next >= tiles.length ? cur : next;
      }),
    [tiles.length],
  );

  // Keyboard nav while the lightbox is open.
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

  const download = useCallback(
    async (tile: MediaTile) => {
      if (downloadingId) return;
      setDownloadingId(tile.mediaId);
      try {
        const { url } = await getGuestMediaDownloadUrl(token, tile.mediaId);
        triggerBrowserDownload(url);
      } catch {
        // silent — the button re-enables so the guest can retry
      } finally {
        setDownloadingId(null);
      }
    },
    [token, downloadingId],
  );

  const initialLoading = loading && tiles.length === 0;
  const isEmpty = !loading && !error && tiles.length === 0 && nextOffset === null;

  return (
    <section className="px-5 pt-10">
      {tiles.length > 0 && (
        <div className="mb-3 flex items-baseline justify-between">
          <h2 className="text-sm font-semibold uppercase tracking-[0.18em] text-[#7C2D3E]">
            {t("heading")}
          </h2>
          <span className="text-xs text-[#A8A29E]">
            {t("count", { count: tiles.length })}
          </span>
        </div>
      )}

      {isEmpty && (
        <p className="rounded-2xl border border-dashed border-[#E7E0D8] px-6 py-8 text-center text-sm text-[#A8A29E]">
          {t("empty")}
        </p>
      )}

      {initialLoading && (
        <div className="grid grid-cols-3 gap-1.5">
          {Array.from({ length: 6 }).map((_, i) => (
            <div
              key={i}
              className="aspect-square animate-pulse rounded-md bg-[#EFE7DC]"
            />
          ))}
        </div>
      )}

      {tiles.length > 0 && (
        <ul className="grid grid-cols-3 gap-1.5">
          {tiles.map((tile, index) => (
            <li key={tile.mediaId}>
              <button
                type="button"
                onClick={() => setLightbox(index)}
                className="group relative block aspect-square w-full overflow-hidden rounded-md bg-[#EFE7DC]"
              >
                <MediaThumb tile={tile} />
              </button>
            </li>
          ))}
        </ul>
      )}

      {error && (
        <div className="mt-4 text-center">
          <p className="text-sm text-[#B4432F]">{t("loadError")}</p>
          <button
            type="button"
            onClick={() => void loadMore()}
            className="mt-2 rounded-full border border-[#7C2D3E] px-4 py-1.5 text-xs font-semibold text-[#7C2D3E]"
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
            className="rounded-full border border-[#E7E0D8] bg-[#FFFDF9] px-6 py-2.5 text-sm font-semibold text-[#44403C] disabled:opacity-60"
          >
            {t("loadMore")}
          </button>
        </div>
      )}

      {lightbox !== null && tiles[lightbox] && (
        <MediaLightbox
          tile={tiles[lightbox]}
          hasPrev={lightbox > 0}
          hasNext={lightbox < tiles.length - 1}
          onPrev={() => step(-1)}
          onNext={() => step(1)}
          onClose={closeLightbox}
          onDownload={() => void download(tiles[lightbox])}
          downloading={downloadingId === tiles[lightbox].mediaId}
          labels={{
            close: t("close"),
            prev: t("prev"),
            next: t("next"),
            unsupported: t("unsupported"),
            download: t("download"),
          }}
        />
      )}
    </section>
  );
}
