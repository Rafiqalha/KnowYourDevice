import type { Category, Device, Snapshot } from "@/types";

export const UNKNOWN = "Tidak dilaporkan";
export function field(device: Device | undefined, label: string) {
  return device?.Fields.find((f) => f.Label === label)?.Value ?? UNKNOWN;
}
export function first(snapshot: Snapshot, id: string, label: string) {
  return (
    snapshot.Categories.find((c) => c.Id === id)
      ?.Devices.flatMap((d) => d.Fields)
      .find((f) => f.Label === label)?.Value ?? UNKNOWN
  );
}
export function bytes(value: number) {
  if (!Number.isFinite(value) || value < 0) return "—";
  const units = ["B", "KiB", "MiB", "GiB", "TiB"];
  let i = 0;
  while (value >= 1024 && i < units.length - 1) {
    value /= 1024;
    i++;
  }
  return `${new Intl.NumberFormat("id-ID", { maximumFractionDigits: 1 }).format(value)} ${units[i]}`;
}
export function matches(
  device: Device,
  query: string,
  includePrivate: boolean,
) {
  const haystack = [
    device.Title,
    device.Kind,
    device.Source,
    device.Note,
    ...device.Fields.filter((f) => !f.Sensitive || includePrivate).map(
      (f) => `${f.Label} ${f.Value}`,
    ),
  ];
  return haystack.some((value) =>
    value?.toLocaleLowerCase("id-ID").includes(query),
  );
}
export function filterDevices(
  category: Category,
  query: string,
  includePrivate: boolean,
) {
  const q = query.trim().toLocaleLowerCase("id-ID");
  if (
    !q ||
    `${category.Id} ${category.Title}`.toLocaleLowerCase("id-ID").includes(q)
  )
    return category.Devices;
  return category.Devices.filter((d) => matches(d, q, includePrivate));
}
export function diskDevices(snapshot: Snapshot) {
  const devices =
    snapshot.Categories.find((c) => c.Id === "storage")?.Devices ?? [];
  const modern = devices.filter((d) => d.Kind === "Disk fisik · Storage API");
  return modern.length
    ? modern
    : devices.filter((d) => d.Kind === "Disk fisik · WMI");
}
