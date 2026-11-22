export const VALIDATION_MESSAGES = {
  required: (label = "This field") => `Enter The ${label}.`,
  selectRequired: (label = "value") => `Select The ${label}.`,
  invalidEmail: "Enter The Valid Email ID.",
  invalidMobile: "Enter The Valid Mobile Number.",
  minLength: (label = "Value", length) =>
    `${label} must contain at least ${length} characters.`,
  maxLength: (label = "Value", length) =>
    `${label} cannot exceed ${length} characters.`,
  exactLength: (label = "Value", length) =>
    `${label} must contain exactly ${length} characters.`,
  minValue: (label = "Value", value) => `${label} must be at least ${value}.`,
  maxValue: (label = "Value", value) =>
    `${label} cannot be greater than ${value}.`,
  invalidNumber: (label = "Value") => `Enter The Valid ${label}.`,
  invalidDate: (label = "date") => `Select The Valid ${label}.`,
  passwordMismatch: "New Password and Confirm Password do not match.",
  invalidPassword:
    "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character.",
};
