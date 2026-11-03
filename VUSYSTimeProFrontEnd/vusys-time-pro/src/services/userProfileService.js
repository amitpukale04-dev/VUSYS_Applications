import apiClient from "./apiClient";
import { master, MultiPartFormDataContentType } from "../utils/url";
import {
  normalizeBoolean,
  normalizeNumber,
  normalizeString,
} from "../utils/valueUtils";
import {
  getApiResponseFirstRow,
  getApiResponseStatus,
} from "../utils/apiResponseUtils";
import { COMMON_CONSTANTS, API_RESPONSE_TABLE } from "../constants/constants";

/**
 * Updates the authenticated TimePro user's profile.
 *
 * The API accepts the user details as query-string parameters and the
 * optional profile image as multipart/form-data. Authentication and token
 * refresh are handled by the shared Axios interceptors.
 *
 * Common ValidationErrors are handled globally by axiosSetup.js. Business
 * Success/Error responses are handled here because the API can return HTTP
 * 200 with Status = "Error".
 */
export async function saveAndUpdateUserDetails({
  isDeleted = false,
  createdBy,
  updatedBy,
  id,
  userName,
  fullName,
  mobileNo,
  userEmailID,
  roleID,
  employeeCode,
  isApprover = false,
  isEnableTwoFactorAuthentication = false,
  profileUserImage = null,
}) {
  const formData = new FormData();

  if (profileUserImage instanceof File) {
    formData.append(
      "ProfileUserImage",
      profileUserImage,
      profileUserImage.name,
    );
  }

  const response = await apiClient.post(
    master.save_and_update_user_details,
    formData,
    {
      params: {
        IsDeleted: normalizeBoolean(isDeleted),
        CreatedBy: normalizeNumber(createdBy),
        UpdatedBy: normalizeNumber(updatedBy),
        Id: normalizeNumber(id),
        UserName: normalizeString(userName),
        FullName: normalizeString(fullName),
        MobileNo: normalizeString(mobileNo),
        UserEmailID: normalizeString(userEmailID),
        RoleID: normalizeNumber(roleID),
        EmployeeCode: normalizeString(employeeCode),
        IsApprover: normalizeBoolean(isApprover),
        IsEnableTwoFactorAuthentication: normalizeBoolean(
          isEnableTwoFactorAuthentication,
        ),
      },
      headers: {
        "Content-Type": MultiPartFormDataContentType,
      },
    },
  );

  const payload = response?.data || {};
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.USER_DETAILS,
  );
  const status = getApiResponseStatus(payload);

  const message =
    details?.Message ||
    details?.message ||
    payload?.Message ||
    payload?.message ||
    (status === COMMON_CONSTANTS.SUCCESS
      ? "User Details Updated Successfully."
      : "Unable to update user details.");

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "UserProfileUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    details,
    message,
  };
}
