import {
  useCallback,
  useDeferredValue,
  useEffect,
  useRef,
  useState,
} from "react";
import {
  ArrowDownToLine,
  ArrowRight,
  ArrowUpRight,
  Box,
  Cable,
  Check,
  ChevronDown,
  ChevronRight,
  CircleAlert,
  Command,
  Cpu,
  Database,
  FileJson,
  FileText,
  Fingerprint,
  GitFork,
  HardDrive,
  Keyboard,
  Laptop,
  LayoutDashboard,
  LockKeyhole,
  MemoryStick,
  Moon,
  Monitor,
  RefreshCw,
  ScanLine,
  Search,
  ShieldCheck,
  SlidersHorizontal,
  Sun,
  Table2,
  X,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { LiveOverview } from "@/components/live-overview";
import { DeviceDetails, SearchResults } from "@/components/device-details";
import { Sources } from "@/components/sources";
import { diskDevices, first } from "@/lib/inventory";
import { cn } from "@/lib/utils";
import type { ExportFormat, Snapshot } from "@/types";

const NAV: {
  id: string;
  title: string;
  icon: LucideIcon;
  hint: string;
  color: string;
}[] = [
  {
    id: "cpu",
    title: "CPU",
    icon: Cpu,
    hint: "Pusat pemrosesan perangkatmu.",
    color: "clay",
  },
  {
    id: "ram",
    title: "RAM",
    icon: MemoryStick,
    hint: "Ruang untuk setiap ide yang berjalan.",
    color: "sage",
  },
  {
    id: "storage",
    title: "Storage",
    icon: HardDrive,
    hint: "Tempat semua hal tersimpan.",
    color: "sand",
  },
  {
    id: "io",
    title: "Input / Output",
    icon: Keyboard,
    hint: "Cara perangkatmu berinteraksi.",
    color: "lavender",
  },
  {
    id: "ports",
    title: "Port & konektivitas",
    icon: Cable,
    hint: "Terhubung dengan dunia di luar.",
    color: "blue",
  },
  {
    id: "system",
    title: "Sistem",
    icon: Monitor,
    hint: "Fondasi di balik semuanya.",
    color: "stone",
  },
];

function HintButton({
  label,
  children,
  onClick,
}: {
  label: string;
  children: React.ReactNode;
  onClick: () => void;
}) {
  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Button
          variant="ghost"
          size="icon-sm"
          aria-label={label}
          onClick={onClick}
        >
          {children}
        </Button>
      </TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  );
}
function Loading() {
  return (
    <div className="loading-layout" aria-label="Memindai perangkat">
      <div className="loading-intro">
        <ScanLine className="scan-pulse" size={30} />
        <h2>Sedang mengenali perangkatmu.</h2>
        <p>Kami membaca spesifikasi langsung dari Windows.</p>
      </div>
      <div className="metric-grid">
        {[0, 1, 2].map((i) => (
          <Card key={i} className="loading-card">
            <Skeleton className="h-3 w-24" />
            <Skeleton className="mt-5 h-8 w-32" />
            <Skeleton className="mt-4 h-3 w-full" />
          </Card>
        ))}
      </div>
      <div className="component-grid">
        {[0, 1, 2, 3, 4, 5].map((i) => (
          <Skeleton className="h-32 rounded-xl" key={i} />
        ))}
      </div>
    </div>
  );
}

