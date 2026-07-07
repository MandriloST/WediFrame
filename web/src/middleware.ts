import createMiddleware from "next-intl/middleware";
import { routing } from "./i18n/routing";

export default createMiddleware(routing);

export const config = {
  // Run on all paths except Next internals, API proxy paths and static files.
  matcher: "/((?!api|_next|_vercel|icons|.*\\..*).*)",
};
