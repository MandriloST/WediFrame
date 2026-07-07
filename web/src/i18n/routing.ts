import { defineRouting } from "next-intl/routing";

/**
 * Locale routing for WediFrame.
 * - "hr" is the default and has NO URL prefix (wediframe.hr/ is Croatian).
 * - "en" lives under /en.
 * Adding a language later (e.g. "sr") = add it here + create messages/sr.json.
 */
export const routing = defineRouting({
  locales: ["hr", "en"],
  defaultLocale: "hr",
  localePrefix: "as-needed",
});

export type Locale = (typeof routing.locales)[number];