function Overview({
  snapshot,
  onNavigate,
}: {
  snapshot: Snapshot;
  onNavigate: (id: string) => void;
}) {
  const successful = snapshot.Sources.filter(
    (s) => s.Status === "Berhasil",
  ).length;
  const unavailable = snapshot.Sources.filter(
    (s) => s.Status === "Tidak tersedia",
  ).length;
  return (
    <>
      <LiveOverview snapshot={snapshot} onNavigate={onNavigate} />
      <div className="section-title component-heading">
        <div>
          <h2>Setiap komponen, lebih jelas.</h2>
          <p>Spesifikasi lengkap, satu per satu.</p>
        </div>
        <span>5 kategori rubrik + sistem</span>
      </div>
      <div className="component-grid">
        {NAV.map(({ id, title, icon: Icon, hint, color }, index) => {
          const category = snapshot.Categories.find((c) => c.Id === id);
          const devices = category?.Devices ?? [];
          let detail = `${devices.length} entri inventaris`;
          if (id === "cpu")
            detail =
              first(snapshot, "cpu", "Cache L3") !== "Tidak dilaporkan"
                ? `${first(snapshot, "cpu", "Cache L3")} cache L3`
                : "Core, clock & virtualisasi";
          if (id === "ram")
            detail = `${first(snapshot, "ram", "Jumlah modul terdeteksi")} modul · slot & kecepatan`;
          if (id === "storage")
            detail = `${diskDevices(snapshot).length} disk · volume & partisi`;
          if (id === "io")
            detail = `${devices.length} entri · layar, audio & input`;
          if (id === "ports")
            detail = `${devices.filter((d) => d.Kind === "Konektor fisik · SMBIOS").length} konektor menurut firmware`;
          if (id === "system") detail = "Windows, BIOS & motherboard";
          return (
            <button
              key={id}
              className="component-tile"
              onClick={() => onNavigate(id)}
              style={{ animationDelay: `${index * 30}ms` }}
              data-testid={`tile-${id}`}
            >
              <div className="tile-top">
                <span className={`icon-tile ${color}`}>
                  <Icon size={21} strokeWidth={1.55} />
                </span>
                <ArrowUpRight size={17} className="tile-arrow" />
              </div>
              <h3>{title}</h3>
              <p>{hint}</p>
              <div className="tile-foot">{detail}</div>
            </button>
          );
        })}
      </div>
      <div className="overview-bottom">
        <div className="data-coverage">
          <Database size={17} />
          <div>
            <strong>
              {successful} dari {snapshot.Sources.length} sumber terbaca
            </strong>
            <p>
              {unavailable
                ? `${unavailable} sumber belum tersedia. Lihat alasannya, tanpa perkiraan.`
                : "Setiap hasil disertai sumber data Windows."}
            </p>
          </div>
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label="Lihat status sumber data"
            onClick={() => onNavigate("sources")}
          >
            <ArrowRight size={17} />
          </Button>
        </div>
        <div className="privacy-note">
          <ShieldCheck size={19} />
          <div>
            <strong>Perangkatmu. Datamu.</strong>
            <p>Tanpa akun, tanpa unggahan ke server.</p>
          </div>
        </div>
      </div>
    </>
  );
}

