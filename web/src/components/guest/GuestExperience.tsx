"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import type { UploadState } from "@/lib/guestApi";
import { UploadSection, type ConfirmedUpload } from "./UploadSection";
import { Gallery, type GalleryPreview } from "./Gallery";

/**
 * Client shell that ties the upload flow to the gallery: the moment a photo is
 * confirmed, its local blob is shown in the grid instantly (no download), and
 * the server-listed copy is deduped against it by mediaId once it appears.
 */
export function GuestExperience({
  token,
  uploadState,
  uploadStartDate,
}: {
  token: string;
  uploadState: UploadState;
  uploadStartDate: string;
}) {
  const [previews, setPreviews] = useState<GalleryPreview[]>([]);
  const objectUrls = useRef<string[]>([]);

  // Free the object URLs when the guest leaves the page.
  useEffect(() => {
    const urls = objectUrls.current;
    return () => urls.forEach((u) => URL.revokeObjectURL(u));
  }, []);

  const handleConfirmed = useCallback(({ mediaId, file, contentType }: ConfirmedUpload) => {
    const url = URL.createObjectURL(file);
    objectUrls.current.push(url);
    setPreviews((prev) =>
      prev.some((p) => p.mediaId === mediaId)
        ? prev
        : [{ mediaId, url, contentType }, ...prev],
    );
  }, []);

  return (
    <>
      <UploadSection
        token={token}
        uploadState={uploadState}
        uploadStartDate={uploadStartDate}
        onConfirmed={handleConfirmed}
      />
      <Gallery token={token} previews={previews} />
    </>
  );
}
