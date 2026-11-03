import { Route, Routes, useLocation } from "react-router-dom";
import ProtectedRoute from "./ProtectedRoute";
import AppLayout from "../components/layout/AppLayout";
import HomePage from "../pages/dashboard/HomePage";
import PlaceholderPage from "../pages/dashboard/PlaceholderPage";
import NotFound from "../pages/errors/NotFound";
import ErrorPage from "../pages/errors/ErrorPage";

import { useAuth } from "../context/AuthContext";
import { getMenuItemByPath } from "../utils/menuUtils";
import { ROUTE_CONSTANTS } from "../constants/RouteConstants";
import AccountRoutes from "./modules/AccountRoutes";
import AuthRoutes from "./modules/AuthRoutes";
import AdminRoutes from "./modules/AdminRoutes";
import AdministrationRoutes from "./modules/AdministrationRoutes";
import MasterRoutes from "./modules/MasterRoutes";
import ConfigurationRoutes from "./modules/ConfigurationRoutes";
import ManageEmployeesRoutes from "./modules/ManageEmployeesRoutes";
import ManageProjectsRoutes from "./modules/ManageProjectsRoutes";
import ManageTimesheetRoutes from "./modules/ManageTimesheetRoutes";
import LeaveManagementRoutes from "./modules/LeaveManagementRoutes";
import ReportsRoutes from "./modules/ReportsRoutes";


function DynamicMenuPlaceholder() {
  const location = useLocation();
  const { menuItems = [] } = useAuth();
  const menuItem = getMenuItemByPath(location.pathname, menuItems);

  return (
    <PlaceholderPage title={menuItem?.label || "Menu"} icon="bi-grid-fill" />
  );
}

export default function AppRoutes() {
  return (
    <Routes>
      {AuthRoutes()}
      <Route
        element={
          <ProtectedRoute requireMenuAccess={false} allowedRoles={["Admin"]} />
        }
      >
        <Route element={<AppLayout />}>
          {AdminRoutes()}
        </Route>
      </Route>

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route index element={<HomePage />} />

          {AdministrationRoutes()}
          {MasterRoutes()}
          {ConfigurationRoutes()}
          {ManageEmployeesRoutes()}
          {ManageProjectsRoutes()}
          {ManageTimesheetRoutes()}
          {LeaveManagementRoutes()}
          {ReportsRoutes()}
          {AccountRoutes()}
          
          <Route path={ROUTE_CONSTANTS.COMMON.ERROR} element={<ErrorPage />} />

          {/*
           * Any backend-controlled menu that does not yet have a dedicated
           * React page is still routable. ProtectedRoute authorizes the path
           * against GetUserMenuAccess before this placeholder is rendered.
           */}
          <Route path="*" element={<DynamicMenuPlaceholder />} />
        </Route>
      </Route>

      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}
