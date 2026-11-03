import { Route } from "react-router-dom";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";
import GenderMaster from "../../pages/masters/GenderMaster";
import CountryMaster from "../../pages/masters/CountryMaster";
import StateMaster from "../../pages/masters/StateMaster";
import HolidayMaster from "../../pages/masters/HolidayMaster";

export default function MasterRoutes() {
  return (
    <>
      <Route
        path={ROUTE_CONSTANTS.MASTER.GENDER_MASTER}
        element={<GenderMaster />}
      />
      <Route
        path={ROUTE_CONSTANTS.MASTER.COUNTRY_MASTER}
        element={<CountryMaster />}
      />
      <Route
        path={ROUTE_CONSTANTS.MASTER.STATE_MASTER}
        element={<StateMaster />}
      />
      <Route
        path={ROUTE_CONSTANTS.MASTER.HOLIDAY_MASTER}
        element={<HolidayMaster />}
      />
    </>
  );
}
