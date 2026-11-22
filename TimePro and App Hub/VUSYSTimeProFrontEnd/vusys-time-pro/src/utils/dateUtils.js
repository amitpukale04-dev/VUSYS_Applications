import { normalizeText } from "./valueUtils";

// src/utils/dateUtils.js
export const formatDate = (isoDate) => {
  if (!isoDate) return "";

  try {
    // Handle ISO date string like "2025-08-22T12:30:08"
    const date = new Date(isoDate);
    // Check if the date is valid
    if (isNaN(date.getTime())) {
      return "";
    }

    // Format as DD/MM/YYYY
    const day = String(date.getDate()).padStart(2, "0");
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const year = date.getFullYear();

    return `${day}/${month}/${year}`;
  } catch (error) {
    console.error("Error formatting date:", error);
    return "";
  }
};

/**
 * Generic date/time parser that handles most formats automatically.
 * Supports:
 * - "dd-MM-yyyy" / "dd/MM/yyyy"
 * - "yyyy-MM-dd" / "yyyy/MM/dd"
 * - "MM/dd/yyyy"
 * - "dd-MM-yyyy HH:mm:ss"
 * - ISO strings: "2025-11-10T12:00:00Z"
 * - Unix timestamps (seconds or milliseconds)
 * - Native Date objects
 */
