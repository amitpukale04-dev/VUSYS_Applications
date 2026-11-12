import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  Checkbox,
  SearchableDropdown,
  TextArea,
  TextBox,
} from "../../components/forms/FormElements";
import HolidayAndTaskDatePicker from "../../components/forms/HolidayAndTaskDatePicker";
import SvgIcon from "../../components/common/SvgIcon";
import DirtyStateModal from "../../components/common/DirtyStateModal";
import AccessDenied from "../../components/common/AccessDenied";
import ToastService from "../../services/toastService";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { toDateOnly } from "../../utils/dateUtils";
import { getDropdownItems } from "../../services/dropdownService";
import {
  applyLeaveRequest,
  calculateAndGetTotalNoofLeaveDays,
  getCurrentYearLeaveBalance,
  getMyLeaveRequestDetails,
} from "../../services/myLeaveRequestService";
import useFormValidation from "../../hooks/useFormValidation";
import { myLeaveRequestValidationRules } from "../../validation/myLeaveRequestValidationRules";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { useAuth } from "../../context/AuthContext";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import { decodeRouteId } from "../../utils/routeIdUtils";
import {
  getCreatedOrUpdatedBy,
  getLoggedInEmployeeCode,
} from "../../utils/userSession";
import { normalizeNumber } from "../../utils/valueUtils";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const blankForm = {
  employeeCode: "",
  leaveTypeID: "",
  currentLeaveBalance: "",
  fromDate: "",
  toDate: "",
  isHalfDay: false,
  totalLeaveDays: 0,
  reason: "",
};

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());
const NavigateMyLeaveRequest =
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS;

const MY_LEAVE_REQUEST_API_FIELD_MAPPINGS = {
  employeeCode: { apiKeys: ["EmployeeCode"] },
  leaveTypeID: { apiKeys: ["LeaveTypeID"] },
  fromDate: { apiKeys: ["FromDate"] },
  toDate: { apiKeys: ["ToDate"] },
  reason: { apiKeys: ["Reason"] },
};

// ============================================================
// APPLY / VIEW LEAVE REQUEST - NO EDIT MODE
// ============================================================

