/**
 * Host-facing API client (authenticated). The couple registers/logs in; the
 * JWT access token authorizes /events and later gallery management. Tokens live
 * in localStorage; a 401 triggers one silent refresh before giving up.
 *
 * Error strings from the backend are machine codes ("auth.invalid_credentials");
 * callers map them to localized text via authErrorSubkey().
 */
import { API_BASE } from "./guestApi";

const ACCESS_KEY = "wf_access";
const REFRESH_KEY = "wf_refresh";
const USER_KEY = "wf_user";

export type AuthUser = {
  id: string;
  email: string;
  role: string;
  language: string;
};

type AuthResponse = {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: AuthUser;
};

export type HostEvent = {
  id: string;
  title: string;
  type: string;
  uploadStartDate: string; // "yyyy-MM-dd"
  status: string; // Draft | Active | UploadClosed | Expired | Deleted
  guestToken: string;
  guestUrl: string;
  coverPhotoKey: string | null;
  coverPhotoUrl: string | null;
  createdAt: string;
  packageSlug: string | null;
  packageName: string | null;
  uploadEndsAt: string | null; // "yyyy-MM-dd", set on activation
  expiresAt: string | null; // "yyyy-MM-dd", set on activation
};

export class ApiError extends Error {
  constructor(
    public status: number,
    public code: string | null,
  ) {
    super(code ?? `http ${status}`);
  }
}

// --- session -----------------------------------------------------------------

export function isAuthed(): boolean {
  return typeof window !== "undefined" && !!localStorage.getItem(ACCESS_KEY);
}

export function getUser(): AuthUser | null {
  try {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as AuthUser) : null;
  } catch {
    return null;
  }
}

export function clearSession(): void {
  localStorage.removeItem(ACCESS_KEY);
  localStorage.removeItem(REFRESH_KEY);
  localStorage.removeItem(USER_KEY);
}

function setSession(auth: AuthResponse): void {
  localStorage.setItem(ACCESS_KEY, auth.accessToken);
  localStorage.setItem(REFRESH_KEY, auth.refreshToken);
  localStorage.setItem(USER_KEY, JSON.stringify(auth.user));
}

async function parseErrorCode(res: Response): Promise<string | null> {
  try {
    const body = await res.json();
    // ValidationProblem → { errors: { field: ["code"] } }
    if (body?.errors && typeof body.errors === "object") {
      const first = Object.values(body.errors)[0];
      if (Array.isArray(first) && typeof first[0] === "string") return first[0];
    }
    if (typeof body?.detail === "string") return body.detail;
  } catch {
    // no JSON body
  }
  return null;
}

/** Maps a backend auth error code to an i18n subkey under auth.errors.*. */
export function authErrorSubkey(code: string | null): string {
  switch (code) {
    case "auth.invalid_credentials":
      return "invalidCredentials";
    case "auth.email_taken":
      return "emailTaken";
    case "auth.email_invalid":
      return "emailInvalid";
    case "auth.password_length":
      return "passwordLength";
    case "auth.magic_link_invalid":
      return "magicLinkInvalid";
    default:
      return "generic";
  }
}

// --- auth ---------------------------------------------------------------------

export async function register(
  email: string,
  password: string,
  language: string,
): Promise<void> {
  const res = await fetch(`${API_BASE}/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password, language }),
  });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  setSession((await res.json()) as AuthResponse);
}

export async function login(email: string, password: string): Promise<void> {
  const res = await fetch(`${API_BASE}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  setSession((await res.json()) as AuthResponse);
}

export function logout(): void {
  clearSession();
}

/**
 * Request a passwordless sign-in link. Always resolves for a well-formed email
 * (the backend answers 200 regardless of whether the account exists — no
 * enumeration), so the caller shows the same "check your email" screen either
 * way. Throws only on transport/feature errors (e.g. magic link disabled → 404).
 */
export async function requestMagicLink(
  email: string,
  language: string,
): Promise<void> {
  const res = await fetch(`${API_BASE}/auth/magic-link/request`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, language }),
  });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
}

/**
 * Consume a magic-link token (from the emailed link). On success the session is
 * stored exactly like password login. An expired/used/unknown token throws
 * ApiError with code "auth.magic_link_invalid".
 */
export async function consumeMagicLink(token: string): Promise<void> {
  const res = await fetch(`${API_BASE}/auth/magic-link/consume`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token }),
  });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  setSession((await res.json()) as AuthResponse);
}

