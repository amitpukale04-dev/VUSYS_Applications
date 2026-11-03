import { Route } from "react-router-dom";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";
import EmailConfigurationMaster from "../../pages/configuration/EmailConfigurationMaster";
import EmailConfigurationFormPage from "../../pages/configuration/EmailConfigurationFormPage";

export default function ConfigurationRoutes() {
  return (
    <>
      <Route
        path={ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION}
        element={<EmailConfigurationMaster />}
      />
      <Route
        path={ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION_ADD}
        element={<EmailConfigurationFormPage />}
      />
      <Route
        path={ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION_EDIT}
        element={<EmailConfigurationFormPage />}
      />
    </>
  );
}
