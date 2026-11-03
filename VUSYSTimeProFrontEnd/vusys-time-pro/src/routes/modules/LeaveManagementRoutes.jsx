import { Route } from "react-router-dom";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";
import LeaveTypeMaster from "../../pages/masters/LeaveTypeMaster";
import LeaveTypeFormPage from "../../pages/masters/LeaveTypeFormPage";
import MyLeaveRequest from "../../pages/leaveManagement/MyLeaveRequest";
import MyLeaveRequestFormPage from "../../pages/leaveManagement/MyLeaveRequestFormPage";
import LeaveApprovalRejectionList from "../../pages/leaveManagement/LeaveApprovalRejectionList";
import EmployeeLeaveBalanceMaster from "../../pages/leaveManagement/EmployeeLeaveBalanceMaster";
import EmployeeLeaveBalanceFormPage from "../../pages/leaveManagement/EmployeeLeaveBalanceFormPage";

export default function LeaveManagementRoutes() {
  return (
    <>
      <Route
        path={ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_TYPES}
        element={<LeaveTypeMaster />}
      />
      <Route
        path={ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_TYPE_ADD}
        element={<LeaveTypeFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_TYPE_EDIT}
        element={<LeaveTypeFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS}
        element={<MyLeaveRequest />}
      />
      <Route
        path={ROUTE_CONSTANTS.LEAVE_MANAGEMENT.APPLY_LEAVE}
        element={<MyLeaveRequestFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.LEAVE_MANAGEMENT.APPLY_LEAVE_EDIT}
        element={<MyLeaveRequestFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_APPROVAL_REJECTION_LIST}
        element={<LeaveApprovalRejectionList />}
      />

      <Route
        path={ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE}
        element={<EmployeeLeaveBalanceMaster />}
      />
      <Route
        path={ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE_ADD}
        element={<EmployeeLeaveBalanceFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE_EDIT}
        element={<EmployeeLeaveBalanceFormPage />}
      />
    </>
  );
}
