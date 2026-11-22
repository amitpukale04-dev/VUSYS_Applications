/**
 * Converts a value to boolean.
 */
export const normalizeBoolean = (value) => {
  if (typeof value === "boolean") {
    return value;
  }

  if (typeof value === "number") {
    return value === 1;
  }

  const normalizedValue = String(value ?? "")
    .trim()
    .toLowerCase();

  return (
    normalizedValue === "true" ||
    normalizedValue === "1" ||
    normalizedValue === "yes"
  );
};

/**
 * Converts a value to number.
 * Returns 0 when the value cannot be converted to a valid number.
 */
export const normalizeNumber = (value) => {
  const normalizedValue = Number(value);

  return Number.isFinite(normalizedValue) ? normalizedValue : 0;
};

/**
 * Converts a value to a trimmed string.
 * Null and undefined are converted to an empty string.
 */
export const normalizeString = (value) => {
  return String(value ?? "").trim();
};

/**
 * Returns true when value is:
 *
 * null
 * undefined
 * ""
 * "   "
 */
export const isNullOrWhiteSpace = (value) => {
  return value === null || value === undefined || String(value).trim() === "";
};

/**
 * Converts an array-like value into a unique array of valid numbers.
 * Invalid / empty values are ignored.
 *
 * Example:
 * [1, "10", null, "", 10] => [1, 10]
 */
export const normalizeNumberArray = (value) => {
  if (!Array.isArray(value)) {
    return [];
  }

  return [
    ...new Set(
      value.map((item) => Number(item)).filter((item) => Number.isFinite(item)),
    ),
  ];
};

/**
 * Converts a value to a lower case.
 * Null and undefined are converted to an empty string.
 */
export const normalizeText = (value) => {
  return String(value ?? "").trim().toLowerCase();
};

// ============================================================
// STRING CASE CONVERSION UTILITIES
// ============================================================

/**
 * Convert string to Lower Case.
 */
export const toLowerCase = (value) => {
  return String(value ?? "").toLowerCase();
};

/**
 * Convert string to Upper Case.
 */
export const toUpperCase = (value) => {
  return String(value ?? "").toUpperCase();
};

/**
 * Convert string to Proper Case.
 */
export const toProperCase = (value) => {
  return String(value ?? "")
    .toLowerCase()
    .replace(/\b[a-z]/g, (char) => char.toUpperCase());
};
