import path from "path"
import tailwindcss from "@tailwindcss/vite"
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import { tanstackRouter } from '@tanstack/router-vite-plugin'
import basicSsl from '@vitejs/plugin-basic-ssl'

export default defineConfig({
  plugins: [tanstackRouter(), react(), tailwindcss(), basicSsl()],
  server: {
    port: 5176,
  },
  envDir: '../../',
  define: {
    'import.meta.env.VITE_OIDC_CLIENT_ID': JSON.stringify('artist-web'),
    'import.meta.env.VITE_OIDC_SCOPE': JSON.stringify('openid profile roles concertable.b2b.api offline_access'),
    'import.meta.env.VITE_API_URL': JSON.stringify('https://localhost:7086/api'),
    'import.meta.env.VITE_BASE_URL': JSON.stringify('https://localhost:7086'),
  },
  resolve: {
    alias: [
      { find: /^@\/(components|features|hooks|lib|providers|context|types|assets)(\/.*)?$/, replacement: path.resolve(__dirname, "../../shared/src/$1$2") },
      { find: /^shared\/(.*)$/, replacement: path.resolve(__dirname, "../../shared/src/$1") },
      { find: /^@b2b\/(.*)$/, replacement: path.resolve(__dirname, "../shared/src/$1") },
      { find: "@", replacement: path.resolve(__dirname, "./src") },
    ],
  },
})
