import { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  Checkbox,
  DatePicker,
  FileUpload,
  SearchableDropdown,
  TextArea,
  TextBox,
} from "../../components/forms/FormElements";
import SvgIcon from "../../components/common/SvgIcon";
import DirtyStateModal from "../../components/common/DirtyStateModal";
import ImagePopup from "../../components/common/ImagePopup";
import AccessDenied from "../../components/common/AccessDenied";
import ToastService from "../../services/toastService";
import { getDropdownItems } from "../../services/dropdownService";
import {
  getEmployeeDetails,
  saveAndUpdateEmployeeDetails,
} from "../../services/employeeMasterService";
import useFormValidation from "../../hooks/useFormValidation";
import { employeeValidationRules } from "../../validation/employeeValidationRules";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { toImgSrcFromBase64 } from "../../utils/imageUtils";
import {
  COMMON_MESSAGE,
  DROPDOWN_SOURCE_TABLE,
} from "../../constants/constants";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import { decodeRouteId, encodeRouteId } from "../../utils/routeIdUtils";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const EMPLOYEE_API_FIELD_MAPPINGS = {
  fullName: { apiKeys: ["FullName"] },
  genderID: { apiKeys: ["GenderID"] },
  dateOfBirth: { apiKeys: ["DateofBirth"] },
  mobileNo: { apiKeys: ["MobileNo"] },
  emailID: { apiKeys: ["EmailID"] },
  countryID: { apiKeys: ["CountryID"] },
  stateID: { apiKeys: ["StateID"] },
  cityName: { apiKeys: ["CityName"] },
  pinCode: { apiKeys: ["PinCode"] },
  skills: { apiKeys: ["Skills"] },
  employeeTypeID: { apiKeys: ["EmployeeTypeID"] },
  hireDate: { apiKeys: ["HireDate"] },
  parentCompanyName: { apiKeys: ["ParentCompanyName"] },
};

const blankEmployee = {
  fullName: "",
  genderID: "",
  dateOfBirth: "",
  mobileNo: "",
  emailID: "",
  countryID: "",
  stateID: "",
  cityName: "",
  pinCode: "",
  skills: "",
  hireDate: "",
  employeeTypeID: "",
  parentCompanyName: "",
  employeeProfileImage: "",
  employeeImageFileName: "",
  file: null,
  isDeleted: false,
};

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());
const NavigateEmployee = ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEES;

const createComparableSnapshot = (value) =>
  JSON.stringify({
    fullName: value.fullName,
    genderID: value.genderID,
    dateOfBirth: value.dateOfBirth,
    mobileNo: value.mobileNo,
    emailID: value.emailID,
    countryID: value.countryID,
    stateID: value.stateID,
    cityName: value.cityName,
    pinCode: value.pinCode,
    skills: value.skills,
    hireDate: value.hireDate,
    employeeTypeID: value.employeeTypeID,
    parentCompanyName: value.parentCompanyName,
    isDeleted: value.isDeleted,
    fileName: value.file?.name || "",
  });

