"use client";

import { Suspense, useEffect, useRef, useState } from "react";
import { useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import { ApiError, consumeMagicLink } from "@/lib/hostApi";

type State = "verifying" | "invalid" | "error";

function MagicConsumer() {
  const t = useTranslations("auth");
  const router = useRouter();
  const params = useSearchParams();
  const token = params.get("token");
  const [state, setState] = useState<State>("verifying");
  const ran = useRef(false); // guard React 18 StrictMode double-invoke

  useEffect(() => {
    if (ran.current) return;
    ran.current = true;

    if (!token) {
      setState("invalid");
      return;
    }

    consumeMagicLink(token)
      .then(() => router.replace("/dashboard"))
      .catch((e) => {
        const code = e instanceof ApiError ? e.code : null;
        setState(code === "auth.magic_link_invalid" ? "invalid" : "error");
      });
  }, [token, router]);

  if (state === "verifying") {
    return (
      <div className="text-center">
        <div className="mx-auto h-8 w-8 animate-spin rounded-full border-2 border-[#E7E0D8] border-t-[#7C2D3E]" />
        <p className="mt-4 text-sm text-[#57534E]">{t("magicVerifying")}</p>
      </div>
    );
  }

  const title = state === "invalid" ? t("magicInvalidTitle") : t("magicErrorTitle");
  const body =
    state === "invalid" ? t("errors.magicLinkInvalid") : t("errors.generic");

  return (
    <div className="text-center">
      <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-[#F6EDE9] text-2xl">
        ⏳
      </div>
      <h2 className="mt-3 text-lg font-semibold text-[#1C1917]">{title}</h2>
      <p className="mt-1 text-sm text-[#57534E]">{body}</p>
      <Link
        href="/login"
        className="mt-5 inline-block w-full rounded-xl bg-[#7C2D3E] px-4 py-3 font-medium text-white transition active:scale-[0.99]"
      >
        {t("magicBackToLogin")}
      </Link>
    </div>
  );
}

export default function MagicPage() {
  const t = useTranslations("auth");
  return (
    <main className="flex min-h-dvh flex-col items-center justify-center bg-[#FFFDF9] px-5 py-10">
      <div className="w-full max-w-sm">
        <h1 className="text-center text-2xl font-semibold tracking-tight text-[#1C1917]">
          WediFrame
        </h1>
        <div className="mt-6 rounded-2xl border border-[#E7E0D8] bg-white p-6 shadow-sm">
          <Suspense
            fallback={
              <div className="text-center">
                <div className="mx-auto h-8 w-8 animate-spin rounded-full border-2 border-[#E7E0D8] border-t-[#7C2D3E]" />
                <p className="mt-4 text-sm text-[#57534E]">{t("magicVerifying")}</p>
              </div>
            }
          >
            <MagicConsumer />
          </Suspense>
        </div>
      </div>
    </main>
  );
}
