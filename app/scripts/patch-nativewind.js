// NativeWind resolves `tailwindcss` to v4 (hoisted from web) but only supports v3.
// This re-shims a v3 copy into nativewind's own node_modules every time it runs.
// Runs via root postinstall AND mobile postinstall so any npm install path triggers it.
const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const src = path.join(root, "mobile", "node_modules", "tailwindcss");
const dest = path.join(root, "node_modules", "nativewind", "node_modules", "tailwindcss");

if (!fs.existsSync(src)) {
  console.warn("patch-nativewind: mobile tailwindcss not found, skipping");
  process.exit(0);
}

fs.mkdirSync(path.dirname(dest), { recursive: true });
if (fs.existsSync(dest)) fs.rmSync(dest, { recursive: true, force: true });
fs.symlinkSync(src, dest, "junction");
console.log("patched: nativewind -> tailwindcss@3");
