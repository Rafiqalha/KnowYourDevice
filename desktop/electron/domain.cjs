const os = require("node:os");
const HIDDEN = "Disembunyikan (ID pribadi)";
function redact(snapshot, includePrivate = false) {
  if (!snapshot) return null;
  const copy = structuredClone(snapshot);
  for (const category of copy.Categories)
    for (const device of category.Devices)
      for (const field of device.Fields) {
        if (field.Sensitive && !includePrivate) field.Value = HIDDEN;
      }
  return copy;
}
function validFormat(format) {
  if (!["html", "json", "csv"].includes(format))
    throw new Error("Format laporan tidak valid.");
  return format;
}
function cpuTotals(cpus = os.cpus()) {
  return cpus.reduce(
    (total, cpu) => {
      total.idle += cpu.times.idle;
      total.total += Object.values(cpu.times).reduce(
        (sum, time) => sum + time,
        0,
      );
      return total;
    },
    { idle: 0, total: 0 },
  );
}
function cpuPercent(previous, current) {
  if (
    !previous ||
    current.total <= previous.total ||
    current.idle < previous.idle
  )
    return null;
  return Math.max(
    0,
    Math.min(
      100,
      100 *
        (1 - (current.idle - previous.idle) / (current.total - previous.total)),
    ),
  );
}
module.exports = { HIDDEN, redact, validFormat, cpuTotals, cpuPercent };
