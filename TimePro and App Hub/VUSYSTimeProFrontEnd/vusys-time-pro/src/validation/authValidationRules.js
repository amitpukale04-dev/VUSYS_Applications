export const loginValidationRules = {
  email: {
    label: "Email ID",
    required: true,
    email: true,
  },
  password: {
    label: "Password",
    required: true,
  },
};

export const forgotPasswordValidationRules = {
  email: {
    label: "Email ID",
    required: true,
    email: true,
  },
};

export const changePasswordValidationRules = {
  current: {
    label: "Current Password",
    required: true,
  },
  next: {
    label: "New Password",
    required: true,
    minLength: 8,
    strongPassword: true,
  },
  confirm: {
    label: "Confirm Password",
    required: true,
    matches: "next",
  },
};

export const resetPasswordValidationRules = {
  password: {
    label: "New Password",
    required: true,
    minLength: 8,
    strongPassword: true,
  },
  confirmPassword: {
    label: "Confirm Password",
    required: true,
    matches: "password",
  },
};