export const parseDateTime = (input) => {
  if (!input) return null;
  if (input instanceof Date) return isNaN(input.getTime()) ? null : input;

  // Timestamp check (seconds or milliseconds)
  if (!isNaN(Number(input))) {
    const ts = Number(input);
    const dt = new Date(ts > 1e12 ? ts : ts * 1000);
    return isNaN(dt.getTime()) ? null : dt;
  }

  let s = String(input).trim();
  if (!s) return null;

  // Normalize separators
  s = s.replace(/[.]/g, "-").replace(/\//g, "-");

  const [datePart, timePart = "00:00:00"] = s.split(/\s+/);
  const dmy = datePart.split("-");

  let yyyy = 0,
    mm = 0,
    dd = 0;

  // Detect format automatically
  if (dmy[0].length === 4) {
    // yyyy-MM-dd
    yyyy = parseInt(dmy[0], 10);
    mm = parseInt(dmy[1], 10);
    dd = parseInt(dmy[2], 10);
  } else if (dmy[2] && dmy[2].length === 4) {
    // dd-MM-yyyy
    dd = parseInt(dmy[0], 10);
    mm = parseInt(dmy[1], 10);
    yyyy = parseInt(dmy[2], 10);
  } else {
    return null;
  }

  // Parse time safely
  const [HH = "0", MM = "0", SS = "0"] = timePart
    .split(":")
    .map((x) => parseInt(x || "0", 10));

  // Create Date with manual order (year, monthIndex, day)
  const dt = new Date(yyyy, mm - 1, dd, HH, MM, SS);

  return isNaN(dt.getTime()) ? null : dt;
};

export const formatDateTime = (date) => {
  const d = new Date(date);
  const day = String(d.getDate()).padStart(2, "0");
  const month = String(d.getMonth() + 1).padStart(2, "0");
  const year = d.getFullYear();

  const hours = String(d.getHours()).padStart(2, "0");
  const minutes = String(d.getMinutes()).padStart(2, "0");
  const seconds = String(d.getSeconds()).padStart(2, "0");

  return `${day}/${month}/${year}  ${hours}:${minutes}:${seconds}`;
};

export const getCurrentDate = () => {
  const today = new Date();
  const yyyy = today.getFullYear();
  const mm = String(today.getMonth() + 1).padStart(2, "0");
  const dd = String(today.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
};

export const formatYYYYMMDD = (d) => {
  if (!d) return "";
  const x = d instanceof Date ? d : new Date(d);
  if (!Number.isFinite(x.getTime())) return "";
  const yyyy = x.getFullYear();
  const mm = String(x.getMonth() + 1).padStart(2, "0");
  const dd = String(x.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
};

export const formatDDMMYYYY = (v) => {
  const iso = toISODateOnly(v);
  if (!iso) return "";
  const parts = iso.split("-");
  if (parts.length !== 3) return iso;
  const [yyyy, mm, dd] = parts;
  if (!yyyy || !mm || !dd) return iso;
  return `${dd}/${mm}/${yyyy}`;
};

export const formatDDMMMYYYY = (v) => {
  const iso = toISODateOnly(v);
  if (!iso) return "";

  const parts = iso.split("-");
  if (parts.length !== 3) return iso;

  const [yyyy, mm, dd] = parts;
  const monthNames = [
    "Jan",
    "Feb",
    "Mar",
    "Apr",
    "May",
    "Jun",
    "Jul",
    "Aug",
    "Sep",
    "Oct",
    "Nov",
    "Dec",
  ];

  const monthIndex = Number(mm) - 1;
  if (
    !yyyy ||
    !dd ||
    !Number.isInteger(monthIndex) ||
    monthIndex < 0 ||
    monthIndex > 11
  ) {
    return iso;
  }

  return `${dd}-${monthNames[monthIndex]}-${yyyy}`;
};

export const toISODateOnly = (v) => {
  if (!v) return "";
  const s = String(v);
  return s.length >= 10 ? s.slice(0, 10) : s;
};

/**
 * Validate that a given date (YYYY-MM-DD) is not in the future.
 *
 * @param {string} dateStr - Date in ISO format (e.g. "1995-08-20")
 * @param {string} errorMessage - Custom error message to return if future DOB.
 * @returns {string|null} - Returns the error message if invalid, otherwise null.
 */
export function validateNotFutureDate(
  dateStr,
  errorMessage = "Date cannot be in the future.",
) {
  if (!dateStr) return null; // let required validation handle empty case

  try {
    const selectedDate = new Date(dateStr);
    if (Number.isNaN(selectedDate.getTime())) {
      return "Invalid date format.";
    }

    const today = new Date();
    selectedDate.setHours(0, 0, 0, 0);
    today.setHours(0, 0, 0, 0);

    if (selectedDate > today) {
      return errorMessage;
    }

    return null;
  } catch {
    return "Invalid date format.";
  }
}

/**
 * This Function is Used To Resolve Current Month
 * @param {*} monthOptions
 * @returns
 */
export const resolveCurrentMonth = (monthOptions = []) => {
  const currentDate = new Date();
  const monthNumber = currentDate.getMonth() + 1;
  const candidates = [
    currentDate.toLocaleString("en-US", { month: "long" }),
    currentDate.toLocaleString("en-US", { month: "short" }),
    String(monthNumber),
    String(monthNumber).padStart(2, "0"),
  ].map(normalizeText);

  return monthOptions.find(
    (option) =>
      candidates.includes(normalizeText(option?.value)) ||
      candidates.includes(normalizeText(option?.label)),
  );
};

/**
 * This Function Is Used To Resolve Current Year.
 * @param {*} yearOptions
 * @returns
 */
export const resolveCurrentYear = (yearOptions = []) => {
  const currentYear = String(new Date().getFullYear());
  return yearOptions.find(
    (option) =>
      normalizeText(option?.value) === currentYear ||
      normalizeText(option?.label) === currentYear,
  );
};

// Append these named exports to your existing src/utils/dateUtils.js.
// Do not replace existing dateUtils.js contents.

export const toDateOnly = (value) => {
  if (!value) return "";
  const text = String(value);
  if (/^\d{4}-\d{2}-\d{2}/.test(text)) return text.slice(0, 10);
  const match = text.match(/^(\d{2})-([A-Za-z]{3})-(\d{4})$/);
  if (!match) return "";
  const month = [
    "Jan",
    "Feb",
    "Mar",
    "Apr",
    "May",
    "Jun",
    "Jul",
    "Aug",
    "Sep",
    "Oct",
    "Nov",
    "Dec",
  ].findIndex((item) => item.toLowerCase() === match[2].toLowerCase());
  return month < 0
    ? ""
    : `${match[3]}-${String(month + 1).padStart(2, "0")}-${match[1]}`;
};

/**
 * This Function Is Used To Format A Date Based On The Specified Date Format.
 * @param {string | Date} value - Input Date Value.
 * @param {string} dateFormat - Required Date Format.
 * @returns {string} Formatted Date Value.
 */
export const convertDateToSpecifiedFormat = (
  value,
  dateFormat = "dd-MMM-yyyy",
) => {
  if (!value) return "";

  const text = String(value).trim();

  const monthNames = [
    "January",
    "February",
    "March",
    "April",
    "May",
    "June",
    "July",
    "August",
    "September",
    "October",
    "November",
    "December",
  ];

  let day;
  let month;
  let year;

  if (value instanceof Date) {
    if (Number.isNaN(value.getTime())) return "";

    day = value.getDate();
    month = value.getMonth() + 1;
    year = value.getFullYear();
  } else {
    // Supports yyyy-MM-dd and ISO Date Strings.
    let match = text.match(/^(\d{4})-(\d{2})-(\d{2})(?:$|[T\s])/);

    if (match) {
      year = Number(match[1]);
      month = Number(match[2]);
      day = Number(match[3]);
    } else {
      // Supports dd-MMM-yyyy and dd-MMMM-yyyy.
      match = text.match(/^(\d{1,2})-([A-Za-z]+)-(\d{4})$/);

      if (match) {
        day = Number(match[1]);
        month =
          monthNames.findIndex(
            (item) =>
              item.toLowerCase() === match[2].toLowerCase() ||
              item.slice(0, 3).toLowerCase() === match[2].toLowerCase(),
          ) + 1;
        year = Number(match[3]);
      } else {
        // Supports dd/MM/yyyy and dd-MM-yyyy.
        match = text.match(/^(\d{1,2})[/-](\d{1,2})[/-](\d{4})$/);

        if (!match) return "";

        day = Number(match[1]);
        month = Number(match[2]);
        year = Number(match[3]);
      }
    }
  }

  // Validate Date.
  const date = new Date(year, month - 1, day);

  if (
    date.getFullYear() !== year ||
    date.getMonth() + 1 !== month ||
    date.getDate() !== day
  ) {
    return "";
  }

  const pad = (number) => String(number).padStart(2, "0");

  const hours = value instanceof Date ? value.getHours() : 0;
  const minutes = value instanceof Date ? value.getMinutes() : 0;
  const seconds = value instanceof Date ? value.getSeconds() : 0;

  const formatValues = {
    yyyy: String(year),
    yy: String(year).slice(-2),
    MMMM: monthNames[month - 1],
    MMM: monthNames[month - 1].slice(0, 3),
    MM: pad(month),
    M: String(month),
    dd: pad(day),
    d: String(day),
    HH: pad(hours),
    hh: pad(hours % 12 || 12),
    mm: pad(minutes),
    ss: pad(seconds),
    tt: hours >= 12 ? "PM" : "AM",
  };

  return dateFormat.replace(
    /yyyy|MMMM|MMM|yy|MM|dd|HH|hh|mm|ss|tt|M|d/g,
    (token) => formatValues[token],
  );
};

export const calculateLeaveDays = (fromDate, toDate, isHalfDay) => {
  if (!fromDate || !toDate || toDate < fromDate) return 0;
  const start = Date.parse(`${fromDate}T00:00:00Z`);
  const end = Date.parse(`${toDate}T00:00:00Z`);
  if (!Number.isFinite(start) || !Number.isFinite(end)) return 0;
  const days = Math.round((end - start) / 86400000) + 1;
  return isHalfDay ? Math.max(0, days - 0.5) : days;
};
