import { Route } from "react-router-dom";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";
import EmployeeMaster from "../../pages/masters/EmployeeMaster";
import EmployeeFormPage from "../../pages/masters/EmployeeFormPage";
import EmployeeTypeMaster from "../../pages/masters/EmployeeTypeMaster";

export default function ManageEmployeesRoutes() {
  return (
    <>
      <Route
        path={ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEES}
        element={<EmployeeMaster />}
      />
      <Route
        path={ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEE_ADD}
        element={<EmployeeFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEE_EDIT}
        element={<EmployeeFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEE_TYPE}
        element={<EmployeeTypeMaster />}
      />
    </>
  );
}
