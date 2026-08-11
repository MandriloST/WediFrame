import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";

const withNextIntl = createNextIntlPlugin("./src/i18n/request.ts");

const nextConfig: NextConfig = {
  // R2 image domains are added in M1 when the media flow lands.
  allowedDevOrigins: ["192.168.1.185"],
};

export default withNextIntl(nextConfig);
