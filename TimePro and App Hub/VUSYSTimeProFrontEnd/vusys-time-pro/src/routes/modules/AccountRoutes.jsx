import { Route } from "react-router-dom";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";
import UserProfile from "../../pages/profile/UserProfile";
import ChangePassword from "../../pages/auth/ChangePassword";

export default function AccountRoutes() {
  return (
    <>
      <Route
        path={ROUTE_CONSTANTS.AUTH.USER_PROFILE}
        element={<UserProfile />}
      />
      <Route
        path={ROUTE_CONSTANTS.AUTH.CHANGE_PASSWORD}
        element={<ChangePassword />}
      />
    </>
  );
}
