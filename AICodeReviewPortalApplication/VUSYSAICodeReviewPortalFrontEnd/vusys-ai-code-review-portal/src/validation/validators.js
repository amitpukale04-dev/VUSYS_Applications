const isEmpty = (value) => {
  if (value === null || value === undefined) return true;
  if (typeof value === "string") return value.trim() === "";
  if (Array.isArray(value)) return value.length === 0;
  return false;
};

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const MOBILE_PATTERN = /^[6-9]\d{9}$/;
const STRONG_PASSWORD_PATTERN =
  /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$/;

export const validators = {
  required(value) {
    return !isEmpty(value);
  },

  email(value) {
    return isEmpty(value) || EMAIL_PATTERN.test(String(value).trim());
  },

  mobile(value) {
    return isEmpty(value) || MOBILE_PATTERN.test(String(value).trim());
  },

  minLength(value, length) {
    return isEmpty(value) || String(value).length >= Number(length);
  },

  maxLength(value, length) {
    return isEmpty(value) || String(value).length <= Number(length);
  },

  exactLength(value, length) {
    return isEmpty(value) || String(value).length === Number(length);
  },

  number(value) {
    return isEmpty(value) || !Number.isNaN(Number(value));
  },

  minValue(value, minimum) {
    return isEmpty(value) || Number(value) >= Number(minimum);
  },

  maxValue(value, maximum) {
    return isEmpty(value) || Number(value) <= Number(maximum);
  },

  date(value) {
    return isEmpty(value) || !Number.isNaN(new Date(value).getTime());
  },

  strongPassword(value) {
    return isEmpty(value) || STRONG_PASSWORD_PATTERN.test(String(value));
  },

  matches(value, compareValue) {
    return value === compareValue;
  },

  custom(value, validator, values) {
    return typeof validator !== "function" || Boolean(validator(value, values));
  },
};

export { isEmpty };
