"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { getExport, startExport, type ExportJob } from "@/lib/hostApi";
import { triggerBrowserDownload } from "@/components/media/MediaGallery";

type Phase = "idle" | "working" | "ready" | "error";

const POLL_MS = 2500;

/**
 * "Download everything as a ZIP." Starts a background export, polls until the
 * archive is ready, then triggers the download. The heavy work runs server-side
 * (ExportWorker); this is just start → poll → save.
 */
export function ExportAll({ eventId }: { eventId: string }) {
  const t = useTranslations("hostGallery");
  const [phase, setPhase] = useState<Phase>("idle");
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const alive = useRef(true);

  useEffect(() => {
    alive.current = true;
    return () => {
      alive.current = false;
      if (timer.current) clearTimeout(timer.current);
    };
  }, []);

  const finishReady = useCallback((job: ExportJob) => {
    setPhase("ready");
    if (job.downloadUrl) triggerBrowserDownload(job.downloadUrl);
  }, []);

  // Ref lets the recursive setTimeout call the latest poll without a
  // use-before-declare cycle.
  const pollRef = useRef<(jobId: string) => void>(() => {});

  const poll = useCallback(
    async (jobId: string) => {
      if (!alive.current) return;
      try {
        const job = await getExport(eventId, jobId);
        if (!alive.current) return;
        if (job.status === "Ready") {
          finishReady(job);
        } else if (job.status === "Failed") {
          setPhase("error");
        } else {
          timer.current = setTimeout(() => pollRef.current(jobId), POLL_MS);
        }
      } catch {
        if (alive.current) setPhase("error");
      }
    },
    [eventId, finishReady],
  );

  useEffect(() => {
    pollRef.current = (jobId: string) => void poll(jobId);
  }, [poll]);

  const start = useCallback(async () => {
    if (phase === "working") return;
    setPhase("working");
    try {
      const job = await startExport(eventId);
      if (!alive.current) return;
      if (job.status === "Ready") finishReady(job);
      else if (job.status === "Failed") setPhase("error");
      else void poll(job.jobId);
    } catch {
      if (alive.current) setPhase("error");
    }
  }, [eventId, phase, poll, finishReady]);

  const working = phase === "working";

  return (
    <div className="flex flex-col items-end gap-1">
      <button
        type="button"
        onClick={() => void start()}
        disabled={working}
        className="inline-flex items-center gap-2 rounded-full border border-[#E7E0D8] bg-white px-4 py-2 text-sm font-medium text-[#44403C] transition active:scale-[0.99] disabled:opacity-60"
      >
        {working && (
          <span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-[#D6CCBE] border-t-[#7C2D3E]" />
        )}
        {working
          ? t("exportPreparing")
          : phase === "ready"
            ? t("exportReady")
            : t("exportAll")}
      </button>
      {working && <span className="text-xs text-[#A8A29E]">{t("exportHint")}</span>}
      {phase === "error" && (
        <span className="text-xs text-[#B4432F]">{t("exportError")}</span>
      )}
    </div>
  );
}
