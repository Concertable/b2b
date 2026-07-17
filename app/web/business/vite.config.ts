import path from "path";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import basicSsl from "@vitejs/plugin-basic-ssl";
import { defineConfig, loadEnv } from "vite";

export default defineConfig(({ command, mode }) => {
  const env = loadEnv(mode, path.resolve(__dirname, "../../"), "VITE_");
  return {
    plugins: [react(), tailwindcss(), basicSsl()],
    server: {
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
        { find: /^@\/(components|features|hooks|lib|providers|context|types|assets)(\/.*)?$/, replacement: path.resolve(__dirname, "../../shared/src/$1$2") },
        { find: /^shared\/(.*)$/, replacement: path.resolve(__dirname, "../../shared/src/$1") },
        { find: "@", replacement: path.resolve(__dirname, "./src") },
      ],
    },
  };
});
