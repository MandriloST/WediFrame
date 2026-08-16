import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { notFound } from "next/navigation";
import { Fraunces } from "next/font/google";
import { getGuestEvent } from "@/lib/guestApi";
import { GuestExperience } from "@/components/guest/GuestExperience";

// Display face for the couple's title ONLY — UI text stays on the system stack
// on purpose: guest pages open inside Instagram/WhatsApp webviews on venue wifi,
// and one small display font is the entire webfont budget.
const fraunces = Fraunces({
  subsets: ["latin", "latin-ext"],
  display: "swap",
  weight: "variable",
  axes: ["opsz"],
});

type Props = {
  params: Promise<{ locale: string; token: string }>;
};

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { token } = await params;
  const event = await getGuestEvent(token).catch(() => null);
  return {
    title: event?.title ?? "WediFrame",
    robots: { index: false, follow: false }, // private link — never indexed
  };
}

export default async function GuestPage({ params }: Props) {
  const { locale, token } = await params;
  setRequestLocale(locale);

  const event = await getGuestEvent(token).catch(() => {
    throw new Error("API unreachable");
  });
  if (!event) notFound();

  const t = await getTranslations("guest");

  return (
    <main className="mx-auto flex min-h-dvh w-full max-w-xl flex-col">
      {/* Hero: the couple's photo IS the page. Everything else stays quiet. */}
      <header className="relative">
        {event.coverPhotoUrl ? (
          // Plain <img>: presigned URLs are short-lived and unique per request,
          // so Next's image optimizer cache would miss every time anyway.
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={event.coverPhotoUrl}
            alt=""
            className="h-[52dvh] w-full object-cover"
          />
        ) : (
          <div
            aria-hidden
            className="h-[36dvh] w-full bg-gradient-to-b from-[#EFE7DC] to-[#FBF8F4]"
          />
        )}

        {/* Signature: the title card overlaps the photo's bottom edge —
            a caption label under a framed photograph (WediFrame = frame). */}
        <div className="relative z-10 -mt-14 px-5">
          <div className="rounded-2xl border border-[#E7E0D8] bg-[#FFFDF9] px-6 py-5 text-center shadow-[0_10px_30px_-12px_rgba(28,25,23,0.25)]">
            <p className="text-[11px] font-medium uppercase tracking-[0.22em] text-[#7C2D3E]">
              {t("eyebrow")}
            </p>
            <h1
              className={`${fraunces.className} mt-1 text-balance text-3xl leading-tight text-[#1C1917]`}
            >
              {event.title}
            </h1>
          </div>
        </div>
      </header>

      {/* Upload (the page's single job) + the gallery every guest can see. */}
      <GuestExperience
        token={token}
        uploadState={event.uploadState}
        uploadStartDate={event.uploadStartDate}
      />

      <footer className="mt-auto px-5 pb-6 pt-10 text-center text-xs text-[#A8A29E]">
        WediFrame · Powered by EverFrame
      </footer>
    </main>
  );
}
