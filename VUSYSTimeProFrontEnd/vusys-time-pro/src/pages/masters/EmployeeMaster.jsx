import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import SortableTable from "../../components/common/SortableTable";
import ConfirmModal from "../../components/common/ConfirmModal";
import ImagePopup from "../../components/common/ImagePopup";
import AccessDenied from "../../components/common/AccessDenied";
import ToastService from "../../services/toastService";
import {
  deleteEmployeeMaster,
  getEmployeeList,
} from "../../services/employeeMasterService";
import { toImgSrcFromBase64 } from "../../utils/imageUtils";
import { encodeRouteId } from "../../utils/routeIdUtils";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());
const NavigateEmployee = ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEES;
const NavigateAddEmployee = ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEE_ADD;

export default function EmployeeMaster() {
  const navigate = useNavigate();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenEmployeeList = hasPermission("Employee Master", "Open");
  const canCreateEmployee = hasPermission("Employee Master", "Create");
  const canEditEmployee = hasPermission("Employee Master", "Edit");
  const canViewEmployee = hasPermission("Employee Master", "View");

  const [employees, setEmployees] = useState([]);
  const [loading, setLoading] = useState(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [imagePreview, setImagePreview] = useState(null);
  const initialLoadRef = useRef(false);

  const loadEmployees = useCallback(async ({ showToast = false } = {}) => {
    setLoading(true);

    try {
      const result = await getEmployeeList();
      setEmployees(result.employees);

      if (showToast) {
        ToastService.information("Employee list refreshed successfully.");
      }
    } catch (error) {
      setEmployees([]);
      showApiError(error, "Unable to load Employee List.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (securityAccessLoading || !canOpenEmployeeList) return;
    if (initialLoadRef.current) return;

    initialLoadRef.current = true;
    loadEmployees();
  }, [canOpenEmployeeList, loadEmployees, securityAccessLoading]);

  const confirmDelete = async () => {
    if (!deleteRow || deleteLoading) return;

    setDeleteLoading(true);

    try {
      const result = await deleteEmployeeMaster(
        deleteRow.employeeId,
        loggedInUserId,
      );
      ToastService.success("Employee Details Deleted Successfully.");
      setDeleteRow(null);
      await loadEmployees();
    } catch (error) {
      showApiError(error, "Unable to delete Employee.");
    } finally {
      setDeleteLoading(false);
    }
  };

  const columns = useMemo(
    () => [
      { key: "employeeCode", label: "Employee Code", width: "180px" },
      { key: "fullName", label: "Full Name", width: "160px" },
      { key: "genderName", label: "Gender", width: "130px" },
      {
        key: "displayDateOfBirth",
        label: "Date of Birth",
        width: "170px",
      },
      { key: "mobileNo", label: "Mobile No.", width: "160px" },
      { key: "emailID", label: "Email ID", width: "190px" },
      {
        key: "employeeType",
        label: "Employee Type",
        width: "190px",
      },
      {
        key: "employeeProfileImage",
        label: "User Image",
        width: "125px",
        sortable: false,
        filterable: false,
        render: (value, row) => {
          const src = toImgSrcFromBase64(value, row.employeeImageFileName);

          return src ? (
            <button
              type="button"
              className="tp-user-grid-image-button"
              onClick={() =>
                setImagePreview({
                  src,
                  fileName: row.employeeImageFileName,
                  title: `${row.fullName || row.employeeCode} - User Image`,
                })
              }
              title="View User Image"
              aria-label={`View ${row.fullName || row.employeeCode} image`}
            >
              <img src={src} alt={row.fullName || row.employeeCode} />
            </button>
          ) : (
            <span className="tp-user-grid-image-empty">No Image</span>
          );
        },
      },
      { key: "countryName", label: "Country", width: "140px" },
      { key: "stateName", label: "State", width: "140px" },
      { key: "displayHireDate", label: "Hire Date", width: "145px" },
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

  if (!canOpenEmployeeList) {
    return <AccessDenied />;
  }

  return (
    <div className="tp-master-full-width">
      <div className="tp-card tp-grid-card tp-grid-card-modern">
        <div className="tp-grid-card-body">
          <SortableTable
            columns={columns}
            data={employees}
            keyField="employeeId"
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
            showViewAction={canViewEmployee}
            showEditAction={canEditEmployee}
            showDeleteAction={canEditEmployee}
            showAddAction={canCreateEmployee}
            addActionLabel="Add Employee"
            addActionIconOnly
            onAdd={() => navigate(NavigateAddEmployee)}
            showRefreshAction
            onRefresh={() => loadEmployees({ showToast: false })}
            refreshing={loading}
            loading={loading}
            loadingText="Loading employees..."
            onView={(row) =>
              navigate(`${NavigateEmployee}/${encodeRouteId(row.employeeId)}`, {
                state: { mode: "view" },
              })
            }
            onEdit={(row) =>
              navigate(`${NavigateEmployee}/${encodeRouteId(row.employeeId)}`, {
                state: { mode: "edit" },
              })
            }
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete Employee"
        message={`Are you sure you want to delete ${
          deleteRow?.fullName || deleteRow?.employeeCode || "this employee"
        }?`}
        confirmText="Yes, Delete"
        loading={deleteLoading}
        onConfirm={confirmDelete}
        onCancel={() => setDeleteRow(null)}
      />

      <ImagePopup
        show={Boolean(imagePreview)}
        src={imagePreview?.src || ""}
        title={imagePreview?.title || "User Image"}
        fileName={imagePreview?.fileName || ""}
        alt="Employee"
        onClose={() => setImagePreview(null)}
      />
    </div>
  );
}
