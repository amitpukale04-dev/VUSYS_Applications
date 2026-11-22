/**
 * Returns the first row from the specified API response table.
 *
 *
 * Usage:
 *
 * getApiResponseFirstRow(payload, "CHANGE_PASSWORD_RESPONSE");
 */
export const getApiResponseFirstRow = (payload, tableName) => {
  const rows = payload?.data?.[tableName];
  return Array.isArray(rows) && rows.length > 0 ? rows[0] : null;
};

/**
 * Returns the normalized Status value from an API response.
 *
 * Supports:
 * Status
 * status
 *
 * Example:
 * "Success" => "success"
 * " Error "  => "error"
 * undefined  => ""
 */
export const getApiResponseStatus = (payload) => {
  return String(payload?.Status || payload?.status || "")
    .trim()
    .toLowerCase();
};

/** Returns all rows from the specified API response table. */
export const getApiResponseRows = (payload, tableName) => {
  const rows = payload?.data?.[tableName];
  return Array.isArray(rows) ? rows : [];
};
