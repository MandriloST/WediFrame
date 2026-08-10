/**
 * Guest-facing API client. The token is the only credential (no accounts).
 * Server components call `getGuestEvent`; the upload flow runs in the browser.
 * Error codes ("media.file_too_large") map to i18n keys — never shown raw.
 */

export const API_BASE =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080/api/v1";

export type GuestEventInfo = {
  title: string;
  type: string;
  uploadStartDate: string;
  status: string;
  coverPhotoUrl: string | null;
  uploadOpen: boolean;
};

export type UploadItemRequest = {
  contentType: string;
  sizeBytes: number;
  fileName: string | null;
};

export type UploadItemResponse = {
  mediaId: string;
  objectKey: string;
  uploadUrl: string;
  contentType: string;
  expiresAt: string;
};

export type ConfirmResponse = {
  mediaId: string;
  uploadStatus: string;
  sizeBytes: number;
};

/** Mirrors PhotoRules on the backend — the backend stays the source of truth. */
export const PHOTO_MAX_BYTES = 50 * 1024 * 1024;
export const PHOTO_ALLOWED_TYPES = new Set([
  "image/jpeg",
  "image/png",
  "image/webp",
  "image/heic",
  "image/heif",
  "image/gif",
]);
export const MAX_ITEMS_PER_REQUEST = 30;

export async function getGuestEvent(
  token: string,
): Promise<GuestEventInfo | null> {
  const res = await fetch(`${API_BASE}/guest/${encodeURIComponent(token)}`, {
    cache: "no-store",
  });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`guest info failed: ${res.status}`);
  return (await res.json()) as GuestEventInfo;
}

export async function startUploads(
  token: string,
  items: UploadItemRequest[],
  guestName: string | null,
): Promise<UploadItemResponse[]> {
  const res = await fetch(
    `${API_BASE}/guest/${encodeURIComponent(token)}/uploads`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ items, guestName }),
    },
  );
  if (!res.ok) throw new Error(`presign failed: ${res.status}`);
  const data = (await res.json()) as { items: UploadItemResponse[] };
  return data.items;
}

export async function confirmUpload(
  token: string,
  mediaId: string,
): Promise<ConfirmResponse> {
  const res = await fetch(
    `${API_BASE}/guest/${encodeURIComponent(token)}/uploads/${mediaId}/confirm`,
    { method: "POST" },
  );
  if (!res.ok) throw new Error(`confirm failed: ${res.status}`);
  return (await res.json()) as ConfirmResponse;
}

/**
 * PUT the file straight to R2. XMLHttpRequest instead of fetch because fetch
 * has no upload progress — and progress is the whole UX on wedding wifi.
 */
export function putToStorage(
  uploadUrl: string,
  file: File,
  contentType: string,
  onProgress: (fraction: number) => void,
): Promise<void> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open("PUT", uploadUrl);
    // Must match the presigned content type exactly — it is signed into the URL.
    xhr.setRequestHeader("Content-Type", contentType);
    xhr.upload.onprogress = (e) => {
      if (e.lengthComputable) onProgress(e.loaded / e.total);
    };
    xhr.onload = () =>
      xhr.status >= 200 && xhr.status < 300
        ? resolve()
        : reject(new Error(`storage PUT failed: ${xhr.status}`));
    xhr.onerror = () => reject(new Error("storage PUT network error"));
    xhr.send(file);
  });
}