async function tryRefresh(): Promise<boolean> {
  const refreshToken = localStorage.getItem(REFRESH_KEY);
  if (!refreshToken) return false;
  try {
    const res = await fetch(`${API_BASE}/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
    });
    if (!res.ok) return false;
    setSession((await res.json()) as AuthResponse);
    return true;
  } catch {
    return false;
  }
}

/**
 * Authenticated fetch with one silent refresh on 401. Exported so the admin
 * client (adminApi.ts) reuses the same session + refresh logic.
 */
export async function authFetch(
  path: string,
  init: RequestInit = {},
  allowRefresh = true,
): Promise<Response> {
  const token = localStorage.getItem(ACCESS_KEY);
  const headers = new Headers(init.headers);
  if (token) headers.set("Authorization", `Bearer ${token}`);

  const res = await fetch(`${API_BASE}${path}`, { ...init, headers });
  if (res.status === 401 && allowRefresh && (await tryRefresh())) {
    return authFetch(path, init, false);
  }
  return res;
}

// --- events -------------------------------------------------------------------

export async function listEvents(): Promise<HostEvent[]> {
  const res = await authFetch("/events");
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as HostEvent[];
}

export async function createEvent(
  title: string,
  uploadStartDate: string,
  packageSlug: string,
): Promise<HostEvent> {
  const res = await authFetch("/events", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ title, uploadStartDate, type: "wedding", packageSlug }),
  });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as HostEvent;
}

/** Free activation (Draft → Active) so the guest link starts working. */
export async function activateEvent(id: string): Promise<HostEvent> {
  const res = await authFetch(`/events/${id}/activate`, { method: "POST" });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as HostEvent;
}

/** Close the upload period early (Active → UploadClosed): gallery stays, uploads stop. */
export async function closeUpload(id: string): Promise<HostEvent> {
  const res = await authFetch(`/events/${id}/close-upload`, { method: "POST" });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as HostEvent;
}

/** Reopen a closed upload period (UploadClosed → Active). */
export async function reopenUpload(id: string): Promise<HostEvent> {
  const res = await authFetch(`/events/${id}/reopen-upload`, { method: "POST" });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as HostEvent;
}

/**
 * Regenerate the guest token if the link/QR leaks. Every previously shared
 * link/QR immediately stops working; returns the event with the new token/URL.
 */
export async function rotateGuestToken(id: string): Promise<HostEvent> {
  const res = await authFetch(`/events/${id}/rotate-token`, { method: "POST" });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as HostEvent;
}

export async function getEvent(id: string): Promise<HostEvent> {
  const res = await authFetch(`/events/${id}`);
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as HostEvent;
}

/**
 * Permanently delete the host's own event (right to erasure): the backend
 * purges all media (R2 + rows), removes the cover and marks the event Deleted.
 * Irreversible. Returns nothing (204).
 */
export async function deleteEvent(id: string): Promise<void> {
  const res = await authFetch(`/events/${id}`, { method: "DELETE" });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
}

/** QR as a PNG blob (the endpoint needs auth, so we can't use a plain <img src>). */
export async function getQrPng(id: string, size = 20): Promise<Blob> {
  const res = await authFetch(`/events/${id}/qr?format=png&size=${size}`);
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return res.blob();
}

export type CoverUpload = {
  key: string;
  uploadUrl: string;
  contentType: string;
  expiresAt: string;
  maxBytes: number;
};

export const COVER_MAX_BYTES = 20 * 1024 * 1024; // mirrors CoverPhotoRules
export const COVER_ALLOWED_TYPES = new Set([
  "image/jpeg",
  "image/png",
  "image/webp",
]);

export async function startCoverUpload(
  id: string,
  contentType: string,
  sizeBytes: number,
): Promise<CoverUpload> {
  const res = await authFetch(`/events/${id}/cover`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ contentType, sizeBytes }),
  });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as CoverUpload;
}

export async function confirmCover(id: string, key: string): Promise<HostEvent> {
  const res = await authFetch(`/events/${id}/cover/confirm`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ key }),
  });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as HostEvent;
}

// --- gallery management (host, M2) -------------------------------------------

/**
 * A confirmed item as the host sees it — includes HIDDEN items and per-item
 * visibility so the host can toggle it. Shares the display fields with the
 * guest GalleryItem, so it feeds the same shared tile builder.
 */
export type HostGalleryItem = {
  mediaId: string;
  type: string; // "Photo" | "Video"
  url: string;
  thumbnailUrl: string | null;
  contentType: string;
  guestName: string | null;
  visibility: string; // "Visible" | "Hidden"
  sizeBytes: number;
  createdAt: string;
};

export type HostGalleryPage = {
  items: HostGalleryItem[];
  nextOffset: number | null;
};

export const HOST_GALLERY_PAGE_SIZE = 24;

export async function getHostMedia(
  eventId: string,
  offset = 0,
  limit = HOST_GALLERY_PAGE_SIZE,
): Promise<HostGalleryPage> {
  const res = await authFetch(
    `/events/${eventId}/media?offset=${offset}&limit=${limit}`,
  );
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as HostGalleryPage;
}

/** Hide or unhide an item. Returns the new visibility. */
export async function setMediaVisibility(
  eventId: string,
  mediaId: string,
  visibility: "Visible" | "Hidden",
): Promise<{ mediaId: string; visibility: string }> {
  const res = await authFetch(`/events/${eventId}/media/${mediaId}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ visibility }),
  });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as { mediaId: string; visibility: string };
}

