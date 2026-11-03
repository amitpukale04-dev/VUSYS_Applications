import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { InlineEditableTable } from "../../components/common/InlineEditable";
import ConfirmModal from "../../components/common/ConfirmModal";
import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { useAuth } from "../../context/AuthContext";
import {
  getProjectActivityList,
  saveAndUpdateProjectActivityDetails,
} from "../../services/projectActivityService";
import ToastService from "../../services/toastService";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import {
  applyApiValidationErrors,
  clearRowValidationError,
} from "../../utils/CustomAPIValidation";

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

const PROJECT_ACTIVITY_API_FIELD_MAPPINGS = {
  activityName: { apiKeys: ["ActivityName"] },
};

export default function ProjectActivityMaster() {
  const { user } = useAuth();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenProjectActivityList = hasPermission(
    "Project Activity Master",
    "Open",
  );
  const canCreateProjectActivity = hasPermission(
    "Project Activity Master",
    "Create",
  );
  const canEditProjectActivity = hasPermission(
    "Project Activity Master",
    "Edit",
  );

  const [projectActivities, setProjectActivities] = useState([]);
  const initialLoadRef = useRef(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [rowValidationErrors, setRowValidationErrors] = useState({});

  const handleCellChange = (_row, fieldName) => {
    clearRowValidationError(setRowValidationErrors, fieldName);
  };

  // ==========================================================
  // LOAD PROJECT ACTIVITY LIST
  // ==========================================================

  const loadProjectActivities = useCallback(
    async ({ showMessage = false } = {}) => {
      setLoading(true);

      try {
        const result = await getProjectActivityList();
        setProjectActivities(result.projectActivities || []);

        if (showMessage) {
          ToastService.success("Project Activity List refreshed successfully.");
        }
      } catch (error) {
        setProjectActivities([]);
        showApiError(error, "Unable to load Project Activity List.");
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
    if (securityAccessLoading || !canOpenProjectActivityList) {
      return;
    }

    if (initialLoadRef.current) {
      return;
    }

    initialLoadRef.current = true;
    loadProjectActivities();
  }, [
    canOpenProjectActivityList,
    loadProjectActivities,
    securityAccessLoading,
  ]);

  // ==========================================================
  // GRID COLUMNS
  // ==========================================================

  const columns = useMemo(
    () => [
      {
        key: "activityName",
        label: "Activity Name",
        width: "520px",
        inputType: "text",
        validate: (value) =>
          !String(value || "").trim() ? "Enter The Activity Name." : "",
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
  // NEW PROJECT ACTIVITY ROW
  // ==========================================================

  const createProjectActivityRow = () => ({
    projectActivityId: -Date.now(),
    activityName: "",
    isDeleted: false,
    activeStatus: "Active",
    isActive: true,
  });

  // ==========================================================
  // SAVE / UPDATE PROJECT ACTIVITY
  // ==========================================================

  const saveProjectActivityRow = async (row, { isNew }) => {
    if (saving) return false;
    setRowValidationErrors({});
    setSaving(true);

    try {
      const result = await saveAndUpdateProjectActivityDetails({
        id: isNew ? 0 : row.projectActivityId,
        activityName: row.activityName,
        isDeleted: !row.isActive,
        createdBy: isNew ? loggedInUserId : 0,
        updatedBy: isNew ? 0 : loggedInUserId,
      });

      ToastService.success(
        result?.message ||
          (isNew
            ? "New Project Activity Added Successfully."
            : "Project Activity Updated Successfully."),
      );

      await loadProjectActivities();
      return true;
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          PROJECT_ACTIVITY_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            setRowValidationErrors((current) => ({
              ...current,
              [fieldName]: message,
            }));
          },
        );
      } else {
        showApiError(error, "Unable to save Project Activity Details.");
      }
      return false;
    } finally {
      setSaving(false);
    }
  };

  // ==========================================================
  // DELETE PROJECT ACTIVITY
  // ==========================================================

  const confirmDelete = async () => {
    if (!deleteRow || deleting) return;
    setDeleting(true);

    try {
      const result = await saveAndUpdateProjectActivityDetails({
        id: deleteRow.projectActivityId,
        activityName: deleteRow.activityName,
        isDeleted: true,
        createdBy: 0,
        updatedBy: loggedInUserId,
      });

      ToastService.success("Project Activity Details Deleted Successfully.");
      setDeleteRow(null);
      await loadProjectActivities();
    } catch (error) {
      showApiError(error, "Unable to delete Project Activity Details.");
    } finally {
      setDeleting(false);
    }
  };

  // ==========================================================
  // SECURITY ACCESS
  // ==========================================================

  if (securityAccessLoading) {
    return (
      <div className="tp-form-page-loading">
        <span className="spinner-border text-primary" aria-hidden="true" />
        <p>Loading security permissions...</p>
      </div>
    );
  }

  if (!canOpenProjectActivityList) {
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
            data={projectActivities}
            keyField="projectActivityId"
            onDataChange={setProjectActivities}
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={2}
            defaultPageSize={30}
            maxHeight={390}
            minTableHeight={235}
            showActions={canCreateProjectActivity || canEditProjectActivity}
            actionColumnWidth="50px"
            showEditAction={canEditProjectActivity}
            showDeleteAction={canEditProjectActivity}
            showRowMore={false}
            showAddAction={canCreateProjectActivity}
            addActionLabel="Add Project Activity"
            addActionIconOnly
            newRowFactory={createProjectActivityRow}
            onRowSave={saveProjectActivityRow}
            rowValidationErrors={rowValidationErrors}
            onCellChange={handleCellChange}
            onRowCancel={() => setRowValidationErrors({})}
            showRefreshAction
            onRefresh={() => loadProjectActivities({ showMessage: false })}
            refreshing={loading}
            loading={loading}
            loadingText="Loading project activities..."
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete Project Activity"
        message={`Are you sure you want to delete ${deleteRow?.activityName || "this project activity"}?`}
        confirmText={deleting ? "Deleting..." : "Yes, Delete"}
        onConfirm={confirmDelete}
        onCancel={() => {
          if (!deleting) setDeleteRow(null);
        }}
      />
    </div>
  );
}
