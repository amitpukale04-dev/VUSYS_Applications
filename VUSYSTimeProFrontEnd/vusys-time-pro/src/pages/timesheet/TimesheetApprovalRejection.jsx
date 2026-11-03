import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import ConfirmModal from "../../components/common/ConfirmModal";
import DirtyStateModal from "../../components/common/DirtyStateModal";
import SvgIcon from "../../components/common/SvgIcon";
import {
  Dropdown,
  TextArea,
  TextBox,
} from "../../components/forms/FormElements";
import { useAuth } from "../../context/AuthContext";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import useFormValidation from "../../hooks/useFormValidation";
import {
  approveOrRejectTimesheet,
  checkTimesheetApprovalStatus,
  getTimesheetApprovalRejectionList,
} from "../../services/timesheetApprovalRejectionService";
import { getOrExportEmployeeTimesheetList } from "../../services/downloadTimesheetService";
import ToastService from "../../services/toastService";
import { b64Decode } from "../../utils/base64EncodeDecode";
import { exportBase64File } from "../../utils/fileExportUtils";
import { getLoggedInEmployeeCode } from "../../utils/userSession";
import { normalizeNumber, normalizeText } from "../../utils/valueUtils";
import { timesheetApprovalRejectionUrlValidationRules } from "../../validation/timesheetApprovalRejectionValidationRules";
import EmployeeTimesheetDetailsModal from "./EmployeeTimesheetDetailsModal";
import { TIMESHEET_APPROVAL_STATUS_OPTIONS } from "../../constants/constants";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { showApiError } from "../../utils/showApiError";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const createInitialForm = () => ({
  employeeCode: "",
  timesheetMonth: "",
  timesheetYear: "",
  approvalStatus: "",
  remarks: "",
});

const NavigateTimesheetApprovalRejectionList =
  ROUTE_CONSTANTS.MANAGE_TIMESHEET.TIMESHEET_APPROVAL_REJECTION_LIST;

const decodeQueryValue = (value) => {
  const normalized = String(value || "")
    .trim()
    .replace(/ /g, "+");

  return normalized ? String(b64Decode(normalized) || "").trim() : "";
};

const TIMESHEET_APPROVAL_REJECTION_API_FIELD_MAPPINGS = {
  employeeCode: { apiKeys: ["EmployeeCode"] },
  timesheetMonth: { apiKeys: ["TimesheetMonth"] },
  timesheetYear: { apiKeys: ["TimesheetYear"] },
  remarks: { apiKeys: ["Remarks"] },
  approvalStatus: { apiKeys: ["ApproveStatus"] },
};

