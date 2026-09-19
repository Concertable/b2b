import { useEffect } from "react";
import { createFileRoute } from "@tanstack/react-router";

function RegisterRedirect() {
  useEffect(() => {
    window.location.href = `${import.meta.env.VITE_AUTH_AUTHORITY}/Account/Register`;
  }, []);
  return null;
}

export const Route = createFileRoute("/register")({ component: RegisterRedirect });
