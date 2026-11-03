import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { getMenuIconPresentation } from "../../utils/menuIconPresentation";
import { useEffect, useRef, useState } from "react";
import SvgIcon from "../../components/common/SvgIcon";
import Dropdown from "../../components/forms/Dropdown";
import { useAuth } from "../../context/AuthContext";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import {
  getRoleMenuAccess,
  saveUpdateRoleMenuAccessDetails,
} from "../../services/menuAccessService";
import { getDropdownItems } from "../../services/dropdownService";
import ToastService from "../../services/toastService";
import {
  COMMON_MESSAGE,
  DROPDOWN_SOURCE_TABLE,
} from "../../constants/constants";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy, getRoleId } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";

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
  const handleClick = (event) => {
    event.preventDefault();
    event.stopPropagation();

    if (disabled) {
      return;
    }

    onChange?.(!checked);
  };

  return (
    <button
      type="button"
      className="tp-permission-check tp-permission-check-button"
      onClick={handleClick}
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
// MENU ACCESS FOR ROLE
// ============================================================

export default function MenuAccessForRole() {
  const { refreshMenuAccess } = useAuth();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();
  const canOpenScreen = hasPermission("Role Menu Access", "Open");

  // ==========================================================
  // STATE
  // ==========================================================

  const [expandedKeys, setExpandedKeys] = useState({});

  const [refreshing, setRefreshing] = useState(false);

  const [updating, setUpdating] = useState(false);

  const [roleDropdownLoading, setRoleDropdownLoading] = useState(false);

  const [roleMenuAccessLoading, setRoleMenuAccessLoading] = useState(false);

  const [selectedRoleId, setSelectedRoleId] = useState("");

  const [roleOptions, setRoleOptions] = useState([]);

  const [roleMenuItems, setRoleMenuItems] = useState([]);

  const [checkedMenus, setCheckedMenus] = useState({});

  /*
   * Prevents an older API response from replacing the TreeView
   * when the user changes the Role dropdown quickly.
   */
  const requestSequenceRef = useRef(0);
  const originalCheckedMenusRef = useRef({});

  // ==========================================================
  // LOGGED-IN USER DETAILS
  // ==========================================================

  const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

  const loggedInRoleId = normalizeNumber(getRoleId());

  // ==========================================================
  // INITIALIZE TREE FROM API RESPONSE
  // ==========================================================

  const initializeTree = (items = []) => {
    const normalizedItems = Array.isArray(items) ? items : [];

    setRoleMenuItems(normalizedItems);

    // --------------------------------------------------------
    // EXPAND ALL MAIN MENUS
    // --------------------------------------------------------

    setExpandedKeys(
      Object.fromEntries(
        normalizedItems
          .filter((item) => item.children?.length)
          .map((item) => [item.key, true]),
      ),
    );

    // --------------------------------------------------------
    // INITIALIZE CHECKBOXES USING IsSelected
    // --------------------------------------------------------

    /*
     * /api/Master/GetUserMenuAccess
     *
     * paramType = "RoleMenuAccess"
     *
     * now returns:
     *
     * IsSelected = true
     *      Checkbox Checked
     *
     * IsSelected = false
     *      Checkbox Unchecked
     *
     * Do NOT automatically check every returned SubMenu.
     */

    const initialCheckedMenus = {};
    normalizedItems.forEach((item) => {
      (item.children || []).forEach((child) => {
        initialCheckedMenus[child.key] =
          child.isSelected === true || child.IsSelected === true;
      });
    });

    setCheckedMenus(initialCheckedMenus);
    originalCheckedMenusRef.current = { ...initialCheckedMenus };
  };

  // ==========================================================
  // LOAD ROLE DROPDOWN
  // ==========================================================

  const loadRoleDropdown = async () => {
    if (roleDropdownLoading) {
      return;
    }

    setRoleDropdownLoading(true);

    try {
      const result = await getDropdownItems({
        sourceTable: DROPDOWN_SOURCE_TABLE.ROLE_MASTER,
        argumentValue: "",
        argumentValue1: "",
      });

      const options = result?.options || [];

      setRoleOptions(options);

      /*
       * Default to the logged-in user's Role when it exists
       * in the dropdown.
       *
       * Otherwise use the first available Role.
       */
      const loggedInRoleExists = options.some(
        (option) => normalizeNumber(option.value) === loggedInRoleId,
      );

      if (loggedInRoleExists) {
        setSelectedRoleId(String(loggedInRoleId));
      } else if (options.length > 0) {
        setSelectedRoleId(String(options[0].value));
      } else {
        setSelectedRoleId("");

        initializeTree([]);
      }
    } catch (error) {
      setRoleOptions([]);

      setSelectedRoleId("");

      initializeTree([]);

      /*
       * API ValidationErrors, Network/CORS and
       * GLOBAL_ERROR_RESPONSE are handled globally
       * by axiosSetup.js.
       */
      showApiError(error, "Unable to load Roles.");
    } finally {
      setRoleDropdownLoading(false);
    }
  };

  // ==========================================================
  // LOAD MENU ACCESS FOR SELECTED ROLE
  // ==========================================================

  const loadRoleMenuAccess = async (roleId, { isRefresh = false } = {}) => {
    const normalizedRoleId = normalizeNumber(roleId);

    if (!loggedInUserId || !normalizedRoleId) {
      initializeTree([]);

      return;
    }

    const requestSequence = ++requestSequenceRef.current;

    if (isRefresh) {
      setRefreshing(true);
    } else {
      setRoleMenuAccessLoading(true);
    }

    try {
      // ======================================================
      // GET ROLE MENU ACCESS
      // ======================================================

      /*
       * IMPORTANT:
       *
       * Continue using menuAccessService.
       *
       * No axios API call is made directly from this component.
       *
       * Request:
       *
       * {
       *   userId: 1,
       *   roleID: 1,
       *   paramType: "RoleMenuAccess"
       * }
       */

      const request = {
        userId: loggedInUserId,
        roleID: normalizedRoleId,
      };

      const result = await getRoleMenuAccess(request);

      /*
       * Ignore an older response when the Role was changed
       * before the previous request completed.
       */
      if (requestSequence !== requestSequenceRef.current) {
        return;
      }

      // ======================================================
      // BIND TREE
      // ======================================================

      /*
       * getRoleMenuAccess() returns the normalized menuItems.
       *
       * Each child should now contain:
       *
       * isSelected
       *
       * which comes from API:
       *
       * IsSelected
       */

      initializeTree(result?.menuItems || []);
    } catch (error) {
      if (requestSequence === requestSequenceRef.current) {
        initializeTree([]);
      }

      /*
       * API ValidationErrors, Network/CORS and
       * GLOBAL_ERROR_RESPONSE are handled globally
       * by axiosSetup.js.
       */
      showApiError(error, "Unable to load menu access.");
    } finally {
      if (requestSequence === requestSequenceRef.current) {
        setRoleMenuAccessLoading(false);

        setRefreshing(false);
      }
    }
  };

  // ==========================================================
  // LOAD ROLE DROPDOWN ON PAGE LOAD
  // ==========================================================

  useEffect(() => {
    if (securityAccessLoading || !canOpenScreen) return undefined;
    loadRoleDropdown();
  }, [securityAccessLoading, canOpenScreen]);

  // ==========================================================
  // BIND TREE WHEN ROLE CHANGES
  // ==========================================================

  useEffect(() => {
    if (
      securityAccessLoading ||
      !canOpenScreen ||
      !selectedRoleId ||
      !loggedInUserId
    ) {
      initializeTree([]);

      return;
    }

    loadRoleMenuAccess(selectedRoleId);
  }, [selectedRoleId, loggedInUserId, securityAccessLoading, canOpenScreen]);

  // ==========================================================
  // REFRESH SELECTED ROLE MENU ACCESS
  // ==========================================================

  const handleRefresh = async () => {
    if (
      refreshing ||
      roleDropdownLoading ||
      roleMenuAccessLoading ||
      updating ||
      !selectedRoleId ||
      !loggedInUserId
    ) {
      return;
    }

    await loadRoleMenuAccess(selectedRoleId, {
      isRefresh: true,
    });
  };

  // ==========================================================
  // ROLE CHANGE
  // ==========================================================

  const handleRoleChange = (event) => {
    const roleId = event?.target?.value ?? event ?? "";

    setSelectedRoleId(String(roleId));
  };

  // ==========================================================
  // CHILD CHECKBOX CHANGE
  // ==========================================================

  const handleChildChange = (childKey, checked) => {
    setCheckedMenus((current) => ({
      ...current,

      [childKey]: checked,
    }));
  };

  // ==========================================================
  // PARENT CHECKBOX CHANGE
  // ==========================================================

  const handleParentChange = (children, checked) => {
    setCheckedMenus((current) => {
      const next = {
        ...current,
      };

      children.forEach((child) => {
        next[child.key] = checked;
      });

      return next;
    });
  };

  // ==========================================================
  // GET PARENT CHECKBOX STATE
  // ==========================================================

  const getParentCheckboxState = (children) => {
    if (!children.length) {
      return {
        checked: false,

        indeterminate: false,
      };
    }

    const selectedCount = children.filter((child) =>
      Boolean(checkedMenus[child.key]),
    ).length;

    return {
      checked: selectedCount === children.length,

      indeterminate: selectedCount > 0 && selectedCount < children.length,
    };
  };

  // ==========================================================
  // GET SELECTED CHILD COUNT
  // ==========================================================

  const getSelectedChildCount = (children) =>
    children.filter((child) => Boolean(checkedMenus[child.key])).length;

  // ==========================================================
  // BUILD UPDATE PAYLOAD
  // ==========================================================

  const buildRoleMenuAccessPayload = () => {
    const roleID = normalizeNumber(selectedRoleId);

    return roleMenuItems.flatMap((item) =>
      (item.children || [])
        // ------------------------------------------------------
        // SEND ONLY CHECKED SUB MENUS
        // ------------------------------------------------------
        .filter((child) => Boolean(checkedMenus[child.key]))

        // ------------------------------------------------------
        // BUILD SECURITY ACCESS PAYLOAD
        // ------------------------------------------------------
        .flatMap((child) => {
          const securityAccessIds = Array.isArray(child.securityAccessIds)
            ? child.securityAccessIds
            : [];

          return securityAccessIds
            .map((securityID) => normalizeNumber(securityID))
            .filter((securityID) => securityID > 0)
            .map((securityID) => ({
              isDeleted: false,
              createdBy: loggedInUserId,
              updatedBy: loggedInUserId,
              roleMenuAccessFrontEndId: 0,
              roleID,
              securityID,
            }));
        }),
    );
  };

  // ==========================================================
  // UPDATE ROLE MENU ACCESS
  // ==========================================================

  const handleUpdate = async () => {
    if (
      updating ||
      refreshing ||
      roleDropdownLoading ||
      roleMenuAccessLoading ||
      !selectedRoleId ||
      !loggedInUserId
    ) {
      return;
    }

    const requestPayload = buildRoleMenuAccessPayload();

    const hasMenuAccessChanges =
      JSON.stringify(checkedMenus) !==
      JSON.stringify(originalCheckedMenusRef.current);

    if (!hasMenuAccessChanges) {
      ToastService.error(COMMON_MESSAGE.NO_CHANGES_MADE_TO_FORM);
      return;
    }

    if (requestPayload.length === 0) {
      ToastService.error("No Menu Access Details Found To Update.");
      return;
    }

    const loaderRequestId = showFullPageLoader("Updating Role Menu Access...");
    setUpdating(true);

    try {
      // ======================================================
      // SAVE ROLE MENU ACCESS
      // ======================================================

      const result = await saveUpdateRoleMenuAccessDetails(requestPayload);

      ToastService.success(
        result?.message || "Role Menu Access Details Saved Successfully.",
      );

      // ======================================================
      // RELOAD SELECTED ROLE
      // ======================================================

      /*
       * Reload selected Role using:
       *
       * paramType = "RoleMenuAccess"
       *
       * The returned IsSelected value becomes the source
       * of truth for every TreeView checkbox.
       */
      await loadRoleMenuAccess(selectedRoleId);

      // ======================================================
      // REFRESH LOGGED-IN USER ACCESS
      // ======================================================

      /*
       * Keep the existing behavior.
       *
       * If the logged-in user's own Role was modified,
       * refresh their Sidebar / Search / Route / Action access.
       *
       * This does NOT change their existing API implementation.
       */
      if (normalizeNumber(selectedRoleId) === loggedInRoleId) {
        await refreshMenuAccess?.();
      }
    } catch (error) {
      /*
       * API ValidationErrors, Network/CORS and
       * GLOBAL_ERROR_RESPONSE are already shown globally.
       *
       * HTTP-200 business errors such as:
       *
       * "No Changes Found In Role Menu Access Details."
       *
       * are shown here.
       */
      showApiError(error, "Unable to update Role Menu Access details.");
    } finally {
      hideFullPageLoader(loaderRequestId);
      setUpdating(false);
    }
  };

  // ==========================================================
  // COMMON DISABLED STATE
  // ==========================================================

  const pageBusy =
    roleDropdownLoading || roleMenuAccessLoading || refreshing || updating;

  // ==========================================================
  // UI
  // ==========================================================

  return (
    <div className="tp-menu-access-page">
      {/* ======================================================
          TITLE BAR
          ====================================================== */}

      <div className="tp-form-page-heading tp-form-titlebar tp-menu-access-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="menuAccess" size={18} />

          <h1>Role Menu Access</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={handleRefresh}
            disabled={pageBusy || !selectedRoleId}
            title="Refresh Menu Access"
            aria-label="Refresh Menu Access"
          >
            {refreshing || roleMenuAccessLoading ? (
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

      {/* ======================================================
          CARD
          ====================================================== */}

      <section className="tp-card tp-menu-access-card">
        {/* ====================================================
            ROLE TOOLBAR
            ==================================================== */}

        <div className="tp-menu-access-toolbar">
          <div className="row g-3 align-items-end w-100">
            {/* ================================================
                ROLE DROPDOWN
                ================================================ */}

            <div className="col-12 col-md-5 col-lg-4">
              <Dropdown
                name="roleId"
                label="Role"
                required
                value={selectedRoleId}
                onChange={handleRoleChange}
                options={roleOptions}
                placeholder={
                  roleDropdownLoading ? "Loading Roles..." : "Select Role"
                }
                disabled={pageBusy}
              />
            </div>

            {/* ================================================
                UPDATE BUTTON
                ================================================ */}

            <div className="col-auto">
              <button
                type="button"
                className="tp-primary-button"
                onClick={handleUpdate}
                disabled={pageBusy || !selectedRoleId}
              >
                {updating ? (
                  <>
                    <span
                      className="spinner-border spinner-border-sm"
                      aria-hidden="true"
                    />{" "}
                    Updating...
                  </>
                ) : (
                  <>
                    <SvgIcon name="save" size={16} /> Update
                  </>
                )}
              </button>
            </div>
          </div>
        </div>

        {/* ====================================================
            TREE HEADER
            ==================================================== */}

        <div className="tp-menu-tree-head">
          <span className="tp-form-label">Menu Access</span>
        </div>

        {/* ====================================================
            TREE VIEW
            ==================================================== */}

        <div className="tp-menu-tree" role="tree" aria-label="Role Menu Access">
          {/* ==================================================
              LOADING
              ================================================== */}

          {roleMenuAccessLoading && (
            <div className="tp-search-empty">
              <span
                className="spinner-border spinner-border-sm me-2"
                aria-hidden="true"
              />
              Loading menu access...
            </div>
          )}

          {/* ==================================================
              MENU ITEMS
              ================================================== */}

          {!roleMenuAccessLoading &&
            roleMenuItems.map((item) => {
              const children = item.children || [];

              const expanded = expandedKeys[item.key] !== false;

              const parentState = getParentCheckboxState(children);

              const selectedChildCount = getSelectedChildCount(children);

              if (securityAccessLoading) {
                return (
                  <div className="tp-form-page-loading">
                    <span
                      className="spinner-border text-primary"
                      aria-hidden="true"
                    />
                    <p>Loading security permissions...</p>
                  </div>
                );
              }

              if (!canOpenScreen) return <AccessDenied />;

              return (
                <div
                  key={item.key}
                  className="tp-menu-tree-group"
                  role="treeitem"
                  aria-expanded={children.length ? expanded : undefined}
                >
                  {/* ========================================
                        MAIN MENU
                        ======================================== */}

                  <div className="tp-menu-tree-parent">
                    {children.length ? (
                      <button
                        type="button"
                        className="tp-menu-tree-toggle"
                        onClick={() =>
                          setExpandedKeys((current) => ({
                            ...current,

                            [item.key]: !expanded,
                          }))
                        }
                        aria-label={`${expanded ? "Collapse" : "Expand"} ${
                          item.label
                        }`}
                      >
                        <SvgIcon
                          name="chevron"
                          size={16}
                          className={expanded ? "is-open" : ""}
                        />
                      </button>
                    ) : (
                      <span className="tp-menu-tree-spacer" />
                    )}

                    <span
                      className={`tp-sidebar-svg tp-sidebar-svg-${
                        getMenuIconPresentation(item).tone
                      }`}
                    >
                      <SvgIcon
                        name={getMenuIconPresentation(item).svgIcon}
                        size={17}
                      />
                    </span>

                    <PermissionCheckbox
                      checked={parentState.checked}
                      indeterminate={parentState.indeterminate}
                      label={item.label}
                      onChange={(checked) =>
                        handleParentChange(children, checked)
                      }
                      disabled={pageBusy}
                    />

                    {children.length > 0 && (
                      <span className="tp-menu-tree-count">
                        {selectedChildCount}/{children.length}
                      </span>
                    )}
                  </div>

                  {/* ========================================
                        SUB MENUS
                        ======================================== */}

                  {children.length > 0 && expanded && (
                    <div className="tp-menu-tree-children" role="group">
                      {children.map((child) => (
                        <div key={child.key} className="tp-menu-tree-child">
                          <span className="tp-menu-tree-line" />

                          <span
                            className={`tp-sidebar-svg tp-sidebar-svg-${
                              child.tone || getMenuIconPresentation(item).tone
                            }`}
                          >
                            <SvgIcon
                              name={getMenuIconPresentation(child).svgIcon}
                              size={15}
                            />
                          </span>

                          <div className="d-flex flex-column gap-1 flex-grow-1">
                            <PermissionCheckbox
                              checked={Boolean(checkedMenus[child.key])}
                              label={child.label}
                              onChange={(checked) =>
                                handleChildChange(child.key, checked)
                              }
                              disabled={pageBusy}
                            />
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              );
            })}

          {/* ==================================================
              EMPTY STATE
              ================================================== */}

          {!roleMenuAccessLoading && roleMenuItems.length === 0 && (
            <div className="tp-search-empty">
              <i className="bi bi-shield-x" /> No menu access is assigned.
            </div>
          )}
        </div>
      </section>
    </div>
  );
}
