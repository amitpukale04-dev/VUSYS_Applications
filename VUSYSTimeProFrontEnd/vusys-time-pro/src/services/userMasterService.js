import apiClient from "./apiClient";
import { master, MultiPartFormDataContentType } from "../utils/url";
import { API_RESPONSE_TABLE, COMMON_CONSTANTS } from "../constants/constants";
import {
  getApiResponseFirstRow,
  getApiResponseRows,
  getApiResponseStatus,
} from "../utils/apiResponseUtils";
import {
  normalizeBoolean,
  normalizeNumber,
  normalizeString,
} from "../utils/valueUtils";

const normalizeUserRow = (row = {}) => ({
  slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
  id: normalizeNumber(row?.Id ?? row?.ID ?? row?.id),
  userName: normalizeString(row?.UserName ?? row?.userName),
  fullName: normalizeString(row?.FullName ?? row?.fullName),
  mobileNo: normalizeString(row?.MobileNo ?? row?.mobileNo),
  userEmailID: normalizeString(row?.UserEmailID ?? row?.userEmailID),
  isDeleted: normalizeBoolean(row?.IsDeleted ?? row?.isDeleted),
  activeStatus: normalizeString(row?.ActiveStatus ?? row?.activeStatus),
  roleID: String(
    normalizeNumber(row?.RoleID ?? row?.RoleId ?? row?.roleID ?? row?.roleId),
  ),
  userRole: normalizeString(row?.UserRole ?? row?.userRole),
  isEmailVerified: normalizeBoolean(
    row?.IsEmailVerified ?? row?.isEmailVerified,
  ),
  isApprover: normalizeBoolean(row?.IsApprover ?? row?.isApprover),
  isEnableTwoFactorAuthentication: normalizeBoolean(
    row?.IsEnableTwoFactorAuthentication ??
      row?.isEnableTwoFactorAuthentication,
  ),
  userImage: normalizeString(row?.UserImage ?? row?.userImage),
  userImageFileName: normalizeString(
    row?.UserImageFileName ?? row?.userImageFileName,
  ),
  employeeCode: normalizeString(row?.EmployeeCode ?? row?.employeeCode),
  createdBy: normalizeNumber(row?.CreatedBy ?? row?.createdBy),
  updatedBy: normalizeNumber(row?.UpdatedBy ?? row?.updatedBy),
});

const getMessage = (payload, details, fallback) =>
  details?.Message ||
  details?.message ||
  payload?.Message ||
  payload?.message ||
  fallback;

export async function getUserList() {
  const response = await apiClient.post(master.get_user_list, { id: 0 });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.USER_LIST_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "User List Not Found"),
    );
    error.name = "UserMasterListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    users: rows.map(normalizeUserRow).filter((item) => item.id > 0),
  };
}

export async function getUserDetails(id) {
  const response = await apiClient.post(master.get_user_details, {
    id: normalizeNumber(id),
  });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.USER_DETAILS,
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details?.Id) {
    const error = new Error(
      getMessage(payload, details, "User Details Not Found"),
    );
    error.name = "UserMasterDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    user: normalizeUserRow(details),
  };
}

export async function saveAndUpdateUserMasterDetails({
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
  id = 0,
  userName = "",
  fullName = "",
  mobileNo = "",
  userEmailID = "",
  roleID = 0,
  employeeCode = "",
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
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.USER_DETAILS,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "User Details Saved Successfully."
      : "User Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "UserMasterSaveUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    id: normalizeNumber(details?.ID ?? details?.Id ?? details?.id),
    message,
  };
}

export async function deleteUserMaster(id, updatedBy = 0) {
  const { user } = await getUserDetails(id);

  return saveAndUpdateUserMasterDetails({
    ...user,
    id: user.id,
    roleID: user.roleID,
    isDeleted: true,
    createdBy: 0,
    updatedBy,
    profileUserImage: null,
  });
}
