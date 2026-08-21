import path from "path";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig, loadEnv } from "vite";
import { aspNetDevelopmentHttps } from "../../../scripts/vite-development-https";

export default defineConfig(({ command, mode }) => {
  const env = loadEnv(mode, path.resolve(__dirname, "../../"), "VITE_");
  return {
    plugins: [react(), tailwindcss()],
    server: {
      host: "127.0.0.1",
      https: command === "serve"
        ? aspNetDevelopmentHttps(path.resolve(__dirname, "../../../node_modules/.vite/aspnet-https/business"))
        : undefined,
      port: 5177,
      hmr: false,
    },
    envDir: "../../",
    define: command === "build"
      ? {
          'import.meta.env.VITE_API_URL': JSON.stringify(env.VITE_B2B_API_URL),
          'import.meta.env.VITE_BASE_URL': JSON.stringify(env.VITE_B2B_API_URL.replace(/\/api\/?$/, '')),
        }
      : {
          'import.meta.env.VITE_API_URL': JSON.stringify('https://localhost:7086/api'),
          'import.meta.env.VITE_BASE_URL': JSON.stringify('https://localhost:7086'),
        },
    resolve: {
      alias: [
        { find: "@", replacement: path.resolve(__dirname, "./src") },
      ],
    },
  };
});
