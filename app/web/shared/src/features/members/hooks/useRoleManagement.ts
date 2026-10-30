import { useState } from "react";
import { toast } from "sonner";
import { usePermissionsQuery, useRolesQuery } from "./useRolesQuery";
import { useRoleMutations } from "./useRoleMutations";
import { roleRequestSchema, retireRoleRequestSchema, updateRoleRequestSchema } from "../schemas/roleRequestSchema";
import type { PermissionMetadata, ResourceAudience, Role, RolePermission } from "../types";

interface Draft {
  name: string;
  isInvitationAssignable: boolean;
  permissions: RolePermission[];
}

const emptyDraft = (): Draft => ({ name: "", isInvitationAssignable: false, permissions: [] });

function draftFromRole(role: Role, metadata: ReadonlyArray<PermissionMetadata>): Draft {
  const editable = new Set(metadata.filter((item) => !item.ownerOnly).map((item) => item.permission));
  return {
    name: role.isSystemPreset ? role.name + " copy" : role.name,
    isInvitationAssignable: role.isInvitationAssignable,
    permissions: role.permissions.filter((item) => editable.has(item.permission)),
  };
}

export function useRoleManagement() {
  const rolesQuery = useRolesQuery();
  const permissionsQuery = usePermissionsQuery();
  const { create, update, retire: retireMutation } = useRoleMutations();
  const [selection, setSelection] = useState<Pick<Role, "id" | "version"> | undefined>();
  const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [editTarget, setEditTarget] = useState<{ id: string; version: number } | undefined>();
  const [replacementRoleId, setReplacementRoleId] = useState("");
  const [showEditor, setShowEditor] = useState(false);
  const roles = rolesQuery.data ?? [];
  const metadata = permissionsQuery.data ?? [];
  const selectedId = selection?.id;
  const selected = roles.find((role) => role.id === selectedId);
  const editable = showEditor && (selected === undefined || !selected.isSystemPreset);
  const parsed = roleRequestSchema.safeParse(draft);
  const nameError = parsed.success ? undefined : parsed.error.issues.find((issue) => issue.path[0] === "name")?.message;
  const retirement = selection && selected && !selected.isSystemPreset
    ? retireRoleRequestSchema.safeParse({
        expectedVersion: selection.version,
        replacementRoleId: replacementRoleId || undefined,
      })
    : undefined;

  const resetMutations = () => {
    create.reset();
    update.reset();
    retireMutation.reset();
  };
  const openNew = () => {
    resetMutations();
    setSelection(undefined);
    setDraft(emptyDraft());
    setEditTarget(undefined);
    setReplacementRoleId("");
    setShowEditor(true);
  };
  const openRole = (role: Role) => {
    resetMutations();
    setSelection({ id: role.id, version: role.version });
    setDraft(draftFromRole(role, metadata));
    setEditTarget(undefined);
    setReplacementRoleId("");
    setShowEditor(false);
  };
  const clone = (role: Role) => {
    resetMutations();
    setSelection(undefined);
    setDraft(draftFromRole(role, metadata));
    setEditTarget(undefined);
    setReplacementRoleId("");
    setShowEditor(true);
  };
  const startEditing = (role: Role) => {
    resetMutations();
    setDraft(draftFromRole(role, metadata));
    setEditTarget({ id: role.id, version: role.version });
    setShowEditor(true);
  };
  const setName = (name: string) => setDraft((current) => ({ ...current, name }));
  const setInvitationAssignable = (value: boolean) =>
    setDraft((current) => ({ ...current, isInvitationAssignable: value }));
  const togglePermission = (item: PermissionMetadata, checked: boolean) => {
    const audience = item.assignableAudiences[0];
    setDraft((current) => ({
      ...current,
      permissions: checked && audience
        ? [...current.permissions.filter((entry) => entry.permission !== item.permission), { permission: item.permission, audience }]
        : current.permissions.filter((entry) => entry.permission !== item.permission),
    }));
  };
  const changeAudience = (item: PermissionMetadata, audience: ResourceAudience) =>
    setDraft((current) => ({
      ...current,
      permissions: current.permissions.map((entry) =>
        entry.permission === item.permission ? { ...entry, audience } : entry),
    }));
  const save = () => {
    if (!parsed.success) return;
    if (editTarget) {
      const request = updateRoleRequestSchema.safeParse({ ...parsed.data, expectedVersion: editTarget.version });
      if (!request.success) return;
      update.mutate({ id: editTarget.id, request: request.data }, {
        onSuccess: (role) => { toast.success("Role updated"); openRole(role); },
      });
    } else {
      create.mutate(parsed.data, {
        onSuccess: (role) => { toast.success("Role created"); openRole(role); },
      });
    }
  };
  const retire = () => {
    if (!selected || !retirement?.success) return;
    retireMutation.mutate({ id: selected.id, request: retirement.data }, {
      onSuccess: () => { toast.success("Role retired"); openNew(); },
    });
  };

  return {
    roles, metadata,
    editablePermissions: metadata.filter((item) => !item.ownerOnly),
    isLoading: rolesQuery.isLoading || permissionsQuery.isLoading,
    isLoadError: rolesQuery.isError || permissionsQuery.isError,
    selectedId, selected, draft, editable, showEditor, nameError, replacementRoleId,
    isSaving: create.isPending || update.isPending,
    canSave: parsed.success && !create.isPending && !update.isPending,
    canRetire: retirement?.success === true && !retireMutation.isPending,
    saveError: create.isError || update.isError,
    retireError: retireMutation.isError,
    openNew, openRole, clone, startEditing, setName, setInvitationAssignable,
    togglePermission, changeAudience, save, setReplacementRoleId, retire,
  };
}
