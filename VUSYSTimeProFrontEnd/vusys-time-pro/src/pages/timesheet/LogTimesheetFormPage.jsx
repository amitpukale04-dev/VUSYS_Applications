import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import ConfirmModal from "../../components/common/ConfirmModal";
import DirtyStateModal from "../../components/common/DirtyStateModal";
import SortableTable from "../../components/common/SortableTable";
import SvgIcon from "../../components/common/SvgIcon";
import {
  Checkbox,
  SearchableDropdown,
  TextBox,
  TextEditor,
} from "../../components/forms/FormElements";
import HolidayAndTaskDatePicker from "../../components/forms/HolidayAndTaskDatePicker";
import {
  COMMON_MESSAGE,
  DROPDOWN_SOURCE_TABLE,
} from "../../constants/constants";
import { useAuth } from "../../context/AuthContext";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import useFormValidation from "../../hooks/useFormValidation";
import { getDropdownItems } from "../../services/dropdownService";
import {
  getTimesheetDetails,
  saveAndUpdateTimesheetDetails,
} from "../../services/logTimesheetService";
import ToastService from "../../services/toastService";
import { decodeRouteId } from "../../utils/routeIdUtils";
import {
  getCreatedOrUpdatedBy,
  getLoggedInEmployeeCode,
} from "../../utils/userSession";
import { normalizeNumber, normalizeText } from "../../utils/valueUtils";
import { formatDDMMMYYYY } from "../../utils/dateUtils";
import { logTimesheetTaskValidationRules } from "../../validation/logTimesheetValidationRules";
import { showApiError } from "../../utils/showApiError";
import { applyApiArrayValidationErrors } from "../../utils/CustomAPIValidation";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const TIMESHEET_API_FIELD_MAPPINGS = {
  projectTimeSheetID: { apiKeys: ["ProjectTimeSheetID"] },
  projectTimeSheetDetailID: { apiKeys: ["ProjectTimeSheetDetailID"] },
  employeeCode: { apiKeys: ["EmployeeCode"] },
  projectID: { apiKeys: ["ProjectID"] },
  projectActivityID: { apiKeys: ["ProjectActivityID"] },
  taskDate: { apiKeys: ["TaskDate"] },
  taskDescription: { apiKeys: ["TaskDescription"] },
  taskDescriptionRaw: { apiKeys: ["TaskDescriptionRaw"] },
  noofHoursWorked: { apiKeys: ["NoofHoursWorked"] },
};

const blankTask = {
  projectID: "",
  projectActivityID: "",
  taskDate: "",
  noofHoursWorked: "",
  isOnLeave: false,
  taskDescriptionRaw: "",
};

const NavigateLogTimesheet = ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET;

const isLeaveProjectName = (name) => {
  const value = normalizeText(name);
  return value === "timeaway" || value === "timeway";
};

const buildLeaveDescription = (activityName) => {
  const label = String(activityName || "").trim();
  if (!label) return "";
  const display =
    normalizeText(label) === "on leave"
      ? "On Leave"
      : normalizeText(label) === "compoff"
        ? "Compoff"
        : label.charAt(0).toUpperCase() + label.slice(1).toLowerCase();
  return `<ol><li>&nbsp;${display}</li></ol>`;
};

const htmlToNumberedText = (html) => {
  const source = String(html || "").trim();
  if (!source) return "";
  const wrapper = document.createElement("div");
  wrapper.innerHTML = source;
  const listItems = [...wrapper.querySelectorAll("li")]
    .map((item) =>
      String(item.textContent || "")
        .replace(/\u00a0/g, " ")
        .trim(),
    )
    .filter(Boolean);
  if (listItems.length) {
    return listItems.map((item, index) => `${index + 1}. ${item}`).join("\n\n");
  }

  return String(wrapper.textContent || "")
    .replace(/\u00a0/g, " ")
    .trim();
};

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

