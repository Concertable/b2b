// NativeWind (at workspace root) resolves `tailwindcss` to v4 (hoisted by web),
// but only supports v3. This shims a v3 copy into nativewind's own node_modules
// so its version check passes. Must run after every `npm install`.
const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const src = path.join(root, "mobile", "node_modules", "tailwindcss");
const dest = path.join(root, "node_modules", "nativewind", "node_modules", "tailwindcss");

if (fs.existsSync(dest)) return;

fs.mkdirSync(path.dirname(dest), { recursive: true });
fs.symlinkSync(src, dest, "junction");
console.log("patched: nativewind -> tailwindcss@3");
