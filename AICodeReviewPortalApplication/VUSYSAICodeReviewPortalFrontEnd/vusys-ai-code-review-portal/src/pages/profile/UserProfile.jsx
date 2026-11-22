import { useEffect, useMemo, useRef, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import ColorIcon from "../../components/common/ColorIcon";
import DirtyStateModal from "../../components/common/DirtyStateModal";
import SvgIcon from "../../components/common/SvgIcon";
import ToastService from "../../services/toastService";
import { saveAndUpdateUserDetails } from "../../services/userProfileService";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { COMMON_MESSAGE } from "../../constants/constants";
import { showApiError } from "../../utils/showApiError";

const getFallbackImage = (user) =>
  String(user?.role).toLowerCase() === "employee"
    ? "/images/profile-employee.svg"
    : "/images/profile-admin.svg";

const getUserId = (user) =>
  user?.Id ?? user?.ID ?? user?.id ?? user?.userId ?? "";

const getRoleId = (user) => user?.RoleID ?? user?.roleId ?? "";

const getUserName = (user) =>
  user?.UserName ?? user?.userName ?? user?.name ?? "";

const getFullName = (user) =>
  user?.FullName ?? user?.fullName ?? user?.name ?? "";

const getEmailId = (user) =>
  user?.EmailID ?? user?.email ?? user?.emailID ?? "";

const getMobileNo = (user) => user?.MobileNo ?? user?.mobileNo ?? "";

const getEmployeeCode = (user) =>
  user?.EmployeeCode ?? user?.employeeCode ?? user?.employeeID ?? "";

const getIsApprover = (user) => Boolean(user?.IsApprover ?? user?.isApprover);

const getIsDeleted = (user) =>
  Boolean(user?.IsDeleted ?? user?.isDeleted ?? user?.Obsolete ?? false);

export default function UserProfile() {
  const { user, updateUser } = useAuth();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const fileInputRef = useRef(null);

  const savedProfileImage = user?.profileImage || getFallbackImage(user);
  const savedTwoFactor = Boolean(
    user?.twoFactorEnabled ?? user?.IsEnableTwoFactorAuthentication,
  );

  const [isEditing, setIsEditing] = useState(false);
  const [profileImage, setProfileImage] = useState(savedProfileImage);
  const [profileImageFile, setProfileImageFile] = useState(null);
  const [twoFactorEnabled, setTwoFactorState] = useState(savedTwoFactor);
  const [dirtyModalOpen, setDirtyModalOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (isEditing) return;

    setProfileImage(savedProfileImage);
    setProfileImageFile(null);
    setTwoFactorState(savedTwoFactor);
  }, [isEditing, savedProfileImage, savedTwoFactor]);

  const isDirty = useMemo(
    () =>
      Boolean(profileImageFile) ||
      profileImage !== savedProfileImage ||
      twoFactorEnabled !== savedTwoFactor,
    [
      profileImage,
      profileImageFile,
      savedProfileImage,
      twoFactorEnabled,
      savedTwoFactor,
    ],
  );

  useEffect(() => {
    const handleBeforeUnload = (event) => {
      if (!isEditing || !isDirty) return;

      event.preventDefault();
      event.returnValue = "";
    };

    window.addEventListener("beforeunload", handleBeforeUnload);

    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [isEditing, isDirty]);

  const startEdit = () => {
    setProfileImage(savedProfileImage);
    setProfileImageFile(null);
    setTwoFactorState(savedTwoFactor);
    setIsEditing(true);
  };

  const closeEdit = () => {
    if (saving) return;

    setProfileImage(savedProfileImage);
    setProfileImageFile(null);
    setTwoFactorState(savedTwoFactor);
    setDirtyModalOpen(false);
    setIsEditing(false);
  };

  const requestCancel = () => {
    if (!isEditing || saving) return;

    if (isDirty) {
      setDirtyModalOpen(true);
      return;
    }

    closeEdit();
  };

  const saveProfile = async () => {
    if (!isEditing || !isDirty || saving) return;

    if (!isDirty) {
      ToastService.error(COMMON_MESSAGE.NO_CHANGES_MADE_TO_FORM);
      return;
    }

    const userId = getUserId(user);
    const roleId = getRoleId(user);

    if (userId === "" || userId === null || userId === undefined) {
      ToastService.error("User ID is not available.");
      return;
    }

    if (roleId === "" || roleId === null || roleId === undefined) {
      ToastService.error("Role ID is not available.");
      return;
    }

    const loaderRequestId = showFullPageLoader("Saving User Profile...");
    setSaving(true);

    try {
      const result = await saveAndUpdateUserDetails({
        isDeleted: getIsDeleted(user),
        createdBy: userId,
        updatedBy: userId,
        id: userId,
        userName: getUserName(user),
        fullName: getFullName(user),
        mobileNo: getMobileNo(user),
        userEmailID: getEmailId(user),
        roleID: roleId,
        employeeCode: getEmployeeCode(user),
        isApprover: getIsApprover(user),
        isEnableTwoFactorAuthentication: twoFactorEnabled,
        profileUserImage: profileImageFile,
      });

      /*
       * updateUser persists profileImage in IndexedDB and updates AuthContext,
       * so the Header avatar changes immediately after a successful save.
       */
      updateUser({
        profileImage,
        twoFactorEnabled: Boolean(twoFactorEnabled),
        IsEnableTwoFactorAuthentication: Boolean(twoFactorEnabled),
      });

      setProfileImageFile(null);
      setIsEditing(false);
      setDirtyModalOpen(false);

      ToastService.success(
        result?.message || "User Details Updated Successfully.",
      );
    } catch (error) {
      /*
       * API ValidationErrors, Network/CORS and GLOBAL_ERROR_RESPONSE are
       * already displayed globally by axiosSetup.js.
       */
      showApiError(error, "Unable to update user details.");
    } finally {
      hideFullPageLoader(loaderRequestId);
      setSaving(false);
    }
  };

  const openImagePicker = () => {
    if (!isEditing || saving) return;

    fileInputRef.current?.click();
  };

  const handleImageChange = (event) => {
    const file = event.target.files?.[0];
    event.target.value = "";

    if (!file) return;

    if (!file.type.startsWith("image/")) {
      ToastService.error("Please select a valid image file");
      return;
    }

    setProfileImageFile(file);

    const reader = new FileReader();

    reader.onload = () => {
      setProfileImage(String(reader.result || savedProfileImage));
    };

    reader.onerror = () => {
      setProfileImageFile(null);
      ToastService.error("Unable to read the selected image");
    };

    reader.readAsDataURL(file);
  };

  return (
    <div className="tp-profile-page">
      <div className="tp-form-page-heading tp-form-titlebar tp-profile-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="profile" size={17} />
          <h1>User Profile</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          {!isEditing ? (
            <button
              type="button"
              className="tp-form-page-icon-button"
              onClick={startEdit}
              title="Edit Profile"
              aria-label="Edit Profile"
            >
              <SvgIcon name="edit" size={18} />
            </button>
          ) : (
            <button
              type="button"
              className="tp-form-page-icon-button"
              onClick={saveProfile}
              disabled={saving}
              title="Save Profile"
              aria-label="Save Profile"
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
            onClick={requestCancel}
            disabled={!isEditing || saving}
            title="Cancel"
            aria-label="Cancel"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>
      </div>

      <div className="tp-profile-page-body">
        <div className="row g-3">
          <div className="col-xl-4">
            <section className="tp-card tp-profile-summary text-center h-100">
              <button
                type="button"
                className={`tp-profile-avatar tp-profile-avatar-button ${
                  isEditing ? "is-editable" : ""
                }`}
                onClick={openImagePicker}
                disabled={!isEditing || saving}
                title={isEditing ? "Upload profile image" : "Profile image"}
                aria-label={
                  isEditing ? "Upload profile image" : "Profile image"
                }
              >
                {profileImage ? (
                  <img
                    src={profileImage}
                    alt={`${user?.name || "User"} profile`}
                  />
                ) : (
                  (user?.name || "")
                    .split(" ")
                    .map((part) => part[0])
                    .slice(0, 2)
                    .join("")
                )}

                {isEditing && !saving && (
                  <span className="tp-profile-avatar-edit-overlay">
                    <SvgIcon name="upload" size={20} />
                    <small>Change</small>
                  </span>
                )}
              </button>

              <input
                ref={fileInputRef}
                type="file"
                className="d-none"
                accept="image/*"
                onChange={handleImageChange}
                disabled={saving}
              />

              <h3>{user?.name || ""}</h3>
              <p>{user?.role || ""}</p>

              <span className="tp-status-pill">
                <i className="bi bi-circle-fill" /> Active
              </span>
            </section>
          </div>

          <div className="col-xl-8">
            <section className="tp-card p-4 h-100">
              <div className="tp-section-title">
                <ColorIcon icon="bi-person-vcard-fill" tone="cyan" />

                <div>
                  <h3>Profile information</h3>
                  <p>Your Basic Account Details</p>
                </div>
              </div>

              <div className="row g-3 mt-1">
                {[
                  ["Full Name", user?.FullName || user?.name || ""],
                  ["Email ID", user?.EmailID || user?.email || ""],
                  ["Role", user?.UserRole || user?.role || ""],
                  [
                    "Employee Code",
                    user?.EmployeeCode || user?.employeeCode || "",
                  ],
                  ["Mobile No", user?.MobileNo || user?.mobileNo || ""],
                  ["User Name", user?.UserName || user?.userName || ""],
                ].map(([label, value]) => (
                  <div className="col-md-6" key={label}>
                    <label className="tp-info-label tp-mandatory-mark">
                      {label}
                    </label>

                    <div className="tp-info-value">{value}</div>
                  </div>
                ))}
              </div>

              <div
                className={`tp-profile-security-setting ${
                  !isEditing ? "is-readonly" : ""
                }`}
              >
                <div>
                  <strong>Enable Two Factor Authentication</strong>
                  <small>
                    Require a 4-digit OTP after entering your email id and
                    password.
                  </small>
                </div>

                <label className="tp-form-switch">
                  <input
                    type="checkbox"
                    checked={twoFactorEnabled}
                    onChange={(event) =>
                      setTwoFactorState(event.target.checked)
                    }
                    disabled={!isEditing || saving}
                  />
                  <span className="tp-form-switch-slider" />
                </label>
              </div>
            </section>
          </div>
        </div>
      </div>

      <DirtyStateModal
        open={dirtyModalOpen}
        onStay={() => setDirtyModalOpen(false)}
        onDiscard={closeEdit}
        title="Unsaved Changes"
        message="You have unsaved profile changes. Do you want to close without saving?"
        stayText="No, Stay"
        discardText="Yes, Close"
      />
    </div>
  );
}
