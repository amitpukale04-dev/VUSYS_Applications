import { Route } from "react-router-dom";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";
import LogTimesheet from "../../pages/timesheet/LogTimesheet";
import LogTimesheetFormPage from "../../pages/timesheet/LogTimesheetFormPage";
import DownloadTimesheet from "../../pages/timesheet/DownloadTimesheet";
import TimesheetSubmission from "../../pages/timesheet/TimesheetSubmission";
import TimesheetApprovalRejectionList from "../../pages/timesheet/TimesheetApprovalRejectionList";

export default function ManageTimesheetRoutes() {
  return (
    <>
      <Route
        path={ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET}
        element={<LogTimesheet />}
      />
      <Route
        path={ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET_ADD}
        element={<LogTimesheetFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET_EDIT}
        element={<LogTimesheetFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.MANAGE_TIMESHEET.DOWNLOAD_TIMESHEET}
        element={<DownloadTimesheet />}
      />
      <Route
        path={ROUTE_CONSTANTS.MANAGE_TIMESHEET.TIMESHEET_SUBMISSION}
        element={<TimesheetSubmission />}
      />
      <Route
        path={ROUTE_CONSTANTS.MANAGE_TIMESHEET.TIMESHEET_APPROVAL_REJECTION_LIST}
        element={<TimesheetApprovalRejectionList />}
      />
    </>
  );
}
