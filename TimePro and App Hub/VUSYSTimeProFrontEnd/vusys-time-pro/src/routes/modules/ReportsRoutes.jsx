import { Route } from "react-router-dom";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";
import TimesheetReport from "../../pages/reports/TimesheetReport";
import AllocatedConsumedReport from "../../pages/reports/AllocatedConsumedReport";
import LeaveReport from "../../pages/reports/LeaveReport";

export default function ReportsRoutes() {
  return (
    <>
      <Route
        path={ROUTE_CONSTANTS.REPORTS.TIMESHEET_REPORT}
        element={<TimesheetReport />}
      />
      <Route
        path={ROUTE_CONSTANTS.REPORTS.ALLOCATED_CONSUMED_REPORT}
        element={<AllocatedConsumedReport />}
      />
      <Route
        path={ROUTE_CONSTANTS.REPORTS.LEAVE_REPORT}
        element={<LeaveReport />}
      />
    </>
  );
}
