import { setRequestLocale } from "next-intl/server";
import { LegalArticle } from "@/components/legal/LegalArticle";
import { legal } from "@/content/legal";

export default async function PrivacyPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const doc = (legal[locale as "hr" | "en"] ?? legal.hr).privacy;
  return <LegalArticle doc={doc} />;
}
