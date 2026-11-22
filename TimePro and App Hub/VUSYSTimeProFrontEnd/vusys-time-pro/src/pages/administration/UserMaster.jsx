import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import SortableTable from "../../components/common/SortableTable";
import ConfirmModal from "../../components/common/ConfirmModal";
import ImagePopup from "../../components/common/ImagePopup";
import AccessDenied from "../../components/common/AccessDenied";
import ToastService from "../../services/toastService";
import {
  deleteUserMaster,
  getUserList,
} from "../../services/userMasterService";
import { toImgSrcFromBase64 } from "../../utils/imageUtils";
import { encodeRouteId } from "../../utils/routeIdUtils";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import { normalizeNumber } from "../../utils/valueUtils";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());
const NavigateUser = ROUTE_CONSTANTS.ADMINISTRATION.USERS;
const NavigateAddUser = ROUTE_CONSTANTS.ADMINISTRATION.USER_ADD;

export default function UserMaster() {
  const navigate = useNavigate();

  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenUserList = hasPermission("User Master", "Open");
  const canCreateUser = hasPermission("User Master", "Create");
  const canEditUser = hasPermission("User Master", "Edit");
  const canViewUser = hasPermission("User Master", "View");

  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [imagePreview, setImagePreview] = useState(null);
  const initialLoadRef = useRef(false);

  const loadUsers = useCallback(async ({ showToast = false } = {}) => {
    setLoading(true);

    try {
      const result = await getUserList();
      setUsers(result.users);

      if (showToast) {
        ToastService.information("User list refreshed successfully.");
      }
    } catch (error) {
      setUsers([]);
      showApiError(error, "Unable to load User List.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (securityAccessLoading || !canOpenUserList) return;
    if (initialLoadRef.current) return;

    initialLoadRef.current = true;
    loadUsers();
  }, [canOpenUserList, loadUsers, securityAccessLoading]);

  const confirmDelete = async () => {
    if (!deleteRow || deleteLoading) return;

    setDeleteLoading(true);

    try {
      const result = await deleteUserMaster(deleteRow.id, loggedInUserId);
      ToastService.success("User Details Deleted Successfully.");
      setDeleteRow(null);
      await loadUsers();
    } catch (error) {
      showApiError(error, "Unable to delete User.");
    } finally {
      setDeleteLoading(false);
    }
  };

  const columns = useMemo(
    () => [
      { key: "userName", label: "User Name", width: "150px" },
      { key: "fullName", label: "Full Name", width: "160px" },
      { key: "mobileNo", label: "Mobile No.", width: "155px" },
      { key: "userEmailID", label: "Email ID", width: "180px" },
      { key: "userRole", label: "Role", width: "120px" },
      {
        key: "userImage",
        label: "User Image",
        width: "125px",
        sortable: false,
        filterable: false,
        render: (value, row) => {
          const src = toImgSrcFromBase64(value, row.userImageFileName);

          return src ? (
            <button
              type="button"
              className="tp-user-grid-image-button"
              onClick={() =>
                setImagePreview({
                  src,
                  fileName: row.userImageFileName,
                  title: `${row.fullName || row.userName} - User Image`,
                })
              }
              title="View User Image"
              aria-label={`View ${row.fullName || row.userName} image`}
            >
              <img src={src} alt={row.fullName || row.userName} />
            </button>
          ) : (
            <span className="tp-user-grid-image-empty">No Image</span>
          );
        },
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
            className={`tp-status-pill ${String(value).toLowerCase() === "inactive" ? "inactive" : ""}`}
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

  if (!canOpenUserList) {
    return <AccessDenied />;
  }

  return (
    <div className="tp-master-full-width">
      <div className="tp-card tp-grid-card tp-grid-card-modern">
        <div className="tp-grid-card-body">
          <SortableTable
            columns={columns}
            data={users}
            keyField="id"
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
            showViewAction={canViewUser}
            showEditAction={canEditUser}
            showDeleteAction={canEditUser}
            showAddAction={canCreateUser}
            addActionLabel="Add User"
            addActionIconOnly
            onAdd={() => navigate(NavigateAddUser)}
            showRefreshAction
            onRefresh={() => loadUsers({ showToast: false })}
            refreshing={loading}
            loading={loading}
            loadingText="Loading users..."
            onView={(row) =>
              navigate(`${NavigateUser}/${encodeRouteId(row.id)}`, {
                state: { mode: "view" },
              })
            }
            onEdit={(row) =>
              navigate(`${NavigateUser}/${encodeRouteId(row.id)}`, {
                state: { mode: "edit" },
              })
            }
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete User"
        message={`Are you sure you want to delete ${
          deleteRow?.fullName || deleteRow?.userName || "this user"
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
        alt="User"
        onClose={() => setImagePreview(null)}
      />
    </div>
  );
}
