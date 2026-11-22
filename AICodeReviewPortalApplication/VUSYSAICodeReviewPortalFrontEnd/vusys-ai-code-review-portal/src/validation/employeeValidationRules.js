export const employeeValidationRules = {
  fullName: {
    label: "Full Name",
    required: true,
    minLength: 2,
    maxLength: 150
  },
  genderID: {
    label: "Gender",
    required: true,
    messages: { required: "Select The Gender." },
  },
  dateOfBirth: {
    label: "Date of Birth",
    required: true,
    date: true,
    messages: { required: "Select The Date of Birth." },
  },
  mobileNo: {
    label: "Mobile Number",
    required: true,
    maxLength: 20,
  },
  emailID: {
    label: "Email ID",
    required: true,
    email: true,
  },
  countryID: {
    label: "Country",
    required: true,
    messages: { required: "Select The Country." },
  },
  stateID: {
    label: "State",
    required: true,
    messages: { required: "Select The State." },
  },
  cityName: {
    label: "City",
    required: true,
    maxLength: 100,
  },
  pinCode: {
    label: "Pincode",
    required: true,
    maxLength: 20,
  },
  skills: {
    label: "Skills",
    required: true,
    maxLength: 1000,
  },
  hireDate: {
    label: "Hire Date",
    required: true,
    date: true,
    messages: { required: "Select The Hire Date." },
  },
  employeeTypeID: {
    label: "Employee Type",
    required: true,
    messages: { required: "Select The Employee Type." },
  },
  parentCompanyName: {
    label: "Parent Company",
    required: true,
    maxLength: 200,
  },
};
