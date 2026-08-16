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