export default function MyLeaveRequestFormPage() {
  const { leaveRequestId } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { registerTabDirtyState } = useTabs();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();
  const isAddMode = !leaveRequestId || leaveRequestId === "new";
  const decodedId = isAddMode ? 0 : decodeRouteId(leaveRequestId);
  const employeeCode = getLoggedInEmployeeCode(user);
  const hasCreatePermission = hasPermission("My Leave Requests", "Apply");
  const hasViewPermission = hasPermission("My Leave Requests", "View");
  const [form, setForm] = useState({ ...blankForm, employeeCode });
  const [leaveTypes, setLeaveTypes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [dirtyOpen, setDirtyOpen] = useState(false);
  const originalRef = useRef("");
  const pendingActionRef = useRef(null);
  const balanceRequestRef = useRef(0);
  const leaveDaysRequestRef = useRef(0);
  const [calculatingLeaveDays, setCalculatingLeaveDays] = useState(false);
  const [leaveDaysCalculationFailed, setLeaveDaysCalculationFailed] =
    useState(false);
  const { errors, validate, clearError, setFieldError } = useFormValidation(
    myLeaveRequestValidationRules,
  );
  const isDirty = useMemo(
    () =>
      Boolean(originalRef.current) &&
      JSON.stringify(form) !== originalRef.current,
    [form],
  );

  useEffect(() => {
    registerTabDirtyState(NavigateMyLeaveRequest, isAddMode && isDirty);
    return () => registerTabDirtyState(NavigateMyLeaveRequest, false);
  }, [isAddMode, isDirty, registerTabDirtyState]);

  // ============================================================
  // LOAD LEAVE REQUEST DETAILS WITH FULL PAGE LOADER
  // ============================================================

  useEffect(() => {
    let active = true;
    const loaderRequestId = showFullPageLoader(
      "Loading Leave Request Details...",
    );

    const load = async () => {
      setLoading(true);

      try {
        const [dropdown, details] = await Promise.all([
          getDropdownItems({
            sourceTable: "LeaveType",
            argumentValue: "",
            argumentValue1: "",
          }),
          isAddMode
            ? Promise.resolve(null)
            : getMyLeaveRequestDetails(decodedId),
        ]);
        if (!active) return;
        setLeaveTypes(
          (dropdown.options || []).map((option) => ({
            ...option,
            value: String(option.value),
          })),
        );
        const nextForm = details
          ? {
              ...blankForm,
              ...details,
              fromDate: toDateOnly(details.fromDate),
              toDate: toDateOnly(details.toDate),
              leaveTypeID: String(details.leaveTypeID),
            }
          : { ...blankForm, employeeCode };
        setForm(nextForm);
        originalRef.current = JSON.stringify(nextForm);
      } catch (error) {
        if (!active) return;
        showApiError(error, "Unable to load Leave Request Details.");
        if (!isAddMode) navigate(NavigateMyLeaveRequest, { replace: true });
      } finally {
        hideFullPageLoader(loaderRequestId);

        if (active) {
          setLoading(false);
        }
      }
    };

    void load();

    return () => {
      active = false;
      hideFullPageLoader(loaderRequestId);
    };
  }, [
    decodedId,
    employeeCode,
    hideFullPageLoader,
    isAddMode,
    navigate,
    showFullPageLoader,
  ]);

  // ============================================================
  // LOAD CURRENT LEAVE BALANCE - APPLY / VIEW MODE
  // ============================================================

  useEffect(() => {
    const selectedEmployeeCode = isAddMode ? employeeCode : form.employeeCode;
    if (!form.leaveTypeID || !selectedEmployeeCode) return;
    const requestId = ++balanceRequestRef.current;
    getCurrentYearLeaveBalance(selectedEmployeeCode, form.leaveTypeID)
      .then((balance) => {
        if (requestId === balanceRequestRef.current)
          setForm((current) => ({
            ...current,
            currentLeaveBalance: String(balance),
          }));
      })
      .catch((error) => {
        if (requestId !== balanceRequestRef.current) return;

        setForm((current) => ({ ...current, currentLeaveBalance: "" }));

        showApiError(error, "Unable to load Current Leave Balance.");
      });

    return () => {
      balanceRequestRef.current += 1;
    };
  }, [employeeCode, form.employeeCode, form.leaveTypeID, isAddMode]);

  // ============================================================
  // CALCULATE TOTAL LEAVE DAYS FROM API - APPLY MODE ONLY
  // ============================================================

  useEffect(() => {
    if (!isAddMode || loading) return;

    const requestId = ++leaveDaysRequestRef.current;
    const hasValidDates =
      form.fromDate && form.toDate && form.toDate >= form.fromDate;
    const canCalculate =
      employeeCode && normalizeNumber(form.leaveTypeID) > 0 && hasValidDates;

    if (!canCalculate) {
      setCalculatingLeaveDays(false);
      setLeaveDaysCalculationFailed(false);
      setForm((current) =>
        current.totalLeaveDays === 0
          ? current
          : { ...current, totalLeaveDays: 0 },
      );
      return;
    }

    setCalculatingLeaveDays(true);
    setLeaveDaysCalculationFailed(false);
    setForm((current) => ({ ...current, totalLeaveDays: 0 }));

    calculateAndGetTotalNoofLeaveDays({
      employeeCode,
      leaveTypeID: form.leaveTypeID,
      fromDate: form.fromDate,
      toDate: form.toDate,
      isHalfDay: form.isHalfDay,
    })
      .then((result) => {
        if (requestId !== leaveDaysRequestRef.current) return;
        setForm((current) => ({
          ...current,
          totalLeaveDays: result.totalLeaveDays,
        }));
        setLeaveDaysCalculationFailed(false);
      })
      .catch((error) => {
        if (requestId !== leaveDaysRequestRef.current) return;
        setLeaveDaysCalculationFailed(true);
        setForm((current) => ({ ...current, totalLeaveDays: 0 }));
        showApiError(error, "Unable to calculate Total No of Leave Days.");
      })
      .finally(() => {
        if (requestId === leaveDaysRequestRef.current)
          setCalculatingLeaveDays(false);
      });

    return () => {
      leaveDaysRequestRef.current += 1;
    };
  }, [
    employeeCode,
    form.leaveTypeID,
    form.fromDate,
    form.toDate,
    form.isHalfDay,
    isAddMode,
    loading,
  ]);

  const update = (event) => {
    const { name, type, checked, value } = event.target;
    setForm((current) => ({
      ...current,
      [name]: type === "checkbox" ? checked : value,
      ...(name === "leaveTypeID" ? { currentLeaveBalance: "" } : {}),
    }));
    clearError(name);
  };

  const runWithDirtyGuard = (action) => {
    if (isAddMode && isDirty) {
      pendingActionRef.current = action;
      setDirtyOpen(true);
    } else action();
  };

  const save = async (event) => {
    event.preventDefault();
    if (!isAddMode || saving) return;
    if (!validate(form)) return;
    const loaderId = showFullPageLoader("Submitting Leave Request...");
    setSaving(true);
    try {
      const message = await applyLeaveRequest({
        ...form,
        employeeCode,
        createdBy: loggedInUserId,
      });
      ToastService.success(message);
      originalRef.current = JSON.stringify(form);
      navigate(NavigateMyLeaveRequest);
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors || [],
          MY_LEAVE_REQUEST_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(error, "Unable to submit Leave Request.");
      }
    } finally {
      hideFullPageLoader(loaderId);
      setSaving(false);
    }
  };

  if (securityAccessLoading)
    return (
      <div className="tp-form-page-loading">
        <span className="spinner-border text-primary" /> Loading security
        permissions...
      </div>
    );
  if (isAddMode ? !hasCreatePermission : !hasViewPermission)
    return <AccessDenied />;
  if (loading) {
    return null;
  }

  return (
    <div className="tp-screen-form-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="leaveType" size={18} />
          <h1>{isAddMode ? "Apply Leave" : "View Leave Request"}</h1>
        </div>
        <div className="tp-form-page-heading-actions">
          {isAddMode && (
            <button
              type="submit"
              form="my-leave-request-form"
              className="tp-form-page-icon-button"
              disabled={saving}
              title="Submit Leave Request"
            >
              {saving ? (
                <span className="spinner-border spinner-border-sm" />
              ) : (
                <SvgIcon name="save" size={18} />
              )}
            </button>
          )}
          <button
            type="button"
            className="tp-form-page-icon-button"
            title="Cancel"
            onClick={() =>
              runWithDirtyGuard(() => navigate(NavigateMyLeaveRequest))
            }
          >
            <SvgIcon name="close" size={18} />
          </button>
          <button
            type="button"
            className="tp-form-page-icon-button"
            title="Back to My Leave Requests"
            onClick={() =>
              runWithDirtyGuard(() => navigate(NavigateMyLeaveRequest))
            }
          >
            <SvgIcon name="back" size={18} />
          </button>
        </div>
      </div>
      <form
        id="my-leave-request-form"
        className="tp-card tp-form-card tp-form-card-attached"
        onSubmit={save}
        noValidate
      >
        <div className="row g-3">
          {/* First Row: Employee, Leave Type, From Date and To Date */}
          <div className="col-md-3">
            <TextBox
              name="employeeCode"
              label="Employee Code"
              required
              value={form.employeeCode}
              error={errors.employeeCode}
              disabled
            />
          </div>
          <div className="col-md-3">
            <SearchableDropdown
              name="leaveTypeID"
              label="Leave Type"
              required
              value={form.leaveTypeID}
              onChange={update}
              options={leaveTypes}
              columns={[{ key: "label", label: "Leave Type" }]}
              error={errors.leaveTypeID}
              disabled={!isAddMode || saving}
              placeholder="Search and Select Leave Type"
            />
          </div>
          <div className="col-md-3">
            <HolidayAndTaskDatePicker
              name="fromDate"
              label="From Date"
              required
              value={form.fromDate}
              onChange={update}
              showHolidays={true}
              disableHolidays={true}
              showSaturday={true}
              showSunday={true}
              disableSaturday={false}
              disableSunday={false}
              error={errors.fromDate}
              disabled={!isAddMode || saving}
            />
          </div>
          <div className="col-md-3">
            <HolidayAndTaskDatePicker
              name="toDate"
              label="To Date"
              required
              value={form.toDate}
              onChange={update}
              showHolidays={true}
              disableHolidays={true}
              showSaturday={true}
              showSunday={true}
              disableSaturday={false}
              disableSunday={false}
              error={errors.toDate}
              disabled={!isAddMode || saving}
            />
          </div>
          {/* Second Row: Current Leave Balance, Total Leave Days and Half Day */}
          <div className="col-md-3">
            <TextBox
              name="currentLeaveBalance"
              required
              label="Current Leave Balance"
              value={form.currentLeaveBalance}
              disabled
            />
          </div>
          <div className="col-md-3">
            <TextBox
              name="totalLeaveDays"
              required
              label="Total No of Leave Days"
              value={String(form.totalLeaveDays)}
              error={errors.totalLeaveDays}
              disabled
            />
          </div>
          <div className="col-md-4 d-flex align-items-start pt-md-4">
            <Checkbox
              name="isHalfDay"
              label="Half Day"
              checked={form.isHalfDay}
              onChange={update}
              switchStyle
              disabled={!isAddMode || saving}
            />
          </div>
          {/* Third Row: Total Leave Days and Leave Reason */}
          <div className="col-md-12">
            <TextArea
              name="reason"
              label="Leave Reason"
              required
              rows={3}
              value={form.reason}
              onChange={update}
              error={errors.reason}
              disabled={!isAddMode || saving}
              placeholder="Enter Leave Reason"
            />
          </div>
        </div>
      </form>
      <DirtyStateModal
        open={dirtyOpen}
        onStay={() => {
          setDirtyOpen(false);
          pendingActionRef.current = null;
        }}
        onDiscard={() => {
          setDirtyOpen(false);
          pendingActionRef.current?.();
          pendingActionRef.current = null;
        }}
      />
    </div>
  );
}
