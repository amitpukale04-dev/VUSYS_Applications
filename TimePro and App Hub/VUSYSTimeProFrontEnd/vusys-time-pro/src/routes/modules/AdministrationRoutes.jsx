import { Route } from "react-router-dom";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";
import SecurityMaster from "../../pages/administration/SecurityMaster";
import SecurityAccess from "../../pages/administration/SecurityAccess";
import UserMaster from "../../pages/administration/UserMaster";
import UserFormPage from "../../pages/administration/UserFormPage";
import MenuAccessForRole from "../../pages/administration/MenuAccessForRole";
import RoleMaster from "../../pages/administration/RoleMaster";

export default function AdministrationRoutes() {
  return (
    <>
      <Route
        path={ROUTE_CONSTANTS.ADMINISTRATION.SECURITY_MASTER}
        element={<SecurityMaster />}
      />
      <Route
        path={ROUTE_CONSTANTS.ADMINISTRATION.SECURITY_ACCESS}
        element={<SecurityAccess />}
      />
      <Route
        path={ROUTE_CONSTANTS.ADMINISTRATION.USERS}
        element={<UserMaster />}
      />
      <Route
        path={ROUTE_CONSTANTS.ADMINISTRATION.USER_ADD}
        element={<UserFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.ADMINISTRATION.USER_EDIT}
        element={<UserFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.ADMINISTRATION.ROLE_MENU_ACCESS}
        element={<MenuAccessForRole />}
      />
      <Route
        path={ROUTE_CONSTANTS.ADMINISTRATION.ROLE_MASTER}
        element={<RoleMaster />}
      />
    </>
  );
}