/** Soft-delete an item (recoverable until retention removes it physically). */
export async function deleteMedia(
  eventId: string,
  mediaId: string,
): Promise<void> {
  const res = await authFetch(`/events/${eventId}/media/${mediaId}`, {
    method: "DELETE",
  });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
}

/** Presigned attachment URL to save one item (any visibility). */
export async function getMediaDownloadUrl(
  eventId: string,
  mediaId: string,
): Promise<{ url: string; fileName: string }> {
  const res = await authFetch(`/events/${eventId}/media/${mediaId}/download`);
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as { url: string; fileName: string };
}

// --- gallery ZIP export (host, M2) -------------------------------------------

export type ExportJob = {
  jobId: string;
  status: "Pending" | "Running" | "Ready" | "Failed" | string;
  itemCount: number | null;
  sizeBytes: number | null;
  downloadUrl: string | null;
  fileName: string | null;
  error: string | null;
};

/** Start (or reuse) a whole-gallery ZIP export. Poll getExport until Ready/Failed. */
export async function startExport(eventId: string): Promise<ExportJob> {
  const res = await authFetch(`/events/${eventId}/export`, { method: "POST" });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as ExportJob;
}

/** Poll an export job. When status is "Ready", downloadUrl is a presigned ZIP link. */
export async function getExport(
  eventId: string,
  jobId: string,
): Promise<ExportJob> {
  const res = await authFetch(`/events/${eventId}/export/${jobId}`);
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as ExportJob;
}

/** Confirmed usage vs package limits for an event (GET /events/{id}/stats). */
export type EventStats = {
  photoCount: number;
  maxPhotoCount: number | null;
  videoBytes: number;
  maxVideoTotalBytes: number | null;
  totalBytes: number;
  maxTotalBytes: number | null;
  packageSlug: string | null;
  packageName: string | null;
  uploadStartDate: string;
  uploadEndsAt: string | null;
  expiresAt: string | null;
};

export async function getEventStats(id: string): Promise<EventStats> {
  const res = await authFetch(`/events/${id}/stats`);
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as EventStats;
}

/** R1 (company invoice) details for checkout. */
export type CheckoutR1 = {
  needsR1: boolean;
  companyName: string | null;
  companyOib: string | null;
  companyAddress: string | null;
};

/** Start Stripe checkout for the event's paid package → returns the hosted payment URL. */
export async function startCheckout(
  id: string,
  r1: CheckoutR1,
  bonusCode?: string | null,
): Promise<{ url: string }> {
  const res = await authFetch(`/events/${id}/checkout`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ...r1, bonusCode: bonusCode?.trim() || null }),
  });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as { url: string };
}

/** Preview of a bonus code applied to this event's package. */
export type BonusCodePreview = {
  valid: boolean;
  reason: string | null;
  originalCents: number;
  discountCents: number;
  finalCents: number;
  approxPercent: number;
  currency: string;
};

/** Validate a bonus code against the event's package before paying (shows the discount). */
export async function previewBonusCode(
  id: string,
  code: string,
): Promise<BonusCodePreview> {
  const res = await authFetch(`/events/${id}/bonus-code/preview`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ code }),
  });
  if (!res.ok) throw new ApiError(res.status, await parseErrorCode(res));
  return (await res.json()) as BonusCodePreview;
}
