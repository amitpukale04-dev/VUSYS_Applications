import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { InlineEditableTable } from "../../components/common/InlineEditable";
import ConfirmModal from "../../components/common/ConfirmModal";
import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import {
  getCountryList,
  saveAndUpdateCountryDetails,
} from "../../services/countryService";
import ToastService from "../../services/toastService";
import { normalizeNumber } from "../../utils/valueUtils";
import {
  applyApiValidationErrors,
  clearRowValidationError,
} from "../../utils/CustomAPIValidation";
import { showApiError } from "../../utils/showApiError";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

const COUNTRY_API_FIELD_MAPPINGS = {
  countryCode: { apiKeys: ["CountryCode"] },
  countryName: { apiKeys: ["CountryName"] },
};

export default function CountryMaster() {
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenCountryList = hasPermission("Country Master", "Open");
  const canCreateCountry = hasPermission("Country Master", "Create");
  const canEditCountry = hasPermission("Country Master", "Edit");

  const [countries, setCountries] = useState([]);
  const initialCountryListLoadRef = useRef(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [rowValidationErrors, setRowValidationErrors] = useState({});

  const handleCellChange = (_row, fieldName) => {
    clearRowValidationError(setRowValidationErrors, fieldName);
  };

  // ==========================================================
  // LOAD COUNTRY LIST
  // ==========================================================

  const loadCountries = useCallback(async ({ showMessage = false } = {}) => {
    setLoading(true);

    try {
      const result = await getCountryList();
      setCountries(result.countries || []);

      if (showMessage) {
        ToastService.success("Country List refreshed successfully.");
      }
    } catch (error) {
      setCountries([]);

      /*
       * API ValidationErrors, Network/CORS and GLOBAL_ERROR_RESPONSE are
       * handled globally by axiosSetup.js.
       */
      showApiError(error, "Unable to load Country List.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (securityAccessLoading || !canOpenCountryList) {
      return;
    }

    /*
     * React StrictMode intentionally re-runs Effects in development.
     * Guard only the initial Country List binding so GetCountryList is not
     * requested twice. Manual Refresh and post-save reloads continue
     * to call loadCountries() normally.
     */
    if (initialCountryListLoadRef.current) {
      return;
    }

    initialCountryListLoadRef.current = true;
    loadCountries();
  }, [canOpenCountryList, loadCountries, securityAccessLoading]);

  // ==========================================================
  // GRID COLUMNS
  // ==========================================================

  const columns = useMemo(
    () => [
      {
        key: "countryCode",
        label: "Country Code",
        width: "180px",
        inputType: "text",
        validate: (value) =>
          !String(value || "").trim() ? "Enter The Country Code." : "",
      },
      {
        key: "countryName",
        label: "Country Name",
        width: "360px",
        inputType: "text",
        validate: (value) =>
          !String(value || "").trim() ? "Enter The Country Name." : "",
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
  // NEW COUNTRY ROW
  // ==========================================================

  const createCountryRow = () => ({
    /*
     * Temporary negative key is used only by the client grid so it cannot
     * collide with database Country IDs. The API receives id = 0 for inserts.
     */
    countryId: -Date.now(),
    countryCode: "",
    countryName: "",
    isDeleted: false,
    activeStatus: "Active",
    isActive: true,
  });

  // ==========================================================
  // SAVE / UPDATE COUNTRY
  // ==========================================================

  const saveCountryRow = async (row, { isNew }) => {
    if (saving) return;

    setRowValidationErrors({});
    setSaving(true);

    try {
      const result = await saveAndUpdateCountryDetails({
        id: isNew ? 0 : row.countryId,
        countryCode: row.countryCode,
        countryName: row.countryName,
        isDeleted: !row.isActive,
        createdBy: isNew ? loggedInUserId : 0,
        updatedBy: isNew ? 0 : loggedInUserId,
      });

      ToastService.success(
        result?.message ||
          (isNew
            ? "New Country Added Successfully."
            : "Country Updated Successfully."),
      );

      /* Always rebind from the API after persistence. */
      await loadCountries();
      return true;
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          COUNTRY_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            setRowValidationErrors((current) => ({
              ...current,
              [fieldName]: message,
            }));
          },
        );
      } else {
        showApiError(error, "Unable to save Country Details.");
      }

      // Preserve the draft row so API validation errors can be corrected.
      return false;
    } finally {
      setSaving(false);
    }
  };

  // ==========================================================
  // DELETE COUNTRY
  // ==========================================================

  const confirmDelete = async () => {
    if (!deleteRow || deleting) return;

    setDeleting(true);

    try {
      const result = await saveAndUpdateCountryDetails({
        id: deleteRow.countryId,
        countryCode: deleteRow.countryCode,
        countryName: deleteRow.countryName,
        isDeleted: true,
        createdBy: 0,
        updatedBy: loggedInUserId,
      });

      ToastService.success("Country Details Deleted successfully.");
      setDeleteRow(null);
      await loadCountries();
    } catch (error) {
      showApiError(error, "Unable to delete Country Details.");
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

  if (!canOpenCountryList) {
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
            data={countries}
            keyField="countryId"
            onDataChange={setCountries}
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={2}
            defaultPageSize={30}
            maxHeight={390}
            minTableHeight={235}
            showActions={canCreateCountry || canEditCountry}
            actionColumnWidth="50px"
            showEditAction={canEditCountry}
            showDeleteAction={canEditCountry}
            showRowMore={false}
            showAddAction={canCreateCountry}
            addActionLabel="Add Country"
            addActionIconOnly
            newRowFactory={createCountryRow}
            onRowSave={saveCountryRow}
            rowValidationErrors={rowValidationErrors}
            onCellChange={handleCellChange}
            onRowCancel={() => setRowValidationErrors({})}
            showRefreshAction
            onRefresh={() => loadCountries({ showMessage: false })}
            refreshing={loading}
            loading={loading}
            loadingText="Loading countries..."
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete Country"
        message={`Are you sure you want to delete ${deleteRow?.countryName || "this country"}?`}
        confirmText={deleting ? "Deleting..." : "Yes, Delete"}
        onConfirm={confirmDelete}
        onCancel={() => {
          if (!deleting) setDeleteRow(null);
        }}
      />
    </div>
  );
}
