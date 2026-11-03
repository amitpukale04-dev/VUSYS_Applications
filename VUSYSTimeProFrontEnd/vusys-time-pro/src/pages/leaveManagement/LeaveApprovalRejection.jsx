import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import AccessDenied from "../../components/common/AccessDenied";
import ConfirmModal from "../../components/common/ConfirmModal";
import DirtyStateModal from "../../components/common/DirtyStateModal";
import SvgIcon from "../../components/common/SvgIcon";
import {
  Dropdown,
  TextArea,
  TextBox,
} from "../../components/forms/FormElements";
import { DROPDOWN_SOURCE_TABLE } from "../../constants/constants";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import useFormValidation from "../../hooks/useFormValidation";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { getDropdownItems } from "../../services/dropdownService";
import {
  approveOrRejectLeaveRequest,
  checkLeaveRequestApprovalStatus,
  getLeaveApprovalRequestDetails,
} from "../../services/leaveApprovalRejectionService";
import ToastService from "../../services/toastService";
import { b64Decode } from "../../utils/base64EncodeDecode";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { normalizeNumber } from "../../utils/valueUtils";
import { leaveApproveRejectValidationRules } from "../../validation/leaveApprovalRejectionValidationRules";
import LeaveDetailsPopup from "./LeaveDetailsPopup";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

// ============================================================
// LEAVE APPROVAL / REJECTION - EMAIL URL HELPERS
// ============================================================

const decodeQueryValue = (value) => {
  const normalized = String(value || "")
    .trim()
    .replace(/ /g, "+");
  return normalized ? String(b64Decode(normalized) || "").trim() : "";
};

const NavigateLeaveApprovalRejectionList =
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_APPROVAL_REJECTION_LIST;

const createInitialForm = (employeeCode = "", employeeName = "") => ({
  employeeCode,
  employeeName,
  approvalStatus: "",
  remarks: "",
});

const LEAVE_APPROVAL_REJECTION_API_FIELD_MAPPINGS = {
  employeeCode: { apiKeys: ["EmployeeCode"] },
  approvalStatus: { apiKeys: ["ActionType"] },
  actionBy: { apiKeys: ["ActionBy"] },
  remarks: { apiKeys: ["Remarks"] },
};

// ============================================================
// LEAVE APPROVAL / REJECTION - DIRECT EMAIL LINK
// ============================================================

