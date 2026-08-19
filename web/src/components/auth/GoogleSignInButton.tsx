"use client";

import { useEffect, useRef } from "react";

/**
 * Renders the official "Sign in with Google" button (Google Identity Services)
 * and hands the resulting ID token to `onCredential`. Approach B: the token is
 * verified server-side by POST /auth/google — there is NO client secret anywhere.
 *
 * Renders nothing when NEXT_PUBLIC_GOOGLE_CLIENT_ID is unset (feature hidden,
 * mirroring the backend 404) or when the GIS script can't load (e.g. blocked in
 * an in-app webview) — in those cases the email/password + magic link paths remain.
 */

const CLIENT_ID = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID ?? "";
const GSI_SRC = "https://accounts.google.com/gsi/client";

type CredentialResponse = { credential?: string };

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: {
            client_id: string;
            callback: (res: CredentialResponse) => void;
          }) => void;
          renderButton: (
            parent: HTMLElement,
            options: Record<string, unknown>,
          ) => void;
        };
      };
    };
  }
}

let gsiPromise: Promise<void> | null = null;

function loadGsi(): Promise<void> {
  if (typeof window === "undefined") return Promise.resolve();
  if (window.google?.accounts?.id) return Promise.resolve();
  if (gsiPromise) return gsiPromise;

  gsiPromise = new Promise<void>((resolve, reject) => {
    const existing = document.querySelector<HTMLScriptElement>(
      `script[src="${GSI_SRC}"]`,
    );
    if (existing) {
      existing.addEventListener("load", () => resolve());
      existing.addEventListener("error", () => reject(new Error("gsi")));
      return;
    }
    const s = document.createElement("script");
    s.src = GSI_SRC;
    s.async = true;
    s.defer = true;
    s.onload = () => resolve();
    s.onerror = () => reject(new Error("gsi"));
    document.head.appendChild(s);
  });
  return gsiPromise;
}

/** True when a Google Client ID is configured — pages use it to show/hide the divider. */
export function isGoogleConfigured(): boolean {
  return CLIENT_ID.length > 0;
}

export default function GoogleSignInButton({
  onCredential,
  text = "continue_with",
  locale,
}: {
  onCredential: (idToken: string) => void;
  text?: "signin_with" | "signup_with" | "continue_with";
  locale?: string;
}) {
  const containerRef = useRef<HTMLDivElement>(null);
  const cbRef = useRef(onCredential);
  cbRef.current = onCredential;
  const rendered = useRef(false);

  useEffect(() => {
    if (!CLIENT_ID || rendered.current) return;
    let cancelled = false;

    loadGsi()
      .then(() => {
        if (cancelled || rendered.current) return;
        const el = containerRef.current;
        if (!el || !window.google) return;
        rendered.current = true;

        window.google.accounts.id.initialize({
          client_id: CLIENT_ID,
          callback: (res) => {
            if (res.credential) cbRef.current(res.credential);
          },
        });

        const width = Math.min(el.offsetWidth || 320, 400);
        window.google.accounts.id.renderButton(el, {
          type: "standard",
          theme: "outline",
          size: "large",
          shape: "pill",
          logo_alignment: "left",
          text,
          width,
          locale,
        });
      })
      .catch(() => {
        /* GIS blocked/unavailable — silently hide, other sign-in paths remain */
      });

    return () => {
      cancelled = true;
    };
  }, [text, locale]);

  if (!CLIENT_ID) return null;
  return <div ref={containerRef} className="flex justify-center" />;
}
