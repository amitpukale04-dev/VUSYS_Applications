import { Route } from "react-router-dom";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";
import Login from "../../pages/auth/Login";
import ForgotPassword from "../../pages/auth/ForgotPassword";
import OtpAuthentication from "../../pages/auth/OtpAuthentication";
import ResetPassword from "../../pages/auth/ResetPassword";

export default function AuthRoutes() {
  return (
    <>
      <Route path={ROUTE_CONSTANTS.AUTH.LOGIN} element={<Login />} />
      <Route
        path={ROUTE_CONSTANTS.AUTH.FORGOT_PASSWORD}
        element={<ForgotPassword />}
      />
      <Route
        path={ROUTE_CONSTANTS.AUTH.OTP_AUTHENTICATION}
        element={<OtpAuthentication />}
      />
      <Route
        path={ROUTE_CONSTANTS.AUTH.RESET_PASSWORD}
        element={<ResetPassword />}
      />
    </>
  );
}
