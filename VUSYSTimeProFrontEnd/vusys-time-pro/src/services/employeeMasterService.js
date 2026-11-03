import apiClient from "./apiClient";
import { employee, MultiPartFormDataContentType } from "../utils/url";
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
import { toISODateOnly } from "../utils/dateUtils";
import { clearDropdownItemsCache } from "./dropdownService";

const getMessage = (payload, details, fallback) =>
  details?.Message ||
  details?.message ||
  payload?.Message ||
  payload?.message ||
  fallback;

const normalizeEmployeeRow = (row = {}) => {
  const activeStatus = normalizeString(
    row?.ActiveStatus ?? row?.activeStatus,
  );
  const isDeletedValue = row?.IsDeleted ?? row?.isDeleted;
  const isDeleted =
    isDeletedValue === undefined || isDeletedValue === null
      ? activeStatus.trim().toLowerCase() === "inactive"
      : normalizeBoolean(isDeletedValue);

  return {
    slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
    employeeId: normalizeNumber(
      row?.EmployeeID ?? row?.EmployeeId ?? row?.employeeID ?? row?.employeeId,
    ),
    employeeCode: normalizeString(row?.EmployeeCode ?? row?.employeeCode),
    fullName: normalizeString(row?.FullName ?? row?.fullName),
    genderID: String(
      normalizeNumber(row?.GenderID ?? row?.GenderId ?? row?.genderID ?? row?.genderId),
    ),
    genderName: normalizeString(row?.GenderName ?? row?.genderName),
    dateOfBirth: toISODateOnly(row?.DateofBirth ?? row?.DateOfBirth ?? row?.dateOfBirth),
    displayDateOfBirth: normalizeString(
      row?.DisplayDateofBirth ?? row?.DisplayDateOfBirth ?? row?.displayDateOfBirth,
    ),
    mobileNo: normalizeString(row?.MobileNo ?? row?.mobileNo),
    emailID: normalizeString(row?.EmailID ?? row?.EmailId ?? row?.emailID ?? row?.emailId),
    countryID: String(
      normalizeNumber(row?.CountryID ?? row?.CountryId ?? row?.countryID ?? row?.countryId),
    ),
    countryName: normalizeString(row?.CountryName ?? row?.countryName),
    stateID: String(
      normalizeNumber(row?.StateID ?? row?.StateId ?? row?.stateID ?? row?.stateId),
    ),
    stateName: normalizeString(row?.StateName ?? row?.stateName),
    cityName: normalizeString(row?.CityName ?? row?.cityName),
    pinCode: normalizeString(row?.PinCode ?? row?.Pincode ?? row?.pinCode ?? row?.pincode),
    skills: normalizeString(row?.Skills ?? row?.skills),
    employeeTypeID: String(
      normalizeNumber(
        row?.EmployeeTypeID ??
          row?.EmployeeTypeId ??
          row?.employeeTypeID ??
          row?.employeeTypeId,
      ),
    ),
    employeeType: normalizeString(row?.EmployeeType ?? row?.employeeType),
    parentCompanyName: normalizeString(
      row?.ParentCompanyName ?? row?.parentCompanyName,
    ),
    hireDate: toISODateOnly(row?.HireDate ?? row?.hireDate),
    displayHireDate: normalizeString(row?.DisplayHireDate ?? row?.displayHireDate),
    employeeProfileImage: normalizeString(
      row?.EmployeeProfileImage ?? row?.employeeProfileImage,
    ),
    employeeImageFileName: normalizeString(
      row?.EmployeeImageFileName ?? row?.employeeImageFileName,
    ),
    isDeleted,
    activeStatus: activeStatus || (isDeleted ? "Inactive" : "Active"),
  };
};

export async function getEmployeeList() {
  const response = await apiClient.post(employee.get_employee_list, { id: 0 });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(payload, API_RESPONSE_TABLE.EMPLOYEE_LIST_RESPONSE);
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Unable to load Employee List."),
    );
    error.name = "EmployeeListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    employees: rows
      .map(normalizeEmployeeRow)
      .filter((item) => item.employeeId > 0),
  };
}

export async function getEmployeeDetails(employeeId) {
  const response = await apiClient.post(employee.get_employee_details, {
    id: normalizeNumber(employeeId),
  });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.EMPLOYEE_DETAILS_RESPONSE,
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(
      getMessage(payload, details, "Employee Details Not Found"),
    );
    error.name = "EmployeeDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  const normalized = normalizeEmployeeRow(details);
  if (!normalized.employeeId) {
    const error = new Error("Employee Details Not Found");
    error.name = "EmployeeDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    employee: normalized,
  };
}

export async function saveAndUpdateEmployeeDetails({
  id = 0,
  fullName = "",
  genderID = 0,
  dateOfBirth = "",
  mobileNo = "",
  emailID = "",
  countryID = 0,
  stateID = 0,
  cityName = "",
  pinCode = "",
  skills = "",
  hireDate = "",
  employeeTypeID = 0,
  parentCompanyName = "",
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
  employeeProfileImage = null,
}) {
  const formData = new FormData();

  if (employeeProfileImage instanceof File) {
    formData.append(
      "EmployeeProfileImage",
      employeeProfileImage,
      employeeProfileImage.name,
    );
  }

  const response = await apiClient.post(
    employee.save_and_update_employee_details,
    formData,
    {
      params: {
        EmployeeID: normalizeNumber(id),
        FullName: normalizeString(fullName),
        GenderID: normalizeNumber(genderID),
        DateofBirth: normalizeString(dateOfBirth),
        MobileNo: normalizeString(mobileNo),
        EmailID: normalizeString(emailID),
        CountryID: normalizeNumber(countryID),
        StateID: normalizeNumber(stateID),
        CityName: normalizeString(cityName),
        PinCode: normalizeString(pinCode),
        Skills: normalizeString(skills),
        HireDate: normalizeString(hireDate),
        EmployeeTypeID: normalizeNumber(employeeTypeID),
        ParentCompanyName: normalizeString(parentCompanyName),
        IsDeleted: normalizeBoolean(isDeleted),
        CreatedBy: normalizeNumber(createdBy),
        UpdatedBy: normalizeNumber(updatedBy),
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
    API_RESPONSE_TABLE.EMPLOYEE_DETAILS_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "Employee Details Saved Successfully."
      : "Employee Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "EmployeeSaveUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  // Employee is a common dropdown source in User Master. Refresh its cache
  // after any successful Employee add/update/obsolete operation.
  clearDropdownItemsCache("Employee");

  return {
    payload,
    status,
    details,
    id: normalizeNumber(details?.Id ?? details?.ID ?? details?.id),
    message,
  };
}

export async function deleteEmployeeMaster(employeeId, updatedBy = 0) {
  const { employee: employeeDetails } = await getEmployeeDetails(employeeId);

  return saveAndUpdateEmployeeDetails({
    ...employeeDetails,
    id: employeeDetails.employeeId,
    dateOfBirth: employeeDetails.dateOfBirth,
    emailID: employeeDetails.emailID,
    employeeTypeID: employeeDetails.employeeTypeID,
    isDeleted: true,
    createdBy: 0,
    updatedBy,
    employeeProfileImage: null,
  });
}
