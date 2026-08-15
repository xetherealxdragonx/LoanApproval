import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The dev server proxies /api to the .NET API. Going through the proxy rather
// than calling https://localhost:54744 directly from the browser keeps every
// request same-origin, so the API needs no CORS policy for local development.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        // Matches applicationUrl in src/LoanApproval.Api/Properties/launchSettings.json
        target: 'https://localhost:54744',
        changeOrigin: true,
        // The ASP.NET Core developer certificate is self-signed, so Node would
        // otherwise reject it. Only ever appropriate for local development.
        secure: false,
      },
    },
  },
})
