import apiClient from "./apiClient";
import { frontEndUserTabs } from "../utils/url";
import { getApiResponseRows, getApiResponseStatus } from "../utils/apiResponseUtils";

// Both operations are intentionally silent. Authentication/session handling
// remains with the existing Axios interceptors.
const SILENT_REQUEST = { silentUserTabs: true };

export async function getUserTabList({ userId, roleId }) {
  const response = await apiClient.post(
    frontEndUserTabs.get_user_tab_list,
    { userId, roleId },
    SILENT_REQUEST,
  );
  const payload = response?.data;
  if (getApiResponseStatus(payload) !== "success") {
    throw new Error("Unable to retrieve user tabs.");
  }
  return getApiResponseRows(payload, "USER_TAB_RESPONSE")
    .filter((row) => typeof row?.Path === "string" && row.Path.startsWith("/"))
    .map((row) => ({
      path: row.Path,
      label: row.Label,
      svgIcon: row.SvgIcon,
      tone: row.Tone,
      closable: Boolean(row.Closable),
    }));
}

export async function saveAndUpdateUserTabs({ userId, roleId, tabs }) {
  const response = await apiClient.post(
    frontEndUserTabs.save_and_update_user_tabs,
    { userId, roleId, tabs },
    SILENT_REQUEST,
  );
  return getApiResponseStatus(response?.data) === "success";
}
