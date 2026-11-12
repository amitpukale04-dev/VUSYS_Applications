import { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  Checkbox,
  SearchableDropdown,
  TextBox,
} from "../../components/forms/FormElements";
import SvgIcon from "../../components/common/SvgIcon";
import DirtyStateModal from "../../components/common/DirtyStateModal";
import AccessDenied from "../../components/common/AccessDenied";
import ToastService from "../../services/toastService";
import {
  getEmployeeLeaveBalanceDetails,
  saveAndUpdateEmployeeLeaveBalanceDetails,
} from "../../services/employeeLeaveBalanceService";
import useFormValidation from "../../hooks/useFormValidation";
import { employeeLeaveBalanceValidationRules } from "../../validation/employeeLeaveBalanceValidationRules";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { COMMON_MESSAGE } from "../../constants/constants";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import { decodeRouteId } from "../../utils/routeIdUtils";
import { getDropdownItems } from "../../services/dropdownService";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const EMPLOYEE_LEAVE_BALANCE_API_FIELD_MAPPINGS = {
  employeeCode: { apiKeys: ["EmployeeCode"] },
  leaveTypeID: { apiKeys: ["LeaveTypeID"] },
  leaveYear: { apiKeys: ["LeaveYear"] },
  openingBalance: { apiKeys: ["OpeningBalance"] },
  allocatedLeave: { apiKeys: ["AllocatedLeave"] },
  usedLeave: { apiKeys: ["UsedLeave"] },
  pendingLeave: { apiKeys: ["PendingLeave"] },
  availableBalance: { apiKeys: ["AvailableBalance"] },
};

const currentYear = String(new Date().getFullYear());
const NavigateEmployeeLeaveBalance =
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE;

const blankEmployeeLeaveBalance = {
  employeeCode: "",
  leaveTypeID: "",
  leaveYear: currentYear,
  openingBalance: "0",
  allocatedLeave: "0",
  usedLeave: "0",
  pendingLeave: "0",
  availableBalance: "0",
  isDeleted: false,
};

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

const createComparableSnapshot = (value) =>
  JSON.stringify({
    employeeCode: value.employeeCode,
    leaveTypeID: String(value.leaveTypeID),
    leaveYear: String(value.leaveYear),
    openingBalance: String(value.openingBalance),
    allocatedLeave: String(value.allocatedLeave),
    usedLeave: String(value.usedLeave),
    pendingLeave: String(value.pendingLeave),
    availableBalance: String(value.availableBalance),
    isDeleted: value.isDeleted,
  });

