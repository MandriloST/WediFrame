"use client";

import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";

export default function AdminHomePage() {
  const t = useTranslations("admin");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-stone-900">
          {t("home.title")}
        </h1>
        <p className="mt-1 text-stone-500">{t("home.subtitle")}</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Link
          href="/admin/audit"
          className="group rounded-2xl border border-stone-200 bg-white p-5 shadow-sm transition hover:border-rose-300 hover:shadow"
        >
          <h2 className="font-semibold text-stone-900 group-hover:text-rose-700">
            {t("nav.audit")}
          </h2>
          <p className="mt-1 text-sm text-stone-500">{t("home.auditDesc")}</p>
        </Link>

        <Link
          href="/admin/users"
          className="group rounded-2xl border border-stone-200 bg-white p-5 shadow-sm transition hover:border-rose-300 hover:shadow"
        >
          <h2 className="font-semibold text-stone-900 group-hover:text-rose-700">
            {t("nav.users")}
          </h2>
          <p className="mt-1 text-sm text-stone-500">{t("home.usersDesc")}</p>
        </Link>

        <Link
          href="/admin/events"
          className="group rounded-2xl border border-stone-200 bg-white p-5 shadow-sm transition hover:border-rose-300 hover:shadow"
        >
          <h2 className="font-semibold text-stone-900 group-hover:text-rose-700">
            {t("nav.events")}
          </h2>
          <p className="mt-1 text-sm text-stone-500">{t("home.eventsDesc")}</p>
        </Link>
      </div>
    </div>
  );
}