const createComparableRows = (rows) =>
  JSON.stringify(
    (Array.isArray(rows) ? rows : []).map((row) => ({
      projectTimeSheetID: row.projectTimeSheetID,
      projectTimeSheetDetailID: row.projectTimeSheetDetailID,
      projectID: row.projectID,
      projectActivityID: row.projectActivityID,
      taskDate: row.taskDate,
      taskDescription: row.taskDescription,
      taskDescriptionRaw: row.taskDescriptionRaw,
      noofHoursWorked: String(row.noofHoursWorked),
      isOnLeave: Boolean(row.isOnLeave),
      isDeleted: Boolean(row.isDeleted),
    })),
  );

export default function LogTimesheetFormPage() {
  const { timesheetId } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { registerTabDirtyState } = useTabs();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();

  const employeeCode = getLoggedInEmployeeCode(user);
  const userId = loggedInUserId;
  const decodedTimesheetId = timesheetId ? decodeRouteId(timesheetId) : 0;
  const isAddMode = !timesheetId;
  const isViewMode = location.state?.mode === "view";
  const readOnly = isViewMode;

  const [task, setTask] = useState(blankTask);
  const [taskRows, setTaskRows] = useState([]);
  const [projectOptions, setProjectOptions] = useState([]);
  const [activityOptions, setActivityOptions] = useState([]);
  const [editingLocalKey, setEditingLocalKey] = useState(null);
  const [deleteRow, setDeleteRow] = useState(null);
  const [loading, setLoading] = useState(!isAddMode);
  const [saving, setSaving] = useState(false);
  const [rowApiValidationErrors, setRowApiValidationErrors] = useState({});
  const [dirtyOpen, setDirtyOpen] = useState(false);
  const [pendingDirtyAction, setPendingDirtyAction] = useState(null);
  const originalRowsRef = useRef("[]");

  const selectedProject = useMemo(
    () =>
      projectOptions.find(
        (option) => String(option.value) === String(task.projectID),
      ) || null,
    [projectOptions, task.projectID],
  );

  const isOnLeaveProject = isLeaveProjectName(selectedProject?.label);
  const allowZeroHours = Boolean(isOnLeaveProject && task.projectActivityID);

  const taskValidationRules = useMemo(
    () => ({
      ...logTimesheetTaskValidationRules,
      noofHoursWorked: {
        ...logTimesheetTaskValidationRules.noofHoursWorked,
        minValue: allowZeroHours ? 0 : 0.01,
        messages: {
          ...logTimesheetTaskValidationRules.noofHoursWorked.messages,
          minValue: allowZeroHours
            ? "No. of Hours Should Be 0 Or Greater."
            : "No. of Hours Should Be Greater Than 0.",
        },
      },
    }),
    [allowZeroHours],
  );

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(taskValidationRules);

  const taskHasValues = Boolean(
    task.projectID ||
    task.projectActivityID ||
    task.taskDate ||
    task.noofHoursWorked ||
    task.taskDescriptionRaw,
  );

  const rowsDirty = createComparableRows(taskRows) !== originalRowsRef.current;
  const isDirty = !readOnly && (rowsDirty || taskHasValues);

  useEffect(() => {
    registerTabDirtyState(NavigateLogTimesheet, isDirty);
    return () => registerTabDirtyState(NavigateLogTimesheet, false);
  }, [isDirty, registerTabDirtyState]);

  useEffect(() => {
    const handleBeforeUnload = (event) => {
      if (!isDirty) return;
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [isDirty]);

  const loadActivities = useCallback(async (leaveProject) => {
    const result = await getDropdownItems({
      sourceTable: leaveProject
        ? DROPDOWN_SOURCE_TABLE.PROJECT_ACTIVITY_FOR_LEAVE
        : DROPDOWN_SOURCE_TABLE.PROJECT_ACTIVITY,
      forceRefresh: true,
    });
    setActivityOptions(result.options);
    return result.options;
  }, []);

  useEffect(() => {
    let active = true;
    const initialize = async () => {
      const loaderId = showFullPageLoader(
        isAddMode ? "Loading Timesheet..." : "Loading Timesheet Details...",
      );
      setLoading(true);

      try {
        if (!employeeCode) {
          throw new Error("LoggedIn Employee Code Not Found.");
        }

        const projectResult = await getDropdownItems({
          sourceTable: DROPDOWN_SOURCE_TABLE.PROJECT_BASED_ON_EMPLOYEE_CODE,
          argumentValue: employeeCode,
          forceRefresh: true,
        });

        if (!active) return;
        setProjectOptions(projectResult.options);

        if (isAddMode) {
          await loadActivities(false);
          originalRowsRef.current = "[]";
          return;
        }

        if (!decodedTimesheetId) {
          throw new Error("Invalid Timesheet ID.");
        }

        const state = location.state || {};
        if (!state.taskDate || !state.timesheetMonth || !state.timesheetYear) {
          throw new Error(
            "Timesheet edit information is not available. Please open the Timesheet again from the Log Timesheet List.",
          );
        }

        const result = await getTimesheetDetails({
          projectTimeSheetID: decodedTimesheetId,
          employeeCode,
          taskDate: state.taskDate,
          timesheetMonth: state.timesheetMonth,
          timesheetYear: state.timesheetYear,
        });

        if (!active) return;
        setTaskRows(result.detailRows);
        originalRowsRef.current = createComparableRows(result.detailRows);
        await loadActivities(false);
      } catch (error) {
        if (!error?._globalToastHandled && !error?.__globalToastHandled) {
          ToastService.error(
            error?.message || "Unable to load Timesheet Details.",
          );
        }

        if (!isAddMode) {
          navigate(NavigateLogTimesheet, { replace: true });
        }
      } finally {
        if (active) setLoading(false);
        hideFullPageLoader(loaderId);
      }
    };

    initialize();
    return () => {
      active = false;
    };
  }, [
    decodedTimesheetId,
    employeeCode,
    hideFullPageLoader,
    isAddMode,
    loadActivities,
    location.state,
    navigate,
    showFullPageLoader,
  ]);

  const resetTask = useCallback(() => {
    setTask(blankTask);
    setEditingLocalKey(null);
    clearErrors();
    void loadActivities(false);
  }, [clearErrors, loadActivities]);

  const updateTask = async (event) => {
    const { name, value } = event.target;
    if (editingLocalKey !== null) {
      const index = taskRows.findIndex(
        (row) => row.localKey === editingLocalKey,
      );
      if (index >= 0) {
        setRowApiValidationErrors((current) => {
          const next = { ...current };
          if (!next[index]) return current;
          const fields = { ...next[index] };
          delete fields[name];
          if (name === "taskDescriptionRaw") delete fields.taskDescription;
          if (Object.keys(fields).length) next[index] = fields;
          else delete next[index];
          return next;
        });
      }
    }

    if (name === "projectID") {
      const project = projectOptions.find(
        (option) => String(option.value) === String(value),
      );
      const leaveProject = isLeaveProjectName(project?.label);

      setTask((current) => ({
        ...current,
        projectID: value,
        projectActivityID: "",
        isOnLeave: leaveProject,
        taskDescriptionRaw: leaveProject ? "" : current.taskDescriptionRaw,
      }));
      clearError("projectID");
      clearError("projectActivityID");
      clearError("taskDescriptionRaw");

      try {
        await loadActivities(leaveProject);
      } catch (error) {
        setActivityOptions([]);
        if (!error?._globalToastHandled && !error?.__globalToastHandled) {
          ToastService.error(
            error?.message || "Unable to load Activity dropdown.",
          );
        }
      }
      return;
    }

    if (name === "projectActivityID") {
      const activity = activityOptions.find(
        (option) => String(option.value) === String(value),
      );
      setTask((current) => ({
        ...current,
        projectActivityID: value,
        taskDescriptionRaw: isOnLeaveProject
          ? buildLeaveDescription(activity?.label)
          : current.taskDescriptionRaw,
      }));
      clearError("projectActivityID");
      if (isOnLeaveProject) clearError("taskDescriptionRaw");
      return;
    }

    setTask((current) => ({ ...current, [name]: value }));
    clearError(name);
  };

  const addOrUpdateTask = () => {
    if (readOnly) return;
    if (!validate(task)) return;

    const project = projectOptions.find(
      (option) => String(option.value) === String(task.projectID),
    );
    const activity = activityOptions.find(
      (option) => String(option.value) === String(task.projectActivityID),
    );

    const next = {
      localKey: editingLocalKey || -Date.now(),
      projectTimeSheetID: decodedTimesheetId || 0,
      projectTimeSheetDetailID:
        taskRows.find((row) => row.localKey === editingLocalKey)
          ?.projectTimeSheetDetailID || 0,
      employeeCode,
      projectID: Number(task.projectID),
      projectName: project?.label || "",
      projectActivityID: Number(task.projectActivityID),
      activityName: activity?.label || "",
      taskDate: task.taskDate,
      taskDescription: htmlToNumberedText(task.taskDescriptionRaw),
      taskDescriptionRaw: task.taskDescriptionRaw,
      noofHoursWorked: String(task.noofHoursWorked),
      isOnLeave: Boolean(task.isOnLeave),
      isDeleted: false,
    };

    if (editingLocalKey !== null) {
      const editedRowIndex = taskRows.findIndex(
        (row) => row.localKey === editingLocalKey,
      );
      setRowApiValidationErrors((current) => {
        if (!Object.prototype.hasOwnProperty.call(current, editedRowIndex)) {
          return current;
        }
        const nextErrors = { ...current };
        delete nextErrors[editedRowIndex];
        return nextErrors;
      });
    }
    setTaskRows((current) =>
      editingLocalKey
        ? current.map((row) => (row.localKey === editingLocalKey ? next : row))
        : [...current, next],
    );

    resetTask();
  };

  const editTaskRow = async (row) => {
    if (readOnly) return;

    const leaveProject = isLeaveProjectName(row.projectName);
    try {
      await loadActivities(leaveProject);
    } catch (error) {
      if (!error?._globalToastHandled && !error?.__globalToastHandled) {
        ToastService.error(
          error?.message || "Unable to load Activity dropdown.",
        );
      }
    }

    setTask({
      projectID: String(row.projectID),
      projectActivityID: String(row.projectActivityID),
      taskDate: row.taskDate,
      noofHoursWorked: String(row.noofHoursWorked),
      isOnLeave: Boolean(row.isOnLeave),
      taskDescriptionRaw: row.taskDescriptionRaw,
    });
    setEditingLocalKey(row.localKey);
    clearErrors();
    const rowIndex = taskRows.findIndex(
      (item) => item.localKey === row.localKey,
    );
    const rowErrors = rowApiValidationErrors[rowIndex] || {};
    Object.entries(rowErrors).forEach(([field, message]) => {
      const formField =
        field === "taskDescription" ? "taskDescriptionRaw" : field;
      if (Object.prototype.hasOwnProperty.call(blankTask, formField)) {
        setFieldError(formField, message);
      }
    });
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  const confirmDeleteTask = () => {
    if (!deleteRow) return;

    setTaskRows((current) =>
      current.map((row) =>
        row.localKey === deleteRow.localKey ? { ...row, isDeleted: true } : row,
      ),
    );

    if (editingLocalKey === deleteRow.localKey) {
      resetTask();
    }
    setDeleteRow(null);
    setRowApiValidationErrors({});
  };

  const saveTimesheet = async () => {
    if (readOnly || saving) return;

    if (!isAddMode && !rowsDirty && !taskHasValues) {
      ToastService.error(COMMON_MESSAGE.NO_CHANGES_MADE_TO_FORM);
      return;
    }

    const rowsToSave = taskRows;
    if (!rowsToSave.length) {
      ToastService.error("Add At Least One Timesheet Task Before Saving.");
      return;
    }

    if (taskHasValues) {
      ToastService.error(
        "You Have Unsaved Task Details. Click Add Before Saving The Timesheet.",
      );
      return;
    }

    setRowApiValidationErrors({});
    const loaderId = showFullPageLoader("Saving Timesheet...");
    setSaving(true);

    try {
      const payloadRows = rowsToSave.map((row) => ({
        ...row,
        employeeCode,
        projectTimeSheetID: row.projectTimeSheetID || decodedTimesheetId || 0,
        createdBy: row.projectTimeSheetDetailID > 0 ? 0 : userId,
        updatedBy: row.projectTimeSheetDetailID > 0 ? userId : 0,
      }));

      const result = await saveAndUpdateTimesheetDetails(payloadRows);
      if (
        String(result?.Status || result?.status || "").toLowerCase() ===
          "error" &&
        (result?.data?.ValidationErrors?.length ||
          result?.validationErrors?.length)
      ) {
        const validationError = new Error("Timesheet API validation failed.");
        validationError.isApiValidationError = true;
        validationError.validationErrors =
          result?.data?.ValidationErrors || result?.validationErrors;
        throw validationError;
      }
      ToastService.success(
        result.message || "Your Timesheet Saved Successfully.",
      );

      originalRowsRef.current = createComparableRows(taskRows);
      registerTabDirtyState(NavigateLogTimesheet, false);
      navigate(NavigateLogTimesheet, { replace: true });
    } catch (error) {
      if (
        error?.isApiValidationError ||
        error?.validationErrors?.length ||
        error?.data?.ValidationErrors?.length ||
        error?.response?.data?.data?.ValidationErrors?.length
      ) {
        const validationErrors =
          error?.validationErrors ||
          error?.data?.ValidationErrors ||
          error?.response?.data?.data?.ValidationErrors ||
          [];
        const { rowErrors, generalErrors } = applyApiArrayValidationErrors(
          validationErrors,
          TIMESHEET_API_FIELD_MAPPINGS,
        );
        setRowApiValidationErrors(rowErrors);

        // Keep all task rows and identify the affected row(s) in the grid.
        // EmployeeCode and timesheet IDs do not have editable form inputs.
        const messages = [
          ...Object.entries(rowErrors).flatMap(([index, fields]) =>
            Object.entries(fields).map(
              ([field, message]) =>
                `Task ${Number(index) + 1} - ${field}: ${message}`,
            ),
          ),
          ...generalErrors,
        ];
        if (
          messages.length &&
          !error?._globalToastHandled &&
          !error?.__globalToastHandled
        ) {
          ToastService.error(messages.join("\n"), "API Validations", {
            autoClose: 5000,
          });
        }
      } else if (!error?._globalToastHandled && !error?.__globalToastHandled) {
        showApiError(error, "Unable To Save Your Timesheet.");
      }
    } finally {
      setSaving(false);
      hideFullPageLoader(loaderId);
    }
  };

  const runWithDirtyGuard = (action) => {
    if (!isDirty) {
      action();
      return;
    }
    setPendingDirtyAction(() => action);
    setDirtyOpen(true);
  };

  const discardDirty = () => {
    const action = pendingDirtyAction;
    setDirtyOpen(false);
    setPendingDirtyAction(null);
    registerTabDirtyState(NavigateLogTimesheet, false);
    action?.();
  };

  const visibleRows = taskRows.filter((row) => !row.isDeleted);
  const totalHoursWorked = visibleRows.reduce((total, row) => {
    const hours = Number(row.noofHoursWorked);
    return total + (Number.isFinite(hours) ? hours : 0);
  }, 0);

  // Display each API validation message under its matching grid column.
  // The API row index refers to taskRows (the original save payload order),
  // not visibleRows (which excludes deleted tasks).
  const renderValidatedCell = (value, row, fieldNames, formatValue) => {
    const rowIndex = taskRows.findIndex(
      (item) => item.localKey === row.localKey,
    );
    const rowErrors = rowApiValidationErrors[rowIndex] || {};
    const messages = [
      ...new Set(fieldNames.map((field) => rowErrors[field]).filter(Boolean)),
    ];

    return (
      <div>
        {formatValue ? formatValue(value) : value}
        {messages.length > 0 && (
          <div className="text-danger small mt-1" role="alert">
            {messages.map((message, index) => (
              <div key={index}>{message}</div>
            ))}
          </div>
        )}
      </div>
    );
  };

  const columns = useMemo(
    () => [
      {
        key: "slNo",
        label: "SL No.",
        width: "120px",
        render: (_value, row) =>
          visibleRows.findIndex((item) => item.localKey === row.localKey) + 1,
      },
      {
        key: "projectName",
        label: "Project",
        width: "170px",
        render: (value, row) => renderValidatedCell(value, row, ["projectID"]),
      },
      {
        key: "activityName",
        label: "Activity",
        width: "180px",
        render: (value, row) =>
          renderValidatedCell(value, row, ["projectActivityID"]),
      },
      {
        key: "taskDate",
        label: "Date",
        width: "145px",
        render: (value, row) =>
          renderValidatedCell(value, row, ["taskDate"], formatDDMMMYYYY),
      },
      {
        key: "taskDescription",
        label: "Description",
        width: "380px",
        render: (value, row) =>
          renderValidatedCell(
            value,
            row,
            ["taskDescription", "taskDescriptionRaw"],
            (description) => (
              <span className="tp-task-description-text">{description}</span>
            ),
          ),
      },
      {
        key: "isOnLeave",
        label: "On Leave",
        width: "140px",
        render: (value) => (value ? "Yes" : "No"),
      },
      {
        key: "noofHoursWorked",
        label: "Hours Worked",
        width: "180px",
        render: (value, row) =>
          renderValidatedCell(value, row, ["noofHoursWorked"]),
      },
    ],
    [visibleRows, taskRows, rowApiValidationErrors],
  );

  const renderTaskActions = useCallback(
    (row) => (
      <div className="tp-row-actions tp-log-task-actions">
        <button
          type="button"
          className="tp-row-edit"
          title="Edit Task"
          aria-label="Edit Task"
          onClick={() => editTaskRow(row)}
          disabled={readOnly}
        >
          <SvgIcon name="edit" size={17} />
        </button>
        <button
          type="button"
          className="tp-row-delete"
          title="Delete Task"
          aria-label="Delete Task"
          onClick={() => setDeleteRow(row)}
          disabled={readOnly}
        >
          <SvgIcon name="delete" size={17} />
        </button>
      </div>
    ),
    [readOnly],
  );

  if (loading) return null;

  return (
    <div className="tp-employee-form-page tp-log-timesheet-form-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="timeEntry" size={18} />
          <h1>
            {isAddMode
              ? "Add Timesheet"
              : readOnly
                ? "View Timesheet"
                : "Edit Timesheet"}
          </h1>
        </div>

        <div className="tp-form-page-heading-actions">
          {!readOnly && (
            <>
              <button
                type="button"
                className="tp-form-page-icon-button"
                onClick={addOrUpdateTask}
                disabled={saving}
                title={editingLocalKey ? "Update Task" : "Add Task"}
                aria-label={editingLocalKey ? "Update Task" : "Add Task"}
              >
                <SvgIcon name="add" size={18} />
              </button>

              <button
                type="button"
                className="tp-form-page-icon-button"
                onClick={saveTimesheet}
                disabled={saving}
                title="Save Timesheet"
                aria-label="Save Timesheet"
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
                onClick={resetTask}
                disabled={saving || !taskHasValues}
                title="Clear Task Details"
                aria-label="Clear Task Details"
              >
                <SvgIcon name="clear" size={18} />
              </button>

              <button
                type="button"
                className="tp-form-page-icon-button"
                onClick={() =>
                  runWithDirtyGuard(() => navigate(NavigateLogTimesheet))
                }
                disabled={saving}
                title="Cancel"
                aria-label="Cancel"
              >
                <SvgIcon name="close" size={18} />
              </button>
            </>
          )}

          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() =>
              runWithDirtyGuard(() => navigate(NavigateLogTimesheet))
            }
            disabled={saving}
            title="Back To Log Timesheet List"
            aria-label="Back To Log Timesheet List"
          >
            <SvgIcon name="back" size={18} />
          </button>
        </div>
      </div>

      <div className="tp-card tp-log-timesheet-form-card tp-employee-form-card-attached">
        <div className="row g-3">
          <div className="col-md-4">
            <SearchableDropdown
              name="projectID"
              label="Project"
              required
              value={task.projectID}
              onChange={updateTask}
              options={projectOptions}
              columns={[{ key: "label", label: "Project" }]}
              error={errors.projectID}
              disabled={readOnly}
              placeholder="Search and select..."
            />
          </div>

          <div className="col-md-4">
            <SearchableDropdown
              name="projectActivityID"
              label="Activity"
              required
              value={task.projectActivityID}
              onChange={updateTask}
              options={activityOptions}
              columns={[{ key: "label", label: "Activity" }]}
              error={errors.projectActivityID}
              disabled={readOnly || !task.projectID}
              placeholder={
                task.projectID
                  ? "Search and select..."
                  : "Select project first..."
              }
            />
          </div>

          <div className="col-md-4">
            <HolidayAndTaskDatePicker
              name="taskDate"
              label="Task Date"
              required
              value={task.taskDate}
              onChange={updateTask}
              showHolidays={true}
              disableHolidays={false}
              showSaturday={true}
              showSunday={true}
              disableSaturday={false}
              disableSunday={false}
              error={errors.taskDate}
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="noofHoursWorked"
              label="No. of Hours"
              required
              type="number"
              min={allowZeroHours ? "0" : "0.01"}
              step="0.01"
              inputMode="decimal"
              value={task.noofHoursWorked}
              onChange={updateTask}
              error={errors.noofHoursWorked}
              placeholder="Enter no. of hours"
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4 tp-log-timesheet-leave-field">
            <div className="tp-log-timesheet-leave-switch">
              <Checkbox
                name="isOnLeave"
                label="Is On Leave"
                switchStyle
                checked={task.isOnLeave}
                disabled
              />
              <span className="tp-required" aria-hidden="true">
                *
              </span>
            </div>
          </div>

          <div className="col-12">
            <TextEditor
              name="taskDescriptionRaw"
              label="Task Description"
              required
              value={task.taskDescriptionRaw}
              onChange={updateTask}
              error={errors.taskDescriptionRaw}
              disabled={readOnly || isOnLeaveProject}
              minHeight={145}
              toolbar={!isOnLeaveProject && !readOnly}
              showBold={false}
              showItalic={false}
              showUnderline={false}
              showBulletedList={false}
              showNumberedList
              showAlignLeft={false}
              showAlignCenter={false}
              showAlignRight={false}
              showUndo={false}
              showRedo={false}
              showClearFormatting={false}
              defaultListType="ordered"
            />
          </div>
        </div>

        <div className="tp-log-task-grid">
          <SortableTable
            columns={columns}
            data={visibleRows}
            toolbarExtraPlacement="beforeSearch"
            toolbarExtra={
              <div className="tp-log-total-hours" aria-live="polite">
                <span>Total No. of Hours Worked :</span>
                <strong>{totalHoursWorked.toFixed(2)}</strong>
              </div>
            }
            keyField="localKey"
            emptyText="Add task details to view them here."
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={1}
            defaultPageSize={30}
            maxHeight={340}
            minTableHeight={180}
            showActions={!readOnly}
            actionColumnWidth="95px"
            showViewAction={false}
            showEditAction={false}
            showDeleteAction={false}
            showRowMore={false}
            renderActions={renderTaskActions}
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete Task"
        message="Are You Sure You Want To Delete This Task ?"
        confirmText="Yes, Confirm"
        onConfirm={confirmDeleteTask}
        onCancel={() => setDeleteRow(null)}
      />

      <DirtyStateModal
        open={dirtyOpen}
        onStay={() => {
          setDirtyOpen(false);
          setPendingDirtyAction(null);
        }}
        onDiscard={discardDirty}
      />
    </div>
  );
}
