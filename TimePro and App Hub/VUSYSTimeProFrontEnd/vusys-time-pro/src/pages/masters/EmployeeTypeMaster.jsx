import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { InlineEditableTable } from "../../components/common/InlineEditable";
import ConfirmModal from "../../components/common/ConfirmModal";
import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import {
  getEmployeeTypeList,
  saveAndUpdateEmployeeTypeDetails,
} from "../../services/employeeTypeService";
import ToastService from "../../services/toastService";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import {
  applyApiValidationErrors,
  clearRowValidationError,
} from "../../utils/CustomAPIValidation";

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

const EMPLOYEE_TYPE_API_FIELD_MAPPINGS = {
  employeeType: { apiKeys: ["EmployeeType"] },
};

export default function EmployeeTypeMaster() {
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenEmployeeTypeList = hasPermission("Employee Type Master", "Open");
  const canCreateEmployeeType = hasPermission("Employee Type Master", "Create");
  const canEditEmployeeType = hasPermission("Employee Type Master", "Edit");

  const [employeeTypes, setEmployeeTypes] = useState([]);
  const initialEmployeeTypeListLoadRef = useRef(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [rowValidationErrors, setRowValidationErrors] = useState({});

  const handleCellChange = (_row, fieldName) => {
    clearRowValidationError(setRowValidationErrors, fieldName);
  };

  // ==========================================================
  // LOAD EMPLOYEE TYPE LIST
  // ==========================================================

  const loadEmployeeTypes = useCallback(
    async ({ showMessage = false } = {}) => {
      setLoading(true);

      try {
        const result = await getEmployeeTypeList();
        setEmployeeTypes(result.employeeTypes || []);

        if (showMessage) {
          ToastService.success("Employee Type List refreshed successfully.");
        }
      } catch (error) {
        setEmployeeTypes([]);
        showApiError(error, "Unable to load Employee Type List.");
      } finally {
        setLoading(false);
      }
    },
    [],
  );

  // ==========================================================
  // INITIAL BINDING
  // ==========================================================

  useEffect(() => {
    if (securityAccessLoading || !canOpenEmployeeTypeList) {
      return;
    }

    if (initialEmployeeTypeListLoadRef.current) {
      return;
    }

    initialEmployeeTypeListLoadRef.current = true;
    loadEmployeeTypes();
  }, [canOpenEmployeeTypeList, loadEmployeeTypes, securityAccessLoading]);

  // ==========================================================
  // GRID COLUMNS
  // ==========================================================

  const columns = useMemo(
    () => [
      {
        key: "employeeType",
        label: "Employee Type",
        width: "420px",
        inputType: "text",
        validate: (value) =>
          !String(value || "").trim() ? "Enter The Employee Type." : "",
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
  // NEW EMPLOYEE TYPE ROW
  // ==========================================================

  const createEmployeeTypeRow = () => ({
    employeeTypeId: -Date.now(),
    employeeType: "",
    isDeleted: false,
    activeStatus: "Active",
    isActive: true,
  });

  // ==========================================================
  // SAVE / UPDATE EMPLOYEE TYPE
  // ==========================================================

  const saveEmployeeTypeRow = async (row, { isNew }) => {
    if (saving) return false;
    setRowValidationErrors({});
    setSaving(true);

    try {
      const result = await saveAndUpdateEmployeeTypeDetails({
        id: isNew ? 0 : row.employeeTypeId,
        employeeType: row.employeeType,
        isDeleted: !row.isActive,
        createdBy: isNew ? loggedInUserId : 0,
        updatedBy: isNew ? 0 : loggedInUserId,
      });

      ToastService.success(
        result?.message ||
          (isNew
            ? "Employee Type Added Successfully."
            : "Employee Type Updated Successfully."),
      );

      await loadEmployeeTypes();
      return true;
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          EMPLOYEE_TYPE_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            setRowValidationErrors((current) => ({
              ...current,
              [fieldName]: message,
            }));
          },
        );
      } else {
        showApiError(error, "Unable to save Employee Type Details.");
      }

      // Keep the edited row open so the user can correct the error.
      return false;
    } finally {
      setSaving(false);
    }
  };

  // ==========================================================
  // DELETE EMPLOYEE TYPE
  // ==========================================================

  const confirmDelete = async () => {
    if (!deleteRow || deleting) return;
    setDeleting(true);

    try {
      const result = await saveAndUpdateEmployeeTypeDetails({
        id: deleteRow.employeeTypeId,
        employeeType: deleteRow.employeeType,
        isDeleted: true,
        createdBy: 0,
        updatedBy: loggedInUserId,
      });

      ToastService.success("Employee Type Details Deleted Successfully.");
      setDeleteRow(null);
      await loadEmployeeTypes();
    } catch (error) {
      showApiError(error, "Unable to delete Employee Type Details.");
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

  if (!canOpenEmployeeTypeList) {
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
            data={employeeTypes}
            keyField="employeeTypeId"
            onDataChange={setEmployeeTypes}
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={2}
            defaultPageSize={30}
            maxHeight={390}
            minTableHeight={235}
            showActions={canCreateEmployeeType || canEditEmployeeType}
            actionColumnWidth="50px"
            showEditAction={canEditEmployeeType}
            showDeleteAction={canEditEmployeeType}
            showRowMore={false}
            showAddAction={canCreateEmployeeType}
            addActionLabel="Add Employee Type"
            addActionIconOnly
            newRowFactory={createEmployeeTypeRow}
            onRowSave={saveEmployeeTypeRow}
            rowValidationErrors={rowValidationErrors}
            onCellChange={handleCellChange}
            onRowCancel={() => setRowValidationErrors({})}
            showRefreshAction
            onRefresh={() => loadEmployeeTypes({ showMessage: false })}
            refreshing={loading}
            loading={loading}
            loadingText="Loading employee types..."
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete Employee Type"
        message={`Are you sure you want to delete ${deleteRow?.employeeType || "this employee type"}?`}
        confirmText={deleting ? "Deleting..." : "Yes, Delete"}
        onConfirm={confirmDelete}
        onCancel={() => {
          if (!deleting) setDeleteRow(null);
        }}
      />
    </div>
  );
}
