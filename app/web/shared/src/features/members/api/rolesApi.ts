import { apiClient } from "@concertable/shared/lib/apiClient";
import type { Role, PermissionMetadata, RoleRequest, UpdateRoleRequest, RetireRoleRequest } from "../types";

const BASE = "/organization";

const rolesApi = {
  list: async (): Promise<Role[]> => {
    const { data } = await apiClient.get<Role[]>(BASE + "/roles");
    return data;
  },
  permissions: async (): Promise<PermissionMetadata[]> => {
    const { data } = await apiClient.get<PermissionMetadata[]>(BASE + "/permissions");
    return data;
  },
  create: async (request: RoleRequest): Promise<Role> => {
    const { data } = await apiClient.post<Role>(BASE + "/roles", request);
    return data;
  },
  update: async (id: string, request: UpdateRoleRequest): Promise<Role> => {
    const { data } = await apiClient.put<Role>(BASE + "/roles/" + id, request);
    return data;
  },
  retire: async (id: string, request: RetireRoleRequest): Promise<void> => {
    await apiClient.post(BASE + "/roles/" + id + "/retire", request);
  },
};

export default rolesApi;
