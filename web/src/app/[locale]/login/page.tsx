"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import { ApiError, authErrorSubkey, isAuthed, login } from "@/lib/hostApi";

export default function LoginPage() {
  const t = useTranslations("auth");
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (isAuthed()) router.replace("/dashboard");
  }, [router]);

  const submit = async () => {
    if (busy) return;
    setBusy(true);
    setError(null);
    try {
      await login(email.trim(), password);
      router.replace("/dashboard");
    } catch (e) {
      setError(
        t(`errors.${authErrorSubkey(e instanceof ApiError ? e.code : null)}`),
      );
      setBusy(false);
    }
  };

  return (
    <main className="flex min-h-dvh flex-col items-center justify-center bg-[#FFFDF9] px-5 py-10">
      <div className="w-full max-w-sm">
        <h1 className="text-center text-2xl font-semibold tracking-tight text-[#1C1917]">
          WediFrame
        </h1>
        <p className="mt-1 text-center text-sm text-[#57534E]">{t("loginTitle")}</p>

        <div className="mt-6 rounded-2xl border border-[#E7E0D8] bg-white p-6 shadow-sm">
          <label className="block text-sm font-medium text-[#44403C]">
            {t("email")}
            <input
              type="email"
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="mt-1 w-full rounded-lg border border-[#E7E0D8] bg-[#FFFDF9] px-3 py-2.5 text-[#1C1917] outline-none focus:border-[#7C2D3E]"
            />
          </label>

          <label className="mt-4 block text-sm font-medium text-[#44403C]">
            {t("password")}
            <input
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && submit()}
              className="mt-1 w-full rounded-lg border border-[#E7E0D8] bg-[#FFFDF9] px-3 py-2.5 text-[#1C1917] outline-none focus:border-[#7C2D3E]"
            />
          </label>

          {error && <p className="mt-3 text-sm text-[#B4432F]">{error}</p>}

          <button
            type="button"
            onClick={submit}
            disabled={busy}
            className="mt-5 w-full rounded-xl bg-[#7C2D3E] px-4 py-3 font-medium text-white transition active:scale-[0.99] disabled:opacity-60"
          >
            {busy ? t("pleaseWait") : t("loginButton")}
          </button>
        </div>

        <p className="mt-4 text-center text-sm text-[#57534E]">
          {t("noAccount")}{" "}
          <Link href="/register" className="font-medium text-[#7C2D3E]">
            {t("registerLink")}
          </Link>
        </p>
      </div>
    </main>
  );
}
