import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const source = await readFile(new URL("../../OrderHub.Web/wwwroot/js/order-local-time.js", import.meta.url), "utf8");
const { formatLocalTime, localDayBounds } = await import("data:text/javascript;base64," + Buffer.from(source).toString("base64"));
process.env.TZ = "America/New_York";
assert.match(formatLocalTime("2026-01-15T12:00:00Z", "en-US"), /7:00:00 AM/);
assert.match(formatLocalTime("2026-07-15T12:00:00Z", "en-US"), /8:00:00 AM/);
assert.equal(formatLocalTime("invalid", "en-US"), "التاريخ غير متاح");
const spring = localDayBounds(2026, 3, 8);
const fall = localDayBounds(2026, 11, 1);
assert.equal((new Date(spring.toUtcExclusive) - new Date(spring.fromUtc)) / 3600000, 23);
assert.equal((new Date(fall.toUtcExclusive) - new Date(fall.fromUtc)) / 3600000, 25);
console.log("Passed: browser-local formatting, invalid dates, and 23/25-hour DST boundaries.");
