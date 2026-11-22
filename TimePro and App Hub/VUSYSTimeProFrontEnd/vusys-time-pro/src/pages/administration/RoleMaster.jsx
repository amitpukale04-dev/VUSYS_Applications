import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { InlineEditableTable } from "../../components/common/InlineEditable";
import ConfirmModal from "../../components/common/ConfirmModal";
import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import {
  getRoleList,
  saveAndUpdateRoleDetails,
} from "../../services/roleService";
import ToastService from "../../services/toastService";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import {
  applyApiValidationErrors,
  clearRowValidationError,
} from "../../utils/CustomAPIValidation";

const ROLE_MASTER_API_FIELD_MAPPINGS = {
  roleName: { apiKeys: ["RoleName"] },
};

export default function RoleMaster() {
  const userId = normalizeNumber(getCreatedOrUpdatedBy());
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenRoleList = hasPermission("Role Master", "Open");
  const canCreateRole = hasPermission("Role Master", "Create");
  const canEditRole = hasPermission("Role Master", "Edit");

  const [roles, setRoles] = useState([]);
  const initialRoleListLoadRef = useRef(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [rowValidationErrors, setRowValidationErrors] = useState({});

  const handleCellChange = (_row, fieldName) => {
    clearRowValidationError(setRowValidationErrors, fieldName);
  };

  // ==========================================================
  // LOAD ROLE LIST
  // ==========================================================

  const loadRoles = useCallback(async ({ showMessage = false } = {}) => {
    setLoading(true);

    try {
      const result = await getRoleList();
      setRoles(result.roles || []);

      if (showMessage) {
        ToastService.success("Role List refreshed successfully.");
      }
    } catch (error) {
      setRoles([]);

      /*
       * API ValidationErrors, Network/CORS and GLOBAL_ERROR_RESPONSE are
       * handled globally by axiosSetup.js.
       */
      showApiError(error, "Unable to load Role List.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (securityAccessLoading || !canOpenRoleList) {
      return;
    }

    /*
     * React StrictMode intentionally re-runs Effects in development.
     * Guard only the initial Role List binding so GetRoleList is not
     * requested twice. Manual Refresh and post-save reloads continue
     * to call loadRoles() normally.
     */
    if (initialRoleListLoadRef.current) {
      return;
    }

    initialRoleListLoadRef.current = true;
    loadRoles();
  }, [canOpenRoleList, loadRoles, securityAccessLoading]);

  // ==========================================================
  // GRID COLUMNS
  // ==========================================================

  const columns = useMemo(
    () => [
      {
        key: "roleName",
        label: "Role Name",
        width: "320px",
        inputType: "text",
        validate: (value) =>
          !String(value || "").trim() ? "Enter The Role Name." : "",
      },
      {
        key: "isActive",
        label: "Active",
        width: "120px",
        inputType: "checkbox",
        filterType: "checkbox",
      },
    ],
    [],
  );

  // ==========================================================
  // NEW ROLE ROW
  // ==========================================================

  const createRoleRow = () => ({
    /*
     * Temporary negative key is used only by the client grid so it cannot
     * collide with database Role IDs. The API receives id = 0 for inserts.
     */
    roleId: -Date.now(),
    roleName: "",
    isDeleted: false,
    activeStatus: "Active",
    isActive: true,
  });

  // ==========================================================
  // SAVE / UPDATE ROLE
  // ==========================================================

  const saveRoleRow = async (row, { isNew }) => {
    if (saving) return;

    const loggedInUserId = userId;
    setRowValidationErrors({});
    setSaving(true);

    try {
      const result = await saveAndUpdateRoleDetails({
        id: isNew ? 0 : row.roleId,
        roleName: row.roleName,
        isDeleted: !row.isActive,
        createdBy: isNew ? loggedInUserId : 0,
        updatedBy: isNew ? 0 : loggedInUserId,
      });

      ToastService.success(
        result?.message ||
          (isNew
            ? "New Role Added Successfully."
            : "Role Updated Successfully."),
      );

      /* Always rebind from the API after persistence. */
      await loadRoles();
      return true;
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          ROLE_MASTER_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            setRowValidationErrors((current) => ({
              ...current,
              [fieldName]: message,
            }));
          },
        );
      } else {
        showApiError(error, "Unable to save Role Details.");
      }

      // Preserve the draft row so the user can correct the API validation error.
      return false;
    } finally {
      setSaving(false);
    }
  };

  // ==========================================================
  // DELETE ROLE
  // ==========================================================

  const confirmDelete = async () => {
    if (!deleteRow || deleting) return;

    const loggedInUserId = userId;
    setDeleting(true);

    try {
      const result = await saveAndUpdateRoleDetails({
        id: deleteRow.roleId,
        roleName: deleteRow.roleName,
        isDeleted: true,
        createdBy: 0,
        updatedBy: loggedInUserId,
      });

      ToastService.success("Role Details Deleted Successfully.");
      setDeleteRow(null);
      await loadRoles();
    } catch (error) {
      showApiError(error, "Unable to delete Role Details.");
    } finally {
      setDeleting(false);
    }
  };

  // ==========================================================
  // SECURITY ACCESS LOADING / DENIED
  // ==========================================================

  if (securityAccessLoading) {
    return (
      <div className="tp-form-page-loading">
        <span className="spinner-border text-primary" aria-hidden="true" />
        <p>Loading security permissions...</p>
      </div>
    );
  }

  if (!canOpenRoleList) {
    return <AccessDenied />;
  }

  // ==========================================================
  // UI
  // ==========================================================

  return (
    <div>
      <div className="tp-card tp-grid-card tp-grid-card-modern">
        <div className="tp-grid-card-body">
          <InlineEditableTable
            columns={columns}
            data={roles}
            keyField="roleId"
            onDataChange={setRoles}
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={2}
            defaultPageSize={30}
            maxHeight={390}
            minTableHeight={235}
            showActions={canCreateRole || canEditRole}
            actionColumnWidth="50px"
            showEditAction={canEditRole}
            showDeleteAction={canEditRole}
            showRowMore={false}
            showAddAction={canCreateRole}
            addActionLabel="Add Role"
            addActionIconOnly
            newRowFactory={createRoleRow}
            onRowSave={saveRoleRow}
            rowValidationErrors={rowValidationErrors}
            onCellChange={handleCellChange}
            onRowCancel={() => setRowValidationErrors({})}
            showRefreshAction
            onRefresh={() => loadRoles({ showMessage: false })}
            refreshing={loading}
            loading={loading}
            loadingText="Loading roles..."
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete Role"
        message={`Are you sure you want to delete ${deleteRow?.roleName || "this role"}?`}
        confirmText={deleting ? "Deleting..." : "Yes, Delete"}
        onConfirm={confirmDelete}
        onCancel={() => {
          if (!deleting) setDeleteRow(null);
        }}
      />
    </div>
  );
}
