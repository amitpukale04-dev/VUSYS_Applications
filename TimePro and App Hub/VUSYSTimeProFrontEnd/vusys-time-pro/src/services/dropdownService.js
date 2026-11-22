import apiClient from "./apiClient";
import { master } from "../utils/url";
import { API_RESPONSE_TABLE, COMMON_CONSTANTS } from "../constants/constants";
import {
  getApiResponseFirstRow,
  getApiResponseRows,
  getApiResponseStatus,
} from "../utils/apiResponseUtils";
import { normalizeString } from "../utils/valueUtils";

// ============================================================
// DROPDOWN CACHE
// ============================================================

/*
 * Dropdown master data is reused across several TimePro screens.
 * Keep a small in-memory cache so navigating between screens does not
 * repeatedly wait for the same dropdown text to be resolved.
 *
 * The pending-request cache also prevents duplicate requests when two
 * controls request the same dropdown at the same time.
 */
const DROPDOWN_CACHE_TTL_MS = 5 * 60 * 1000;
const dropdownCache = new Map();
const dropdownPendingRequests = new Map();

const getDropdownCacheKey = ({
  sourceTable,
  argumentValue = "",
  argumentValue1 = "",
}) =>
  [
    normalizeString(sourceTable).trim().toLowerCase(),
    normalizeString(argumentValue).trim().toLowerCase(),
    normalizeString(argumentValue1).trim().toLowerCase(),
  ].join("::");

export function clearDropdownItemsCache(sourceTable = "") {
  const normalizedSource = normalizeString(sourceTable).trim().toLowerCase();

  if (!normalizedSource) {
    dropdownCache.clear();
    dropdownPendingRequests.clear();
    return;
  }

  [...dropdownCache.keys()].forEach((key) => {
    if (key.startsWith(`${normalizedSource}::`)) {
      dropdownCache.delete(key);
    }
  });

  [...dropdownPendingRequests.keys()].forEach((key) => {
    if (key.startsWith(`${normalizedSource}::`)) {
      dropdownPendingRequests.delete(key);
    }
  });
}

/**
 * Common dropdown service used throughout TimePro.
 *
 * API ValidationErrors are handled globally by axiosSetup.js.
 * Business Success/Error responses are handled here because the API can
 * return HTTP 200 with Status = "Error".
 */
export async function getDropdownItems({
  sourceTable,
  argumentValue = "",
  argumentValue1 = "",
  forceRefresh = false,
}) {
  const request = {
    sourceTable: normalizeString(sourceTable),
    argumentValue: normalizeString(argumentValue),
    argumentValue1: normalizeString(argumentValue1),
  };

  const cacheKey = getDropdownCacheKey(request);
  const cached = dropdownCache.get(cacheKey);

  if (
    !forceRefresh &&
    cached &&
    Date.now() - cached.cachedAt < DROPDOWN_CACHE_TTL_MS
  ) {
    return cached.result;
  }

  if (!forceRefresh && dropdownPendingRequests.has(cacheKey)) {
    return dropdownPendingRequests.get(cacheKey);
  }

  const pendingRequest = (async () => {
    const response = await apiClient.post(master.get_dropdown_items, request);

    const payload = response?.data || {};
    const status = getApiResponseStatus(payload);
    const rows = getApiResponseRows(payload, API_RESPONSE_TABLE.DROPDOWN_LIST);
    const firstRow = getApiResponseFirstRow(
      payload,
      API_RESPONSE_TABLE.DROPDOWN_LIST,
    );

    const message =
      firstRow?.Message ||
      firstRow?.message ||
      payload?.Message ||
      payload?.message ||
      (status === COMMON_CONSTANTS.SUCCESS
        ? "Dropdown items loaded successfully."
        : "Unable to load dropdown items.");

    if (status !== COMMON_CONSTANTS.SUCCESS) {
      const error = new Error(message);
      error.name = "DropdownItemsError";
      error.payload = payload;
      error.rows = rows;
      throw error;
    }

    const options = rows
      .filter(
        (item) =>
          item?.ValueText !== undefined &&
          item?.ValueText !== null &&
          item?.DisplayText !== undefined &&
          item?.DisplayText !== null,
      )
      .map((item) => ({
        value: String(item.ValueText),
        label: normalizeString(item.DisplayText),
      }));

    const result = {
      payload,
      rows,
      options,
      status,
      message,
    };

    dropdownCache.set(cacheKey, {
      result,
      cachedAt: Date.now(),
    });

    return result;
  })();

  dropdownPendingRequests.set(cacheKey, pendingRequest);

  try {
    return await pendingRequest;
  } finally {
    dropdownPendingRequests.delete(cacheKey);
  }
}
