"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import {
  BROWSER_DISPLAYABLE_TYPES,
  GALLERY_PAGE_SIZE,
  getGuestMedia,
  type GalleryItem,
} from "@/lib/guestApi";

/** A locally-uploaded item shown instantly from its blob, before the server lists it. */
export type GalleryPreview = {
  mediaId: string;
  url: string; // object URL of the local file
  contentType: string;
};

type Tile = {
  mediaId: string;
  gridUrl: string; // shown in the grid (thumbnail if available, else original/local)
  fullUrl: string; // shown in the lightbox
  contentType: string;
  isVideo: boolean;
  displayable: boolean; // can the browser render it in an <img>?
  guestName: string | null;
};

function serverTile(item: GalleryItem): Tile {
  return {
    mediaId: item.mediaId,
    gridUrl: item.thumbnailUrl ?? item.url,
    fullUrl: item.url,
    contentType: item.contentType,
    isVideo: item.type === "Video",
    // A thumbnail (jpg/webp from the job) is always displayable; otherwise it
    // depends on the original's type (HEIC/HEIF cannot render yet).
    displayable:
      item.thumbnailUrl != null ||
      BROWSER_DISPLAYABLE_TYPES.has(item.contentType.toLowerCase()),
    guestName: item.guestName,
  };
}

function previewTile(preview: GalleryPreview): Tile {
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
  const loadedIds = useRef<Set<string>>(new Set());
  const started = useRef(false);

  const loadMore = useCallback(async () => {
    setLoading(true);
    setError(false);
    try {
      const offset = nextOffset ?? 0;
      const page = await getGuestMedia(token, offset, GALLERY_PAGE_SIZE);
      setItems((prev) => {
        const fresh = page.items.filter((i) => !loadedIds.current.has(i.mediaId));
        fresh.forEach((i) => loadedIds.current.add(i.mediaId));
        return [...prev, ...fresh];
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
  const tiles: Tile[] = useMemo(() => {
    const previewIds = new Set(previews.map((p) => p.mediaId));
    return [
      ...previews.map(previewTile),
      ...items.filter((i) => !previewIds.has(i.mediaId)).map(serverTile),
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
                {tile.displayable ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img
                    src={tile.gridUrl}
                    alt=""
                    loading="lazy"
                    className="h-full w-full object-cover transition group-active:scale-[0.97]"
                  />
                ) : (
                  <PlaceholderTile />
                )}
                {tile.isVideo && <PlayBadge />}
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
        <Lightbox
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
          }}
        />
      )}
    </section>
  );
}

function PlaceholderTile() {
  return (
    <div className="flex h-full w-full items-center justify-center bg-[#EFE7DC] text-[#B8A99A]">
      <svg
        width="28"
        height="28"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        aria-hidden
      >
        <rect x="3" y="5" width="18" height="14" rx="2" />
        <circle cx="12" cy="12" r="3.2" />
        <path d="M8 5l1.2-2h5.6L16 5" />
      </svg>
    </div>
  );
}

function PlayBadge() {
  return (
    <span className="absolute bottom-1.5 right-1.5 flex h-6 w-6 items-center justify-center rounded-full bg-black/55">
      <svg width="12" height="12" viewBox="0 0 24 24" fill="#fff" aria-hidden>
        <path d="M8 5v14l11-7z" />
      </svg>
    </span>
  );
}

function Lightbox({
  tile,
  hasPrev,
  hasNext,
  onPrev,
  onNext,
  onClose,
  labels,
}: {
  tile: Tile;
  hasPrev: boolean;
  hasNext: boolean;
  onPrev: () => void;
  onNext: () => void;
  onClose: () => void;
  labels: { close: string; prev: string; next: string; unsupported: string };
}) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/90 p-4"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
    >
      <button
        type="button"
        onClick={onClose}
        aria-label={labels.close}
        className="absolute right-4 top-4 flex h-10 w-10 items-center justify-center rounded-full bg-white/10 text-2xl leading-none text-white"
      >
        ×
      </button>

      {tile.displayable ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img
          src={tile.fullUrl}
          alt=""
          onClick={(e) => e.stopPropagation()}
          className="max-h-[85dvh] max-w-full rounded-lg object-contain"
        />
      ) : (
        <p
          onClick={(e) => e.stopPropagation()}
          className="max-w-xs rounded-lg bg-white/10 px-6 py-8 text-center text-sm text-white"
        >
          {labels.unsupported}
        </p>
      )}

      {hasPrev && (
        <button
          type="button"
          aria-label={labels.prev}
          onClick={(e) => {
            e.stopPropagation();
            onPrev();
          }}
          className="absolute left-3 top-1/2 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full bg-white/10 text-2xl text-white"
        >
          ‹
        </button>
      )}
      {hasNext && (
        <button
          type="button"
          aria-label={labels.next}
          onClick={(e) => {
            e.stopPropagation();
            onNext();
          }}
          className="absolute right-3 top-1/2 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full bg-white/10 text-2xl text-white"
        >
          ›
        </button>
      )}
    </div>
  );
}
