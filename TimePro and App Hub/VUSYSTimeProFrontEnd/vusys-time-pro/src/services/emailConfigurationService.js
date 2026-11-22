import apiClient from "./apiClient";
import { configuration } from "../utils/url";
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

const normalizeEmailConfigurationRow = (row = {}) => ({
  id: normalizeNumber(row?.Id ?? row?.ID ?? row?.id),
  slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
  fromEmail: normalizeString(row?.FromEmail ?? row?.fromEmail),
  password: normalizeString(row?.Password ?? row?.password),
  plainPassword: normalizeString(row?.PlainPassword ?? row?.plainPassword),
  smtpHost: normalizeString(row?.SmtpHost ?? row?.SMTPHost ?? row?.smtpHost),
  portNo: normalizeNumber(row?.PortNo ?? row?.portNo),
  isDeleted: normalizeBoolean(row?.IsDeleted ?? row?.isDeleted),
  activeStatus: normalizeString(row?.ActiveStatus ?? row?.activeStatus),
});

const getMessage = (payload, details, fallback) =>
  details?.Message ||
  details?.message ||
  payload?.Message ||
  payload?.message ||
  fallback;

export async function getEmailConfigurationList() {
  const response = await apiClient.post(
    configuration.get_email_configuration_list,
    { id: 0 },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.EMAIL_CONFIGURATION_LIST_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Email Configuration List Not Found"),
    );
    error.name = "EmailConfigurationListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    items: rows
      .map(normalizeEmailConfigurationRow)
      .filter((item) => item.id > 0),
  };
}

export async function getEmailConfigurationDetails(id) {
  const response = await apiClient.post(
    configuration.get_email_configuration_details,
    { id: normalizeNumber(id) },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.EMAIL_CONFIGURATION_DETAILS_RESPONSE,
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details?.Id) {
    const error = new Error(
      getMessage(payload, details, "Email Configuration Details Not Found"),
    );
    error.name = "EmailConfigurationDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    item: normalizeEmailConfigurationRow(details),
  };
}

export async function saveAndUpdateEmailConfigurationDetails({
  id = 0,
  fromEmail = "",
  password = "",
  smtpHost = "",
  portNo = 0,
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
}) {
  const response = await apiClient.post(
    configuration.save_and_update_email_configuration_details,
    {
      id: normalizeNumber(id),
      fromEmail: normalizeString(fromEmail),
      password: normalizeString(password),
      smtpHost: normalizeString(smtpHost),
      portNo: normalizeNumber(portNo),
      isDeleted: normalizeBoolean(isDeleted),
      createdBy: normalizeNumber(createdBy),
      updatedBy: normalizeNumber(updatedBy),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.EMAIL_CONFIGURATION_DETAILS_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "Email Configuration Details Saved Successfully."
      : "Email Configuration Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "EmailConfigurationSaveUpdateError";
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

export async function deleteEmailConfiguration(id, updatedBy = 0) {
  const { item } = await getEmailConfigurationDetails(id);

  return saveAndUpdateEmailConfigurationDetails({
    id: item.id,
    fromEmail: item.fromEmail,
    password: item.plainPassword,
    smtpHost: item.smtpHost,
    portNo: item.portNo,
    isDeleted: true,
    createdBy: 0,
    updatedBy,
  });
}
