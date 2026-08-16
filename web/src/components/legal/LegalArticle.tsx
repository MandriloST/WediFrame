import { Link } from "@/i18n/navigation";
import type { LegalDoc } from "@/content/legal";

/** Renders a Privacy/Terms document (server-friendly, brand palette). */
export function LegalArticle({ doc }: { doc: LegalDoc }) {
  return (
    <main className="mx-auto min-h-dvh w-full max-w-2xl bg-[#FFFDF9] px-5 py-12">
      <Link href="/" className="text-sm text-[#57534E]">
        ‹ {doc.backLabel}
      </Link>

      <h1 className="mt-6 text-2xl font-semibold tracking-tight text-[#1C1917]">
        {doc.title}
      </h1>
      <p className="mt-1 text-xs text-[#A8A29E]">
        {doc.updatedLabel}: {doc.updated}
      </p>

      <div className="mt-4 rounded-xl border border-[#E7E0D8] bg-[#FBF3F0] px-4 py-3 text-xs text-[#7C2D3E]">
        {doc.disclaimer}
      </div>

      <p className="mt-6 text-sm leading-relaxed text-[#44403C]">{doc.intro}</p>

      {doc.sections.map((section) => (
        <section key={section.heading} className="mt-6">
          <h2 className="text-sm font-semibold text-[#1C1917]">{section.heading}</h2>
          {section.paragraphs.map((p, i) => (
            <p key={i} className="mt-2 text-sm leading-relaxed text-[#57534E]">
              {p}
            </p>
          ))}
        </section>
      ))}
    </main>
  );
}