export default function EmployeeFormPage() {
  const { registerTabDirtyState } = useTabs();
  const { employeeId } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const isAddMode = !employeeId || employeeId === "new";
  const decodedEmployeeId = isAddMode ? 0 : decodeRouteId(employeeId);
  const requestedMode =
    location.state?.mode ||
    new URLSearchParams(location.search).get("mode") ||
    "view";

  const hasCreatePermission = hasPermission("Employee Master", "Create");
  const hasEditPermission = hasPermission("Employee Master", "Edit");
  const hasViewPermission = hasPermission("Employee Master", "View");

  const [mode, setMode] = useState(isAddMode ? "add" : requestedMode);
  const [form, setForm] = useState(blankEmployee);
  const [countryOptions, setCountryOptions] = useState([]);
  const [stateOptions, setStateOptions] = useState([]);
  const [genderOptions, setGenderOptions] = useState([]);
  const [employeeTypeOptions, setEmployeeTypeOptions] = useState([]);
  const [stateLoading, setStateLoading] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [dirtyOpen, setDirtyOpen] = useState(false);
  const [imagePreviewOpen, setImagePreviewOpen] = useState(false);

  const originalSnapshotRef = useRef(createComparableSnapshot(blankEmployee));
  const dropdownLoadPromiseRef = useRef(null);
  const detailsLoadPromiseRef = useRef({ id: "", promise: null });
  const pendingActionRef = useRef(null);
  const selectedFileUrlRef = useRef("");

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(employeeValidationRules);

  const readOnly = mode === "view";
  const canSave = mode === "add" || mode === "edit";
  const canCancel = mode === "add" || mode === "edit";
  const canEdit = !isAddMode && mode !== "edit" && hasEditPermission;

  const title = isAddMode
    ? "Add Employee"
    : mode === "edit"
      ? "Edit Employee"
      : "View Employee";

  const isDirty = useMemo(
    () => createComparableSnapshot(form) !== originalSnapshotRef.current,
    [form],
  );

  useEffect(() => {
    registerTabDirtyState(NavigateEmployee, isDirty);

    return () => {
      registerTabDirtyState(NavigateEmployee, false);
    };
  }, [isDirty, registerTabDirtyState]);

  const loadStateOptions = async (countryID) => {
    const normalizedCountryID = String(countryID || "").trim();

    if (!normalizedCountryID) {
      setStateOptions([]);
      return [];
    }

    setStateLoading(true);

    try {
      const result = await getDropdownItems({
        sourceTable: DROPDOWN_SOURCE_TABLE.STATE,
        argumentValue: normalizedCountryID,
      });
      setStateOptions(result.options);
      return result.options;
    } catch (error) {
      setStateOptions([]);
      showApiError(error, "Unable to load State dropdown.");
      return [];
    } finally {
      setStateLoading(false);
    }
  };

  useEffect(() => {
    let active = true;
    const normalizedEmployeeId = String(decodedEmployeeId || "");

    if (!isAddMode && !decodedEmployeeId) {
      navigate(NavigateEmployee, { replace: true });
      return undefined;
    }

    setLoading(true);
    const loaderRequestId = showFullPageLoader("Loading Employee Details...");

    if (!dropdownLoadPromiseRef.current) {
      dropdownLoadPromiseRef.current = Promise.all([
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.COUNTRY }),
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.GENDER }),
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.EMPLOYEE_TYPE }),
      ]);
    }

    if (
      !isAddMode &&
      (detailsLoadPromiseRef.current.id !== normalizedEmployeeId ||
        !detailsLoadPromiseRef.current.promise)
    ) {
      detailsLoadPromiseRef.current = {
        id: normalizedEmployeeId,
        promise: getEmployeeDetails(decodedEmployeeId),
      };
    }

    Promise.all([
      dropdownLoadPromiseRef.current,
      isAddMode ? Promise.resolve(null) : detailsLoadPromiseRef.current.promise,
    ])
      .then(
        async ([
          [countriesResult, gendersResult, employeeTypesResult],
          detailsResult,
        ]) => {
          if (!active) return;

          setCountryOptions(countriesResult.options);
          setGenderOptions(gendersResult.options);
          setEmployeeTypeOptions(employeeTypesResult.options);

          if (!detailsResult?.employee) {
            setForm(blankEmployee);
            originalSnapshotRef.current =
              createComparableSnapshot(blankEmployee);
            return;
          }

          const nextForm = {
            ...blankEmployee,
            ...detailsResult.employee,
            genderID: String(detailsResult.employee.genderID || ""),
            countryID: String(detailsResult.employee.countryID || ""),
            stateID: String(detailsResult.employee.stateID || ""),
            employeeTypeID: String(detailsResult.employee.employeeTypeID || ""),
            file: null,
          };

          if (nextForm.countryID) {
            const statesResult = await getDropdownItems({
              sourceTable: DROPDOWN_SOURCE_TABLE.STATE,
              argumentValue: nextForm.countryID,
            });

            if (!active) return;
            setStateOptions(statesResult.options);
          }

          setForm(nextForm);
          originalSnapshotRef.current = createComparableSnapshot(nextForm);
        },
      )
      .catch((error) => {
        if (!active) return;
        showApiError(error, "Unable to load Employee Details.");
        if (!isAddMode) {
          navigate(NavigateEmployee, { replace: true });
        }
      })
      .finally(() => {
        hideFullPageLoader(loaderRequestId);
        if (active) setLoading(false);
      });

    return () => {
      active = false;
      hideFullPageLoader(loaderRequestId);
    };
  }, [
    decodedEmployeeId,
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

  useEffect(() => {
    const handleBeforeUnload = (event) => {
      if (!canSave || !isDirty) return;
      event.preventDefault();
      event.returnValue = "";
    };

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [canSave, isDirty]);

  const update = (event) => {
    const { name, type, checked, value } = event.target;
    const nextValue = type === "checkbox" ? checked : value;

    if (name === "countryID") {
      setForm((current) => ({
        ...current,
        countryID: nextValue,
        stateID: "",
      }));
      clearError("countryID");
      clearError("stateID");
      loadStateOptions(nextValue);
      return;
    }

    setForm((current) => ({ ...current, [name]: nextValue }));
    clearError(name);
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

    return toImgSrcFromBase64(
      form.employeeProfileImage,
      form.employeeImageFileName,
    );
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

  const reloadDetails = async () => {
    const result = await getEmployeeDetails(decodedEmployeeId);
    const nextForm = {
      ...blankEmployee,
      ...result.employee,
      genderID: String(result.employee.genderID || ""),
      countryID: String(result.employee.countryID || ""),
      stateID: String(result.employee.stateID || ""),
      employeeTypeID: String(result.employee.employeeTypeID || ""),
      file: null,
    };

    if (nextForm.countryID) {
      const stateResult = await getDropdownItems({
        sourceTable: DROPDOWN_SOURCE_TABLE.STATE,
        argumentValue: nextForm.countryID,
      });
      setStateOptions(stateResult.options);
    } else {
      setStateOptions([]);
    }

    setForm(nextForm);
    originalSnapshotRef.current = createComparableSnapshot(nextForm);
    return nextForm;
  };

  const cancelEdit = async () => {
    clearErrors();

    if (isAddMode) {
      navigate(NavigateEmployee);
      return;
    }

    setLoading(true);
    const loaderRequestId = showFullPageLoader("Loading Employee Details...");

    try {
      await reloadDetails();
      setMode("view");
    } catch (error) {
      showApiError(error, "Unable to reload Employee Details.");
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

    const loaderRequestId = showFullPageLoader("Saving Employee Details...");
    setSaving(true);

    try {
      const result = await saveAndUpdateEmployeeDetails({
        id: isAddMode ? 0 : decodedEmployeeId,
        fullName: form.fullName,
        genderID: form.genderID,
        dateOfBirth: form.dateOfBirth,
        mobileNo: form.mobileNo,
        emailID: form.emailID,
        countryID: form.countryID,
        stateID: form.stateID,
        cityName: form.cityName,
        pinCode: form.pinCode,
        skills: form.skills,
        hireDate: form.hireDate,
        employeeTypeID: form.employeeTypeID,
        parentCompanyName: form.parentCompanyName,
        isDeleted: mode === "edit" ? form.isDeleted : false,
        createdBy: isAddMode ? loggedInUserId : 0,
        updatedBy: isAddMode ? 0 : loggedInUserId,
        employeeProfileImage: form.file,
      });

      ToastService.success(
        result.message || "Employee Details Saved Successfully.",
      );

      const savedId = result.id || decodedEmployeeId;

      if (savedId > 0) {
        const detailsResult = await getEmployeeDetails(savedId);
        const nextForm = {
          ...blankEmployee,
          ...detailsResult.employee,
          genderID: String(detailsResult.employee.genderID || ""),
          countryID: String(detailsResult.employee.countryID || ""),
          stateID: String(detailsResult.employee.stateID || ""),
          employeeTypeID: String(detailsResult.employee.employeeTypeID || ""),
          file: null,
        };

        if (nextForm.countryID) {
          const stateResult = await getDropdownItems({
            sourceTable: DROPDOWN_SOURCE_TABLE.STATE,
            argumentValue: nextForm.countryID,
          });
          setStateOptions(stateResult.options);
        }

        setForm(nextForm);
        originalSnapshotRef.current = createComparableSnapshot(nextForm);
        setMode("view");

        if (isAddMode) {
          navigate(NavigateEmployee);
        } else {
          navigate(NavigateEmployee);
        }
      } else {
        navigate(NavigateEmployee);
      }
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          EMPLOYEE_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(error, "Unable to save Employee Details.");
      }
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
        message="You don't have permission to access this Employee action."
        returnPath={NavigateEmployee}
        returnLabel="Back to Employees"
      />
    );
  }

  if (loading) {
    return null;
  }

  const previewSource = getPreviewSource();

  return (
    <div className="tp-employee-form-page">
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
              title="Edit Employee"
              aria-label="Edit Employee"
            >
              <SvgIcon name="edit" size={18} />
            </button>
          )}

          <button
            type="submit"
            form="employee-form"
            className="tp-form-page-icon-button"
            disabled={!canSave || saving}
            title="Save Employee"
            aria-label="Save Employee"
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
            onClick={() => runWithDirtyGuard(() => navigate(NavigateEmployee))}
            title="Back to Employees"
            aria-label="Back to Employees"
          >
            <SvgIcon name="back" size={18} />
          </button>
        </div>
      </div>

      <form
        id="employee-form"
        className="tp-card tp-employee-form-card tp-employee-form-card-attached"
        onSubmit={save}
        noValidate
      >
        <div className="row g-3">
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
            <SearchableDropdown
              name="genderID"
              label="Gender"
              required
              placeholder="Search and Select Gender"
              value={form.genderID}
              onChange={update}
              options={genderOptions}
              columns={[{ key: "label", label: "Gender" }]}
              error={errors.genderID}
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4">
            <DatePicker
              name="dateOfBirth"
              label="Date of Birth"
              required
              value={form.dateOfBirth}
              onChange={update}
              error={errors.dateOfBirth}
              disabled={readOnly}
              allowFutureDates={false}
              allowPastDates
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
              maxLength={20}
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="emailID"
              label="Email ID"
              required
              type="email"
              icon="bi-envelope-fill"
              value={form.emailID}
              onChange={update}
              error={errors.emailID}
              placeholder="Enter Email ID"
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4">
            <SearchableDropdown
              name="countryID"
              label="Country"
              required
              placeholder="Search and Select Country"
              value={form.countryID}
              onChange={update}
              options={countryOptions}
              columns={[{ key: "label", label: "Country" }]}
              error={errors.countryID}
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4">
            <SearchableDropdown
              name="stateID"
              label="State"
              required
              value={form.stateID}
              onChange={update}
              options={stateOptions}
              columns={[{ key: "label", label: "State" }]}
              error={errors.stateID}
              disabled={readOnly || !form.countryID || stateLoading}
              placeholder={
                !form.countryID
                  ? "Select Country First."
                  : stateLoading
                    ? "Loading states..."
                    : "Search and Select State"
              }
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="cityName"
              label="City"
              required
              value={form.cityName}
              onChange={update}
              error={errors.cityName}
              placeholder="Enter City"
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="pinCode"
              label="Pincode"
              required
              value={form.pinCode}
              onChange={update}
              error={errors.pinCode}
              placeholder="Enter Pincode"
              disabled={readOnly}
              maxLength={20}
            />
          </div>

          <div className="col-md-6">
            <TextArea
              name="skills"
              label="Skills"
              required
              value={form.skills}
              onChange={update}
              error={errors.skills}
              placeholder="Enter Skills"
              disabled={readOnly}
              rows={4}
              maxLength={1000}
            />
          </div>

          <div className="col-md-6">
            <DatePicker
              name="hireDate"
              label="Hire Date"
              required
              value={form.hireDate}
              onChange={update}
              error={errors.hireDate}
              disabled={readOnly}
              allowFutureDates={false}
              allowPastDates
            />
          </div>

          <div className="col-md-4">
            <SearchableDropdown
              name="employeeTypeID"
              label="Employee Type"
              required
              placeholder="Search and Select Employee Type"
              value={form.employeeTypeID}
              onChange={update}
              options={employeeTypeOptions}
              columns={[{ key: "label", label: "Employee Type" }]}
              error={errors.employeeTypeID}
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="parentCompanyName"
              label="Parent Company"
              required
              value={form.parentCompanyName}
              onChange={update}
              error={errors.parentCompanyName}
              placeholder="Enter Parent Company"
              disabled={readOnly}
            />
          </div>

          {!readOnly && (
            <div className="col-md-4">
              <FileUpload
                name="employeeProfileImage"
                label="User Image"
                accept="image/*"
                onChange={handleFileChange}
              />

              {(previewSource || form.employeeImageFileName || form.file) && (
                <div className="tp-user-image-file-actions">
                  <span>
                    {form.file?.name ||
                      form.employeeImageFileName ||
                      "User Image"}
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
            <div className="col-md-4">
              <div className="tp-form-field">
                <label className="tp-form-label">User Image</label>
                <div className="tp-user-image-readonly">
                  {previewSource ? (
                    <>
                      <img
                        src={previewSource}
                        alt={form.fullName || "Employee"}
                      />
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

          {mode === "edit" && (
            <div className="col-12 d-flex align-items-end pb-2">
              <Checkbox
                name="isDeleted"
                label="Obsolete"
                checked={form.isDeleted}
                onChange={update}
                switchStyle
                disabled={saving}
              />
            </div>
          )}
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
        title={`${form.fullName || "Employee"} - User Image`}
        fileName={form.file?.name || form.employeeImageFileName}
        alt={form.fullName || "Employee"}
        onClose={() => setImagePreviewOpen(false)}
      />
    </div>
  );
}
