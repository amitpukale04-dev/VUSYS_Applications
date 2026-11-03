import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import SortableTable from "../../components/common/SortableTable";
import ConfirmModal from "../../components/common/ConfirmModal";
import AccessDenied from "../../components/common/AccessDenied";
import ToastService from "../../services/toastService";
import {
  deleteEmployeeProjectMapping,
  getEmployeeProjectMappingList,
} from "../../services/employeeProjectMappingService";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { encodeRouteId } from "../../utils/routeIdUtils";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());
const NavigateEmployeeProjectMapping =
  ROUTE_CONSTANTS.MANAGE_PROJECTS.EMPLOYEE_PROJECT_MAPPING;
const NavigateAddEmployeeProjectMapping =
  ROUTE_CONSTANTS.MANAGE_PROJECTS.EMPLOYEE_PROJECT_MAPPING_ADD;

export default function EmployeeProjectMappingMaster() {
  const navigate = useNavigate();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenList = hasPermission("Employee Project Mapping Master", "Open");
  const canCreate = hasPermission("Employee Project Mapping Master", "Create");
  const canEdit = hasPermission("Employee Project Mapping Master", "Edit");
  const canView = hasPermission("Employee Project Mapping Master", "View");

  const [mappings, setMappings] = useState([]);
  const [loading, setLoading] = useState(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const initialLoadRef = useRef(false);

  const loadMappings = useCallback(async ({ showToast = false } = {}) => {
    setLoading(true);

    try {
      const result = await getEmployeeProjectMappingList();
      setMappings(result.mappings);

      if (showToast) {
        ToastService.information(
          "Employee Project Mapping list refreshed successfully.",
        );
      }
    } catch (error) {
      setMappings([]);
      showApiError(error, "Unable to load Employee Project Mapping List.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (securityAccessLoading || !canOpenList) return;
    if (initialLoadRef.current) return;

    initialLoadRef.current = true;
    loadMappings();
  }, [canOpenList, loadMappings, securityAccessLoading]);

  const confirmDelete = async () => {
    if (!deleteRow || deleteLoading) return;

    setDeleteLoading(true);

    try {
      const result = await deleteEmployeeProjectMapping(
        deleteRow.employeeProjectMappingID,
        loggedInUserId,
      );
      ToastService.success(
        "Employee Project Mapping Details Deleted Successfully.",
      );
      setDeleteRow(null);
      await loadMappings();
    } catch (error) {
      showApiError(error, "Unable to delete Employee Project Mapping.");
    } finally {
      setDeleteLoading(false);
    }
  };

  const columns = useMemo(
    () => [
      { key: "projectCode", label: "Project Code", width: "170px" },
      { key: "projectName", label: "Project Name", width: "200px" },
      { key: "employeeName", label: "Employee Name", width: "180px" },
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

  if (!canOpenList) {
    return <AccessDenied />;
  }

  return (
    <div className="tp-master-full-width">
      <div className="tp-card tp-grid-card tp-grid-card-modern">
        <div className="tp-grid-card-body">
          <SortableTable
            columns={columns}
            data={mappings}
            keyField="employeeProjectMappingID"
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
            showViewAction={canView}
            showEditAction={canEdit}
            showDeleteAction={canEdit}
            showAddAction={canCreate}
            addActionLabel="Add Employee Project Mapping"
            addActionIconOnly
            onAdd={() => navigate(NavigateAddEmployeeProjectMapping)}
            showRefreshAction
            onRefresh={() => loadMappings({ showToast: false })}
            refreshing={loading}
            loading={loading}
            loadingText="Loading employee project mappings..."
            onView={(row) =>
              navigate(
                `${NavigateEmployeeProjectMapping}/${encodeRouteId(row.employeeProjectMappingID)}`,
                { state: { mode: "view" } },
              )
            }
            onEdit={(row) =>
              navigate(
                `${NavigateEmployeeProjectMapping}/${encodeRouteId(row.employeeProjectMappingID)}`,
                { state: { mode: "edit" } },
              )
            }
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete Employee Project Mapping"
        message={`Are you sure you want to delete the mapping for ${
          deleteRow?.employeeName || "this employee"
        }?`}
        confirmText="Yes, Delete"
        loading={deleteLoading}
        onConfirm={confirmDelete}
        onCancel={() => setDeleteRow(null)}
      />
    </div>
  );
}
