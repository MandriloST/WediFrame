"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import {
  ApiError,
  authErrorSubkey,
  isAuthed,
  login,
  requestMagicLink,
} from "@/lib/hostApi";

type Mode = "password" | "magic";

export default function LoginPage() {
  const t = useTranslations("auth");
  const locale = useLocale();
  const router = useRouter();

  const [mode, setMode] = useState<Mode>("password");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [magicSent, setMagicSent] = useState(false);

  useEffect(() => {
    if (isAuthed()) router.replace("/dashboard");
  }, [router]);

  const submitPassword = async () => {
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

  const submitMagic = async () => {
    if (busy) return;
    const trimmed = email.trim();
    if (!trimmed) {
      setError(t("errors.emailInvalid"));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await requestMagicLink(trimmed, locale);
      setMagicSent(true);
    } catch (e) {
      setError(
        t(`errors.${authErrorSubkey(e instanceof ApiError ? e.code : null)}`),
      );
    } finally {
      setBusy(false);
    }
  };

  const switchMode = (next: Mode) => {
    setMode(next);
    setError(null);
    setMagicSent(false);
  };

  return (
    <main className="flex min-h-dvh flex-col items-center justify-center bg-[#FFFDF9] px-5 py-10">
      <div className="w-full max-w-sm">
        <h1 className="text-center text-2xl font-semibold tracking-tight text-[#1C1917]">
          WediFrame
        </h1>
        <p className="mt-1 text-center text-sm text-[#57534E]">
          {mode === "magic" ? t("magicLinkTitle") : t("loginTitle")}
        </p>

        <div className="mt-6 rounded-2xl border border-[#E7E0D8] bg-white p-6 shadow-sm">
          {/* --- Confirmation screen after a magic link was requested --- */}
          {mode === "magic" && magicSent ? (
            <div className="text-center">
              <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-[#F6EDE9] text-2xl">
                ✉️
              </div>
              <h2 className="mt-3 text-lg font-semibold text-[#1C1917]">
                {t("magicSentTitle")}
              </h2>
              <p className="mt-1 text-sm text-[#57534E]">
                {t("magicSentBody", { email: email.trim() })}
              </p>
              <button
                type="button"
                onClick={submitMagic}
                disabled={busy}
                className="mt-5 w-full rounded-xl border border-[#E7E0D8] px-4 py-2.5 font-medium text-[#7C2D3E] transition active:scale-[0.99] disabled:opacity-60"
              >
                {busy ? t("pleaseWait") : t("magicResend")}
              </button>
              <button
                type="button"
                onClick={() => switchMode("password")}
                className="mt-3 text-sm font-medium text-[#57534E]"
              >
                {t("magicLinkBack")}
              </button>
            </div>
          ) : (
            <>
              <label className="block text-sm font-medium text-[#44403C]">
                {t("email")}
                <input
                  type="email"
                  autoComplete="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  onKeyDown={(e) =>
                    e.key === "Enter" &&
                    mode === "magic" &&
                    submitMagic()
                  }
                  className="mt-1 w-full rounded-lg border border-[#E7E0D8] bg-[#FFFDF9] px-3 py-2.5 text-[#1C1917] outline-none focus:border-[#7C2D3E]"
                />
              </label>

              {mode === "password" && (
                <label className="mt-4 block text-sm font-medium text-[#44403C]">
                  {t("password")}
                  <input
                    type="password"
                    autoComplete="current-password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    onKeyDown={(e) => e.key === "Enter" && submitPassword()}
                    className="mt-1 w-full rounded-lg border border-[#E7E0D8] bg-[#FFFDF9] px-3 py-2.5 text-[#1C1917] outline-none focus:border-[#7C2D3E]"
                  />
                </label>
              )}

              {mode === "magic" && (
                <p className="mt-2 text-xs text-[#78716C]">{t("magicLinkHint")}</p>
              )}

              {error && <p className="mt-3 text-sm text-[#B4432F]">{error}</p>}

              <button
                type="button"
                onClick={mode === "magic" ? submitMagic : submitPassword}
                disabled={busy}
                className="mt-5 w-full rounded-xl bg-[#7C2D3E] px-4 py-3 font-medium text-white transition active:scale-[0.99] disabled:opacity-60"
              >
                {busy
                  ? t("pleaseWait")
                  : mode === "magic"
                    ? t("magicLinkButton")
                    : t("loginButton")}
              </button>

              {/* --- Divider + switch between password and magic link --- */}
              <div className="mt-5 flex items-center gap-3">
                <span className="h-px flex-1 bg-[#EFE8E0]" />
                <span className="text-xs uppercase tracking-wide text-[#A8A29E]">
                  {t("or")}
                </span>
                <span className="h-px flex-1 bg-[#EFE8E0]" />
              </div>

              <button
                type="button"
                onClick={() =>
                  switchMode(mode === "magic" ? "password" : "magic")
                }
                className="mt-4 w-full rounded-xl border border-[#E7E0D8] px-4 py-2.5 font-medium text-[#7C2D3E] transition active:scale-[0.99]"
              >
                {mode === "magic" ? t("magicLinkBack") : t("magicLinkToggle")}
              </button>
            </>
          )}
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
