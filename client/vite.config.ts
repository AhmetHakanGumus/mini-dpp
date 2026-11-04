import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

const api = "http://localhost:5031";

export default defineConfig({
  plugins: [react()],
  build: {
    // Derlenen arayüzü API sunar; böylece uygulama tek "dotnet run" ile açılır.
    outDir: "../src/MiniDpp.Api/wwwroot",
    emptyOutDir: true,
  },
  server: {
    proxy: {
      "/api": api,
      "/dpp": api,
    },
  },
});
