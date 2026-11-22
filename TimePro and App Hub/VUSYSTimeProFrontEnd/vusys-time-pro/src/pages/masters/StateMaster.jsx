import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { InlineEditableTable } from "../../components/common/InlineEditable";
import ConfirmModal from "../../components/common/ConfirmModal";
import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import {
  getStateList,
  saveAndUpdateStateDetails,
} from "../../services/stateService";
import { getDropdownItems } from "../../services/dropdownService";
import ToastService from "../../services/toastService";
import { DROPDOWN_SOURCE_TABLE } from "../../constants/constants";
import { normalizeNumber } from "../../utils/valueUtils";
import {
  applyApiValidationErrors,
  clearRowValidationError,
} from "../../utils/CustomAPIValidation";
import { showApiError } from "../../utils/showApiError";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

const STATE_API_FIELD_MAPPINGS = {
  stateCode: { apiKeys: ["StateCode"] },
  stateName: { apiKeys: ["StateName"] },
  countryId: { apiKeys: ["CountryID"] },
};

export default function StateMaster() {
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenStateList = hasPermission("State Master", "Open");
  const canCreateState = hasPermission("State Master", "Create");
  const canEditState = hasPermission("State Master", "Edit");

  const [states, setStates] = useState([]);
  const [countryOptions, setCountryOptions] = useState([]);
  const initialStateMasterLoadRef = useRef(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [rowValidationErrors, setRowValidationErrors] = useState({});

  const handleCellChange = (_row, fieldName) => {
    clearRowValidationError(setRowValidationErrors, fieldName);
  };

  // ==========================================================
  // LOAD COUNTRY DROPDOWN
  // ==========================================================

  const loadCountryOptions = useCallback(async () => {
    try {
      const result = await getDropdownItems({
        sourceTable: DROPDOWN_SOURCE_TABLE.COUNTRY,
        argumentValue: "",
        argumentValue1: "",
      });

      setCountryOptions(result.options || []);
    } catch (error) {
      setCountryOptions([]);

      /*
       * API ValidationErrors, Network/CORS and GLOBAL_ERROR_RESPONSE are
       * handled globally by axiosSetup.js.
       */
      showApiError(error, "Unable to load Country List.");
    }
  }, []);

  // ==========================================================
  // LOAD STATE LIST
  // ==========================================================

  const loadStates = useCallback(async ({ showMessage = false } = {}) => {
    setLoading(true);

    try {
      const result = await getStateList();
      setStates(result.states || []);

      if (showMessage) {
        ToastService.success("State List refreshed successfully.");
      }
    } catch (error) {
      setStates([]);

      /*
       * API ValidationErrors, Network/CORS and GLOBAL_ERROR_RESPONSE are
       * handled globally by axiosSetup.js.
       */
      showApiError(error, "Unable to load State List.");
    } finally {
      setLoading(false);
    }
  }, []);

  // ==========================================================
  // INITIAL BINDING
  // ==========================================================

  useEffect(() => {
    if (securityAccessLoading || !canOpenStateList) {
      return;
    }

    /*
     * React StrictMode intentionally re-runs Effects in development.
     * Guard only the initial State Master binding so the State List and
     * Country Dropdown APIs are not requested twice. Manual Refresh and
     * post-save reloads continue to work normally.
     */
    if (initialStateMasterLoadRef.current) {
      return;
    }

    initialStateMasterLoadRef.current = true;
    loadCountryOptions();
    loadStates();
  }, [canOpenStateList, loadCountryOptions, loadStates, securityAccessLoading]);

  // ==========================================================
  // GRID COLUMNS
  // ==========================================================

  const columns = useMemo(
    () => [
      {
        key: "countryId",
        label: "Country",
        width: "260px",
        inputType: "select",
        options: countryOptions,
        filterType: "select",
        filterOptions: countryOptions,
        validate: (value) =>
          !String(value || "").trim() ? "Select The Country." : "",
      },
      {
        key: "stateCode",
        label: "State Code",
        width: "180px",
        inputType: "text",
        validate: (value) =>
          !String(value || "").trim() ? "Enter The State Code." : "",
      },
      {
        key: "stateName",
        label: "State Name",
        width: "260px",
        inputType: "text",
        disabled: (row, { isNew }) => !isNew,
        validate: (value) =>
          !String(value || "").trim() ? "Enter The State Name." : "",
      },
      {
        key: "isActive",
        label: "Active",
        width: "120px",
        inputType: "checkbox",
        filterType: "checkbox",
      },
    ],
    [countryOptions],
  );

  // ==========================================================
  // NEW STATE ROW
  // ==========================================================

  const createStateRow = () => ({
    /*
     * Temporary negative key is used only by the client grid so it cannot
     * collide with database State IDs. The API receives id = 0 for inserts.
     */
    stateId: -Date.now(),
    countryId: "",
    countryName: "",
    stateCode: "",
    stateName: "",
    isDeleted: false,
    activeStatus: "Active",
    isActive: true,
  });

  // ==========================================================
  // SAVE / UPDATE STATE
  // ==========================================================

  const saveStateRow = async (row, { isNew }) => {
    if (saving) return;
    setRowValidationErrors({});
    setSaving(true);

    try {
      const result = await saveAndUpdateStateDetails({
        id: isNew ? 0 : row.stateId,
        stateCode: row.stateCode,
        stateName: row.stateName,
        countryID: row.countryId,
        isDeleted: !row.isActive,
        createdBy: isNew ? loggedInUserId : 0,
        updatedBy: isNew ? 0 : loggedInUserId,
      });

      ToastService.success(
        result?.message ||
          (isNew
            ? "New State Added Successfully."
            : "State Updated Successfully."),
      );

      /* Always rebind from the API after persistence. */
      await loadStates();
      return true;
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          STATE_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            setRowValidationErrors((current) => ({
              ...current,
              [fieldName]: message,
            }));
          },
        );
      } else {
        showApiError(error, "Unable to save State Details.");
      }

      // Preserve the draft row so API validation errors can be corrected.
      return false;
    } finally {
      setSaving(false);
    }
  };

  // ==========================================================
  // DELETE STATE
  // ==========================================================

  const confirmDelete = async () => {
    if (!deleteRow || deleting) return;
    setDeleting(true);

    try {
      const result = await saveAndUpdateStateDetails({
        id: deleteRow.stateId,
        stateCode: deleteRow.stateCode,
        stateName: deleteRow.stateName,
        countryID: deleteRow.countryId,
        isDeleted: true,
        createdBy: 0,
        updatedBy: loggedInUserId,
      });

      ToastService.success("State Details Deleted Successfully.");
      setDeleteRow(null);
      await loadStates();
    } catch (error) {
      showApiError(error, "Unable to delete State Details.");
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

  if (!canOpenStateList) {
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
            data={states}
            keyField="stateId"
            onDataChange={setStates}
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={2}
            defaultPageSize={30}
            maxHeight={390}
            minTableHeight={235}
            showActions={canCreateState || canEditState}
            showEditAction={canEditState}
            showDeleteAction={canEditState}
            showRowMore={false}
            showAddAction={canCreateState}
            addActionLabel="Add State"
            addActionIconOnly
            newRowFactory={createStateRow}
            onRowSave={saveStateRow}
            rowValidationErrors={rowValidationErrors}
            onCellChange={handleCellChange}
            onRowCancel={() => setRowValidationErrors({})}
            showRefreshAction
            onRefresh={() => loadStates({ showMessage: false })}
            refreshing={loading}
            loading={loading}
            loadingText="Loading states..."
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete State"
        message={`Are you sure you want to delete ${deleteRow?.stateName || "this state"}?`}
        confirmText={deleting ? "Deleting..." : "Yes, Delete"}
        onConfirm={confirmDelete}
        onCancel={() => {
          if (!deleting) setDeleteRow(null);
        }}
      />
    </div>
  );
}
