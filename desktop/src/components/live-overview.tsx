import { memo, useEffect, useId, useState } from "react";
import {
  Activity,
  ArrowUpRight,
  Cpu,
  HardDrive,
  MemoryStick,
} from "lucide-react";
import { Card } from "@/components/ui/card";
import { bytes, diskDevices, field, first } from "@/lib/inventory";
import type { Metrics, Snapshot } from "@/types";

function Sparkline({ samples, color }: { samples: number[]; color: string }) {
  const id = useId().replaceAll(":", "");
  if (!samples.length)
    return <div className="chart-await">Menunggu sampel pertama…</div>;
  const points = samples
    .map((value, i) => `${(i * 220) / 29},${43 - value * 0.38}`)
    .join(" ");
  return (
    <svg
      className="sparkline"
      viewBox="0 0 220 46"
      preserveAspectRatio="none"
      aria-hidden="true"
    >
      <defs>
        <linearGradient id={id} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity=".16" />
          <stop offset="100%" stopColor={color} stopOpacity="0" />
        </linearGradient>
      </defs>
      <path
        d={`M0,46 L${points.replaceAll(" ", " L")} L${((samples.length - 1) * 220) / 29},46 Z`}
        fill={`url(#${id})`}
      />
      <polyline
        points={points}
        fill="none"
        stroke={color}
        strokeWidth="1.7"
        vectorEffect="non-scaling-stroke"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export const LiveOverview = memo(function LiveOverview({
  snapshot,
  onNavigate,
}: {
  snapshot: Snapshot;
  onNavigate: (id: string) => void;
}) {
  const [metrics, setMetrics] = useState<Metrics | null>(null);
  const [history, setHistory] = useState<{ cpu: number[]; ram: number[] }>({
    cpu: [],
    ram: [],
  });
  useEffect(
    () =>
      window.device?.onMetrics((reading) => {
        setMetrics(reading);
        setHistory((previous) => ({
          cpu:
            reading.cpuPercent === null
              ? previous.cpu
              : [...previous.cpu, reading.cpuPercent].slice(-30),
          ram:
            reading.memoryPercent === null
              ? previous.ram
              : [...previous.ram, reading.memoryPercent].slice(-30),
        }));
      }),
    [],
  );
  const disks = diskDevices(snapshot);
  const disk = disks[0];
  const volumes =
    snapshot.Categories.find((c) => c.Id === "storage")?.Devices.filter(
      (d) => d.Kind === "Volume",
    ) ?? [];
  const cpu = snapshot.Categories.find((c) => c.Id === "cpu")?.Devices[0];
  return (
    <section
      className="metric-grid"
      aria-label="Ringkasan hardware dan pemakaian langsung"
    >
      <Card className="metric-card">
        <button
          className="metric-open"
          aria-label="Lihat detail CPU"
          onClick={() => onNavigate("cpu")}
        />
        <div className="metric-label">
          <Cpu size={16} />
          <span>Prosesor</span>
          <span className="live-label">
            <i />
            LIVE
          </span>
        </div>
        <div className="metric-value">
          {first(snapshot, "cpu", "Core fisik").replace(" core", "")}
          <small> core</small>
          <span className="metric-divider" />
          {first(snapshot, "cpu", "Thread logis").replace(" thread", "")}
          <small> thread</small>
        </div>
        <div className="metric-caption truncate" title={cpu?.Title}>
          {cpu?.Title ?? "Data prosesor belum tersedia"}
        </div>
        <div className="metric-bottom">
          <span>
            <Activity size={12} />
            {metrics?.cpuPercent != null
              ? `${metrics.cpuPercent.toFixed(1)}% digunakan`
              : "Mengukur aktivitas…"}
          </span>
          <ArrowUpRight size={15} />
        </div>
        <Sparkline samples={history.cpu} color="#bc795e" />
      </Card>
      <Card className="metric-card">
        <button
          className="metric-open"
          aria-label="Lihat detail RAM"
          onClick={() => onNavigate("ram")}
        />
        <div className="metric-label">
          <MemoryStick size={16} />
          <span>Memori</span>
          <span className="live-label">
            <i />
            LIVE
          </span>
        </div>
        <div className="metric-value">
          {first(snapshot, "ram", "Total modul terpasang")}
        </div>
        <div className="metric-caption">
          {first(snapshot, "ram", "Jumlah modul terdeteksi") !==
          "Tidak dilaporkan"
            ? `${first(snapshot, "ram", "Jumlah modul terdeteksi")} modul terdeteksi`
            : "Kapasitas menurut firmware"}
        </div>
        <div className="metric-bottom">
          <span>
            {metrics
              ? `${bytes(metrics.totalMemory - metrics.availableMemory)} / ${bytes(metrics.totalMemory)} dipakai OS`
              : "Membaca memori Windows…"}
          </span>
          <ArrowUpRight size={15} />
        </div>
        <Sparkline samples={history.ram} color="#6e8a78" />
      </Card>
      <Card className="metric-card storage-metric">
        <button
          className="metric-open"
          aria-label="Lihat detail storage"
          onClick={() => onNavigate("storage")}
        />
        <div className="metric-label">
          <HardDrive size={16} />
          <span>Penyimpanan</span>
          <span className="soft-tag">
            {field(disk, "Bus / protokol") === "Tidak dilaporkan"
              ? "DISK"
              : field(disk, "Bus / protokol")}
          </span>
        </div>
        <div className="metric-value">{field(disk, "Kapasitas")}</div>
        <div className="metric-caption">
          {disks.length
            ? `${disks.length} disk fisik · ${volumes.length} volume`
            : "Data disk fisik belum tersedia"}
        </div>
        <div className="storage-volumes">
          {volumes.slice(0, 2).map((volume) => {
            const percent = Number.parseFloat(
              field(volume, "Pemakaian").replace(",", "."),
            );
            return (
              <div className="volume-mini" key={volume.Title}>
                <span>{volume.Title}</span>
                <div className="volume-track">
                  <div
                    style={{
                      width: `${Number.isFinite(percent) ? Math.min(100, Math.max(0, percent)) : 0}%`,
                    }}
                  />
                </div>
                <span>{field(volume, "Tersedia")} kosong</span>
              </div>
            );
          })}
        </div>
        <span className="metric-link">
          Lihat disk & partisi <ArrowUpRight size={14} />
        </span>
      </Card>
    </section>
  );
});
