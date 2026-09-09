import { createContext, useContext, type ReactNode } from "react";

const ActiveTenantContext = createContext<string | undefined>(undefined);

export function ActiveTenantProvider({
  tenantId,
  children,
}: Readonly<{ tenantId: string; children: ReactNode }>) {
  return (
    <ActiveTenantContext.Provider value={tenantId}>
      {children}
    </ActiveTenantContext.Provider>
  );
}

export function useActiveTenantId() {
  return useContext(ActiveTenantContext);
}
