import { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  Checkbox,
  FileUpload,
  SearchableDropdown,
  TextBox,
} from "../../components/forms/FormElements";
import SvgIcon from "../../components/common/SvgIcon";
import DirtyStateModal from "../../components/common/DirtyStateModal";
import ImagePopup from "../../components/common/ImagePopup";
import AccessDenied from "../../components/common/AccessDenied";
import ToastService from "../../services/toastService";
import { getDropdownItems } from "../../services/dropdownService";
import {
  getUserDetails,
  saveAndUpdateUserMasterDetails,
} from "../../services/userMasterService";
import useFormValidation from "../../hooks/useFormValidation";
import { userMasterValidationRules } from "../../validation/userMasterValidationRules";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { toImgSrcFromBase64 } from "../../utils/imageUtils";
import {
  COMMON_MESSAGE,
  DROPDOWN_SOURCE_TABLE,
} from "../../constants/constants";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import { decodeRouteId, encodeRouteId } from "../../utils/routeIdUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { normalizeNumber } from "../../utils/valueUtils";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const blankUser = {
  employeeCode: "",
  userName: "",
  fullName: "",
  mobileNo: "",
  userEmailID: "",
  roleID: "",
  userImage: "",
  userImageFileName: "",
  file: null,
  isEnableTwoFactorAuthentication: false,
  isApprover: false,
  isDeleted: false,
};

// ============================================================
// MAP API VALIDATIONS TO USER FORM FIELDS
// ============================================================
const USER_API_FIELD_MAPPINGS = {
  userName: { apiKeys: ["UserName"] },
  fullName: { apiKeys: ["FullName"] },
  mobileNo: { apiKeys: ["MobileNo"] },
  userEmailID: { apiKeys: ["UserEmailID"] },
  roleID: { apiKeys: ["RoleID"] },
  employeeCode: { apiKeys: ["EmployeeCode"] },
};

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());
const NavigateUser = ROUTE_CONSTANTS.ADMINISTRATION.USERS;

const createComparableSnapshot = (value) =>
  JSON.stringify({
    employeeCode: value.employeeCode,
    userName: value.userName,
    fullName: value.fullName,
    mobileNo: value.mobileNo,
    userEmailID: value.userEmailID,
    roleID: value.roleID,
    isEnableTwoFactorAuthentication: value.isEnableTwoFactorAuthentication,
    isApprover: value.isApprover,
    isDeleted: value.isDeleted,
    fileName: value.file?.name || "",
  });

