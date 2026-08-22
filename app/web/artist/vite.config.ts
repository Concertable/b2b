import path from "path"
import tailwindcss from "@tailwindcss/vite"
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'
import { tanstackRouter } from '@tanstack/router-vite-plugin'
import { aspNetDevelopmentHttps } from '../../../scripts/vite-development-https'

export default defineConfig(({ command, mode }) => {
  const env = loadEnv(mode, path.resolve(__dirname, '../../'), 'VITE_')
  return {
    plugins: [tanstackRouter(), react(), tailwindcss()],
    server: {
      host: '127.0.0.1',
      https: command === 'serve'
        ? aspNetDevelopmentHttps(path.resolve(__dirname, '../../../node_modules/.vite/aspnet-https/artist'))
        : undefined,
      port: 5176,
    },
    envDir: '../../',
    define: command === 'build'
      ? {
          'import.meta.env.VITE_OIDC_CLIENT_ID': JSON.stringify('artist-web'),
          'import.meta.env.VITE_OIDC_SCOPE': JSON.stringify('openid profile concertable.b2b.api offline_access'),
          'import.meta.env.VITE_API_URL': JSON.stringify(env.VITE_B2B_API_URL),
          'import.meta.env.VITE_BASE_URL': JSON.stringify(env.VITE_B2B_API_URL.replace(/\/api\/?$/, '')),
          // Payout calls go through B2B's own backend (tenant-scoped StripeAccount proxy), not Payment.
          'import.meta.env.VITE_PAYMENT_API_URL': JSON.stringify(env.VITE_B2B_API_URL),
        }
      : {
          'import.meta.env.VITE_AUTH_AUTHORITY': JSON.stringify('https://localhost:7083'),
          'import.meta.env.VITE_OIDC_CLIENT_ID': JSON.stringify('artist-web'),
          'import.meta.env.VITE_OIDC_SCOPE': JSON.stringify('openid profile concertable.b2b.api offline_access'),
          'import.meta.env.VITE_API_URL': JSON.stringify('https://localhost:7086/api'),
          'import.meta.env.VITE_BASE_URL': JSON.stringify('https://localhost:7086'),
          // Payout calls go through B2B's own backend (the tenant-scoped StripeAccount proxy), not the Payment
          // host — B2B resolves the owner as the active tenant. Customer still points straight at Payment (7088).
          'import.meta.env.VITE_PAYMENT_API_URL': JSON.stringify('https://localhost:7086/api'),
        },
    resolve: {
      alias: [
        { find: "@", replacement: path.resolve(__dirname, "./src") },
      ],
    },
  }
})
