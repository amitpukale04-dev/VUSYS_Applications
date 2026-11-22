import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { useEffect, useMemo, useRef, useState } from "react";
import SearchableDropdown from "../../components/forms/SearchableDropdown";
import TextBox from "../../components/forms/TextBox";
import SvgIcon from "../../components/common/SvgIcon";
import { getDropdownItems } from "../../services/dropdownService";
import {
  getUserSecurityAccessList,
  saveUpdateUserSecurityAccessDetails,
} from "../../services/securityAccessService";
import ToastService from "../../services/toastService";
import {
  COMMON_MESSAGE,
  DROPDOWN_SOURCE_TABLE,
} from "../../constants/constants";
import { normalizeNumber, normalizeString } from "../../utils/valueUtils";
import { buildSecurityAccessGroups } from "../../utils/securityAccessUtils";
import { useAuth } from "../../context/AuthContext";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { showApiError } from "../../utils/showApiError";

// ============================================================
// COMMON HELPERS
// ============================================================

const cloneGroups = (groups = []) =>
  (Array.isArray(groups) ? groups : []).map((group) => ({
    ...group,
    rules: (group.rules || []).map((rule) => ({ ...rule })),
  }));

const buildExpandedState = (groups = [], expanded = true) =>
  Object.fromEntries(
    (Array.isArray(groups) ? groups : []).map((group) => [group.key, expanded]),
  );

const getVisibleGroups = (groups = [], searchText = "") => {
  const term = String(searchText || "")
    .trim()
    .toLowerCase();

  if (!term) {
    return groups;
  }

  return (Array.isArray(groups) ? groups : [])
    .map((group) => {
      const groupMatches = String(group.label || "")
        .toLowerCase()
        .includes(term);

      const matchingRules = (group.rules || []).filter((rule) =>
        String(rule.label || "")
          .toLowerCase()
          .includes(term),
      );

      if (!groupMatches && matchingRules.length === 0) {
        return null;
      }

      return {
        ...group,
        rules: groupMatches ? group.rules : matchingRules,
      };
    })
    .filter(Boolean);
};

// ============================================================
// PERMISSION CHECKBOX
// ============================================================

function PermissionCheckbox({
  checked,
  label,
  onChange,
  indeterminate = false,
  disabled = false,
}) {
  return (
    <button
      type="button"
      className="tp-permission-check tp-permission-check-button"
      onClick={(event) => {
        event.preventDefault();
        event.stopPropagation();

        if (disabled) {
          return;
        }

        onChange?.(!checked);
      }}
      disabled={disabled}
      role="checkbox"
      aria-checked={indeterminate ? "mixed" : checked}
      aria-label={label}
    >
      <span
        className={`tp-permission-box ${checked ? "is-checked" : ""} ${
          indeterminate ? "is-indeterminate" : ""
        }`}
        aria-hidden="true"
      >
        {checked && !indeterminate && <SvgIcon name="yes" size={14} />}
        {indeterminate && <span className="tp-permission-minus">−</span>}
      </span>

      <span>{label}</span>
    </button>
  );
}

// ============================================================
// SECURITY RULE PANEL
// ============================================================

