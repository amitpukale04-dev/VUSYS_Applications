import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import SortableTable from "../../components/common/SortableTable";
import ConfirmModal from "../../components/common/ConfirmModal";
import AccessDenied from "../../components/common/AccessDenied";
import ToastService from "../../services/toastService";
import { getDropdownItems } from "../../services/dropdownService";
import {
  deleteProjectMaster,
  getProjectList,
} from "../../services/projectService";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { encodeRouteId } from "../../utils/routeIdUtils";
import { DROPDOWN_SOURCE_TABLE } from "../../constants/constants";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());
const NavigateProject = ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECTS;
const NavigateAddProject = ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECT_ADD;

export default function ProjectMaster() {
  const navigate = useNavigate();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenProjectList = hasPermission("Project Master", "Open");
  const canCreateProject = hasPermission("Project Master", "Create");
  const canEditProject = hasPermission("Project Master", "Edit");
  const canViewProject = hasPermission("Project Master", "View");

  const [projects, setProjects] = useState([]);
  const [loading, setLoading] = useState(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const initialLoadRef = useRef(false);

  const loadProjects = useCallback(async ({ showToast = false } = {}) => {
    setLoading(true);

    try {
      const [listResult, statusResult] = await Promise.all([
        getProjectList(),
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.PROJECT_STATUS }),
      ]);

      const statusMap = new Map(
        statusResult.options.map((option) => [
          String(option.value),
          String(option.label),
        ]),
      );

      setProjects(
        listResult.projects.map((item) => ({
          ...item,
          projectStatusName:
            statusMap.get(String(item.projectStatus)) || item.projectStatus,
        })),
      );

      if (showToast) {
        ToastService.information("Project list refreshed successfully.");
      }
    } catch (error) {
      setProjects([]);
      showApiError(error, "Unable to load Project List.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (securityAccessLoading || !canOpenProjectList) return;
    if (initialLoadRef.current) return;

    initialLoadRef.current = true;
    loadProjects();
  }, [canOpenProjectList, loadProjects, securityAccessLoading]);

  const confirmDelete = async () => {
    if (!deleteRow || deleteLoading) return;

    setDeleteLoading(true);

    try {
      const result = await deleteProjectMaster(
        deleteRow.projectID,
        loggedInUserId,
      );
      ToastService.success("Project Details Deleted Successfully.");
      setDeleteRow(null);
      await loadProjects();
    } catch (error) {
      showApiError(error, "Unable to delete Project.");
    } finally {
      setDeleteLoading(false);
    }
  };

  const columns = useMemo(
    () => [
      { key: "projectCode", label: "Project Code", width: "180px" },
      { key: "clientName", label: "Client Name", width: "190px" },
      { key: "projectName", label: "Project Name", width: "220px" },
      { key: "displayStartDate", label: "Start Date", width: "155px" },
      { key: "displayEndDate", label: "End Date", width: "155px" },
      {
        key: "projectStatusName",
        label: "Project Status",
        width: "190px",
      },
      {
        key: "activeStatus",
        label: "Status",
        width: "130px",
        filterType: "select",
        filterOptions: ["Active", "Inactive"].map((value) => ({
          value,
          label: value,
        })),
        render: (value) => (
          <span
            className={`tp-status-pill ${
              String(value).toLowerCase() === "inactive" ? "inactive" : ""
            }`}
          >
            <i className="bi bi-circle-fill" />
            {value || "Inactive"}
          </span>
        ),
      },
    ],
    [],
  );

  if (securityAccessLoading) {
    return (
      <div className="tp-form-page-loading">
        <span className="spinner-border text-primary" aria-hidden="true" />
        <p>Loading security permissions...</p>
      </div>
    );
  }

  if (!canOpenProjectList) {
    return <AccessDenied />;
  }

  return (
    <div className="tp-master-full-width">
      <div className="tp-card tp-grid-card tp-grid-card-modern">
        <div className="tp-grid-card-body">
          <SortableTable
            columns={columns}
            data={projects}
            keyField="projectID"
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={2}
            defaultPageSize={30}
            maxHeight={420}
            showActions
            actionColumnWidth="90px"
            showViewAction={canViewProject}
            showEditAction={canEditProject}
            showDeleteAction={canEditProject}
            showAddAction={canCreateProject}
            addActionLabel="Add Project"
            addActionIconOnly
            onAdd={() => navigate(NavigateAddProject)}
            showRefreshAction
            onRefresh={() => loadProjects({ showToast: false })}
            refreshing={loading}
            loading={loading}
            loadingText="Loading projects..."
            onView={(row) =>
              navigate(`${NavigateProject}/${encodeRouteId(row.projectID)}`, {
                state: { mode: "view" },
              })
            }
            onEdit={(row) =>
              navigate(`${NavigateProject}/${encodeRouteId(row.projectID)}`, {
                state: { mode: "edit" },
              })
            }
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete Project"
        message={`Are you sure you want to delete ${
          deleteRow?.projectName || deleteRow?.projectCode || "this project"
        }?`}
        confirmText="Yes, Delete"
        loading={deleteLoading}
        onConfirm={confirmDelete}
        onCancel={() => setDeleteRow(null)}
      />
    </div>
  );
}
