const { test } = require("node:test");
const assert = require("node:assert/strict");
const {
  redact,
  HIDDEN,
  validFormat,
  cpuPercent,
} = require("../electron/domain.cjs");
test("Nomor seri tidak menyeberang ke renderer dan redaksi tidak mengubah snapshot asli", () => {
  const input = {
    Categories: [
      {
        Devices: [
          {
            Fields: [
              { Label: "Serial", Value: "private-id", Sensitive: true },
              { Label: "Model", Value: "CPU", Sensitive: false },
            ],
          },
        ],
      },
    ],
  };
  const output = redact(input);
  assert.equal(output.Categories[0].Devices[0].Fields[0].Value, HIDDEN);
  assert.equal(input.Categories[0].Devices[0].Fields[0].Value, "private-id");
  assert.equal(
    redact(input, true).Categories[0].Devices[0].Fields[0].Value,
    "private-id",
  );
  assert.equal(output.Categories[0].Devices[0].Fields[1].Value, "CPU");
});
test("Format ekspor tidak menerima path atau perintah", () => {
  for (const invalid of [
    "exe",
    "../../file",
    "html && whoami",
    null,
    {},
    "HTML",
  ])
    assert.throws(() => validFormat(invalid));
  assert.equal(validFormat("html"), "html");
});
test("Pemakaian CPU hanya dihitung dari dua sampel yang valid", () => {
  assert.equal(cpuPercent(null, { total: 50, idle: 20 }), null);
  assert.equal(
    cpuPercent({ total: 10, idle: 10 }, { total: 10, idle: 10 }),
    null,
  );
  assert.equal(
    cpuPercent({ total: 100, idle: 50 }, { total: 200, idle: 100 }),
    50,
  );
});