function SecurityRulePanel({
  title,
  groups,
  selectedIds,
  expandedKeys,
  searchText,
  onSearchChange,
  onToggleRule,
  onToggleGroup,
  onToggleExpanded,
  onToggleAllExpanded,
  emptyText,
  disabled = false,
}) {
  const visibleGroups = useMemo(
    () => getVisibleGroups(groups, searchText),
    [groups, searchText],
  );

  const allExpanded =
    groups.length > 0 &&
    groups.every((group) => expandedKeys[group.key] !== false);

  return (
    <section className="tp-security-access-panel">
      <div className="tp-security-access-panel-head">
        <div className="tp-security-access-panel-title">
          <SvgIcon name="securityAccess" size={17} />
          <span>{title}</span>
        </div>

        <div className="tp-security-access-panel-actions">
          <span className="tp-security-access-count">{groups.length}</span>

          <button
            type="button"
            className="tp-security-access-expand-all"
            onClick={() => onToggleAllExpanded?.(!allExpanded)}
            disabled={disabled || groups.length === 0}
            title={allExpanded ? "Collapse All" : "Expand All"}
            aria-label={`${allExpanded ? "Collapse" : "Expand"} all ${title}`}
          >
            <SvgIcon
              name="chevron"
              size={15}
              className={allExpanded ? "is-open" : ""}
            />
          </button>
        </div>
      </div>

      <div className="tp-security-access-search">
        <TextBox
          name={`${title.replace(/\s+/g, "-").toLowerCase()}-search`}
          value={searchText}
          onChange={(event) => onSearchChange(event.target.value)}
          icon="bi-search"
          placeholder={`Search ${title.toLowerCase()}...`}
          disabled={disabled}
        />
      </div>

      <div className="tp-security-access-scroll">
        {visibleGroups.map((group) => {
          const rules = group.rules || [];
          const selectedCount = rules.filter((rule) =>
            selectedIds.has(rule.id),
          ).length;
          const allChecked = rules.length > 0 && selectedCount === rules.length;
          const indeterminate =
            selectedCount > 0 && selectedCount < rules.length;
          const expanded = expandedKeys[group.key] !== false;

          return (
            <div key={group.key} className="tp-security-access-group">
              <div className="tp-security-access-group-head">
                <button
                  type="button"
                  className="tp-security-access-expand"
                  onClick={() => onToggleExpanded(group.key)}
                  disabled={disabled}
                  aria-label={`${expanded ? "Collapse" : "Expand"} ${group.label}`}
                >
                  <SvgIcon
                    name="chevron"
                    size={15}
                    className={expanded ? "is-open" : ""}
                  />
                </button>

                <span
                  className={`tp-sidebar-svg tp-sidebar-svg-${
                    group.tone || "indigo"
                  }`}
                >
                  <SvgIcon name={group.svgIcon || "security"} size={15} />
                </span>

                <PermissionCheckbox
                  checked={allChecked}
                  indeterminate={indeterminate}
                  label={group.label}
                  onChange={(checked) => onToggleGroup(group, checked)}
                  disabled={disabled}
                />

                <span className="tp-security-access-group-count">
                  {selectedCount}/{rules.length}
                </span>
              </div>

              {expanded && (
                <div className="tp-security-access-rules">
                  {rules.map((rule) => (
                    <div key={rule.id} className="tp-security-access-rule">
                      <PermissionCheckbox
                        checked={selectedIds.has(rule.id)}
                        label={rule.label}
                        onChange={(checked) => onToggleRule(rule.id, checked)}
                        disabled={disabled}
                      />
                    </div>
                  ))}
                </div>
              )}
            </div>
          );
        })}

        {visibleGroups.length === 0 && (
          <div className="tp-security-access-empty">
            {disabled ? (
              <span
                className="spinner-border spinner-border-sm"
                aria-hidden="true"
              />
            ) : (
              <SvgIcon name="search" size={20} />
            )}
            <span>
              {disabled
                ? "Loading security access..."
                : searchText
                  ? "No matching permissions found."
                  : emptyText}
            </span>
          </div>
        )}
      </div>
    </section>
  );
}

// ============================================================
// SECURITY ACCESS
// ============================================================