export default function App() {
  const [snapshot, setSnapshot] = useState<Snapshot | null>(null);
  const [page, setPage] = useState("overview");
  const [query, setQuery] = useState("");
  const deferredQuery = useDeferredValue(query);
  const [scanning, setScanning] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [includePrivate, setIncludePrivate] = useState(false);
  const [privacyBusy, setPrivacyBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [theme, setTheme] = useState<"light" | "dark">(() => {
    try {
      return localStorage.getItem("kyd.theme.v1") === "dark" ? "dark" : "light";
    } catch {
      return "light";
    }
  });
  const searchRef = useRef<HTMLInputElement>(null);
  const contentRef = useRef<HTMLElement>(null);
  const activeScan = useRef(false);
  const mounted = useRef(false);
  useEffect(() => {
    document.documentElement.classList.toggle("dark", theme === "dark");
    try {
      localStorage.setItem("kyd.theme.v1", theme);
    } catch {
      /* Settings are optional. */
    }
    window.device?.setTheme(theme).catch(() => {});
  }, [theme]);
  const scan = useCallback(async () => {
    if (activeScan.current || !window.device) return;
    activeScan.current = true;
    setScanning(true);
    setError("");
    try {
      const result = await window.device.scan();
      setSnapshot(result);
    } catch (e) {
      setError(
        e instanceof Error
          ? e.message
          : "Pemindaian belum berhasil. Coba kembali.",
      );
    } finally {
      activeScan.current = false;
      setScanning(false);
    }
  }, []);
  useEffect(() => {
    if (!mounted.current) {
      mounted.current = true;
      if (window.device) void scan();
      else
        setError(
          "Buka aplikasi desktop KnowYourDevice untuk membaca hardware Windows. Tidak ada data contoh yang ditampilkan di browser.",
        );
    }
  }, [scan]);
  useEffect(() => {
    const keydown = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        searchRef.current?.focus();
      }
      if (event.key === "F5") {
        event.preventDefault();
        void scan();
      }
      if (event.key === "Escape") setQuery("");
    };
    window.addEventListener("keydown", keydown);
    return () => window.removeEventListener("keydown", keydown);
  }, [scan]);
  useEffect(() => {
    if (!notice) return;
    const timeout = window.setTimeout(() => setNotice(""), 6500);
    return () => window.clearTimeout(timeout);
  }, [notice]);
  const navigate = useCallback((id: string) => {
    setPage(id);
    setQuery("");
    contentRef.current?.scrollTo({ top: 0, behavior: "instant" });
  }, []);
  async function togglePrivacy(checked: boolean) {
    if (!window.device) return;
    setPrivacyBusy(true);
    try {
      const result = await window.device.snapshot(checked);
      setIncludePrivate(checked);
      if (result) setSnapshot(result);
    } catch {
      setError("Pengaturan privasi belum berhasil diterapkan.");
    } finally {
      setPrivacyBusy(false);
    }
  }
  async function exportReport(format: ExportFormat) {
    if (!window.device) return;
    setExporting(true);
    try {
      const result = await window.device.exportReport(format);
      if (!result.canceled) setNotice(`Laporan disimpan: ${result.fileName}`);
    } catch (e) {
      setError(
        e instanceof Error ? e.message : "Laporan belum dapat disimpan.",
      );
    } finally {
      setExporting(false);
    }
  }
  const selected = NAV.find((item) => item.id === page);
  const activeCategory = snapshot?.Categories.find((c) => c.Id === page);
  const title =
    page === "overview"
      ? query
        ? "Temukan detail perangkatmu."
        : "Kenali perangkatmu, lebih dekat."
      : page === "sources"
        ? "Data yang bisa kamu percaya."
        : (selected?.title ?? "Perangkat");
  const subtitle =
    page === "overview"
      ? query
        ? `Hasil pencarian untuk “${query}” di seluruh komponen.`
        : "Pahami apa yang ada di dalamnya. Dari prosesor hingga setiap koneksi."
      : page === "sources"
        ? "Sumber, status, dan batas deteksi. Semuanya terbuka."
        : (activeCategory?.Description ??
          "Membaca spesifikasi perangkat Windows.");
  const model = snapshot
    ? first(snapshot, "system", "Model perangkat")
    : "Perangkat Windows";
  const properties =
    snapshot?.Categories.reduce(
      (sum, category) =>
        sum + category.Devices.reduce((n, d) => n + d.Fields.length, 0),
      0,
    ) ?? 0;
  return (
    <TooltipProvider delayDuration={350}>
      <div className="windowbar">
        <span>
          <ScanLine size={13} />
          KnowYourDevice
        </span>
        <span className="windowbar-caption">
          A little clarity for your everyday device.
        </span>
      </div>
      <div className="app-shell">
        <aside className="sidebar">
          <button
            className="brand"
            onClick={() => navigate("overview")}
            aria-label="KnowYourDevice · Ringkasan"
          >
            <span className="brand-mark">
              <ScanLine size={24} strokeWidth={1.6} />
            </span>
            <span>
              KnowYourDevice<small>YOUR DEVICE, UNDERSTOOD.</small>
            </span>
          </button>
          <div className="workspace-tag">
            <span className="local-dot" />
            <span>Personal workspace</span>
            <LockKeyhole size={12} />
          </div>
          <nav aria-label="Navigasi utama">
            <span className="nav-label">WORKSPACE</span>
            <button
              className={cn("nav-item", page === "overview" && "active")}
              aria-current={page === "overview" ? "page" : undefined}
              onClick={() => navigate("overview")}
              data-testid="nav-overview"
            >
              <LayoutDashboard size={17} />
              <span>Ringkasan</span>
              <span className="nav-shortcut">⌘</span>
            </button>
            <span className="nav-label nav-hardware">JELAJAHI HARDWARE</span>
            {NAV.map(({ id, title, icon: Icon }) => (
              <button
                key={id}
                className={cn("nav-item", page === id && "active")}
                aria-current={page === id ? "page" : undefined}
                onClick={() => navigate(id)}
                data-testid={`nav-${id}`}
              >
                <Icon size={17} />
                <span>{title}</span>
                {page === id ? <span className="nav-selected-dot" /> : null}
              </button>
            ))}
            <div className="nav-divider" />
            <button
              className={cn("nav-item", page === "sources" && "active")}
              onClick={() => navigate("sources")}
              aria-current={page === "sources" ? "page" : undefined}
              data-testid="nav-sources"
            >
              <Database size={17} />
              <span>Sumber data</span>
              <ArrowUpRight size={13} className="nav-tail" />
            </button>
          </nav>
          <div className="sidebar-bottom">
            <div className="local-card">
              <ShieldCheck size={19} />
              <strong>Sepenuhnya lokal.</strong>
              <p>
                Informasi perangkat tetap
                <br />
                di perangkatmu sendiri.
              </p>
              <span>
                <i />
                Tidak memerlukan internet
              </span>
            </div>
            <div className="sidebar-tools">
              <span>Made for the curious.</span>
              <HintButton
                label={
                  theme === "light"
                    ? "Aktifkan mode gelap"
                    : "Aktifkan mode terang"
                }
                onClick={() => setTheme(theme === "light" ? "dark" : "light")}
              >
                {theme === "light" ? <Moon size={15} /> : <Sun size={15} />}
              </HintButton>
              <HintButton
                label="Buka repositori GitHub"
                onClick={() => {
                  void window.device?.openRepository();
                }}
              >
                <GitFork size={15} />
              </HintButton>
            </div>
          </div>
        </aside>
        <div className="workspace">
          <header className="toolbar">
            <div className="breadcrumb">
              <span>Workspace</span>
              <ChevronRight size={12} />
              <strong>
                {page === "overview"
                  ? "Ringkasan"
                  : page === "sources"
                    ? "Sumber data"
                    : selected?.title}
              </strong>
            </div>
            <div className="toolbar-actions">
              <span className="scan-stamp">
                {scanning
                  ? "Membaca hardware…"
                  : snapshot
                    ? `Diperbarui ${new Date(snapshot.CapturedAt).toLocaleTimeString("id-ID", { hour: "2-digit", minute: "2-digit" })}`
                    : "Perangkat lokal"}
              </span>
              <Button
                variant="outline"
                size="sm"
                onClick={() => void scan()}
                disabled={scanning || !window.device}
                data-testid="scan-button"
              >
                <RefreshCw
                  size={14}
                  className={scanning ? "animate-spin" : ""}
                />
                {scanning ? "Memindai" : "Pindai ulang"}
              </Button>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    size="sm"
                    disabled={!snapshot || exporting || privacyBusy}
                    data-testid="export-button"
                  >
                    <ArrowDownToLine size={14} />
                    Ekspor laporan
                    <ChevronDown size={13} />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="export-menu">
                  <DropdownMenuLabel>
                    Simpan seluruh inventaris
                  </DropdownMenuLabel>
                  <DropdownMenuLabel className="export-privacy">
                    {includePrivate
                      ? "ID pribadi akan disertakan"
                      : "ID pribadi disembunyikan"}
                  </DropdownMenuLabel>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onSelect={() => void exportReport("html")}>
                    <FileText size={17} />
                    <div>
                      Laporan HTML
                      <small>Buka di browser · cetak sebagai PDF</small>
                    </div>
                  </DropdownMenuItem>
                  <DropdownMenuItem onSelect={() => void exportReport("json")}>
                    <FileJson size={17} />
                    <div>
                      Data JSON
                      <small>Inventaris terstruktur & status sumber</small>
                    </div>
                  </DropdownMenuItem>
                  <DropdownMenuItem onSelect={() => void exportReport("csv")}>
                    <Table2 size={17} />
                    <div>
                      Tabel CSV<small>Buka dengan Excel atau spreadsheet</small>
                    </div>
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </header>
          <main className="main-content" ref={contentRef} id="main-content">
            <div className="content-width">
              <div className="page-heading">
                <div>
                  <div className="eyebrow heading-eyebrow">
                    <span className="clay-dot" />
                    {page === "overview"
                      ? "DEVICE EXPLORER"
                      : page === "sources"
                        ? "BUILT ON TRANSPARENCY"
                        : "INSIDE YOUR DEVICE"}
                  </div>
                  <h1>{title}</h1>
                  <p>{subtitle}</p>
                </div>
                {page === "overview" && !query ? (
                  <div className="device-label">
                    <Laptop size={28} strokeWidth={1.3} />
                    <div>
                      <strong title={model}>{model}</strong>
                      <span>Windows · perangkat lokal</span>
                    </div>
                  </div>
                ) : selected ? (
                  <span className={`page-icon icon-tile ${selected.color}`}>
                    <selected.icon size={28} strokeWidth={1.3} />
                  </span>
                ) : null}
              </div>
              <div className="content-controls">
                <div className="search-control">
                  <Search size={15} />
                  <Input
                    ref={searchRef}
                    value={query}
                    onChange={(event) => setQuery(event.target.value)}
                    placeholder={
                      page === "overview"
                        ? "Cari di seluruh perangkat…"
                        : `Cari di ${page === "sources" ? "sumber data" : selected?.title}…`
                    }
                    aria-label="Cari perangkat atau spesifikasi"
                  />
                  {query ? (
                    <button
                      aria-label="Hapus pencarian"
                      onClick={() => setQuery("")}
                    >
                      <X size={13} />
                    </button>
                  ) : (
                    <kbd>
                      <Command size={10} />K
                    </kbd>
                  )}
                </div>
                <div className="privacy-control">
                  <Fingerprint size={15} />
                  <label htmlFor="privacy-switch">
                    ID pribadi{" "}
                    <span>{includePrivate ? "terlihat" : "tersembunyi"}</span>
                  </label>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <span>
                        <Switch
                          id="privacy-switch"
                          size="sm"
                          checked={includePrivate}
                          onCheckedChange={(checked) =>
                            void togglePrivacy(checked)
                          }
                          disabled={!snapshot || privacyBusy || scanning}
                          aria-label="Tampilkan dan ekspor ID pribadi"
                        />
                      </span>
                    </TooltipTrigger>
                    <TooltipContent>
                      Nomor seri, MAC, dan hardware ID. Pilihan ini juga berlaku
                      pada ekspor.
                    </TooltipContent>
                  </Tooltip>
                </div>
              </div>
              {error ? (
                <div className="error-banner" role="alert">
                  <CircleAlert size={18} />
                  <span>{error}</span>
                  {!scanning && window.device ? (
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => void scan()}
                    >
                      Coba lagi
                    </Button>
                  ) : null}
                </div>
              ) : null}
              {!snapshot ? (
                scanning ? (
                  <Loading />
                ) : !error ? (
                  <Loading />
                ) : (
                  <div className="empty-initial">
                    <Box size={45} strokeWidth={1} />
                    <h2>Perangkatmu menunggu untuk dikenali.</h2>
                    <p>
                      Mulai pemindaian untuk melihat spesifikasi sebenarnya.
                    </p>
                  </div>
                )
              ) : (
                <div className="page-transition" key={page}>
                  {page === "overview" ? (
                    deferredQuery ? (
                      <SearchResults
                        categories={snapshot.Categories}
                        query={deferredQuery}
                        includePrivate={includePrivate}
                      />
                    ) : (
                      <Overview snapshot={snapshot} onNavigate={navigate} />
                    )
                  ) : page === "sources" ? (
                    <Sources snapshot={snapshot} query={deferredQuery} />
                  ) : activeCategory ? (
                    <DeviceDetails
                      key={page}
                      category={activeCategory}
                      query={deferredQuery}
                      includePrivate={includePrivate}
                    />
                  ) : null}
                </div>
              )}
              <footer className="content-footer">
                <span>
                  <LockKeyhole size={12} />
                  Hanya membaca perangkat. Tidak mengubah pengaturan.
                </span>
                <Dialog>
                  <DialogTrigger asChild>
                    <button className="about-button">
                      <SlidersHorizontal size={12} />
                      Tentang aplikasi
                    </button>
                  </DialogTrigger>
                  <DialogContent>
                    <DialogHeader>
                      <DialogTitle>KnowYourDevice 2.0</DialogTitle>
                      <DialogDescription>
                        Kenali perangkatmu, lebih dekat.
                      </DialogDescription>
                    </DialogHeader>
                    <div className="about-copy">
                      <p>
                        Dibangun dengan React, shadcn/ui, Lucide, dan Electron.
                        Mesin deteksi lokal membaca inventaris Windows melalui
                        WMI dan API sistem.
                      </p>
                      <p>
                        Tipografi Lora & Inter menghadirkan nuansa editorial
                        yang terinspirasi Anthropic. Keduanya memakai lisensi
                        font terbuka; aplikasi ini tidak menggunakan font khusus
                        atau berafiliasi dengan Anthropic.
                      </p>
                      <p>
                        Informasi firmware mungkin tidak lengkap. Jumlah entri
                        mencakup perangkat internal dan virtual. Periksa halaman
                        Sumber data untuk batas deteksi.
                      </p>
                      <Badge variant="outline">
                        Windows 10/11 x64 · .NET Framework 4.8
                      </Badge>
                    </div>
                  </DialogContent>
                </Dialog>
              </footer>
            </div>
          </main>
          <div className="statusbar">
            <span>
              <i className={cn("status-dot", scanning && "busy")} />
              {scanning
                ? "Membaca perangkat Windows…"
                : snapshot
                  ? `${properties} properti · ${snapshot.DurationSeconds.toLocaleString("id-ID")} detik saat pemindaian`
                  : "Siap mengenali perangkat"}
            </span>
            <span>
              KnowYourDevice <span className="version">v2.0.0</span>
              <span className="status-divider">/</span>
              <ShieldCheck size={11} />
              Lokal & privat
            </span>
          </div>
        </div>
      </div>
      <div className="toast-region" role="status" aria-live="polite">
        {notice ? (
          <div className="toast">
            <Check size={17} />
            <span>{notice}</span>
            <button
              aria-label="Tutup pemberitahuan"
              onClick={() => setNotice("")}
            >
              <X size={14} />
            </button>
          </div>
        ) : null}
      </div>
    </TooltipProvider>
  );
}
