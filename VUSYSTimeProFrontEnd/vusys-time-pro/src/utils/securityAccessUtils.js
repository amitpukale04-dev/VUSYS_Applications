import { normalizeNumber, normalizeString } from "./valueUtils";

const toGroupKey = (screenName = "", index = 0) => {
  const normalized = normalizeString(screenName)
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");

  return normalized || `security-group-${index}`;
};

const normalizeSelectedSecurityIds = (rows = []) =>
  new Set(
    (Array.isArray(rows) ? rows : [])
      .map((row) => normalizeNumber(row?.SecurityID ?? row?.SecurityId))
      .filter((id) => id > 0),
  );

/**
 * Converts GetUserSecurityAccessList API data into the grouped model consumed
 * by SecurityAccess.jsx.
 *
 * Rules Applied:
 *   USER_SECURITY_ACCESS_SELECTED_LIST
 *
 * Rules Not Applied:
 *   USER_SECURITY_ACCESS_LIST MINUS USER_SECURITY_ACCESS_SELECTED_LIST
 */
export const buildSecurityAccessGroups = ({
  securityRows = [],
  selectedRows = [],
} = {}) => {
  const selectedIds = normalizeSelectedSecurityIds(selectedRows);
  const grouped = new Map();

  (Array.isArray(securityRows) ? securityRows : []).forEach((row, index) => {
    const securityId = normalizeNumber(row?.SecurityId ?? row?.SecurityID);
    const securityName = normalizeString(
      row?.SecurityName ?? row?.securityName,
    );
    const screenName =
      normalizeString(row?.ScreenName ?? row?.screenName) ||
      "Other Permissions";

    if (!securityId || !securityName) {
      return;
    }

    const key = toGroupKey(screenName, index);

    if (!grouped.has(key)) {
      grouped.set(key, {
        key,
        label: screenName,
        svgIcon: "security",
        tone: "indigo",
        rules: [],
      });
    }

    grouped.get(key).rules.push({
      id: securityId,
      label: securityName,
      roleId: normalizeNumber(row?.RoleID ?? row?.roleID),
      isDefault: Boolean(row?.IsDefault ?? row?.isDefault),
    });
  });

  const notApplied = [];
  const applied = [];

  grouped.forEach((group) => {
    const appliedRules = group.rules.filter((rule) => selectedIds.has(rule.id));
    const notAppliedRules = group.rules.filter(
      (rule) => !selectedIds.has(rule.id),
    );

    if (notAppliedRules.length > 0) {
      notApplied.push({
        ...group,
        rules: notAppliedRules,
      });
    }

    if (appliedRules.length > 0) {
      applied.push({
        ...group,
        rules: appliedRules,
      });
    }
  });

  return {
    notApplied,
    applied,
    appliedSecurityIds: selectedIds,
  };
};
