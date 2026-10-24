export type Field = { Label: string; Value: string; Sensitive: boolean };
export type Device = {
  Title: string;
  Kind: string;
  Source: string;
  Note: string | null;
  Fields: Field[];
};
export type Category = {
  Id: string;
  Title: string;
  Description: string;
  Explanation: string;
  Devices: Device[];
};
export type Source = {
  Source: string;
  Status: string;
  Count: number;
  DurationMs: number;
  Message: string;
};
export type Snapshot = {
  AppVersion: string;
  CapturedAt: string;
  DurationSeconds: number;
  Categories: Category[];
  Sources: Source[];
  Limitations: string[];
};
export type Metrics = {
  cpuPercent: number | null;
  memoryPercent: number | null;
  totalMemory: number;
  availableMemory: number;
  sampledAt: number;
};
export type ExportFormat = "html" | "json" | "csv";
declare global {
  interface Window {
    device?: {
      scan: () => Promise<Snapshot>;
      snapshot: (includePrivate: boolean) => Promise<Snapshot | null>;
      exportReport: (
        format: ExportFormat,
      ) => Promise<{ canceled: boolean; fileName?: string }>;
      onMetrics: (callback: (metrics: Metrics) => void) => () => void;
      openRepository: () => Promise<void>;
      setTheme: (theme: "light" | "dark") => Promise<void>;
    };
  }
}