export default function TimesheetApprovalRejection() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { tabs } = useTabs();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();

  const encodedProjectTimeSheetID = searchParams.get("PTSID") || "";
  const encodedEmailID = searchParams.get("S") || "";
  const encodedUserToken = searchParams.get("ID") || "";
  const encodedTimesheetMonth = searchParams.get("M") || "";
  const encodedTimesheetYear = searchParams.get("Y") || "";

  const decodedUrlData = useMemo(
    () => ({
      projectTimeSheetID: normalizeNumber(
        decodeQueryValue(encodedProjectTimeSheetID),
      ),
      emailID: decodeQueryValue(encodedEmailID),
      userToken: decodeQueryValue(encodedUserToken),
      timesheetMonth: decodeQueryValue(encodedTimesheetMonth),
      timesheetYear: normalizeNumber(decodeQueryValue(encodedTimesheetYear)),
    }),
    [
      encodedProjectTimeSheetID,
      encodedEmailID,
      encodedUserToken,
      encodedTimesheetMonth,
      encodedTimesheetYear,
    ],
  );

  const {
    projectTimeSheetID,
    emailID,
    userToken,
    timesheetMonth,
    timesheetYear,
  } = decodedUrlData;

  const loggedInEmployeeCode = getLoggedInEmployeeCode(user);
  const loggedInEmail = normalizeText(
    user?.email || user?.EmailID || user?.emailID || "",
  );

  const [form, setForm] = useState(createInitialForm);
  const [resolvedRow, setResolvedRow] = useState(null);
  const [loadingInitialData, setLoadingInitialData] = useState(true);
  const [saving, setSaving] = useState(false);
  const [confirmRequest, setConfirmRequest] = useState(null);
  const [timesheetDetailsRows, setTimesheetDetailsRows] = useState([]);
  const [showTimesheetDetails, setShowTimesheetDetails] = useState(false);
  const [urlValid, setUrlValid] = useState(true);
  const [approvalStatusVerified, setApprovalStatusVerified] = useState(false);
  const [dirtyOpen, setDirtyOpen] = useState(false);
  const [allowPageExit, setAllowPageExit] = useState(false);

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(timesheetApprovalRejectionUrlValidationRules);

  const isDirty = Boolean(
    String(form.approvalStatus || "").trim() ||
    String(form.remarks || "").trim(),
  );

  useEffect(() => {
    let active = true;

    const resolveTimesheet = async () => {
      const isValidUrl =
        projectTimeSheetID > 0 &&
        Boolean(emailID) &&
        Boolean(userToken) &&
        Boolean(timesheetMonth) &&
        timesheetYear > 0;

      if (!isValidUrl) {
        setUrlValid(false);
        setApprovalStatusVerified(false);
        setLoadingInitialData(false);
        setForm({
          employeeCode: "",
          timesheetMonth,
          timesheetYear: timesheetYear > 0 ? String(timesheetYear) : "",
          approvalStatus: "",
          remarks: "",
        });
        ToastService.error(
          "Invalid Timesheet Approval/Rejection URL. Required URL parameters are missing or invalid.",
        );
        return;
      }

      const loaderRequestId = showFullPageLoader(
        "Loading Timesheet Approval/Rejection...",
      );

      setLoadingInitialData(true);
      setUrlValid(true);
      setApprovalStatusVerified(false);
      clearErrors();

      try {
        /*
         * The email URL contains ProjectTimeSheetID, EmailID, Month and Year,
         * but it does not contain EmployeeCode. Reuse the existing approval
         * list API to resolve the exact employee row by ProjectTimeSheetID.
         */
        const result = await getTimesheetApprovalRejectionList({
          employeeCode: "0",
          timesheetMonth,
          timesheetYear,
          paramType: "GetAllEmployeeProjectTimesheetForApproval",
        });

        if (!active) return;

        const matchingRow = result.approvalRows.find(
          (item) =>
            Number(item.projectTimeSheetID) === Number(projectTimeSheetID),
        );

        /*
         * Fallback for a valid authenticated employee link when the list API
         * no longer returns the row after a state transition.
         */
        const canUseLoggedInEmployee =
          loggedInEmployeeCode && loggedInEmail === normalizeText(emailID);

        const employeeCode =
          matchingRow?.employeeCode ||
          (canUseLoggedInEmployee ? loggedInEmployeeCode : "");

        if (!employeeCode) {
          throw new Error(
            "Unable to resolve Employee Code for this Timesheet Approval/Rejection URL.",
          );
        }

        /*
         * Before displaying the direct URL approval screen, verify that this
         * Timesheet is still pending and available for approval/rejection.
         */
        await checkTimesheetApprovalStatus({
          employeeCode,
          timesheetMonth,
          timesheetYear,
        });

        if (!active) return;

        const nextRow = matchingRow || {
          projectTimeSheetID,
          employeeCode,
          timeSheetMonth: timesheetMonth,
          timeSheetYear: timesheetYear,
        };

        setResolvedRow(nextRow);
        setForm({
          employeeCode,
          timesheetMonth,
          timesheetYear: String(timesheetYear),
          approvalStatus: "",
          remarks: "",
        });
        setApprovalStatusVerified(true);
      } catch (error) {
        if (!active) return;

        setResolvedRow(null);
        setApprovalStatusVerified(false);

        const isApprovalStatusValidationError =
          error?.isApiValidationError && error?.payload?.data?.ValidationErrors;

        if (
          error?.isTimesheetApprovalStatusError ||
          isApprovalStatusValidationError
        ) {
          const message =
            error?.message || "Unable to verify Timesheet Approval Status.";

          ToastService.error(message);

          /*
           * The email-link screen must not remain available once the backend
           * says the Timesheet is already Approved/Rejected or the status
           * request is invalid.
           */
          window.setTimeout(() => {
            window.close();

            window.setTimeout(() => {
              if (!window.closed) {
                navigate("/", { replace: true });
              }
            }, 150);
          }, 500);

          return;
        }

        if (error?.isApiValidationError) {
          applyApiValidationErrors(
            error.validationErrors || [],
            TIMESHEET_APPROVAL_REJECTION_API_FIELD_MAPPINGS,
            setFieldError,
          );
        } else {
          showApiError(
            error,
            "Unable to load Timesheet Approval/Rejection details.",
          );
        }
      } finally {
        if (active) {
          setLoadingInitialData(false);
        }

        hideFullPageLoader(loaderRequestId);
      }
    };

    resolveTimesheet();

    return () => {
      active = false;
    };
  }, [
    clearErrors,
    projectTimeSheetID,
    emailID,
    userToken,
    timesheetMonth,
    timesheetYear,
    loggedInEmployeeCode,
    loggedInEmail,
    setFieldError,
    hideFullPageLoader,
    showFullPageLoader,
  ]);

  const updateForm = (event) => {
    const { name, value } = event.target;

    setForm((current) => ({
      ...current,
      [name]: value,
    }));

    clearError(name);
  };

  const handleViewTimesheet = async () => {
    if (loadingInitialData || saving || !urlValid || !form.employeeCode) {
      return;
    }

    const loaderRequestId = showFullPageLoader("Loading Timesheet Details...");

    try {
      const result = await getOrExportEmployeeTimesheetList({
        employeeCode: form.employeeCode,
        timesheetMonth: form.timesheetMonth,
        timesheetYear: form.timesheetYear,
        projectTimeSheetID: decodedUrlData.projectTimeSheetID,
        action: "View",
      });

      setTimesheetDetailsRows(result.timesheetRows);
      setShowTimesheetDetails(true);
    } catch (error) {
      setTimesheetDetailsRows([]);

      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors || [],
          TIMESHEET_APPROVAL_REJECTION_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(error, "Unable to load Timesheet Details.");
      }
    } finally {
      hideFullPageLoader(loaderRequestId);
    }
  };

  const handleDownloadTimesheet = async () => {
    if (loadingInitialData || saving || !urlValid || !form.employeeCode) {
      return;
    }

    const loaderRequestId = showFullPageLoader("Downloading Timesheet...");

    try {
      const result = await getOrExportEmployeeTimesheetList({
        employeeCode: form.employeeCode,
        timesheetMonth: form.timesheetMonth,
        timesheetYear: form.timesheetYear,
        projectTimeSheetID: decodedUrlData.projectTimeSheetID,
        action: "Export",
      });

      exportBase64File({
        base64Content: result.exportDetails.fileBase64,
        contentType: result.exportDetails.contentType,
        fileName: result.exportDetails.fileName,
      });
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors || [],
          TIMESHEET_APPROVAL_REJECTION_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(error, "Unable to download Timesheet.");
      }
    } finally {
      hideFullPageLoader(loaderRequestId);
    }
  };

  const requestSaveConfirmation = (event) => {
    event.preventDefault();

    if (loadingInitialData || saving || !urlValid || !resolvedRow) {
      return;
    }

    if (!validate(form)) {
      return;
    }

    const approveStatus = form.approvalStatus === "1" ? "Approve" : "Reject";

    setConfirmRequest({
      approveStatus,
    });
  };

  const confirmApproveReject = async () => {
    if (!confirmRequest || saving) return;

    const approveStatus = confirmRequest.approveStatus;
    const isApprove = approveStatus === "Approve";
    const loaderRequestId = showFullPageLoader(
      isApprove ? "Approving Timesheet..." : "Rejecting Timesheet...",
    );

    setSaving(true);

    try {
      const result = await approveOrRejectTimesheet({
        employeeCode: form.employeeCode,
        timesheetMonth: form.timesheetMonth,
        timesheetYear: form.timesheetYear,
        projectTimeSheetID: decodedUrlData.projectTimeSheetID,
        remarks: form.remarks,
        userToken: decodedUrlData.userToken,
        source: "Email",
        approveStatus,
      });

      ToastService.success(
        result.message ||
          (isApprove
            ? "Timesheet Approved Successfully."
            : "Timesheet Rejected Successfully."),
      );

      setConfirmRequest(null);
      clearErrors();

      /*
       * The approval/rejection action is complete. Disable the browser's
       * unsaved-change protection before closing/navigating away so Chrome
       * does not show its native "Leave site?" confirmation.
       */
      setAllowPageExit(true);

      // Keep the browser tab open after approval or rejection.
      // Return to the Leave Approval/Rejection List only if its application
      // tab is already open; otherwise return to Home.
      const isListTabOpen = tabs.some(
        (tab) => tab.path === NavigateTimesheetApprovalRejectionList,
      );
      window.setTimeout(() => {
        navigate(isListTabOpen ? NavigateTimesheetApprovalRejectionList : "/", {
          replace: true,
        });
      }, 3000);
    } catch (error) {
      setConfirmRequest(null);

      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors || [],
          TIMESHEET_APPROVAL_REJECTION_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(error, "Unable to Approve/Reject Timesheet.");
      }
    } finally {
      setSaving(false);
      hideFullPageLoader(loaderRequestId);
    }
  };

  const performCancel = () => {
    navigate("/");
  };

  const handleCancel = () => {
    if (isDirty) {
      setDirtyOpen(true);
      return;
    }

    performCancel();
  };

  const confirmMessage =
    confirmRequest?.approveStatus === "Approve"
      ? "Are You Sure You Want To Approve This Timesheet ?"
      : "Are You Sure You Want To Reject This Timesheet ?";

  useEffect(() => {
    if (!isDirty || allowPageExit) return undefined;

    const handleBeforeUnload = (event) => {
      event.preventDefault();
      event.returnValue = "";
    };

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [isDirty, allowPageExit]);

  if (loadingInitialData || !approvalStatusVerified) {
    return null;
  }

  return (
    <div className="tp-employee-form-page tp-timesheet-approval-rejection-url-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="timesheetApproval" size={18} />
          <h1>Timesheet Approval/Rejection</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          <button
            type="submit"
            form="timesheet-approval-rejection-url-form"
            className="tp-form-page-icon-button"
            disabled={loadingInitialData || saving || !urlValid || !resolvedRow}
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
        id="timesheet-approval-rejection-url-form"
        className="tp-card tp-employee-form-card tp-employee-form-card-attached tp-timesheet-approval-rejection-url-card"
        onSubmit={requestSaveConfirmation}
        noValidate
      >
        <div className="row g-3">
          <div className="col-md-4">
            <TextBox
              name="employeeCode"
              label="Employee Code"
              required
              value={form.employeeCode}
              error={errors.employeeCode}
              readOnly
              disabled={loadingInitialData}
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="timesheetMonth"
              label="Timesheet Month"
              required
              value={form.timesheetMonth}
              error={errors.timesheetMonth}
              readOnly
              disabled={loadingInitialData}
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="timesheetYear"
              label="Timesheet Year"
              required
              value={form.timesheetYear}
              error={errors.timesheetYear}
              readOnly
              disabled={loadingInitialData}
            />
          </div>

          <div className="col-md-4">
            <Dropdown
              name="approvalStatus"
              label="Approval Status"
              required
              value={form.approvalStatus}
              onChange={updateForm}
              options={TIMESHEET_APPROVAL_STATUS_OPTIONS}
              error={errors.approvalStatus}
              placeholder="Select approval status"
              disabled={loadingInitialData || saving || !urlValid}
            />
          </div>

          <div className="col-md-8">
            <TextArea
              name="remarks"
              label="Remarks"
              required
              value={form.remarks}
              onChange={updateForm}
              error={errors.remarks}
              placeholder="Enter remarks"
              rows={4}
              disabled={loadingInitialData || saving || !urlValid}
            />
          </div>
        </div>

        <div className="tp-timesheet-approval-rejection-url-links">
          <button
            type="button"
            className="tp-report-link-button"
            onClick={handleViewTimesheet}
            disabled={
              loadingInitialData || saving || !urlValid || !form.employeeCode
            }
            title="View Timesheet"
          >
            <SvgIcon name="viewTimesheet" size={16} />
            View Timesheet
          </button>

          <button
            type="button"
            className="tp-report-link-button"
            onClick={handleDownloadTimesheet}
            disabled={
              loadingInitialData || saving || !urlValid || !form.employeeCode
            }
            title="Download Timesheet"
          >
            <SvgIcon name="downloadTimesheet" size={16} />
            Download Timesheet
          </button>
        </div>
      </form>

      <ConfirmModal
        show={Boolean(confirmRequest)}
        title={
          confirmRequest?.approveStatus === "Approve"
            ? "Approve Timesheet"
            : "Reject Timesheet"
        }
        message={confirmMessage}
        confirmText="Yes, Confirm"
        cancelText="No"
        loading={saving}
        onConfirm={confirmApproveReject}
        onCancel={() => {
          if (!saving) {
            setConfirmRequest(null);
          }
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

      <EmployeeTimesheetDetailsModal
        show={showTimesheetDetails}
        rows={timesheetDetailsRows}
        onClose={() => {
          setShowTimesheetDetails(false);
          setTimesheetDetailsRows([]);
        }}
      />
    </div>
  );
}
