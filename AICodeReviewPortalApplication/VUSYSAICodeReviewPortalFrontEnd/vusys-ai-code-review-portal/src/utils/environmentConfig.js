const normalizeValue = (value, fallback = "") => {
  if (value === undefined || value === null) {
    return fallback;
  }

  const normalizedValue = String(value).trim();
  return normalizedValue || fallback;
};

const removeTrailingSlash = (value) => value.replace(/\/+$/, "");

/**
 * Reads the Vite environment values injected from the active .env file.
 *
 * Vite mode mapping:
 * - npm run dev / npm run build       -> .env
 * - npm run dev:staging               -> .env + .env.staging
 * - npm run build:staging             -> .env + .env.staging
 * - npm run build:prod                -> .env + .env.prod
 */
export const getEnvironmentConfig = () => {
  const apiBaseUrl = removeTrailingSlash(
    normalizeValue(import.meta.env.VITE_API_BASE_URL),
  );

  return Object.freeze({
    apiBaseUrl,
    environment: normalizeValue(
      import.meta.env.VITE_ENVIRONMENT,
      import.meta.env.MODE || "Development",
    ),
    versionNo: normalizeValue(import.meta.env.VITE_VERISON_NO, "1.0"),
    isEnableServerSideUserTabs: normalizeValue(import.meta.env.VITE_IsEnableServerSideUserTabs, "false").toLowerCase() === "true",
    mode: normalizeValue(import.meta.env.MODE, "development"),
    isDevelopment: Boolean(import.meta.env.DEV),
    isProductionBuild: Boolean(import.meta.env.PROD),
  });
};

export const APP_CONFIG = getEnvironmentConfig();

export const getApiBaseUrl = () => APP_CONFIG.apiBaseUrl;
export const getEnvironmentName = () => APP_CONFIG.environment;
export const getVersionNo = () => APP_CONFIG.versionNo;
export const getEnableServerSideUserTabs = () => APP_CONFIG.isEnableServerSideUserTabs;

export const buildApiUrl = (path = "") => {
  const normalizedPath = String(path || "").trim();

  if (!normalizedPath) {
    return APP_CONFIG.apiBaseUrl;
  }

  if (/^https?:\/\//i.test(normalizedPath)) {
    return normalizedPath;
  }

  return `${APP_CONFIG.apiBaseUrl}/${normalizedPath.replace(/^\/+/, "")}`;
};
