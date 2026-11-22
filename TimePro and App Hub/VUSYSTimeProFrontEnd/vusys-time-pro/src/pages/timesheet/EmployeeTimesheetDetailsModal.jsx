import { useEffect, useMemo } from "react";
import { createPortal } from "react-dom";
import SortableTable from "../../components/common/SortableTable";
import SvgIcon from "../../components/common/SvgIcon";

export default function EmployeeTimesheetDetailsModal({
  show,
  rows = [],
  onClose,
}) {
  useEffect(() => {
    if (!show) return undefined;

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const handleKeyDown = (event) => {
      if (event.key === "Escape") onClose?.();
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [onClose, show]);

  const columns = useMemo(
    () => [
      { key: "slNo", label: "SL No.", width: "120px" },
      { key: "employeeCode", label: "Employee Code", width: "180px" },
      { key: "name", label: "Name", width: "135px" },
      { key: "taskDate", label: "Task Date", width: "150px" },
      { key: "clientName", label: "Client", width: "130px" },
      { key: "projectName", label: "Project", width: "135px" },
      { key: "activityName", label: "Activity", width: "155px" },
      {
        key: "taskDescription",
        label: "Task Description",
        width: "260px",
        render: (value) => (
          <div className="tp-timesheet-task-description">{value}</div>
        ),
      },
      {
        key: "totalHoursWorked",
        label: "Hours Worked",
        width: "180px",
      },
    ],
    [],
  );

  if (!show) return null;

  return createPortal(
    <div
      className="tp-confirm-layer"
      role="dialog"
      aria-modal="true"
      aria-labelledby="employee-timesheet-details-title"
    >
      <div className="tp-confirm-backdrop" aria-hidden="true" />
      <div className="tp-confirm-card tp-confirm-card-titlebar tp-download-timesheet-details-modal">
        <div className="tp-confirm-titlebar">
          <div className="tp-confirm-titlebar-text">
            <SvgIcon name="viewTimesheet" size={19} />
            <span id="employee-timesheet-details-title">Timesheet Details</span>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close Timesheet Details"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>

        <div className="tp-download-timesheet-details-body">
          <SortableTable
            columns={columns}
            data={rows}
            keyField="slNo"
            emptyText="Timesheet Data Not Found."
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={1}
            defaultPageSize={30}
            maxHeight={430}
            minTableHeight={250}
          />
        </div>

        <div className="tp-download-timesheet-details-footer tp-confirm-actions">
          <button type="button" className="tp-confirm-no" onClick={onClose}>
            <SvgIcon name="close" size={17} /> Close
          </button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
