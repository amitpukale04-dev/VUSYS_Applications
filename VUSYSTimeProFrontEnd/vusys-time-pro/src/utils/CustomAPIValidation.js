import ToastService from "../services/toastService";

const getValidationMessage = (error) =>
  error?.Message ||
  error?.message ||
  error?.Error ||
  error?.error ||
  "Validation failed.";

/**
 * Displays common API ValidationErrors through the TimePro toast service.
 *
 * Example:
 * 1. Enter The Email ID.
 * 2. Enter The Password.
 *
 * Returns true when one or more validation errors were handled.
 */
export function handleApiValidationErrors(validationErrors, showToast) {
  if (!Array.isArray(validationErrors) || validationErrors.length === 0) {
    return false;
  }

  const toastMessage = validationErrors
    .map((error, index) => `${index + 1}. ${getValidationMessage(error)}`)
    .join("\n");

  if (typeof showToast === "function") {
    showToast(toastMessage, "error", "API Validations");
  } else {
    ToastService.error(toastMessage, "API Validations", {
      autoClose: 5000,
    });
  }

  return true;
}

/**
 * Returns the validation entry for a specific API field key.
 * Useful when a page also wants to highlight the corresponding input.
 */
export function getApiValidationError(validationErrors, key) {
  if (!Array.isArray(validationErrors) || !key) return null;

  const normalizedKey = String(key).trim().toLowerCase();

  return (
    validationErrors.find(
      (error) =>
        String(error?.Key || error?.key || "")
          .trim()
          .toLowerCase() === normalizedKey,
    ) || null
  );
}

/**
 * Applies API validation errors to the corresponding form controls.
 *
 * @param {Array} validationErrors - ValidationErrors returned by the API.
 * @param {Object} fieldMappings - Mapping between form fields and API keys.
 * @param {Function} setFieldError - Existing form field error setter.
 */
export const applyApiValidationErrors = (
  validationErrors,
  fieldMappings,
  setFieldError,
) => {
  if (
    !Array.isArray(validationErrors) ||
    !fieldMappings ||
    typeof setFieldError !== "function"
  ) {
    return;
  }

  // ----------------------------------------------------------
  // NORMALIZE API VALIDATION ERRORS
  // ----------------------------------------------------------

  const normalizedErrors = new Map();

  validationErrors.forEach((error) => {
    const key = String(error?.Key || error?.key || "")
      .trim()
      .toLowerCase();

    const message = String(error?.Message || error?.message || "").trim();

    if (key && message && !normalizedErrors.has(key)) {
      normalizedErrors.set(key, message);
    }
  });

  // ----------------------------------------------------------
  // APPLY ERRORS TO THE CORRESPONDING FORM CONTROLS
  // ----------------------------------------------------------

  Object.entries(fieldMappings).forEach(([fieldName, mapping]) => {
    const apiKeys = Array.isArray(mapping?.apiKeys) ? mapping.apiKeys : [];

    const defaultMessage = mapping?.defaultMessage || "Invalid value.";

    for (const apiKey of apiKeys) {
      const validationMessage = normalizedErrors.get(
        String(apiKey).trim().toLowerCase(),
      );

      if (validationMessage) {
        setFieldError(fieldName, validationMessage || defaultMessage);
        break;
      }
    }
  });
};

/** Maps API ValidationErrors keys such as "[0].ProjectID" to individual payload rows. */
export const applyApiArrayValidationErrors = (
  validationErrors,
  fieldMappings,
) => {
  const rowErrors = {};
  const generalErrors = [];
  const lookup = {};

  Object.entries(fieldMappings || {}).forEach(([field, config]) => {
    (config.apiKeys || []).forEach((key) => {
      lookup[String(key).toLowerCase()] = field;
    });
  });

  (Array.isArray(validationErrors) ? validationErrors : []).forEach((item) => {
    const key = String(item?.Key ?? item?.key ?? "").trim();
    const message = String(item?.Message ?? item?.message ?? "Invalid value.");
    const match = key.match(/^\[(\d+)\]\.(.+)$/);
    if (!match) {
      generalErrors.push(`${key ? `${key}: ` : ""}${message}`);
      return;
    }

    const index = Number(match[1]);
    const field = lookup[match[2].toLowerCase()];
    if (!field) {
      generalErrors.push(`Task ${index + 1} - ${key}: ${message}`);
      return;
    }
    if (!rowErrors[index]) rowErrors[index] = {};
    rowErrors[index][field] = rowErrors[index][field]
      ? `${rowErrors[index][field]}\n${message}`
      : message;
  });

  return { rowErrors, generalErrors };
};

/**
 * Clears an individual API validation error for an editable row field.
 */
export const clearRowValidationError = (setRowValidationErrors, fieldName) => {
  setRowValidationErrors((current) => {
    if (!current[fieldName]) return current;

    const next = { ...current };
    delete next[fieldName];

    return next;
  });
};
