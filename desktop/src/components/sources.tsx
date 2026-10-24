import { CheckCircle2, CircleAlert, CircleDashed, Info } from "lucide-react";
import { Card } from "@/components/ui/card";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";
import type { Snapshot } from "@/types";

export function Sources({
  snapshot,
  query,
}: {
  snapshot: Snapshot;
  query: string;
}) {
  const counts = ["Berhasil", "Kosong", "Tidak tersedia"].map(
    (status) => snapshot.Sources.filter((s) => s.Status === status).length,
  );
  const icons = [CheckCircle2, CircleDashed, CircleAlert];
  const labels = ["Sumber terbaca", "Tidak ada entri", "Belum bisa diakses"];
  const filtered = snapshot.Sources.filter((s) =>
    `${s.Source} ${s.Status} ${s.Message}`
      .toLowerCase()
      .includes(query.trim().toLowerCase()),
  );
  return (
    <>
      <div className="source-summary">
        {counts.map((value, i) => {
          const Icon = icons[i];
          return (
            <Card className="source-stat" key={labels[i]}>
              <Icon
                size={19}
                className={i === 0 ? "sage" : i === 2 ? "amber" : "muted"}
              />
              <strong>{value}</strong>
              <span>{labels[i]}</span>
            </Card>
          );
        })}
      </div>
      <div className="section-title">
        <h2>Transparansi di setiap pembacaan.</h2>
        <span>{filtered.length} penyedia</span>
      </div>
      <div className="source-table">
        <div className="source-table-head">
          <span>Penyedia Windows</span>
          <span>Status</span>
          <span>Entri</span>
          <span>Waktu</span>
        </div>
        {filtered.map((source) => (
          <details className="source-table-row" key={source.Source}>
            <summary>
              <span className="source-name" title={source.Source}>
                {source.Source.split(":").at(-1)}
              </span>
              <span
                className={`source-status ${source.Status === "Berhasil" ? "success" : source.Status === "Kosong" ? "empty" : "warning"}`}
              >
                <i />
                {source.Status}
              </span>
              <span>{source.Count}</span>
              <span>{source.DurationMs} ms</span>
            </summary>
            <div className="source-message">
              <code>{source.Source}</code>
              <p>{source.Message}</p>
            </div>
          </details>
        ))}
      </div>
      <div className="limitations">
        <h2 className="section-title">
          <Info size={18} />
          Batas deteksi, dijelaskan.
        </h2>
        <Accordion type="multiple">
          {snapshot.Limitations.map((limitation, index) => (
            <AccordionItem key={index} value={String(index)}>
              <AccordionTrigger>
                {[
                  "Sumber data & nilai kosong",
                  "Port dan kemampuan konektor",
                  "Clock, kanal RAM & upgrade",
                  "Sensor & kesehatan hardware",
                  "Perangkat virtual & jumlah entri",
                ][index] ?? `Catatan ${index + 1}`}
              </AccordionTrigger>
              <AccordionContent>{limitation}</AccordionContent>
            </AccordionItem>
          ))}
        </Accordion>
      </div>
    </>
  );
}