export default function EmployeeLeaveBalanceFormPage() {
  const { registerTabDirtyState } = useTabs();
  const { employeeLeaveBalanceId } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const isAddMode = !employeeLeaveBalanceId || employeeLeaveBalanceId === "new";
  const decodedEmployeeLeaveBalanceId = isAddMode
    ? 0
    : decodeRouteId(employeeLeaveBalanceId);
  const requestedMode =
    location.state?.mode ||
    new URLSearchParams(location.search).get("mode") ||
    "view";

  const hasCreatePermission = hasPermission("Employee Leave Balance", "Create");
  const hasEditPermission = hasPermission("Employee Leave Balance", "Edit");
  const hasViewPermission = hasPermission("Employee Leave Balance", "View");

  const [mode, setMode] = useState(isAddMode ? "add" : requestedMode);
  const [form, setForm] = useState(blankEmployeeLeaveBalance);
  const [employees, setEmployees] = useState([]);
  const [leaveTypes, setLeaveTypes] = useState([]);
  const [years, setYears] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [dirtyOpen, setDirtyOpen] = useState(false);

  const originalSnapshotRef = useRef(
    createComparableSnapshot(blankEmployeeLeaveBalance),
  );
  const detailsLoadPromiseRef = useRef({ id: "", promise: null });
  const pendingActionRef = useRef(null);

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(employeeLeaveBalanceValidationRules);

  const readOnly = mode === "view";
  const canSave = mode === "add" || mode === "edit";
  const canCancel = mode === "add" || mode === "edit";
  const canEdit = !isAddMode && mode !== "edit" && hasEditPermission;

  const title = isAddMode
    ? "Add Employee Leave Balance"
    : mode === "edit"
      ? "Edit Employee Leave Balance"
      : "View Employee Leave Balance";

  const isDirty = useMemo(
    () => createComparableSnapshot(form) !== originalSnapshotRef.current,
    [form],
  );

  useEffect(() => {
    registerTabDirtyState(NavigateEmployeeLeaveBalance, isDirty);

    return () => {
      registerTabDirtyState(NavigateEmployeeLeaveBalance, false);
    };
  }, [isDirty, registerTabDirtyState]);

  useEffect(() => {
    let active = true;
    const normalizedEmployeeLeaveBalanceId = String(
      decodedEmployeeLeaveBalanceId || "",
    );

    if (!isAddMode && !decodedEmployeeLeaveBalanceId) {
      navigate(NavigateEmployeeLeaveBalance, { replace: true });
      return undefined;
    }

    setLoading(true);
    const loaderRequestId = !isAddMode
      ? showFullPageLoader("Loading employee leave balance details...")
      : null;

    if (
      !isAddMode &&
      (detailsLoadPromiseRef.current.id !== normalizedEmployeeLeaveBalanceId ||
        !detailsLoadPromiseRef.current.promise)
    ) {
      detailsLoadPromiseRef.current = {
        id: normalizedEmployeeLeaveBalanceId,
        promise: getEmployeeLeaveBalanceDetails({
          id: decodedEmployeeLeaveBalanceId,
        }),
      };
    }

    const detailsPromise = isAddMode
      ? Promise.resolve(null)
      : detailsLoadPromiseRef.current.promise;

    detailsPromise
      .then((detailsResult) => {
        if (!active) return;

        if (!detailsResult?.leaveBalance) {
          setForm(blankEmployeeLeaveBalance);
          originalSnapshotRef.current = createComparableSnapshot(
            blankEmployeeLeaveBalance,
          );
          return;
        }

        const nextForm = {
          ...blankEmployeeLeaveBalance,
          ...detailsResult.leaveBalance,
          leaveTypeID: String(detailsResult.leaveBalance.leaveTypeID ?? ""),
          leaveYear: String(
            detailsResult.leaveBalance.leaveYear ?? currentYear,
          ),
          openingBalance: String(
            detailsResult.leaveBalance.openingBalance ?? 0,
          ),
          allocatedLeave: String(
            detailsResult.leaveBalance.allocatedLeave ?? 0,
          ),
          usedLeave: String(detailsResult.leaveBalance.usedLeave ?? 0),
          pendingLeave: String(detailsResult.leaveBalance.pendingLeave ?? 0),
          availableBalance: String(
            detailsResult.leaveBalance.availableBalance ?? 0,
          ),
        };

        setForm(nextForm);
        originalSnapshotRef.current = createComparableSnapshot(nextForm);
      })
      .catch((error) => {
        if (!active) return;
        showApiError(error, "Unable to load Employee Leave Balance Details.");
        if (!isAddMode) {
          navigate(NavigateEmployeeLeaveBalance, { replace: true });
        }
      })
      .finally(() => {
        if (active) setLoading(false);
        if (loaderRequestId !== null) hideFullPageLoader(loaderRequestId);
      });

    return () => {
      active = false;
    };
  }, [
    decodedEmployeeLeaveBalanceId,
    isAddMode,
    navigate,
    showFullPageLoader,
    hideFullPageLoader,
  ]);

  useEffect(() => {
    const handleBeforeUnload = (event) => {
      if (!canSave || !isDirty) return;
      event.preventDefault();
      event.returnValue = "";
    };

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [canSave, isDirty]);

  // ==========================================================
  // LOAD EMPLOYEE, LEAVE TYPE AND YEAR DROPDOWNS
  // ==========================================================

  useEffect(() => {
    if (securityAccessLoading) return;

    let active = true;

    Promise.all([
      getDropdownItems({ sourceTable: "Employee" }),
      getDropdownItems({ sourceTable: "LeaveType" }),
      getDropdownItems({ sourceTable: "Year" }),
    ])
      .then(([employeeResult, leaveTypeResult, yearResult]) => {
        if (!active) return;
        setEmployees(employeeResult.options || []);
        setLeaveTypes(leaveTypeResult.options || []);
        setYears(yearResult.options || []);
      })
      .catch((error) => {
        if (!active) return;
        showApiError(error, "Unable to load dropdown items.");
      });

    return () => {
      active = false;
    };
  }, [securityAccessLoading]);

  const update = (event) => {
    const { name, type, checked, value } = event.target;
    const nextValue = type === "checkbox" ? checked : value;

    setForm((current) => ({ ...current, [name]: nextValue }));
    clearError(name);
  };

  const runWithDirtyGuard = (action) => {
    if (canSave && isDirty) {
      pendingActionRef.current = action;
      setDirtyOpen(true);
      return;
    }

    action();
  };

  const discardChanges = () => {
    setDirtyOpen(false);
    const action = pendingActionRef.current;
    pendingActionRef.current = null;
    action?.();
  };

  const reloadDetails = async () => {
    const result = await getEmployeeLeaveBalanceDetails({
      id: decodedEmployeeLeaveBalanceId,
    });
    const nextForm = {
      ...blankEmployeeLeaveBalance,
      ...result.leaveBalance,
      leaveTypeID: String(result.leaveBalance.leaveTypeID ?? ""),
      leaveYear: String(result.leaveBalance.leaveYear ?? currentYear),
      openingBalance: String(result.leaveBalance.openingBalance ?? 0),
      allocatedLeave: String(result.leaveBalance.allocatedLeave ?? 0),
      usedLeave: String(result.leaveBalance.usedLeave ?? 0),
      pendingLeave: String(result.leaveBalance.pendingLeave ?? 0),
      availableBalance: String(result.leaveBalance.availableBalance ?? 0),
    };

    setForm(nextForm);
    originalSnapshotRef.current = createComparableSnapshot(nextForm);
    return result;
  };

  const cancelEdit = async () => {
    clearErrors();

    if (isAddMode) {
      navigate(NavigateEmployeeLeaveBalance);
      return;
    }

    setLoading(true);
    const loaderRequestId = showFullPageLoader(
      "Loading employee leave balance details...",
    );

    try {
      await reloadDetails();
      setMode("view");
    } catch (error) {
      showApiError(error, "Unable to reload Employee Leave Balance Details.");
    } finally {
      hideFullPageLoader(loaderRequestId);
      setLoading(false);
    }
  };

  const save = async (event) => {
    event.preventDefault();

    if (!canSave || saving) return;

    /*
     * Edit mode must never call the API when the loaded record has not
     * actually been modified.
     */
    if (!isAddMode && mode === "edit" && !isDirty) {
      ToastService.error(COMMON_MESSAGE.NO_CHANGES_MADE_TO_FORM);
      return;
    }

    if (!validate(form)) return;

    const loaderRequestId = showFullPageLoader(
      "Saving Employee Leave Balance Details...",
    );
    setSaving(true);

    try {
      const result = await saveAndUpdateEmployeeLeaveBalanceDetails({
        id: isAddMode ? 0 : decodedEmployeeLeaveBalanceId,
        employeeCode: form.employeeCode,
        leaveTypeID: form.leaveTypeID,
        leaveYear: form.leaveYear,
        openingBalance: form.openingBalance,
        allocatedLeave: form.allocatedLeave,
        usedLeave: form.usedLeave,
        pendingLeave: form.pendingLeave,
        availableBalance: form.availableBalance,
        isDeleted: mode === "edit" ? form.isDeleted : false,
        createdBy: isAddMode ? loggedInUserId : 0,
        updatedBy: isAddMode ? 0 : loggedInUserId,
      });

      ToastService.success(
        result.message || "Employee Leave Balance Details Saved Successfully.",
      );

      const savedId = result.id || decodedEmployeeLeaveBalanceId;

      if (!isAddMode && form.isDeleted) {
        navigate(NavigateEmployeeLeaveBalance);
        return;
      }

      if (savedId > 0) {
        const detailsResult = await getEmployeeLeaveBalanceDetails({
          id: savedId,
        });
        const nextForm = {
          ...blankEmployeeLeaveBalance,
          ...detailsResult.leaveBalance,
          leaveTypeID: String(detailsResult.leaveBalance.leaveTypeID ?? ""),
          leaveYear: String(
            detailsResult.leaveBalance.leaveYear ?? currentYear,
          ),
          openingBalance: String(
            detailsResult.leaveBalance.openingBalance ?? 0,
          ),
          allocatedLeave: String(
            detailsResult.leaveBalance.allocatedLeave ?? 0,
          ),
          usedLeave: String(detailsResult.leaveBalance.usedLeave ?? 0),
          pendingLeave: String(detailsResult.leaveBalance.pendingLeave ?? 0),
          availableBalance: String(
            detailsResult.leaveBalance.availableBalance ?? 0,
          ),
        };

        setForm(nextForm);
        originalSnapshotRef.current = createComparableSnapshot(nextForm);
        setMode("view");

        if (isAddMode) {
          detailsLoadPromiseRef.current = {
            id: String(savedId),
            promise: Promise.resolve(detailsResult),
          };
          navigate(NavigateEmployeeLeaveBalance);
        } else {
          navigate(NavigateEmployeeLeaveBalance);
        }
      } else {
        navigate(NavigateEmployeeLeaveBalance);
      }
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors || [],
          EMPLOYEE_LEAVE_BALANCE_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(error, "Unable to save Employee Leave Balance Details.");
      }
    } finally {
      hideFullPageLoader(loaderRequestId);
      setSaving(false);
    }
  };

  if (securityAccessLoading) {
    return (
      <div className="tp-form-page-loading">
        <span className="spinner-border text-primary" aria-hidden="true" />
        <p>Loading security permissions...</p>
      </div>
    );
  }

  const hasRequestedAccess = isAddMode
    ? hasCreatePermission
    : requestedMode === "edit"
      ? hasEditPermission
      : hasViewPermission;

  if (!hasRequestedAccess) {
    return (
      <AccessDenied
        message="You don't have permission to access this Employee Leave Balance action."
        returnPath={NavigateEmployeeLeaveBalance}
        returnLabel="Back to Employee Leave Balances"
      />
    );
  }

  if (loading) return null;

  return (
    <div className="tp-screen-form-page tp-employee-leave-balance-form-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="leaveBalance" size={16} />
          <h1>{title}</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          {hasEditPermission && !isAddMode && (
            <button
              type="button"
              className="tp-form-page-icon-button"
              onClick={() => setMode("edit")}
              disabled={!canEdit}
              title="Edit Employee Leave Balance"
              aria-label="Edit Employee Leave Balance"
            >
              <SvgIcon name="edit" size={18} />
            </button>
          )}

          <button
            type="submit"
            form="employeeLeaveBalance-form"
            className="tp-form-page-icon-button"
            disabled={!canSave || saving}
            title="Save Employee Leave Balance"
            aria-label="Save Employee Leave Balance"
          >
            {saving ? (
              <span className="spinner-border spinner-border-sm" />
            ) : (
              <SvgIcon name="save" size={18} />
            )}
          </button>

          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => runWithDirtyGuard(cancelEdit)}
            disabled={!canCancel || saving}
            title="Cancel"
            aria-label="Cancel"
          >
            <SvgIcon name="close" size={18} />
          </button>

          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() =>
              runWithDirtyGuard(() => navigate(NavigateEmployeeLeaveBalance))
            }
            title="Back to Employee Leave Balances"
            aria-label="Back to Employee Leave Balances"
          >
            <SvgIcon name="back" size={18} />
          </button>
        </div>
      </div>

      <form
        id="employeeLeaveBalance-form"
        className="tp-card tp-form-card tp-form-card-attached"
        onSubmit={save}
        noValidate
      >
        <div className="row g-3">
          {/* First Row: Employee, Leave Type and Year */}
          <div className="col-md-4">
            <SearchableDropdown
              name="employeeCode"
              label="Employee"
              required
              value={form.employeeCode}
              onChange={update}
              options={employees}
              columns={[{ key: "label", label: "Employee" }]}
              placeholder="Search and Select Employee"
              error={errors.employeeCode}
              disabled={readOnly || saving}
            />
          </div>

          <div className="col-md-4">
            <SearchableDropdown
              name="leaveTypeID"
              label="Leave Type"
              required
              value={String(form.leaveTypeID)}
              onChange={update}
              options={leaveTypes}
              columns={[{ key: "label", label: "Leave Type" }]}
              placeholder="Search and Select Leave Type"
              error={errors.leaveTypeID}
              disabled={readOnly || saving}
            />
          </div>

          <div className="col-md-4">
            <SearchableDropdown
              name="leaveYear"
              label="Year"
              required
              value={String(form.leaveYear)}
              onChange={update}
              options={
                years.some(
                  (item) => String(item.value) === String(form.leaveYear),
                )
                  ? years
                  : [
                      {
                        value: String(form.leaveYear),
                        label: String(form.leaveYear),
                      },
                      ...years,
                    ]
              }
              columns={[{ key: "label", label: "Year" }]}
              placeholder="Search and Select Year"
              error={errors.leaveYear}
              disabled={readOnly || saving}
            />
          </div>

          {/* Second Row: Opening, Allocated and Used Leave */}
          {[
            ["openingBalance", "Opening Balance"],
            ["allocatedLeave", "Allocated Leave"],
            ["usedLeave", "Used Leave"],
          ].map(([name, label]) => (
            <div className="col-md-4" key={name}>
              <TextBox
                name={name}
                label={label}
                type="number"
                required
                value={form[name]}
                onChange={update}
                error={errors[name]}
                placeholder={`Enter ${label}`}
                disabled={readOnly || saving}
                min="0"
                step="any"
              />
            </div>
          ))}

          {/* Third Row: Pending Leave, Available Balance and Obsolete */}
          {[
            ["pendingLeave", "Pending Leave"],
            ["availableBalance", "Available Balance"],
          ].map(([name, label]) => (
            <div className="col-md-4" key={name}>
              <TextBox
                name={name}
                label={label}
                type="number"
                required
                value={form[name]}
                onChange={update}
                error={errors[name]}
                placeholder={`Enter ${label}`}
                disabled={readOnly || saving}
                min="0"
                step="any"
              />
            </div>
          ))}

          {mode === "edit" && (
            <div className="col-md-4 d-flex align-items-end pb-2">
              <Checkbox
                name="isDeleted"
                label="Obsolete"
                checked={form.isDeleted}
                onChange={update}
                switchStyle
                disabled={saving}
              />
            </div>
          )}
        </div>
      </form>

      <DirtyStateModal
        open={dirtyOpen}
        onStay={() => {
          setDirtyOpen(false);
          pendingActionRef.current = null;
        }}
        onDiscard={discardChanges}
      />
    </div>
  );
}
