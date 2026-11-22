export const emailConfigurationValidationRules = {
  fromEmail: {
    label: "Email ID",
    required: true,
    email: true,
  },
  password: {
    label: "Password",
    required: true,
  },
  smtpHost: {
    label: "SMTP Host",
    required: true,
    maxLength: 250,
  },
  portNo: {
    label: "Port No",
    required: true,
    number: true,
    minValue: 1,
    maxValue: 65535,
  },
};
