"use client";

import { type ReactNode, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link, usePathname, useRouter } from "@/i18n/navigation";
import { getUser, isAuthed, logout } from "@/lib/hostApi";

/**
 * Gate for the whole /admin area. Non-authed → /login, non-admin → /dashboard.
 * The server still enforces the Admin role on every /admin API call; this is UX.
 */
export default function AdminLayout({ children }: { children: ReactNode }) {
  const t = useTranslations("admin");
  const router = useRouter();
  const pathname = usePathname();
  const [ready, setReady] = useState(false);

  useEffect(() => {
    if (!isAuthed()) {
      router.replace("/login");
      return;
    }
    if (getUser()?.role !== "Admin") {
      router.replace("/dashboard");
      return;
    }
    setReady(true);
  }, [router]);

  if (!ready) return null;

  const nav = [{ href: "/admin", label: t("nav.overview"), exact: true }, { href: "/admin/audit", label: t("nav.audit") }];

  return (
    <div className="min-h-screen bg-stone-50 text-stone-900">
      <header className="border-b border-stone-200 bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3">
          <Link href="/admin" className="flex items-center gap-2 font-semibold">
            <span className="inline-flex h-6 items-center rounded-full bg-rose-600 px-2 text-xs font-bold uppercase tracking-wide text-white">
              Admin
            </span>
            <span className="text-stone-800">WediFrame</span>
          </Link>
          <div className="flex items-center gap-3 text-sm">
            <Link
              href="/dashboard"
              className="text-stone-500 transition hover:text-stone-800"
            >
              {t("backToApp")}
            </Link>
            <button
              type="button"
              onClick={() => {
                logout();
                router.replace("/login");
              }}
              className="rounded-lg border border-stone-300 px-3 py-1.5 font-medium text-stone-700 transition hover:bg-stone-100"
            >
              {t("logout")}
            </button>
          </div>
        </div>
        <nav className="mx-auto max-w-6xl px-2">
          <ul className="flex gap-1 overflow-x-auto">
            {nav.map((item) => {
              const active = item.exact
                ? pathname === item.href
                : pathname.startsWith(item.href);
              return (
                <li key={item.href}>
                  <Link
                    href={item.href}
                    className={`inline-block border-b-2 px-3 py-2 text-sm font-medium transition ${
                      active
                        ? "border-rose-600 text-rose-700"
                        : "border-transparent text-stone-500 hover:text-stone-800"
                    }`}
                  >
                    {item.label}
                  </Link>
                </li>
              );
            })}
          </ul>
        </nav>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-6">{children}</main>
    </div>
  );
}
