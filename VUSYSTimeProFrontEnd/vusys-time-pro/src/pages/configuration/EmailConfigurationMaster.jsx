import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import SortableTable from "../../components/common/SortableTable";
import ConfirmModal from "../../components/common/ConfirmModal";
import AccessDenied from "../../components/common/AccessDenied";
import ToastService from "../../services/toastService";
import {
  deleteEmailConfiguration,
  getEmailConfigurationList,
} from "../../services/emailConfigurationService";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { encodeRouteId } from "../../utils/routeIdUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import { normalizeNumber } from "../../utils/valueUtils";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

const NavigateEmailConfiguration = ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION;
const NavigateAddEmailConfiguration = ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION_ADD;

export default function EmailConfigurationMaster() {
  const navigate = useNavigate();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenList = hasPermission("Email Configuration Master", "Open");
  const canCreate = hasPermission("Email Configuration Master", "Create");
  const canEdit = hasPermission("Email Configuration Master", "Edit");
  const canView = hasPermission("Email Configuration Master", "View");

  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const initialLoadRef = useRef(false);

  const loadItems = useCallback(async ({ showToast = false } = {}) => {
    setLoading(true);

    try {
      const result = await getEmailConfigurationList();
      setItems(result.items);

      if (showToast) {
        ToastService.information(
          "Email Configuration list refreshed successfully.",
        );
      }
    } catch (error) {
      setItems([]);
      showApiError(error, "Unable to load Email Configuration List.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (securityAccessLoading || !canOpenList) return;
    if (initialLoadRef.current) return;

    initialLoadRef.current = true;
    loadItems();
  }, [canOpenList, loadItems, securityAccessLoading]);

  const confirmDelete = async () => {
    if (!deleteRow || deleteLoading) return;

    setDeleteLoading(true);

    try {
      const result = await deleteEmailConfiguration(
        deleteRow.id,
        loggedInUserId,
      );
      ToastService.success("Email Configuration Details Deleted Successfully.");
      setDeleteRow(null);
      await loadItems();
    } catch (error) {
      showApiError(error, "Unable to obsolete Email Configuration.");
    } finally {
      setDeleteLoading(false);
    }
  };

  const columns = useMemo(
    () => [
      { key: "fromEmail", label: "From Email", width: "190px" },
      {
        key: "password",
        label: "Password",
        width: "280px",
        render: (value) => (
          <span className="tp-email-config-encrypted-password" title={value}>
            {value}
          </span>
        ),
      },
      { key: "smtpHost", label: "SMTP Host", width: "130px" },
      { key: "portNo", label: "Port No.", width: "120px" },
      {
        key: "activeStatus",
        label: "Status",
        width: "120px",
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

  if (!canOpenList) {
    return <AccessDenied />;
  }

  return (
    <div className="tp-master-full-width">
      <div className="tp-card tp-grid-card tp-grid-card-modern">
        <div className="tp-grid-card-body">
          <SortableTable
            columns={columns}
            data={items}
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
            showViewAction={canView}
            showEditAction={canEdit}
            showDeleteAction={canEdit}
            showAddAction={canCreate}
            addActionLabel="Add Email Configuration"
            addActionIconOnly
            onAdd={() => navigate(NavigateAddEmailConfiguration)}
            showRefreshAction
            onRefresh={() => loadItems({ showToast: false })}
            refreshing={loading}
            loading={loading}
            loadingText="Loading email configurations..."
            onView={(row) =>
              navigate(
                `${NavigateEmailConfiguration}/${encodeRouteId(row.id)}`,
                {
                  state: { mode: "view" },
                },
              )
            }
            onEdit={(row) =>
              navigate(
                `${NavigateEmailConfiguration}/${encodeRouteId(row.id)}`,
                {
                  state: { mode: "edit" },
                },
              )
            }
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete Email Configuration"
        message={`Are you sure you want to Delete ${deleteRow?.fromEmail || "this email configuration"}?`}
        confirmText="Yes, Delete"
        loading={deleteLoading}
        onConfirm={confirmDelete}
        onCancel={() => setDeleteRow(null)}
      />
    </div>
  );
}