import path from "path";
import { defineConfig } from "vitest/config";

export default defineConfig({
  resolve: {
    alias: [
      {
        find: /^@\/(components|features|hooks|lib|providers|context|types|assets)(\/.*)?$/,
        replacement: path.resolve(__dirname, "../../shared/src/$1$2"),
      },
      {
        find: /^shared\/(.*)$/,
        replacement: path.resolve(__dirname, "../../shared/src/$1"),
      },
      {
        find: /^@b2b\/(.*)$/,
        replacement: path.resolve(__dirname, "../shared/src/$1"),
      },
      { find: "@", replacement: path.resolve(__dirname, "./src") },
    ],
  },
  test: {
    environment: "node",
    include: [
      "../shared/src/features/tenant/**/*.test.ts",
      "../shared/src/features/members/**/*.test.ts",
    ],
  },
});