export default function SecurityAccess() {
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityPermissionLoading } =
    useUserSecurityPermission();
  const canOpenScreen = hasPermission("Security Access", "Open");
  const canCreateSecurityAccess = hasPermission("Security Access", "Create");
  const canEditSecurityAccess = hasPermission("Security Access", "Edit");
  const canViewSecurityAccess = hasPermission("Security Access", "View");
  const [hasExistingSecurityAccess, setHasExistingSecurityAccess] =
    useState(false);
  const canManageSecurityAccess = hasExistingSecurityAccess
    ? canEditSecurityAccess
    : canCreateSecurityAccess;
  const canReadSecurityAccess =
    canViewSecurityAccess || canCreateSecurityAccess || canEditSecurityAccess;
  const { user, refreshUserSecurityAccess, refreshMenuAccess } = useAuth();

  // ==========================================================
  // STATE
  // ==========================================================

  const [userDropdownLoading, setUserDropdownLoading] = useState(false);
  const [securityAccessLoading, setSecurityAccessLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [saving, setSaving] = useState(false);

  const [userOptions, setUserOptions] = useState([]);
  const [selectedUserId, setSelectedUserId] = useState("");
  const [selectedRoleId, setSelectedRoleId] = useState("");

  const [notApplied, setNotApplied] = useState([]);
  const [applied, setApplied] = useState([]);

  const [selectedNotAppliedIds, setSelectedNotAppliedIds] = useState(new Set());
  const [selectedAppliedIds, setSelectedAppliedIds] = useState(new Set());

  const [expandedNotApplied, setExpandedNotApplied] = useState({});
  const [expandedApplied, setExpandedApplied] = useState({});

  const [notAppliedSearch, setNotAppliedSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");

  /*
   * Prevent an older API response from replacing the latest selected User's
   * permissions when the dropdown is changed quickly.
   */
  const requestSequenceRef = useRef(0);

  // ==========================================================
  // RESET / BIND SECURITY ACCESS
  // ==========================================================

  const initializeSecurityAccess = ({
    notAppliedGroups = [],
    appliedGroups = [],
  } = {}) => {
    const normalizedNotApplied = cloneGroups(notAppliedGroups);
    const normalizedApplied = cloneGroups(appliedGroups);

    setNotApplied(normalizedNotApplied);
    setApplied(normalizedApplied);
    setHasExistingSecurityAccess(
      normalizedApplied.some((group) => (group.rules || []).length > 0),
    );

    /*
     * Checkboxes in both panels are selection controls only.
     * They do NOT represent whether a rule is applied.
     *
     * Applied / Not Applied is represented by the panel in which the rule
     * currently exists, therefore both panels always start unchecked.
     */
    setSelectedNotAppliedIds(new Set());
    setSelectedAppliedIds(new Set());

    setExpandedNotApplied(buildExpandedState(normalizedNotApplied));
    setExpandedApplied(buildExpandedState(normalizedApplied));

    setNotAppliedSearch("");
    setAppliedSearch("");
  };

  // ==========================================================
  // LOAD USER + ROLE DROPDOWN
  // ==========================================================

  const loadUserDropdown = async () => {
    setUserDropdownLoading(true);

    try {
      const result = await getDropdownItems({
        sourceTable: DROPDOWN_SOURCE_TABLE.USER_AND_ROLE,
        argumentValue: "",
        argumentValue1: "",
      });

      const options = (Array.isArray(result?.rows) ? result.rows : [])
        .filter(
          (row) =>
            normalizeNumber(row?.UserValueText) > 0 &&
            normalizeNumber(row?.RoleValueText) > 0 &&
            normalizeString(row?.UserDisplayText),
        )
        .map((row) => ({
          value: String(normalizeNumber(row.UserValueText)),
          label: normalizeString(row.UserDisplayText),
          userId: normalizeNumber(row.UserValueText),
          roleId: normalizeNumber(row.RoleValueText),
          roleName: normalizeString(row.RoleDisplayText),
        }));

      setUserOptions(options);

      if (options.length > 0) {
        setSelectedUserId(String(options[0].userId));
        setSelectedRoleId(String(options[0].roleId));
      } else {
        setSelectedUserId("");
        setSelectedRoleId("");
        initializeSecurityAccess();
      }
    } catch (error) {
      setUserOptions([]);
      setSelectedUserId("");
      setSelectedRoleId("");
      initializeSecurityAccess();

      /*
       * API ValidationErrors, Network/CORS and GLOBAL_ERROR_RESPONSE are
       * already displayed globally by axiosSetup.js.
       */
      showApiError(error, "Unable to load Users.");
    } finally {
      setUserDropdownLoading(false);
    }
  };

  // ==========================================================
  // LOAD USER SECURITY ACCESS
  // ==========================================================

  const loadSecurityAccess = async (
    userId,
    roleId,
    { isRefresh = false } = {},
  ) => {
    const normalizedUserId = normalizeNumber(userId);
    const normalizedRoleId = normalizeNumber(roleId);

    if (!normalizedUserId || !normalizedRoleId) {
      initializeSecurityAccess();
      return;
    }

    const requestSequence = ++requestSequenceRef.current;

    if (isRefresh) {
      setRefreshing(true);
    } else {
      setSecurityAccessLoading(true);
    }

    try {
      const result = await getUserSecurityAccessList({
        userID: normalizedUserId,
        roleID: normalizedRoleId,
      });

      if (requestSequence !== requestSequenceRef.current) {
        return;
      }

      const { notApplied: notAppliedGroups, applied: appliedGroups } =
        buildSecurityAccessGroups({
          securityRows: result?.securityRows || [],
          selectedRows: result?.selectedRows || [],
        });

      initializeSecurityAccess({
        notAppliedGroups,
        appliedGroups,
      });
    } catch (error) {
      if (requestSequence !== requestSequenceRef.current) {
        return;
      }

      initializeSecurityAccess();

      /*
       * API ValidationErrors, Network/CORS and GLOBAL_ERROR_RESPONSE are
       * already displayed globally by axiosSetup.js.
       */
      showApiError(error, "Unable to load User Security Access.");
    } finally {
      if (requestSequence === requestSequenceRef.current) {
        setSecurityAccessLoading(false);
        setRefreshing(false);
      }
    }
  };

  // ==========================================================
  // INITIAL LOAD
  // ==========================================================

  useEffect(() => {
    if (securityPermissionLoading || !canOpenScreen || !canReadSecurityAccess) {
      return;
    }

    loadUserDropdown();
  }, [securityPermissionLoading, canOpenScreen, canReadSecurityAccess]);

  useEffect(() => {
    if (
      securityPermissionLoading ||
      !canOpenScreen ||
      !canReadSecurityAccess ||
      !selectedUserId ||
      !selectedRoleId
    ) {
      return;
    }

    loadSecurityAccess(selectedUserId, selectedRoleId);
  }, [
    selectedUserId,
    selectedRoleId,
    securityPermissionLoading,
    canOpenScreen,
    canReadSecurityAccess,
  ]);

  // ==========================================================
  // USER CHANGE
  // ==========================================================

  const handleUserChange = (event) => {
    const userId = String(event?.target?.value || "");
    const selectedOption = userOptions.find(
      (option) => String(option.userId) === userId,
    );

    setSelectedUserId(userId);
    setSelectedRoleId(selectedOption ? String(selectedOption.roleId) : "");
  };

  // ==========================================================
  // CHECKBOX SELECTION
  // ==========================================================

  const toggleSelectedId = (setter, id, checked) => {
    setter((current) => {
      const next = new Set(current);

      if (checked) {
        next.add(id);
      } else {
        next.delete(id);
      }

      return next;
    });
  };

  const toggleGroupSelection = (setter, group, checked) => {
    setter((current) => {
      const next = new Set(current);

      (group.rules || []).forEach((rule) => {
        if (checked) {
          next.add(rule.id);
        } else {
          next.delete(rule.id);
        }
      });

      return next;
    });
  };

  // ==========================================================
  // MOVE RULES BETWEEN PANELS
  // ==========================================================

  const moveSelectedRules = ({
    source,
    setSource,
    target,
    setTarget,
    selectedIds,
    clear,
  }) => {
    if (selectedIds.size === 0) {
      return [];
    }

    const movedRuleIds = [];
    const movedByGroup = new Map();

    const remainingSource = source
      .map((group) => {
        const movedRules = (group.rules || []).filter((rule) =>
          selectedIds.has(rule.id),
        );

        const remainingRules = (group.rules || []).filter(
          (rule) => !selectedIds.has(rule.id),
        );

        movedRules.forEach((rule) => movedRuleIds.push(rule.id));

        if (movedRules.length > 0) {
          movedByGroup.set(group.key, {
            ...group,
            rules: movedRules,
          });
        }

        return remainingRules.length > 0
          ? {
              ...group,
              rules: remainingRules,
            }
          : null;
      })
      .filter(Boolean);

    const nextTarget = cloneGroups(target);

    movedByGroup.forEach((movedGroup) => {
      const existingIndex = nextTarget.findIndex(
        (group) => group.key === movedGroup.key,
      );

      if (existingIndex >= 0) {
        const existingIds = new Set(
          nextTarget[existingIndex].rules.map((rule) => rule.id),
        );

        nextTarget[existingIndex] = {
          ...nextTarget[existingIndex],
          rules: [
            ...nextTarget[existingIndex].rules,
            ...movedGroup.rules.filter((rule) => !existingIds.has(rule.id)),
          ],
        };
      } else {
        nextTarget.push(movedGroup);
      }
    });

    setSource(remainingSource);
    setTarget(nextTarget);
    clear(new Set());

    return movedRuleIds;
  };

  const handleMoveRight = () => {
    if (!canManageSecurityAccess) return;
    const movedIds = moveSelectedRules({
      source: notApplied,
      setSource: setNotApplied,
      target: applied,
      setTarget: setApplied,
      selectedIds: selectedNotAppliedIds,
      clear: setSelectedNotAppliedIds,
    });

    if (movedIds.length === 0) {
      return;
    }

    /*
     * Moved rules must remain unchecked in Rules Applied.
     * Checkboxes are used only to choose the next transfer operation.
     */
    setSelectedAppliedIds((current) => {
      const next = new Set(current);
      movedIds.forEach((id) => next.delete(id));
      return next;
    });

    setExpandedApplied((current) => ({
      ...current,
      ...Object.fromEntries(
        notApplied
          .filter((group) =>
            (group.rules || []).some((rule) => movedIds.includes(rule.id)),
          )
          .map((group) => [group.key, true]),
      ),
    }));
  };

  const handleMoveLeft = () => {
    if (!canManageSecurityAccess) return;
    const movedIds = moveSelectedRules({
      source: applied,
      setSource: setApplied,
      target: notApplied,
      setTarget: setNotApplied,
      selectedIds: selectedAppliedIds,
      clear: setSelectedAppliedIds,
    });

    if (movedIds.length === 0) {
      return;
    }

    /*
     * Moved rules must remain unchecked in Rules Not Applied.
     * Only the rules explicitly checked by the user are moved.
     */
    setSelectedNotAppliedIds((current) => {
      const next = new Set(current);
      movedIds.forEach((id) => next.delete(id));
      return next;
    });

    setExpandedNotApplied((current) => ({
      ...current,
      ...Object.fromEntries(
        applied
          .filter((group) =>
            (group.rules || []).some((rule) => movedIds.includes(rule.id)),
          )
          .map((group) => [group.key, true]),
      ),
    }));
  };

  // ==========================================================
  // REFRESH
  // ==========================================================

  const handleRefresh = async () => {
    if (
      !selectedUserId ||
      !selectedRoleId ||
      refreshing ||
      securityAccessLoading
    ) {
      return;
    }

    await loadSecurityAccess(selectedUserId, selectedRoleId, {
      isRefresh: true,
    });
  };

  // ==========================================================
  // BUILD SAVE PAYLOAD
  // ==========================================================

  const buildSavePayload = () => {
    const normalizedUserId = normalizeNumber(selectedUserId);
    const normalizedRoleId = normalizeNumber(selectedRoleId);
    const loggedInUserId = normalizeNumber(
      user?.userId || user?.id || user?.Id || user?.ID || 0,
    );

    if (!normalizedUserId || !normalizedRoleId) {
      return [];
    }

    return applied.flatMap((group) =>
      (group.rules || [])
        .map((rule) => normalizeNumber(rule?.id))
        .filter((securityID) => securityID > 0)
        .map((securityID) => ({
          isDeleted: false,
          createdBy: loggedInUserId,
          updatedBy: loggedInUserId,
          securityAccessId: 0,
          securityID,
          roleID: normalizedRoleId,
          userID: normalizedUserId,
        })),
    );
  };

  // ==========================================================
  // SAVE / UPDATE USER SECURITY ACCESS
  // ==========================================================

  const handleSave = async () => {
    if (!canManageSecurityAccess) return;
    if (
      saving ||
      refreshing ||
      securityAccessLoading ||
      userDropdownLoading ||
      !selectedUserId ||
      !selectedRoleId
    ) {
      return;
    }

    const requestPayload = buildSavePayload();

    if (requestPayload.length === 0) {
      ToastService.error(COMMON_MESSAGE.NO_CHANGES_MADE_TO_FORM);
      return;
    }

    const loaderRequestId = showFullPageLoader(
      "Saving Security Access Details...",
    );
    setSaving(true);

    try {
      const result = await saveUpdateUserSecurityAccessDetails(requestPayload);

      ToastService.success(
        result?.message || "Security Access Details Saved Successfully.",
      );

      /*
       * Reload from the API so both panels reflect the persisted state and
       * all selection checkboxes return to unchecked.
       */
      await loadSecurityAccess(selectedUserId, selectedRoleId);

      /*
       * If the logged-in user's own Security Access was changed, reload
       * USER_SECURITY_ACCESS_SELECTED_LIST from the backend immediately.
       *
       * The selected-list API is the only source of truth for action permissions;
       * no SecurityID is defined or inferred by the frontend.
       */
      const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

      if (normalizeNumber(selectedUserId) === loggedInUserId) {
        const updatedUser = await refreshUserSecurityAccess?.(user);

        /*
         * Refresh Sidebar/Search/ProtectedRoute and action access using the
         * dynamically reloaded Security IDs, without requiring Logout/Login.
         */
        await refreshMenuAccess?.(updatedUser || user);
      }
    } catch (error) {
      /*
       * API ValidationErrors, Network/CORS and GLOBAL_ERROR_RESPONSE are
       * already displayed globally by axiosSetup.js.
       */
      showApiError(error, "Unable to save Security Access details.");
    } finally {
      hideFullPageLoader(loaderRequestId);
      setSaving(false);
    }
  };

  const isBusy =
    userDropdownLoading || securityAccessLoading || refreshing || saving;

  // ==========================================================
  // UI
  // ==========================================================

  if (securityPermissionLoading) {
    return (
      <div className="tp-form-page-loading">
        <span className="spinner-border text-primary" aria-hidden="true" />
        <p>Loading security permissions...</p>
      </div>
    );
  }

  if (!canOpenScreen || !canReadSecurityAccess) return <AccessDenied />;

  return (
    <div className="tp-security-access-page">
      <div className="tp-form-page-heading tp-form-titlebar tp-security-access-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="securityAccess" size={18} />
          <h1>Security Access</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          {canManageSecurityAccess && (
            <button
              type="button"
              className="tp-form-page-icon-button"
              onClick={handleSave}
              disabled={
                !selectedUserId ||
                !selectedRoleId ||
                isBusy ||
                applied.length === 0
              }
              title="Save Security Access"
              aria-label="Save Security Access"
            >
              {saving ? (
                <span
                  className="spinner-border spinner-border-sm"
                  aria-hidden="true"
                />
              ) : (
                <SvgIcon name="save" size={18} />
              )}
            </button>
          )}

          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={handleRefresh}
            disabled={!selectedUserId || !selectedRoleId || isBusy}
            title="Refresh Security Access"
            aria-label="Refresh Security Access"
          >
            {refreshing ? (
              <span
                className="spinner-border spinner-border-sm"
                aria-hidden="true"
              />
            ) : (
              <SvgIcon name="refresh" size={18} />
            )}
          </button>
        </div>
      </div>

      <section className="tp-card tp-security-access-card">
        <div className="tp-security-access-toolbar">
          <div className="tp-security-access-user-field">
            <SearchableDropdown
              name="userId"
              label="User"
              required
              value={selectedUserId}
              onChange={handleUserChange}
              options={userOptions}
              columns={[
                {
                  key: "label",
                  label: "User",
                },
                {
                  key: "roleName",
                  label: "Role",
                },
              ]}
              placeholder="Search and Select User."
              disabled={isBusy}
            />
          </div>

          <div className="tp-security-access-help">
            <SvgIcon name="security" size={16} />
            <span>
              Select action permissions and move them between sections.
            </span>
          </div>
        </div>

        <div className="tp-security-access-workspace">
          <SecurityRulePanel
            title="Rules Not Applied"
            groups={notApplied}
            selectedIds={selectedNotAppliedIds}
            expandedKeys={expandedNotApplied}
            searchText={notAppliedSearch}
            onSearchChange={setNotAppliedSearch}
            onToggleRule={(id, checked) =>
              toggleSelectedId(setSelectedNotAppliedIds, id, checked)
            }
            onToggleGroup={(group, checked) =>
              toggleGroupSelection(setSelectedNotAppliedIds, group, checked)
            }
            onToggleExpanded={(key) =>
              setExpandedNotApplied((current) => ({
                ...current,
                [key]: current[key] === false,
              }))
            }
            onToggleAllExpanded={(expanded) =>
              setExpandedNotApplied(buildExpandedState(notApplied, expanded))
            }
            emptyText="No permissions are available to apply."
            disabled={isBusy || !canManageSecurityAccess}
          />

          <div
            className="tp-security-access-transfer"
            aria-label="Transfer permissions"
          >
            <button
              type="button"
              className="tp-security-access-transfer-button"
              onClick={handleMoveRight}
              disabled={
                isBusy ||
                !canManageSecurityAccess ||
                selectedNotAppliedIds.size === 0
              }
              title="Apply Selected Rules"
              aria-label="Apply Selected Rules"
            >
              <SvgIcon name="next" size={20} />
            </button>

            <button
              type="button"
              className="tp-security-access-transfer-button"
              onClick={handleMoveLeft}
              disabled={
                isBusy ||
                !canManageSecurityAccess ||
                selectedAppliedIds.size === 0
              }
              title="Remove Selected Rules"
              aria-label="Remove Selected Rules"
            >
              <SvgIcon name="previous" size={20} />
            </button>
          </div>

          <SecurityRulePanel
            title="Rules Applied"
            groups={applied}
            selectedIds={selectedAppliedIds}
            expandedKeys={expandedApplied}
            searchText={appliedSearch}
            onSearchChange={setAppliedSearch}
            onToggleRule={(id, checked) =>
              toggleSelectedId(setSelectedAppliedIds, id, checked)
            }
            onToggleGroup={(group, checked) =>
              toggleGroupSelection(setSelectedAppliedIds, group, checked)
            }
            onToggleExpanded={(key) =>
              setExpandedApplied((current) => ({
                ...current,
                [key]: current[key] === false,
              }))
            }
            onToggleAllExpanded={(expanded) =>
              setExpandedApplied(buildExpandedState(applied, expanded))
            }
            emptyText="No permissions are currently applied."
            disabled={isBusy || !canManageSecurityAccess}
          />
        </div>
      </section>
    </div>
  );
}
