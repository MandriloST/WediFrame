import type { MetadataRoute } from "next";

/**
 * PWA manifest. Croatian strings on purpose — the manifest is a single static
 * file and HR is the product's default market/locale (per docs/PROJECT.md).
 * Served at /manifest.webmanifest.
 */
export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "WediFrame",
    short_name: "WediFrame",
    description:
      "Privatna galerija za vaše vjenčanje — gosti dijele fotografije i video skeniranjem QR koda.",
    id: "/",
    start_url: "/",
    scope: "/",
    display: "standalone",
    orientation: "portrait",
    background_color: "#faf9f7",
    theme_color: "#faf9f7",
    lang: "hr",
    icons: [
      {
        src: "/icons/icon-192.png",
        sizes: "192x192",
        type: "image/png",
        purpose: "any",
      },
      {
        src: "/icons/icon-512.png",
        sizes: "512x512",
        type: "image/png",
        purpose: "any",
      },
      {
        src: "/icons/icon-maskable-512.png",
        sizes: "512x512",
        type: "image/png",
        purpose: "maskable",
      },
    ],
  };
}
