import { Route } from "react-router-dom";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";
import ProjectMaster from "../../pages/masters/ProjectMaster";
import ProjectFormPage from "../../pages/masters/ProjectFormPage";
import EmployeeProjectMappingMaster from "../../pages/masters/EmployeeProjectMappingMaster";
import EmployeeProjectMappingFormPage from "../../pages/masters/EmployeeProjectMappingFormPage";
import ProjectActivityMaster from "../../pages/masters/ProjectActivityMaster";

export default function ManageProjectsRoutes() {
  return (
    <>
      <Route
        path={ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECTS}
        element={<ProjectMaster />}
      />
      <Route
        path={ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECT_ADD}
        element={<ProjectFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECT_EDIT}
        element={<ProjectFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.MANAGE_PROJECTS.EMPLOYEE_PROJECT_MAPPING}
        element={<EmployeeProjectMappingMaster />}
      />
      <Route
        path={ROUTE_CONSTANTS.MANAGE_PROJECTS.EMPLOYEE_PROJECT_MAPPING_ADD}
        element={<EmployeeProjectMappingFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.MANAGE_PROJECTS.EMPLOYEE_PROJECT_MAPPING_EDIT}
        element={<EmployeeProjectMappingFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECT_ACTIVITIES}
        element={<ProjectActivityMaster />}
      />
    </>
  );
}
