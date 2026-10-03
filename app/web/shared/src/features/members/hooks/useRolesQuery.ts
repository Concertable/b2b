import { useQuery } from "@tanstack/react-query";
import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";
import rolesApi from "../api/rolesApi";

export const rolesQueryKey = () => currentPrivateQueryKey("roles");
export const permissionsQueryKey = () => currentPrivateQueryKey("permissions");

export function useRolesQuery(enabled = true) {
  return useQuery({ queryKey: rolesQueryKey(), queryFn: rolesApi.list, enabled });
}

export function usePermissionsQuery(enabled = true) {
  return useQuery({ queryKey: permissionsQueryKey(), queryFn: rolesApi.permissions, enabled });
}
