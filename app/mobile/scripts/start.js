const { spawn } = require("child_process");
const fs = require("fs");
const path = require("path");
const qrcode = require("qrcode-terminal");

const authUrl = process.env["services__auth__https__0"];
const apiUrl = process.env["services__b2b_web__https__0"];
if (authUrl) process.env["EXPO_PUBLIC_AUTH_AUTHORITY"] = authUrl;
if (apiUrl) process.env["EXPO_PUBLIC_API_URL"] = apiUrl;
process.env["EXPO_PUBLIC_OIDC_CLIENT_ID"] = "venue-mobile";
process.env["EXPO_PUBLIC_OIDC_CLIENT_ID_ARTIST"] = "artist-mobile";
process.env["EXPO_PUBLIC_URL_SCHEME"] = "concertable-business";

const host = process.env.REACT_NATIVE_PACKAGER_HOSTNAME || "localhost";
const port = 8083;
const url = `exp://${host}:${port}`;

console.log("\n=== Scan with Expo Go (business) ===");
qrcode.generate(url, { small: true });
console.log(`URL: ${url}\n`);

const clearFlag = path.join(__dirname, "..", ".metro-clear");
const shouldClear = fs.existsSync(clearFlag);
if (shouldClear) fs.unlinkSync(clearFlag);

const args = ["expo", "start", "--port", String(port), "--host", "lan"];
if (shouldClear) args.push("--clear");

const expo = spawn("npx", args, {
  stdio: "inherit",
  shell: true,
  env: { ...process.env, EXPO_OFFLINE: "1" },
});

const forward = (sig) => () => { try { expo.kill(sig); } catch { /* ignore */ } };
process.on("SIGINT", forward("SIGINT"));
process.on("SIGTERM", forward("SIGTERM"));
process.on("SIGHUP", forward("SIGTERM"));

expo.on("close", (code) => process.exit(code ?? 0));
