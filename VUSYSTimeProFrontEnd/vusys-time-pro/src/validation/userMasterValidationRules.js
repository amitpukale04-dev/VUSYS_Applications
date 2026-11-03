export const userMasterValidationRules = {
  employeeCode: {
    label: "Employee",
    required: true,
    messages: { required: "Select The Employee." },
  },
  userName: {
    label: "User Name",
    required: true,
    maxLength: 100,
  },
  fullName: {
    label: "Full Name",
    required: true,
    maxLength: 150,
  },
  mobileNo: {
    label: "Mobile Number",
    required: true,
    mobile: true,
  },
  userEmailID: {
    label: "Email ID",
    required: true,
    email: true,
  },
  roleID: {
    label: "Role",
    required: true,
    messages: { required: "Select The Role." },
  },
};
