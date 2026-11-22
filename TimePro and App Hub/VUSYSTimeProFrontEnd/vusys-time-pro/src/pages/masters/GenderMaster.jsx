import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { InlineEditableTable } from "../../components/common/InlineEditable";
import ConfirmModal from "../../components/common/ConfirmModal";
import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import {
  getGenderList,
  saveAndUpdateGenderDetails,
} from "../../services/genderService";
import ToastService from "../../services/toastService";
import { normalizeNumber } from "../../utils/valueUtils";
import {
  applyApiValidationErrors,
  clearRowValidationError,
} from "../../utils/CustomAPIValidation";
import { showApiError } from "../../utils/showApiError";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

const GENDER_API_FIELD_MAPPINGS = {
  gender: { apiKeys: ["Gender"] },
};

export default function GenderMaster() {
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenGenderList = hasPermission("Gender Master", "Open");
  const canCreateGender = hasPermission("Gender Master", "Create");
  const canEditGender = hasPermission("Gender Master", "Edit");

  const [genders, setGenders] = useState([]);
  const initialGenderListLoadRef = useRef(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [rowValidationErrors, setRowValidationErrors] = useState({});

  const handleCellChange = (_row, fieldName) => {
    clearRowValidationError(setRowValidationErrors, fieldName);
  };

  // ==========================================================
  // LOAD GENDER LIST
  // ==========================================================

  const loadGenders = useCallback(async ({ showMessage = false } = {}) => {
    setLoading(true);

    try {
      const result = await getGenderList();
      setGenders(result.genders || []);

      if (showMessage) {
        ToastService.success("Gender List refreshed successfully.");
      }
    } catch (error) {
      setGenders([]);

      /*
       * API ValidationErrors, Network/CORS and GLOBAL_ERROR_RESPONSE are
       * handled globally by axiosSetup.js.
       */
      showApiError(error, "Unable to load Gender List.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (securityAccessLoading || !canOpenGenderList) {
      return;
    }

    /*
     * React StrictMode intentionally re-runs Effects in development.
     * Guard only the initial Gender List binding so GetGenderList is not
     * requested twice. Manual Refresh and post-save reloads continue
     * to call loadGenders() normally.
     */
    if (initialGenderListLoadRef.current) {
      return;
    }

    initialGenderListLoadRef.current = true;
    loadGenders();
  }, [canOpenGenderList, loadGenders, securityAccessLoading]);

  // ==========================================================
  // GRID COLUMNS
  // ==========================================================

  const columns = useMemo(
    () => [
      {
        key: "gender",
        label: "Gender",
        width: "320px",
        inputType: "text",
        validate: (value) =>
          !String(value || "").trim() ? "Enter The Gender." : "",
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
  // NEW GENDER ROW
  // ==========================================================

  const createGenderRow = () => ({
    /*
     * Temporary negative key is used only by the client grid so it cannot
     * collide with database Gender IDs. The API receives id = 0 for inserts.
     */
    genderId: -Date.now(),
    gender: "",
    isDeleted: false,
    activeStatus: "Active",
    isActive: true,
  });

  // ==========================================================
  // SAVE / UPDATE GENDER
  // ==========================================================

  const saveGenderRow = async (row, { isNew }) => {
    if (saving) return;
    setRowValidationErrors({});
    setSaving(true);

    try {
      const result = await saveAndUpdateGenderDetails({
        id: isNew ? 0 : row.genderId,
        gender: row.gender,
        isDeleted: !row.isActive,
        createdBy: isNew ? loggedInUserId : 0,
        updatedBy: isNew ? 0 : loggedInUserId,
      });

      ToastService.success(
        result?.message ||
          (isNew
            ? "New Gender Added Successfully."
            : "Gender Updated Successfully."),
      );

      /* Always rebind from the API after persistence. */
      await loadGenders();
      return true;
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          GENDER_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            setRowValidationErrors((current) => ({
              ...current,
              [fieldName]: message,
            }));
          },
        );
      } else {
        showApiError(error, "Unable to save Gender Details.");
      }

      // Preserve the draft row so API validation errors can be corrected.
      return false;
    } finally {
      setSaving(false);
    }
  };

  // ==========================================================
  // DELETE GENDER
  // ==========================================================

  const confirmDelete = async () => {
    if (!deleteRow || deleting) return;
    setDeleting(true);

    try {
      const result = await saveAndUpdateGenderDetails({
        id: deleteRow.genderId,
        gender: deleteRow.gender,
        isDeleted: true,
        createdBy: 0,
        updatedBy: loggedInUserId,
      });

      ToastService.success("Gender Details Deleted Successfully.");
      setDeleteRow(null);
      await loadGenders();
    } catch (error) {
      showApiError(error, "Unable to delete Gender Details.");
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

  if (!canOpenGenderList) {
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
            data={genders}
            keyField="genderId"
            onDataChange={setGenders}
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={2}
            defaultPageSize={30}
            maxHeight={390}
            minTableHeight={235}
            showActions={canCreateGender || canEditGender}
            actionColumnWidth="50px"
            showEditAction={canEditGender}
            showDeleteAction={canEditGender}
            showRowMore={false}
            showAddAction={canCreateGender}
            addActionLabel="Add Gender"
            addActionIconOnly
            newRowFactory={createGenderRow}
            onRowSave={saveGenderRow}
            rowValidationErrors={rowValidationErrors}
            onCellChange={handleCellChange}
            onRowCancel={() => setRowValidationErrors({})}
            showRefreshAction
            onRefresh={() => loadGenders({ showMessage: false })}
            refreshing={loading}
            loading={loading}
            loadingText="Loading genders..."
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete Gender"
        message={`Are you sure you want to delete ${deleteRow?.gender || "this gender"}?`}
        confirmText={deleting ? "Deleting..." : "Yes, Delete"}
        onConfirm={confirmDelete}
        onCancel={() => {
          if (!deleting) setDeleteRow(null);
        }}
      />
    </div>
  );
}
