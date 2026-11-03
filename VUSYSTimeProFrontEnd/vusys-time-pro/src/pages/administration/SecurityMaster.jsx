import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { InlineEditableTable } from "../../components/common/InlineEditable";
import ConfirmModal from "../../components/common/ConfirmModal";
import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import {
  getSecurityList,
  saveUpdateSecurityDetails,
} from "../../services/securityMasterService";
import { getDropdownItems } from "../../services/dropdownService";
import ToastService from "../../services/toastService";
import { DROPDOWN_SOURCE_TABLE } from "../../constants/constants";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import {
  applyApiValidationErrors,
  clearRowValidationError,
} from "../../utils/CustomAPIValidation";

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

const SECURITY_MASTER_API_FIELD_MAPPINGS = {
  securityName: { apiKeys: ["SecurityName"] },
  roleId: { apiKeys: ["RoleID"] },
  screenName: { apiKeys: ["ScreenName"] },
};

export default function SecurityMaster() {
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenSecurityList = hasPermission("Security Master", "Open");
  const canCreateSecurity = hasPermission("Security Master", "Create");
  const canEditSecurity = hasPermission("Security Master", "Edit");

  const [securities, setSecurities] = useState([]);
  const [roleOptions, setRoleOptions] = useState([]);
  const initialSecurityMasterLoadRef = useRef(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [rowValidationErrors, setRowValidationErrors] = useState({});

  const handleCellChange = (_row, fieldName) => {
    clearRowValidationError(setRowValidationErrors, fieldName);
  };

  // ==========================================================
  // LOAD ROLE DROPDOWN
  // ==========================================================

  const loadRoleOptions = useCallback(async () => {
    try {
      const result = await getDropdownItems({
        sourceTable: DROPDOWN_SOURCE_TABLE.ROLE,
        argumentValue: "",
        argumentValue1: "",
      });

      setRoleOptions(result.options || []);
    } catch (error) {
      setRoleOptions([]);
      showApiError(error, "Unable to load Role List.");
    }
  }, []);

  // ==========================================================
  // LOAD SECURITY LIST
  // ==========================================================

  const loadSecurities = useCallback(async ({ showMessage = false } = {}) => {
    setLoading(true);

    try {
      const result = await getSecurityList();
      setSecurities(result.securities || []);

      if (showMessage) {
        ToastService.success("Security Master List refreshed successfully.");
      }
    } catch (error) {
      setSecurities([]);
      showApiError(error, "Unable to load Security Master List.");
    } finally {
      setLoading(false);
    }
  }, []);

  // ==========================================================
  // INITIAL BINDING
  // ==========================================================

  useEffect(() => {
    if (securityAccessLoading || !canOpenSecurityList) {
      return;
    }

    /*
     * React StrictMode intentionally re-runs Effects in development.
     * Guard only the initial Security Master binding. Manual Refresh and
     * post-save reloads continue to work normally.
     */
    if (initialSecurityMasterLoadRef.current) {
      return;
    }

    initialSecurityMasterLoadRef.current = true;
    loadRoleOptions();
    loadSecurities();
  }, [
    canOpenSecurityList,
    loadRoleOptions,
    loadSecurities,
    securityAccessLoading,
  ]);

  // ==========================================================
  // GRID COLUMNS
  // ==========================================================

  const columns = useMemo(
    () => [
      {
        key: "securityName",
        label: "Security Name",
        width: "190px",
        inputType: "text",
        validate: (value) =>
          !String(value || "").trim() ? "Enter The Security Name." : "",
      },
      {
        key: "roleId",
        label: "Role",
        width: "100px",
        inputType: "select",
        options: roleOptions,
        filterType: "select",
        filterOptions: roleOptions,
        validate: (value) =>
          !String(value || "").trim() ? "Select The Role." : "",
      },
      {
        key: "screenName",
        label: "Screen Name",
        width: "150px",
        inputType: "text",
        validate: (value) =>
          !String(value || "").trim() ? "Enter The Screen Name." : "",
      },
      {
        key: "isDefault",
        label: "Default",
        width: "120px",
        inputType: "checkbox",
        filterType: "checkbox",
      },
      {
        key: "isActive",
        label: "Active",
        width: "110px",
        inputType: "checkbox",
        filterType: "checkbox",
      },
    ],
    [roleOptions],
  );

  // ==========================================================
  // NEW SECURITY ROW
  // ==========================================================

  const createSecurityRow = () => ({
    /* Temporary negative key is used only by the client grid. */
    securityId: -Date.now(),
    securityName: "",
    roleId: "",
    screenName: "",
    isDefault: false,
    isDeleted: false,
    isActive: true,
  });

  // ==========================================================
  // SAVE / UPDATE SECURITY
  // ==========================================================

  const saveSecurityRow = async (row, { isNew }) => {
    if (saving) return;
    setRowValidationErrors({});
    setSaving(true);

    try {
      const result = await saveUpdateSecurityDetails({
        securityId: isNew ? 0 : row.securityId,
        securityName: row.securityName,
        roleID: row.roleId,
        screenName: row.screenName,
        isDefault: row.isDefault,
        isDeleted: !row.isActive,
        createdBy: isNew ? loggedInUserId : 0,
        updatedBy: isNew ? 0 : loggedInUserId,
      });

      ToastService.success(
        result?.message ||
          (isNew
            ? "New Security Master Added Successfully."
            : "Security Master Updated Successfully."),
      );

      await loadSecurities();
      return true;
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          SECURITY_MASTER_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            setRowValidationErrors((current) => ({
              ...current,
              [fieldName]: message,
            }));
          },
        );
      } else {
        showApiError(error, "Unable to save Security Master Details.");
      }

      // Keep the unsaved row in edit mode so validation errors can be corrected.
      return false;
    } finally {
      setSaving(false);
    }
  };

  // ==========================================================
  // DELETE SECURITY
  // ==========================================================

  const confirmDelete = async () => {
    if (!deleteRow || deleting) return;
    setDeleting(true);

    try {
      const result = await saveUpdateSecurityDetails({
        securityId: deleteRow.securityId,
        securityName: deleteRow.securityName,
        roleID: deleteRow.roleId,
        screenName: deleteRow.screenName,
        isDefault: deleteRow.isDefault,
        isDeleted: true,
        createdBy: 0,
        updatedBy: loggedInUserId,
      });

      ToastService.success("Security Master Details Deleted Successfully.");
      setDeleteRow(null);
      await loadSecurities();
    } catch (error) {
      showApiError(error, "Unable to Delete Security Master Details.");
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

  if (!canOpenSecurityList) {
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
            data={securities}
            keyField="securityId"
            onDataChange={setSecurities}
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={2}
            defaultPageSize={30}
            maxHeight={390}
            minTableHeight={235}
            showActions={canCreateSecurity || canEditSecurity}
            showEditAction={canEditSecurity}
            showDeleteAction={canEditSecurity}
            showRowMore={false}
            showAddAction={canCreateSecurity}
            addActionLabel="Add Security"
            addActionIconOnly
            newRowFactory={createSecurityRow}
            onRowSave={saveSecurityRow}
            rowValidationErrors={rowValidationErrors}
            onCellChange={handleCellChange}
            onRowCancel={() => setRowValidationErrors({})}
            showRefreshAction
            onRefresh={() => loadSecurities({ showMessage: false })}
            refreshing={loading}
            loading={loading}
            loadingText="Loading security master..."
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete Security"
        message={`Are you sure you want to delete ${deleteRow?.securityName || "this security"}?`}
        confirmText={deleting ? "Deleting..." : "Yes, Delete"}
        onConfirm={confirmDelete}
        onCancel={() => {
          if (!deleting) setDeleteRow(null);
        }}
      />
    </div>
  );
}
