/**
 * Admin-facing API client. Reuses the host session (same JWT in localStorage);
 * every /admin endpoint additionally requires the Admin role server-side. A Host
 * token hitting these endpoints gets 403 — the admin pages gate on role too, but
 * the server is the real boundary.
 */
import { authFetch, ApiError } from "./hostApi";

export type AuditLogItem = {
  id: string;
  occurredAt: string; // ISO
  actorUserId: string | null;
  action: string;
  entityType: string | null;
  entityId: string | null;
  details: string | null; // JSON string or null
};

export type Paged<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};

export type AuditFilters = {
  page?: number;
  pageSize?: number;
  entityType?: string;
  entityId?: string;
  action?: string;
  actorUserId?: string;
  from?: string; // ISO date-time
  to?: string; // ISO date-time
};

export async function getAuditLog(
  filters: AuditFilters,
): Promise<Paged<AuditLogItem>> {
  const qs = new URLSearchParams();
  if (filters.page) qs.set("page", String(filters.page));
  if (filters.pageSize) qs.set("pageSize", String(filters.pageSize));
  if (filters.entityType) qs.set("entityType", filters.entityType);
  if (filters.entityId) qs.set("entityId", filters.entityId);
  if (filters.action) qs.set("action", filters.action);
  if (filters.actorUserId) qs.set("actorUserId", filters.actorUserId);
  if (filters.from) qs.set("from", filters.from);
  if (filters.to) qs.set("to", filters.to);

  const suffix = qs.toString() ? `?${qs.toString()}` : "";
  const res = await authFetch(`/admin/audit${suffix}`);
  if (!res.ok) throw new ApiError(res.status, null);
  return (await res.json()) as Paged<AuditLogItem>;
}

// --- users (A2) --------------------------------------------------------------

export type AdminUser = {
  id: string;
  email: string;
  role: string; // "Host" | "Admin"
  preferredLanguage: string;
  createdAt: string; // ISO
};

export type UserFilters = {
  page?: number;
  pageSize?: number;
  q?: string; // email substring
  role?: string; // "Host" | "Admin"
};

export async function getUsers(
  filters: UserFilters,
): Promise<Paged<AdminUser>> {
  const qs = new URLSearchParams();
  if (filters.page) qs.set("page", String(filters.page));
  if (filters.pageSize) qs.set("pageSize", String(filters.pageSize));
  if (filters.q) qs.set("q", filters.q);
  if (filters.role) qs.set("role", filters.role);

  const suffix = qs.toString() ? `?${qs.toString()}` : "";
  const res = await authFetch(`/admin/users${suffix}`);
  if (!res.ok) throw new ApiError(res.status, null);
  return (await res.json()) as Paged<AdminUser>;
}

// --- events (A3) -------------------------------------------------------------

export type AdminEvent = {
  id: string;
  title: string;
  type: string;
  status: string; // Draft | Active | UploadClosed | Expired | Deleted
  ownerUserId: string;
  ownerEmail: string | null;
  packageSlug: string | null;
  packageName: string | null;
  uploadStartDate: string; // yyyy-MM-dd
  uploadEndsAt: string | null;
  expiresAt: string | null;
  hasCover: boolean;
  createdAt: string; // ISO
};

export type AdminEventDetail = AdminEvent & {
  guestToken: string;
  guestUrl: string;
};

export type EventFilters = {
  page?: number;
  pageSize?: number;
  q?: string; // title substring
  status?: string;
};

export async function getEvents(
  filters: EventFilters,
): Promise<Paged<AdminEvent>> {
  const qs = new URLSearchParams();
  if (filters.page) qs.set("page", String(filters.page));
  if (filters.pageSize) qs.set("pageSize", String(filters.pageSize));
  if (filters.q) qs.set("q", filters.q);
  if (filters.status) qs.set("status", filters.status);

  const suffix = qs.toString() ? `?${qs.toString()}` : "";
  const res = await authFetch(`/admin/events${suffix}`);
  if (!res.ok) throw new ApiError(res.status, null);
  return (await res.json()) as Paged<AdminEvent>;
}

export async function getEvent(id: string): Promise<AdminEventDetail> {
  const res = await authFetch(`/admin/events/${id}`);
  if (!res.ok) throw new ApiError(res.status, null);
  return (await res.json()) as AdminEventDetail;
}

/** Best-effort extraction of a machine error code from a ProblemDetails body. */
async function parseProblemCode(res: Response): Promise<string | null> {
  try {
    const body = await res.json();
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

/** Extend an event's gallery retention (move ExpiresAt later). Returns new state. */
export async function extendRetention(
  id: string,
  expiresAt: string, // yyyy-MM-dd
): Promise<{ expiresAt: string; status: string }> {
  const res = await authFetch(`/admin/events/${id}/extend-retention`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ expiresAt }),
  });
  if (!res.ok) throw new ApiError(res.status, await parseProblemCode(res));
  return (await res.json()) as { expiresAt: string; status: string };
}

// --- media moderation (A3b) --------------------------------------------------

export type AdminMediaItem = {
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

export type AdminMediaPage = {
  items: AdminMediaItem[];
  nextOffset: number | null;
};

export const ADMIN_MEDIA_PAGE_SIZE = 24;

export async function getAdminMedia(
  eventId: string,
  offset = 0,
  limit = ADMIN_MEDIA_PAGE_SIZE,
): Promise<AdminMediaPage> {
  const res = await authFetch(
    `/admin/events/${eventId}/media?offset=${offset}&limit=${limit}`,
  );
  if (!res.ok) throw new ApiError(res.status, null);
  return (await res.json()) as AdminMediaPage;
}

export async function setAdminMediaVisibility(
  eventId: string,
  mediaId: string,
  visibility: "Visible" | "Hidden",
): Promise<{ mediaId: string; visibility: string }> {
  const res = await authFetch(`/admin/events/${eventId}/media/${mediaId}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ visibility }),
  });
  if (!res.ok) throw new ApiError(res.status, null);
  return (await res.json()) as { mediaId: string; visibility: string };
}

export async function deleteAdminMedia(
  eventId: string,
  mediaId: string,
): Promise<void> {
  const res = await authFetch(`/admin/events/${eventId}/media/${mediaId}`, {
    method: "DELETE",
  });
  if (!res.ok) throw new ApiError(res.status, null);
}