export default function UserFormPage() {
  const { registerTabDirtyState } = useTabs();
  const { userId } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const isAddMode = !userId || userId === "new";
  const decodedUserId = isAddMode ? 0 : decodeRouteId(userId);
  const requestedMode =
    location.state?.mode ||
    new URLSearchParams(location.search).get("mode") ||
    "view";

  const hasCreatePermission = hasPermission("User Master", "Create");
  const hasEditPermission = hasPermission("User Master", "Edit");
  const hasViewPermission = hasPermission("User Master", "View");

  const [mode, setMode] = useState(isAddMode ? "add" : requestedMode);
  const [form, setForm] = useState(blankUser);
  const [employeeOptions, setEmployeeOptions] = useState([]);
  const [roleOptions, setRoleOptions] = useState([]);
  const [loading, setLoading] = useState(!isAddMode);
  const [saving, setSaving] = useState(false);
  const [dirtyOpen, setDirtyOpen] = useState(false);
  const [imagePreviewOpen, setImagePreviewOpen] = useState(false);
  const originalSnapshotRef = useRef(createComparableSnapshot(blankUser));
  const dropdownLoadPromiseRef = useRef(null);
  const detailsLoadPromiseRef = useRef({ id: "", promise: null });
  const pendingActionRef = useRef(null);
  const selectedFileUrlRef = useRef("");

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(userMasterValidationRules);

  const readOnly = mode === "view";
  const canSave = mode === "add" || mode === "edit";
  const canCancel = mode === "add" || mode === "edit";
  const canEdit = !isAddMode && mode !== "edit" && hasEditPermission;
  const title = isAddMode
    ? "Add User"
    : mode === "edit"
      ? "Edit User"
      : "View User";

  const isDirty = useMemo(
    () => createComparableSnapshot(form) !== originalSnapshotRef.current,
    [form],
  );

  useEffect(() => {
    registerTabDirtyState(NavigateUser, isDirty);

    return () => {
      registerTabDirtyState(NavigateUser, false);
    };
  }, [isDirty, registerTabDirtyState]);

  useEffect(() => {
    let active = true;

    if (!dropdownLoadPromiseRef.current) {
      dropdownLoadPromiseRef.current = Promise.all([
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.EMPLOYEE }),
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.ROLE }),
      ]);
    }

    dropdownLoadPromiseRef.current
      .then(([employeesResult, rolesResult]) => {
        if (!active) return;

        setEmployeeOptions(
          employeesResult.options.map((option) => ({
            ...option,
            employeeCode: option.value,
            employeeName: option.label,
          })),
        );
        setRoleOptions(rolesResult.options);
      })
      .catch((error) => {
        if (active) {
          showApiError(error, "Unable to load User dropdowns.");
        }
      });

    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    if (isAddMode) {
      originalSnapshotRef.current = createComparableSnapshot(blankUser);
      return undefined;
    }

    const normalizedUserId = String(decodedUserId || "");

    if (!decodedUserId) {
      navigate(NavigateUser, { replace: true });
      return undefined;
    }
    let active = true;
    setLoading(true);
    const loaderRequestId = showFullPageLoader("Loading User Details...");

    if (
      detailsLoadPromiseRef.current.id !== normalizedUserId ||
      !detailsLoadPromiseRef.current.promise
    ) {
      detailsLoadPromiseRef.current = {
        id: normalizedUserId,
        promise: getUserDetails(decodedUserId),
      };
    }

    detailsLoadPromiseRef.current.promise
      .then((result) => {
        if (!active) return;

        const nextForm = {
          ...blankUser,
          ...result.user,
          roleID: String(result.user.roleID || ""),
          file: null,
        };

        setForm(nextForm);
        originalSnapshotRef.current = createComparableSnapshot(nextForm);
      })
      .catch((error) => {
        if (!active) return;
        showApiError(error, "Unable to load User Details.");
        navigate(NavigateUser, { replace: true });
      })
      .finally(() => {
        if (active) setLoading(false);
        hideFullPageLoader(loaderRequestId);
      });

    return () => {
      active = false;
      hideFullPageLoader(loaderRequestId);
    };
  }, [
    decodedUserId,
    isAddMode,
    navigate,
    showFullPageLoader,
    hideFullPageLoader,
  ]);

  useEffect(
    () => () => {
      if (selectedFileUrlRef.current) {
        URL.revokeObjectURL(selectedFileUrlRef.current);
      }
    },
    [],
  );

  const update = (event) => {
    const { name, type, checked, value } = event.target;
    const nextValue = type === "checkbox" ? checked : value;

    setForm((current) => {
      const next = { ...current, [name]: nextValue };

      if (name === "employeeCode") {
        const selectedEmployee = employeeOptions.find(
          (option) => option.value === nextValue,
        );

        if (selectedEmployee) {
          next.fullName = selectedEmployee.label;
        }
      }

      return next;
    });

    clearError(name);
    if (name === "employeeCode") {
      clearError("fullName");
    }
  };

  const handleFileChange = (event) => {
    const file = event.target.files?.[0] || null;

    if (selectedFileUrlRef.current) {
      URL.revokeObjectURL(selectedFileUrlRef.current);
      selectedFileUrlRef.current = "";
    }

    if (file) {
      selectedFileUrlRef.current = URL.createObjectURL(file);
    }

    setForm((current) => ({ ...current, file }));
  };

  const getPreviewSource = () => {
    if (form.file && selectedFileUrlRef.current) {
      return selectedFileUrlRef.current;
    }

    return toImgSrcFromBase64(form.userImage, form.userImageFileName);
  };

  const runWithDirtyGuard = (action) => {
    if (canSave && isDirty) {
      pendingActionRef.current = action;
      setDirtyOpen(true);
      return;
    }

    action();
  };

  const discardChanges = () => {
    setDirtyOpen(false);
    const action = pendingActionRef.current;
    pendingActionRef.current = null;
    action?.();
  };

  const cancelEdit = async () => {
    clearErrors();

    if (isAddMode) {
      navigate(NavigateUser);
      return;
    }

    setLoading(true);
    const loaderRequestId = showFullPageLoader("Loading User Details...");

    try {
      const result = await getUserDetails(decodedUserId);
      const nextForm = {
        ...blankUser,
        ...result.user,
        roleID: String(result.user.roleID || ""),
        file: null,
      };
      setForm(nextForm);
      originalSnapshotRef.current = createComparableSnapshot(nextForm);
      setMode("view");
    } catch (error) {
      showApiError(error, "Unable to reload User Details.");
    } finally {
      hideFullPageLoader(loaderRequestId);
      setLoading(false);
    }
  };

  const save = async (event) => {
    event.preventDefault();

    if (!canSave || saving) return;

    /*
     * Edit mode must never call the API when the loaded record has not
     * actually been modified.
     */
    if (!isAddMode && mode === "edit" && !isDirty) {
      ToastService.error(COMMON_MESSAGE.NO_CHANGES_MADE_TO_FORM);
      return;
    }

    if (!validate(form)) return;

    const loaderRequestId = showFullPageLoader("Saving User Details...");
    setSaving(true);

    try {
      const result = await saveAndUpdateUserMasterDetails({
        id: isAddMode ? 0 : decodedUserId,
        userName: form.userName,
        fullName: form.fullName,
        mobileNo: form.mobileNo,
        userEmailID: form.userEmailID,
        roleID: form.roleID,
        employeeCode: form.employeeCode,
        isApprover: form.isApprover,
        isEnableTwoFactorAuthentication: form.isEnableTwoFactorAuthentication,
        isDeleted: mode === "edit" ? form.isDeleted : false,
        createdBy: isAddMode ? loggedInUserId : 0,
        updatedBy: isAddMode ? 0 : loggedInUserId,
        profileUserImage: form.file,
      });

      ToastService.success(
        result.message || "User Details Saved Successfully.",
      );

      const savedId = result.id || decodedUserId;

      if (savedId > 0) {
        const detailsResult = await getUserDetails(savedId);
        const nextForm = {
          ...blankUser,
          ...detailsResult.user,
          roleID: String(detailsResult.user.roleID || ""),
          file: null,
        };
        setForm(nextForm);
        originalSnapshotRef.current = createComparableSnapshot(nextForm);
        setMode("view");
        if (isAddMode) {
          detailsLoadPromiseRef.current = {
            id: String(savedId),
            promise: Promise.resolve(detailsResult),
          };
          // navigate(`/users/${encodeRouteId(savedId)}`, {
          //   replace: true,
          //   state: { mode: "view" },
          // });
          navigate(NavigateUser);
        } else {
          navigate(NavigateUser);
        }
      } else {
        navigate(NavigateUser);
      }
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          USER_API_FIELD_MAPPINGS,
          setFieldError,
        );
        return;
      }

      showApiError(error, "Unable to save User Details.");
    } finally {
      hideFullPageLoader(loaderRequestId);
      setSaving(false);
    }
  };

  if (securityAccessLoading) {
    return (
      <div className="tp-form-page-loading">
        <span className="spinner-border text-primary" aria-hidden="true" />
        <p>Loading security permissions...</p>
      </div>
    );
  }

  const hasRequestedAccess = isAddMode
    ? hasCreatePermission
    : requestedMode === "edit"
      ? hasEditPermission
      : hasViewPermission;

  if (!hasRequestedAccess) {
    return (
      <AccessDenied
        message="You don't have permission to access this User action."
        returnPath={NavigateUser}
        returnLabel="Back to Users"
      />
    );
  }

  if (loading) {
    return null;
  }

  const previewSource = getPreviewSource();

  return (
    <div className="tp-screen-form-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="employees" size={16} />
          <h1>{title}</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          {hasEditPermission && !isAddMode && (
            <button
              type="button"
              className="tp-form-page-icon-button"
              onClick={() => setMode("edit")}
              disabled={!canEdit}
              title="Edit User"
              aria-label="Edit User"
            >
              <SvgIcon name="edit" size={18} />
            </button>
          )}

          <button
            type="submit"
            form="user-master-form"
            className="tp-form-page-icon-button"
            disabled={!canSave || saving}
            title="Save User"
            aria-label="Save User"
          >
            {saving ? (
              <span className="spinner-border spinner-border-sm" />
            ) : (
              <SvgIcon name="save" size={18} />
            )}
          </button>

          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => runWithDirtyGuard(cancelEdit)}
            disabled={!canCancel || saving}
            title="Cancel"
            aria-label="Cancel"
          >
            <SvgIcon name="close" size={18} />
          </button>

          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => runWithDirtyGuard(() => navigate(NavigateUser))}
            title="Back to Users"
            aria-label="Back to Users"
          >
            <SvgIcon name="back" size={18} />
          </button>
        </div>
      </div>

      <form
        id="user-master-form"
        className="tp-card tp-form-card tp-form-card-attached"
        onSubmit={save}
        noValidate
      >
        <div className="row g-3">
          <div className="col-md-4">
            <SearchableDropdown
              name="employeeCode"
              label="Employee"
              required
              placeholder="Search and Select Employee."
              value={form.employeeCode}
              onChange={update}
              options={employeeOptions}
              columns={[
                { key: "employeeCode", label: "Employee Code" },
                { key: "employeeName", label: "Employee Name" },
              ]}
              error={errors.employeeCode}
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="userName"
              label="User Name"
              required
              value={form.userName}
              onChange={update}
              error={errors.userName}
              placeholder="Enter User Name"
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="fullName"
              label="Full Name"
              required
              value={form.fullName}
              onChange={update}
              error={errors.fullName}
              placeholder="Enter Full Name"
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="mobileNo"
              label="Mobile Number"
              required
              icon="bi-telephone-fill"
              value={form.mobileNo}
              onChange={update}
              error={errors.mobileNo}
              placeholder="Enter Mobile Number"
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="userEmailID"
              label="Email ID"
              required
              type="email"
              icon="bi-envelope-fill"
              value={form.userEmailID}
              onChange={update}
              error={errors.userEmailID}
              placeholder="Enter Email ID"
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4">
            <SearchableDropdown
              name="roleID"
              label="Role"
              required
              placeholder="Search and Select Role."
              value={form.roleID}
              onChange={update}
              options={roleOptions}
              columns={[{ key: "label", label: "Role" }]}
              error={errors.roleID}
              disabled={readOnly}
            />
          </div>

          {!readOnly && (
            <div className="col-md-6">
              <FileUpload
                name="profileUserImage"
                label="User Image"
                accept="image/*"
                onChange={handleFileChange}
              />

              {(previewSource || form.userImageFileName || form.file) && (
                <div className="tp-user-image-file-actions">
                  <span>
                    {form.file?.name || form.userImageFileName || "User Image"}
                  </span>
                  {previewSource && (
                    <button
                      type="button"
                      onClick={() => setImagePreviewOpen(true)}
                    >
                      <SvgIcon name="eye" size={15} /> View Image
                    </button>
                  )}
                </div>
              )}
            </div>
          )}

          {readOnly && (
            <div className="col-md-6">
              <div className="tp-form-field">
                <label className="tp-form-label">User Image</label>
                <div className="tp-user-image-readonly">
                  {previewSource ? (
                    <>
                      <img src={previewSource} alt={form.fullName || "User"} />
                      <button
                        type="button"
                        onClick={() => setImagePreviewOpen(true)}
                      >
                        <SvgIcon name="eye" size={15} /> View Image
                      </button>
                    </>
                  ) : (
                    <span>No image available</span>
                  )}
                </div>
              </div>
            </div>
          )}

          <div className="col-md-6 d-flex align-items-end gap-4 pb-2 tp-user-toggle-row">
            <Checkbox
              name="isEnableTwoFactorAuthentication"
              label="Enable Two Factor Authentication"
              checked={form.isEnableTwoFactorAuthentication}
              onChange={update}
              switchStyle
              disabled={readOnly}
            />
            <Checkbox
              name="isApprover"
              label="Is Approver?"
              checked={form.isApprover}
              onChange={update}
              switchStyle
              disabled={readOnly}
            />
            {mode === "edit" && (
              <Checkbox
                name="isDeleted"
                label="Obsolete"
                checked={form.isDeleted}
                onChange={update}
                switchStyle
                disabled={saving}
              />
            )}
          </div>
        </div>
      </form>

      <DirtyStateModal
        open={dirtyOpen}
        onStay={() => {
          setDirtyOpen(false);
          pendingActionRef.current = null;
        }}
        onDiscard={discardChanges}
      />

      <ImagePopup
        show={imagePreviewOpen}
        src={previewSource}
        title={`${form.fullName || form.userName || "User"} - User Image`}
        fileName={form.file?.name || form.userImageFileName}
        alt={form.fullName || form.userName || "User"}
        onClose={() => setImagePreviewOpen(false)}
      />
    </div>
  );
}
