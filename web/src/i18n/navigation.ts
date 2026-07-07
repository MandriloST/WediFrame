import { createNavigation } from "next-intl/navigation";
import { routing } from "./routing";

/**
 * Always import Link/redirect/usePathname/useRouter from here
 * (NOT from next/link / next/navigation) so locale prefixes are handled.
 */
export const { Link, redirect, usePathname, useRouter, getPathname } =
  createNavigation(routing);
