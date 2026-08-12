"use client";

import type { ReactNode } from "react";
import { BROWSER_DISPLAYABLE_TYPES } from "@/lib/guestApi";

/**
 * Presentation layer shared by the guest gallery and the host gallery. The two
 * differ only in their data source and controls (the host can hide/delete);
 * the tile, the placeholder, the play badge and the lightbox are identical, so
 * they live here once. Data fetching + dedupe + previews stay in each consumer.
 */

/** A single display tile — the display-only shape both galleries render. */
export type MediaTile = {
  mediaId: string;
  gridUrl: string; // shown in the grid (thumbnail if available, else original/local)
  fullUrl: string; // shown in the lightbox
  contentType: string;
  isVideo: boolean;
  displayable: boolean; // can the browser render it in an <img>?
  guestName: string | null;
};

/** Common fields shared by the guest (GalleryItem) and host (HostGalleryItem) DTOs. */
export type ServerMediaItem = {
  mediaId: string;
  type: string; // "Photo" | "Video"
  url: string;
  thumbnailUrl: string | null;
  contentType: string;
  guestName: string | null;
};

/** Build a display tile from a server-listed item (guest or host). */
export function tileFromServer(item: ServerMediaItem): MediaTile {
  const originalDisplayable = BROWSER_DISPLAYABLE_TYPES.has(
    item.contentType.toLowerCase(),
  );
  return {
    mediaId: item.mediaId,
    gridUrl: item.thumbnailUrl ?? item.url,
    // Lightbox: full-res original when the browser can show it; otherwise the
    // JPEG thumbnail (HEIC/HEIF can't render, but their thumbnail can).
    fullUrl: originalDisplayable ? item.url : (item.thumbnailUrl ?? item.url),
    contentType: item.contentType,
    isVideo: item.type === "Video",
    // A thumbnail (jpg from the worker) is always displayable; otherwise it
    // depends on the original's type (HEIC/HEIF cannot render yet).
    displayable: item.thumbnailUrl != null || originalDisplayable,
    guestName: item.guestName,
  };
}

/** The inner content of a grid tile: image (or placeholder) + video play badge. */
export function MediaThumb({ tile }: { tile: MediaTile }) {
  return (
    <>
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
    </>
  );
}

export function PlaceholderTile() {
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

export function PlayBadge() {
  return (
    <span className="absolute bottom-1.5 right-1.5 flex h-6 w-6 items-center justify-center rounded-full bg-black/55">
      <svg width="12" height="12" viewBox="0 0 24 24" fill="#fff" aria-hidden>
        <path d="M8 5v14l11-7z" />
      </svg>
    </span>
  );
}

export type LightboxLabels = {
  close: string;
  prev: string;
  next: string;
  unsupported: string;
  download: string;
};

/**
 * Trigger a browser download for a (presigned, cross-origin) URL. The bytes
 * come straight from R2 with Content-Disposition: attachment, so the browser
 * saves rather than navigates; the `download` attribute is a harmless hint.
 */
export function triggerBrowserDownload(url: string) {
  const a = document.createElement("a");
  a.href = url;
  a.rel = "noopener";
  a.download = "";
  document.body.appendChild(a);
  a.click();
  a.remove();
}

export function MediaLightbox({
  tile,
  hasPrev,
  hasNext,
  onPrev,
  onNext,
  onClose,
  labels,
  actions,
  onDownload,
  downloading,
}: {
  tile: MediaTile;
  hasPrev: boolean;
  hasNext: boolean;
  onPrev: () => void;
  onNext: () => void;
  onClose: () => void;
  labels: LightboxLabels;
  /** Optional bottom action bar (host gallery: hide/delete). Guest passes none. */
  actions?: ReactNode;
  /** When set, shows a download button in the top bar. */
  onDownload?: () => void;
  downloading?: boolean;
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

      {onDownload && (
        <button
          type="button"
          disabled={downloading}
          aria-label={labels.download}
          onClick={(e) => {
            e.stopPropagation();
            onDownload();
          }}
          className="absolute right-16 top-4 flex h-10 w-10 items-center justify-center rounded-full bg-white/10 text-white disabled:opacity-50"
        >
          {downloading ? (
            <span className="h-4 w-4 animate-spin rounded-full border-2 border-white/40 border-t-white" />
          ) : (
            <svg
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden
            >
              <path d="M12 3v12" />
              <path d="m7 10 5 5 5-5" />
              <path d="M5 21h14" />
            </svg>
          )}
        </button>
      )}

      {tile.isVideo ? (
        <video
          src={tile.fullUrl}
          controls
          autoPlay
          playsInline
          onClick={(e) => e.stopPropagation()}
          className="max-h-[85dvh] max-w-full rounded-lg bg-black object-contain"
        />
      ) : tile.displayable ? (
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

      {actions && (
        <div
          onClick={(e) => e.stopPropagation()}
          className="absolute inset-x-0 bottom-0 flex items-center justify-center gap-2 bg-gradient-to-t from-black/80 via-black/50 to-transparent px-4 pb-7 pt-12"
        >
          {actions}
        </div>
      )}
    </div>
  );
}
