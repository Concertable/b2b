import { Button } from "@concertable/web/components/ui/button";
import { Checkbox } from "@concertable/web/components/ui/checkbox";
import { Input } from "@concertable/web/components/ui/input";
import { Label } from "@concertable/web/components/ui/label";
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@concertable/web/components/ui/select";
import { useRoleManagement } from "../hooks/useRoleManagement";
import type { ResourceAudience } from "../types";
import { Spinner } from "./Spinner";

function audienceLabel(audience: ResourceAudience) {
  return audience === "TenantResources" ? "All tenant resources" : "Assigned resources";
}

export function RoleManagement() {
  const {
    roles, metadata, editablePermissions, isLoading, isLoadError,
    selectedId, selected, draft, editable, showEditor, nameError, replacementRoleId,
    isSaving, canSave, canRetire, saveError, retireError,
    openNew, openRole, clone, startEditing, setName, setInvitationAssignable,
    togglePermission, changeAudience, save, setReplacementRoleId, retire,
  } = useRoleManagement();

  if (isLoading) return <Spinner />;

  return (
    <section className="space-y-4" data-testid="role-management">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="font-medium">Roles</h3>
          <p className="text-muted-foreground text-sm">Create permission bundles and assign them to members.</p>
        </div>
        <Button variant="outline" onClick={openNew}>New role</Button>
      </div>
      {isLoadError ? (
        <p role="alert" className="text-destructive text-sm">Could not load roles and permissions.</p>
      ) : (
        <>
          <div className="flex flex-wrap gap-2">
            {roles.map((role) => (
              <Button key={role.id} type="button" size="sm" variant={selectedId === role.id ? "default" : "outline"} onClick={() => openRole(role)}>
                {role.name}
              </Button>
            ))}
          </div>
          {selected?.isProtectedOwner && <p className="text-muted-foreground text-sm">Owner is protected and cannot be changed or retired.</p>}
          {selected?.isSystemPreset && !selected.isProtectedOwner && (
            <p className="text-muted-foreground text-sm">This system preset is read only. Clone it to create an editable role.</p>
          )}
          {selected && (
            <div className="flex gap-2">
              {selected.isSystemPreset ? (
                <Button type="button" variant="outline" onClick={() => clone(selected)}>Clone role</Button>
              ) : (
                <Button type="button" variant="outline" onClick={() => startEditing(selected)}>Edit role</Button>
              )}
            </div>
          )}
          {editable ? (
            <div className="space-y-4 rounded-md border p-4">
              <div className="space-y-1">
                <Label htmlFor="role-name">Role name</Label>
                <Input id="role-name" value={draft.name} onChange={(event) => setName(event.target.value)} aria-invalid={nameError !== undefined} />
                {nameError && <p className="text-destructive text-xs">{nameError}</p>}
              </div>
              <div className="flex items-center gap-2">
                <Checkbox id="role-invitable" checked={draft.isInvitationAssignable} onCheckedChange={(checked) => setInvitationAssignable(checked === true)} />
                <Label htmlFor="role-invitable">Can be assigned through invitations</Label>
              </div>
              <div className="space-y-3">
                <p className="text-sm font-medium">Permissions</p>
                {editablePermissions.map((item) => {
                  const selectedPermission = draft.permissions.find((entry) => entry.permission === item.permission);
                  return (
                    <div key={item.permission} className="flex flex-wrap items-center gap-3 rounded-md border p-2">
                      <div className="flex min-w-48 items-center gap-2">
                        <Checkbox id={"permission-" + item.permission} checked={selectedPermission !== undefined} disabled={item.assignableAudiences.length === 0} onCheckedChange={(checked) => togglePermission(item, checked === true)} />
                        <Label htmlFor={"permission-" + item.permission}>{item.label}</Label>
                      </div>
                      <span className="text-muted-foreground text-xs">{item.category}</span>
                      {selectedPermission && (
                        <Select value={selectedPermission.audience} onValueChange={(audience) => changeAudience(item, audience as ResourceAudience)}>
                          <SelectTrigger className="w-44" aria-label={item.label + " audience"}><SelectValue /></SelectTrigger>
                          <SelectContent>{item.assignableAudiences.map((audience) => <SelectItem key={audience} value={audience}>{audienceLabel(audience)}</SelectItem>)}</SelectContent>
                        </Select>
                      )}
                    </div>
                  );
                })}
              </div>
              <Button type="button" onClick={save} disabled={!canSave || isSaving}>Save role</Button>
              {saveError && <p role="alert" className="text-destructive text-sm">We could not save the role. Please try again.</p>}
            </div>
          ) : selected && (
            <div className="space-y-2 rounded-md border p-4">
              <p className="font-medium">{selected.name}</p>
              <p className="text-muted-foreground text-sm">{selected.isInvitationAssignable ? "Invitation assignable" : "Not invitation assignable"}</p>
              <ul className="text-sm">{selected.permissions.map((item) => <li key={item.permission}>{metadata.find((entry) => entry.permission === item.permission)?.label ?? item.permission}: {audienceLabel(item.audience)}</li>)}</ul>
            </div>
          )}
          {selected && !selected.isSystemPreset && !showEditor && (
            <div className="space-y-2 rounded-md border p-4">
              <p className="text-sm font-medium">Retire role</p>
              <p className="text-muted-foreground text-xs">Assigned members receive the replacement role. Pending invitations using this role are revoked.</p>
              <Select value={replacementRoleId} onValueChange={setReplacementRoleId}>
                <SelectTrigger aria-label="Replacement role" className="w-64"><SelectValue placeholder="Choose replacement if members use this role" /></SelectTrigger>
                <SelectContent>{roles.filter((role) => role.id !== selected.id).map((role) => <SelectItem key={role.id} value={role.id}>{role.name}</SelectItem>)}</SelectContent>
              </Select>
              <Button type="button" variant="destructive" disabled={!canRetire} onClick={retire}>Retire role</Button>
              {retireError && <p role="alert" className="text-destructive text-sm">We could not retire the role. If members use it, choose a replacement and try again.</p>}
            </div>
          )}
        </>
      )}
    </section>
  );
}
