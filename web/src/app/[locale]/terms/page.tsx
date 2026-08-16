import { setRequestLocale } from "next-intl/server";
import { LegalArticle } from "@/components/legal/LegalArticle";
import { legal } from "@/content/legal";

export default async function TermsPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const doc = (legal[locale as "hr" | "en"] ?? legal.hr).terms;
  return <LegalArticle doc={doc} />;
}
