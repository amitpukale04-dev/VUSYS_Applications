import { Route } from "react-router-dom";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";
import TimesheetApprovalRejection from "../../pages/timesheet/TimesheetApprovalRejection";
import LeaveApprovalRejection from "../../pages/leaveManagement/LeaveApprovalRejection";
import TransactionHistory from "../../pages/transactions/TransactionHistory";

export default function AdminRoutes() {
  return (
    <>
      <Route
        path={ROUTE_CONSTANTS.MANAGE_TIMESHEET.TIMESHEET_APPROVAL_REJECTION}
        element={<TimesheetApprovalRejection />}
      />
      <Route
        path={ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_APPROVAL_REJECTION}
        element={<LeaveApprovalRejection />}
      />
      {/*<Route
        path={ROUTE_CONSTANTS.TRANSACTIONS.TRANSACTION_HISTORY}
        element={<TransactionHistory />}
      />*/}
    </>
  );
}
