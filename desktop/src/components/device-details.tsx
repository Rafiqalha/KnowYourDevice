import { useState } from "react";
import { BookOpen, Database, Fingerprint, Info, SearchX } from "lucide-react";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { filterDevices } from "@/lib/inventory";
import type { Category, Device } from "@/types";

function DeviceRow({ device, index }: { device: Device; index: number }) {
  return (
    <AccordionItem
      value={String(index)}
      className="device-row"
      data-testid="device-card"
    >
      <AccordionTrigger className="device-trigger">
        <div>
          <span className="eyebrow">{device.Kind}</span>
          <h3>{device.Title}</h3>
          <span className="device-count">{device.Fields.length} properti</span>
        </div>
      </AccordionTrigger>
      <AccordionContent className="device-content">
        <dl className="spec-table">
          {device.Fields.map((field, i) => (
            <div key={`${field.Label}-${i}`} className="spec-row">
              <dt>
                {field.Label}
                {field.Sensitive ? (
                  <Fingerprint size={13} aria-label="ID pribadi" />
                ) : null}
              </dt>
              <dd
                className={
                  field.Value === "Tidak dilaporkan" ||
                  field.Value === "Disembunyikan (ID pribadi)"
                    ? "unreported"
                    : ""
                }
              >
                {field.Value}
              </dd>
            </div>
          ))}
        </dl>
        {device.Note ? (
          <div className="device-note">
            <Info size={14} />
            <p>{device.Note}</p>
          </div>
        ) : null}
        <div className="source-foot">
          <Database size={12} />
          <span>{device.Source}</span>
        </div>
      </AccordionContent>
    </AccordionItem>
  );
}
export function EmptySearch() {
  return (
    <div className="empty-state">
      <SearchX size={30} strokeWidth={1.3} />
      <h3>Tidak ada yang cocok.</h3>
      <p>Coba nama, produsen, jenis, atau spesifikasi lain.</p>
    </div>
  );
}
export function DeviceDetails({
  category,
  query,
  includePrivate,
}: {
  category: Category;
  query: string;
  includePrivate: boolean;
}) {
  const [kind, setKind] = useState("all");
  const [showExplanation, setShowExplanation] = useState(false);
  const kinds = [...new Set(category.Devices.map((d) => d.Kind))];
  const filtered = filterDevices(category, query, includePrivate).filter(
    (d) => kind === "all" || d.Kind === kind,
  );
  return (
    <>
      <div className="education-bar">
        <div>
          <BookOpen size={17} />
          <span>Memahami {category.Title}</span>
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setShowExplanation(!showExplanation)}
          aria-expanded={showExplanation}
        >
          {showExplanation ? "Tutup penjelasan" : "Pelajari komponen"}
        </Button>
      </div>
      {showExplanation ? (
        <p className="education-copy animate-in fade-in slide-in-from-top-1">
          {category.Explanation}
        </p>
      ) : null}
      {kinds.length > 1 ? (
        <div className="filter-chips" aria-label="Filter jenis perangkat">
          <Button
            size="sm"
            variant={kind === "all" ? "secondary" : "ghost"}
            onClick={() => setKind("all")}
          >
            Semua <Badge variant="outline">{category.Devices.length}</Badge>
          </Button>
          {kinds.map((type) => (
            <Button
              key={type}
              size="sm"
              variant={kind === type ? "secondary" : "ghost"}
              onClick={() => setKind(type)}
            >
              {type}
            </Button>
          ))}
        </div>
      ) : null}
      <div className="results-label">
        {filtered.length} entri ditampilkan{" "}
        <span>· Klik untuk melihat spesifikasi</span>
      </div>
      {filtered.length ? (
        <Accordion type="multiple" defaultValue={["0"]} className="device-list">
          {filtered.map((device, index) => (
            <DeviceRow
              key={`${device.Title}-${index}`}
              device={device}
              index={index}
            />
          ))}
        </Accordion>
      ) : query ? (
        <EmptySearch />
      ) : (
        <div className="empty-state">
          <Database size={30} strokeWidth={1.3} />
          <h3>Belum ada data yang tersedia.</h3>
          <p>
            Periksa status penyedia di halaman Sumber data. Hasil kosong tidak
            membuktikan perangkat tidak ada.
          </p>
        </div>
      )}
    </>
  );
}
export function SearchResults({
  categories,
  query,
  includePrivate,
}: {
  categories: Category[];
  query: string;
  includePrivate: boolean;
}) {
  const groups = categories
    .map((category) => ({
      category,
      devices: filterDevices(category, query, includePrivate),
    }))
    .filter((group) => group.devices.length);
  if (!groups.length) return <EmptySearch />;
  return (
    <div className="global-results">
      {groups.map(({ category, devices }) => (
        <section key={category.Id}>
          <h2 className="section-title">
            {category.Title}
            <Badge variant="outline">{devices.length} entri</Badge>
          </h2>
          <Accordion type="multiple" className="device-list">
            {devices.map((device, index) => (
              <DeviceRow
                key={`${device.Title}-${index}`}
                device={device}
                index={index}
              />
            ))}
          </Accordion>
        </section>
      ))}
    </div>
  );
}
