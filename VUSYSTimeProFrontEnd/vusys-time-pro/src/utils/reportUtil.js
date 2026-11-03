import { normalizeText } from "./valueUtils";

/**
 * Resolves default Employee, Month, and Year filters.
 *
 * @param {Array} employeeOptions - Employee dropdown options.
 * @param {Array} monthOptions - Month dropdown options.
 * @param {Array} yearOptions - Year dropdown options.
 * @param {String} reportType - Report Type.
 * @returns {Object} Default filter values.
 */
export const resolveDefaultFilters = (
  employeeOptions = [],
  monthOptions = [],
  yearOptions = [],
  reportType = "Timesheet",
) => {
  const currentDate = new Date();
  const currentMonthNumber = currentDate.getMonth() + 1;
  const currentMonthNumberText = String(currentMonthNumber);
  const currentMonthNumberPadded = currentMonthNumberText.padStart(2, "0");

  const currentMonthName = currentDate.toLocaleString("en-US", {
    month: "long",
  });

  const currentMonthShortName = currentDate.toLocaleString("en-US", {
    month: "short",
  });

  const currentYear = String(currentDate.getFullYear());

  const employeeOption = employeeOptions.find(
    (option) =>
      normalizeText(option.value) === "0" ||
      normalizeText(option.label) === "all",
  );

  const monthOption = monthOptions.find((option) => {
    const value = normalizeText(option.value);
    const label = normalizeText(option.label);

    const currentMonthCandidates = [
      normalizeText(currentMonthName),
      normalizeText(currentMonthShortName),
      currentMonthNumberText,
      currentMonthNumberPadded,
    ];

    return (
      currentMonthCandidates.includes(value) ||
      currentMonthCandidates.includes(label)
    );
  });

  const yearOption = yearOptions.find(
    (option) =>
      normalizeText(option.value) === currentYear ||
      normalizeText(option.label) === currentYear,
  );

  return {
    employeeCode: String(employeeOption?.value ?? "0"),
    ...(reportType === "Leave"
      ? {
          leaveMonth: String(monthOption?.value ?? ""),
          leaveYear: String(yearOption?.value ?? currentYear),
        }
      : {
          timesheetMonth: String(monthOption?.value ?? ""),
          timesheetYear: String(yearOption?.value ?? currentYear),
        }),
  };
};
