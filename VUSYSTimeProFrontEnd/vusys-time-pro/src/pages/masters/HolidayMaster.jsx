import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { InlineEditableTable } from "../../components/common/InlineEditable";
import ConfirmModal from "../../components/common/ConfirmModal";
import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { getDropdownItems } from "../../services/dropdownService";
import {
  getHolidayList,
  getHolidayDetails,
  saveAndUpdateHolidayDetails,
} from "../../services/holidayService";
import ToastService from "../../services/toastService";
import { SearchableDropdown } from "../../components/forms/FormElements";
import { formatDDMMMYYYY } from "../../utils/dateUtils";
import { normalizeNumber } from "../../utils/valueUtils";
import {
  applyApiValidationErrors,
  clearRowValidationError,
} from "../../utils/CustomAPIValidation";
import { showApiError } from "../../utils/showApiError";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

const currentYear = String(new Date().getFullYear());

const HOLIDAY_API_FIELD_MAPPINGS = {
  holidayName: { apiKeys: ["HolidayName"] },
  holidayDate: { apiKeys: ["HolidayDate"] },
  holidayYear: { apiKeys: ["HolidayYear"] },
  holidayDescription: { apiKeys: ["HolidayDescription"] },
};

export default function HolidayMaster() {
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();
  const canOpenHolidayList = hasPermission("Holiday Master", "Open");
  const canCreateHoliday = hasPermission("Holiday Master", "Create");
  const canEditHoliday = hasPermission("Holiday Master", "Edit");
  const [year, setYear] = useState(currentYear);
  const [years, setYears] = useState([]);
  const [holidays, setHolidays] = useState([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [deleting, setDeleting] = useState(false);
  const [rowValidationErrors, setRowValidationErrors] = useState({});

  const handleCellChange = (_row, fieldName) => {
    clearRowValidationError(setRowValidationErrors, fieldName);
  };

  const initialHolidayListLoadRef = useRef(false);

  // ==========================================================
  // LOAD HOLIDAY LIST
  // ==========================================================

  const loadHolidays = useCallback(
    async (selectedYear, { showMessage = false } = {}) => {
      if (!selectedYear) return;

      setLoading(true);

      try {
        const rows = await getHolidayList(selectedYear);
        setHolidays(
          rows.map((row) => ({
            ...row,
            activeStatus: !row.isDeleted,
          })),
        );

        if (showMessage) {
          ToastService.success("Holiday List refreshed successfully.");
        }
      } catch (error) {
        setHolidays([]);

        /*
         * API ValidationErrors, Network/CORS and GLOBAL_ERROR_RESPONSE are
         * handled globally by axiosSetup.js.
         */
        showApiError(error, "Unable to load Holiday List.");
      } finally {
        setLoading(false);
      }
    },
    [],
  );

  // ==========================================================
  // LOAD HOLIDAY YEARS AND INITIAL LIST
  // ==========================================================

  useEffect(() => {
    if (securityAccessLoading || !canOpenHolidayList) {
      return;
    }

    /*
     * React StrictMode intentionally re-runs Effects in development.
     * Guard only the initial Holiday List binding. Manual Refresh and
     * post-save reloads continue to call loadHolidays() normally.
     */
    if (initialHolidayListLoadRef.current) {
      return;
    }

    initialHolidayListLoadRef.current = true;

    const initializeHolidayList = async () => {
      try {
        const result = await getDropdownItems({
          sourceTable: "Year",
          argumentValue: "",
          argumentValue1: "",
        });

        const options = result.options || [];
        setYears(options);

        const selectedYear = options.some(
          (item) => String(item.value) === currentYear,
        )
          ? currentYear
          : String(options[0]?.value || currentYear);

        setYear(selectedYear);
        await loadHolidays(selectedYear);
      } catch (error) {
        /*
         * API ValidationErrors, Network/CORS and GLOBAL_ERROR_RESPONSE are
         * handled globally by axiosSetup.js.
         */
        showApiError(error, "Unable to load Years.");
        await loadHolidays(currentYear);
      }
    };

    void initializeHolidayList();
  }, [canOpenHolidayList, loadHolidays, securityAccessLoading]);

  // ==========================================================
  // GRID COLUMNS
  // ==========================================================

  const columns = useMemo(
    () => [
      {
        key: "holidayName",
        label: "Holiday Name",
        width: "190px",
        inputType: "text",
        validate: (value) =>
          !String(value || "").trim() ? "Enter The Holiday Name." : "",
      },
      {
        key: "holidayDate",
        label: "Holiday Date",
        width: "170px",
        inputType: "date",
        displayValue: (value) => formatDDMMMYYYY(value),
        validate: (value, row) =>
          !value
            ? "Select The Holiday Date."
            : row.holidayYear &&
                String(value).slice(0, 4) !== String(row.holidayYear)
              ? "Holiday Date Must Match Holiday Year."
              : "",
      },
      {
        key: "holidayYear",
        label: "Holiday Year",
        width: "170px",
        inputType: "select",
        options: years,
        validate: (value, row) =>
          !value
            ? "Select The Holiday Year."
            : row.holidayDate &&
                String(row.holidayDate).slice(0, 4) !== String(value)
              ? "Holiday Year Must Match Holiday Date."
              : "",
      },
      {
        key: "holidayDescription",
        label: "Holiday Description",
        width: "220px",
        inputType: "textarea",
        validate: (value) =>
          !String(value || "").trim() ? "Enter The Holiday Description." : "",
      },
      {
        key: "isOptionalHoliday",
        label: "Is Optional",
        width: "160px",
        inputType: "checkbox",
        filterType: "checkbox",
      },
      {
        key: "activeStatus",
        label: "Active",
        width: "130px",
        inputType: "checkbox",
        filterType: "checkbox",
      },
    ],
    [years],
  );

  // ==========================================================
  // NEW HOLIDAY ROW
  // ==========================================================

  const createHolidayRow = () => ({
    /*
     * Temporary negative key is used only by the client grid so it cannot
     * collide with database Holiday IDs. The API receives id = 0 for inserts.
     */
    id: -Date.now(),
    holidayName: "",
    holidayDate: "",
    holidayYear: year,
    holidayDescription: "",
    isOptionalHoliday: false,
    isDeleted: false,
    activeStatus: true,
  });

  // ==========================================================
  // SAVE / UPDATE HOLIDAY
  // ==========================================================

  const saveHolidayRow = async (row, { isNew }) => {
    if (saving) return;
    setRowValidationErrors({});
    setSaving(true);

    try {
      const current = isNew
        ? row
        : await getHolidayDetails(row.id, row.holidayYear);

      const result = await saveAndUpdateHolidayDetails({
        ...current,
        ...row,
        id: isNew ? 0 : row.id,
        isDeleted: !row.activeStatus,
        createdBy: isNew ? loggedInUserId : 0,
        updatedBy: isNew ? 0 : loggedInUserId,
      });

      ToastService.success(
        result?.message ||
          (isNew
            ? "New Holiday Added Successfully."
            : "Holiday Updated Successfully."),
      );

      /* Always rebind from the API after persistence. */
      await loadHolidays(year);
      return true;
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          HOLIDAY_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            setRowValidationErrors((current) => ({
              ...current,
              [fieldName]: message,
            }));
          },
        );
      } else {
        showApiError(error, "Unable to save Holiday Details.");
      }

      // Preserve the draft row so API validation errors can be corrected.
      return false;
    } finally {
      setSaving(false);
    }
  };

  // ==========================================================
  // DELETE HOLIDAY
  // ==========================================================

  const confirmDelete = async () => {
    if (!deleteRow || deleting) return;
    setDeleting(true);

    try {
      const details = await getHolidayDetails(
        deleteRow.id,
        deleteRow.holidayYear,
      );

      const result = await saveAndUpdateHolidayDetails({
        ...details,
        isDeleted: true,
        createdBy: 0,
        updatedBy: loggedInUserId,
      });

      ToastService.success(
        result?.message || "Holiday Details Deleted successfully.",
      );
      setDeleteRow(null);
      await loadHolidays(year);
    } catch (error) {
      showApiError(error, "Unable to delete Holiday Details.");
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

  if (!canOpenHolidayList) {
    return <AccessDenied />;
  }

  // ==========================================================
  // UI
  // ==========================================================

  return (
    <div>
      <div className="tp-card tp-grid-card tp-grid-card-modern">
        <div className="tp-grid-card-body">
          <div className="tp-holiday-year-filter">
            <SearchableDropdown
              name="holidayYearFilter"
              label="Holiday Year"
              required
              value={year}
              onChange={(event) => {
                const next = event.target.value;
                setYear(next);
                void loadHolidays(next);
              }}
              options={
                years.some((item) => String(item.value) === year)
                  ? years
                  : [{ value: year, label: year }, ...years]
              }
              columns={[{ key: "label", label: "Year" }]}
              placeholder="Select year"
              disabled={loading}
            />
          </div>
          <InlineEditableTable
            columns={columns}
            data={holidays}
            keyField="id"
            onDataChange={setHolidays}
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={1}
            defaultPageSize={30}
            maxHeight={390}
            minTableHeight={235}
            showActions={canCreateHoliday || canEditHoliday}
            actionColumnWidth="70px"
            showEditAction={canEditHoliday}
            showDeleteAction={canEditHoliday}
            showRowMore={false}
            showAddAction={canCreateHoliday}
            addActionLabel="Add Holiday"
            addActionIconOnly
            newRowFactory={createHolidayRow}
            onRowSave={saveHolidayRow}
            rowValidationErrors={rowValidationErrors}
            onCellChange={handleCellChange}
            onRowCancel={() => setRowValidationErrors({})}
            showRefreshAction
            onRefresh={() => loadHolidays(year, { showMessage: false })}
            refreshing={loading}
            loading={loading}
            loadingText="Loading holidays..."
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>
      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete Holiday"
        message={`Are you sure you want to delete ${deleteRow?.holidayName || "this holiday"}?`}
        confirmText={deleting ? "Deleting..." : "Yes, Delete"}
        onConfirm={confirmDelete}
        onCancel={() => {
          if (!deleting) setDeleteRow(null);
        }}
      />
    </div>
  );
}