export default function LeaveApprovalRejection() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { tabs } = useTabs();
  const { hasPermission, isLoading: securityPermissionLoading } =
    useUserSecurityPermission();
  const canApproveLeave = hasPermission(
    "Leave Approval/Rejection List",
    "Approve",
  );
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();

  const encodedId = searchParams.get("ID") || "";
  const encodedEmployeeCode = searchParams.get("EC") || "";
  const encodedEmployeeName = searchParams.get("EN") || "";

  const urlData = useMemo(
    () => ({
      id: normalizeNumber(decodeQueryValue(encodedId)),
      employeeCode: decodeQueryValue(encodedEmployeeCode),
      employeeName: decodeQueryValue(encodedEmployeeName),
    }),
    [encodedId, encodedEmployeeCode, encodedEmployeeName],
  );

  const [form, setForm] = useState(() =>
    createInitialForm(urlData.employeeCode, urlData.employeeName),
  );
  const [approvalOptions, setApprovalOptions] = useState([]);
  const [loadingInitialData, setLoadingInitialData] = useState(true);
  const [approvalStatusVerified, setApprovalStatusVerified] = useState(false);
  const [initialError, setInitialError] = useState("");
  const [saving, setSaving] = useState(false);
  const [confirmRequest, setConfirmRequest] = useState(null);
  const [leaveDetailsRow, setLeaveDetailsRow] = useState(null);
  const [loadingDetails, setLoadingDetails] = useState(false);
  const [dirtyOpen, setDirtyOpen] = useState(false);
  const [allowPageExit, setAllowPageExit] = useState(false);

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(leaveApproveRejectValidationRules);

  const isDirty = Boolean(form.approvalStatus || form.remarks.trim());
  const canSubmit =
    canApproveLeave && approvalStatusVerified && !loadingInitialData && !saving;

  // ==========================================================
  // INITIAL LOAD - VERIFY PENDING STATUS / LOAD DROPDOWN
  // ==========================================================

  useEffect(() => {
    if (securityPermissionLoading || !canApproveLeave) {
      if (!securityPermissionLoading) setLoadingInitialData(false);
      return undefined;
    }

    let active = true;
    const initialize = async () => {
      setForm(createInitialForm(urlData.employeeCode, urlData.employeeName));
      setApprovalStatusVerified(false);
      setInitialError("");
      clearErrors();

      if (urlData.id <= 0 || !urlData.employeeCode || !urlData.employeeName) {
        setInitialError(
          "Invalid Leave Approval/Rejection URL. Required URL parameters are missing or invalid.",
        );
        setLoadingInitialData(false);
        return;
      }

      const loaderId = showFullPageLoader(
        "Loading Leave Approval/Rejection...",
      );
      setLoadingInitialData(true);
      try {
        // The backend is the source of truth for the current leave approval status.
        const [, dropdown] = await Promise.all([
          checkLeaveRequestApprovalStatus({
            id: urlData.id,
            employeeCode: urlData.employeeCode,
          }),
          getDropdownItems({
            sourceTable: DROPDOWN_SOURCE_TABLE.LEAVE_APPROVE_REJECT_STATUS,
            argumentValue: "",
            argumentValue1: "",
          }),
        ]);

        if (!active) return;
        setApprovalOptions(dropdown.options || []);
        setApprovalStatusVerified(true);
      } catch (error) {
        if (!active) return;
        setApprovalStatusVerified(false);
        setInitialError(
          error?.message || "Unable to verify Leave Request Approval Status.",
        );
        if (error?.isApiValidationError) {
          applyApiValidationErrors(
            error.validationErrors || [],
            LEAVE_APPROVAL_REJECTION_API_FIELD_MAPPINGS,
            setFieldError,
          );
        } else {
          showApiError(error, "Unable to load Leave Approval/Rejection.");
        }
      } finally {
        if (active) setLoadingInitialData(false);
        hideFullPageLoader(loaderId);
      }
    };

    void initialize();
    return () => {
      active = false;
    };
  }, [
    securityPermissionLoading,
    canApproveLeave,
    urlData,
    showFullPageLoader,
    hideFullPageLoader,
    clearErrors,
    setFieldError,
  ]);

  // ==========================================================
  // FORM / LEAVE DETAILS
  // ==========================================================

  const updateForm = (event) => {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
    clearError(name);
  };

  const handleViewLeaveDetails = async () => {
    if (!canSubmit || loadingDetails) return;
    const loaderId = showFullPageLoader("Loading Leave Details...");
    setLoadingDetails(true);
    try {
      const details = await getLeaveApprovalRequestDetails({
        id: urlData.id,
        employeeCode: urlData.employeeCode,
      });
      setLeaveDetailsRow(details);
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors || [],
          LEAVE_APPROVAL_REJECTION_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(error, "Unable to load Leave Request Details.");
      }
    } finally {
      setLoadingDetails(false);
      hideFullPageLoader(loaderId);
    }
  };

  // ==========================================================
  // SAVE / CONFIRM APPROVE OR REJECT
  // ==========================================================

  const requestSaveConfirmation = (event) => {
    event.preventDefault();
    if (!canSubmit || !validate(form)) return;
    if (form.approvalStatus !== "2" && form.approvalStatus !== "3") {
      setFieldError("approvalStatus", "Select A Valid Approval Status.");
      return;
    }
    setConfirmRequest({
      actionType: form.approvalStatus === "2" ? "APPROVE" : "REJECT",
    });
  };

  const confirmApproveReject = async () => {
    if (!confirmRequest || !canSubmit) return;
    const isApprove = confirmRequest.actionType === "APPROVE";
    const loaderId = showFullPageLoader(
      isApprove ? "Approving Leave Request..." : "Rejecting Leave Request...",
    );
    setSaving(true);
    try {
      const message = await approveOrRejectLeaveRequest({
        id: urlData.id,
        employeeCode: form.employeeCode,
        actionType: confirmRequest.actionType,
        remarks: form.remarks.trim(),
      });
      ToastService.success(message);
      setConfirmRequest(null);
      setApprovalStatusVerified(false);
      setAllowPageExit(true);
      clearErrors();

      // Keep the browser tab open after approval or rejection.
      // Return to the Leave Approval/Rejection List only if its application
      // tab is already open; otherwise return to Home.
      const isListTabOpen = tabs.some(
        (tab) => tab.path === NavigateLeaveApprovalRejectionList,
      );
      window.setTimeout(() => {
        navigate(isListTabOpen ? NavigateLeaveApprovalRejectionList : "/", {
          replace: true,
        });
      }, 3000);
    } catch (error) {
      setConfirmRequest(null);
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors || [],
          LEAVE_APPROVAL_REJECTION_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(error, "Unable to Approve/Reject Leave Request.");
      }
    } finally {
      setSaving(false);
      hideFullPageLoader(loaderId);
    }
  };

  const performCancel = () => navigate("/");
  const handleCancel = () => {
    if (isDirty) setDirtyOpen(true);
    else performCancel();
  };

  useEffect(() => {
    if (!isDirty || allowPageExit) return undefined;
    const handleBeforeUnload = (event) => {
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [isDirty, allowPageExit]);

  // ==========================================================
  // UI
  // ==========================================================

  if (securityPermissionLoading) {
    return (
      <div className="tp-form-page-loading">
        <span className="spinner-border text-primary" aria-hidden="true" />
        <p>Loading security permissions...</p>
      </div>
    );
  }

  if (!canApproveLeave) return <AccessDenied />;

  return (
    <div className="tp-employee-form-page tp-leave-approval-rejection-url-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="ApproveRejectIcon" size={18} />
          <h1>Leave Approval/Rejection</h1>
        </div>
        <div className="tp-form-page-heading-actions">
          <button
            type="submit"
            form="leave-approval-rejection-url-form"
            className="tp-form-page-icon-button"
            disabled={!canSubmit}
            title="Save"
            aria-label="Save"
          >
            {saving ? (
              <span
                className="spinner-border spinner-border-sm"
                aria-hidden="true"
              />
            ) : (
              <SvgIcon name="save" size={18} />
            )}
          </button>
          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={handleCancel}
            disabled={saving}
            title="Cancel"
            aria-label="Cancel"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>
      </div>

      <form
        id="leave-approval-rejection-url-form"
        className="tp-card tp-employee-form-card tp-employee-form-card-attached tp-leave-approval-rejection-url-card"
        onSubmit={requestSaveConfirmation}
        noValidate
      >
        {initialError && !loadingInitialData && (
          <div className="alert alert-warning" role="alert">
            {initialError}
          </div>
        )}
        {errors.actionBy && (
          <div className="alert alert-danger" role="alert">
            {errors.actionBy}
          </div>
        )}
        <div className="row g-3">
          <div className="col-md-4">
            <TextBox
              name="employeeCode"
              label="Employee Code"
              required
              value={form.employeeCode}
              error={errors.employeeCode}
              readOnly
            />
          </div>
          <div className="col-md-4">
            <TextBox
              name="employeeName"
              label="Employee Name"
              required
              value={form.employeeName}
              error={errors.employeeName}
              readOnly
            />
          </div>
          <div className="col-md-4">
            <Dropdown
              name="approvalStatus"
              label="Approval Status"
              required
              value={form.approvalStatus}
              onChange={updateForm}
              options={approvalOptions}
              error={errors.approvalStatus}
              placeholder="Select approval status"
              disabled={!canSubmit}
            />
          </div>
          <div className="col-md-12">
            <TextArea
              name="remarks"
              label="Remarks"
              required
              value={form.remarks}
              onChange={updateForm}
              error={errors.remarks}
              placeholder="Enter remarks"
              rows={4}
              disabled={!canSubmit}
            />
          </div>
        </div>

        <div className="tp-leave-approval-rejection-url-links">
          <button
            type="button"
            className="tp-report-link-button"
            onClick={handleViewLeaveDetails}
            disabled={!canSubmit || loadingDetails}
            title="View Leave Details"
          >
            <SvgIcon name="viewTimesheet" size={16} />
            View Leave Details
          </button>
        </div>
      </form>

      <ConfirmModal
        show={Boolean(confirmRequest)}
        title={
          confirmRequest?.actionType === "APPROVE"
            ? "Approve Leave Request"
            : "Reject Leave Request"
        }
        message={
          confirmRequest?.actionType === "APPROVE"
            ? "Are You Sure You Want To Approve This Leave Request ?"
            : "Are You Sure You Want To Reject This Leave Request ?"
        }
        confirmText="Yes, Confirm"
        cancelText="No"
        loading={saving}
        onConfirm={confirmApproveReject}
        onCancel={() => {
          if (!saving) setConfirmRequest(null);
        }}
      />

      <DirtyStateModal
        open={dirtyOpen}
        onStay={() => setDirtyOpen(false)}
        onDiscard={() => {
          setDirtyOpen(false);
          performCancel();
        }}
      />

      <LeaveDetailsPopup
        show={Boolean(leaveDetailsRow)}
        row={leaveDetailsRow}
        onClose={() => setLeaveDetailsRow(null)}
      />
    </div>
  );
}
