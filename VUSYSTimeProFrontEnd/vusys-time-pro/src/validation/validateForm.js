import { VALIDATION_MESSAGES } from "./validationMessages";
import { validators } from "./validators";

const getRuleMessage = ({ ruleName, ruleValue, fieldRule, label }) => {
  if (fieldRule.messages?.[ruleName]) {
    return typeof fieldRule.messages[ruleName] === "function"
      ? fieldRule.messages[ruleName](ruleValue)
      : fieldRule.messages[ruleName];
  }

  switch (ruleName) {
    case "required":
      return VALIDATION_MESSAGES.required(label);
    case "email":
      return VALIDATION_MESSAGES.invalidEmail;
    case "mobile":
      return VALIDATION_MESSAGES.invalidMobile;
    case "minLength":
      return VALIDATION_MESSAGES.minLength(label, ruleValue);
    case "maxLength":
      return VALIDATION_MESSAGES.maxLength(label, ruleValue);
    case "exactLength":
      return VALIDATION_MESSAGES.exactLength(label, ruleValue);
    case "number":
      return VALIDATION_MESSAGES.invalidNumber(label);
    case "minValue":
      return VALIDATION_MESSAGES.minValue(label, ruleValue);
    case "maxValue":
      return VALIDATION_MESSAGES.maxValue(label, ruleValue);
    case "date":
      return VALIDATION_MESSAGES.invalidDate(label);
    case "strongPassword":
      return VALIDATION_MESSAGES.invalidPassword;
    case "matches":
      return fieldRule.matchMessage || VALIDATION_MESSAGES.passwordMismatch;
    default:
      return `${label} is invalid`;
  }
};

export function validateField(name, value, values, rules = {}) {
  const fieldRule = rules[name];
  if (!fieldRule || fieldRule.disabled) return "";

  const label = fieldRule.label || name;
  const ruleOrder = [
    "required",
    "email",
    "mobile",
    "number",
    "date",
    "minLength",
    "maxLength",
    "exactLength",
    "minValue",
    "maxValue",
    "strongPassword",
    "matches",
    "custom",
  ];

  for (const ruleName of ruleOrder) {
    const ruleValue = fieldRule[ruleName];
    if (ruleValue === undefined || ruleValue === false) continue;

    let isValid = true;

    if (ruleName === "matches") {
      isValid = validators.matches(value, values?.[ruleValue]);
    } else if (ruleName === "custom") {
      isValid = validators.custom(value, ruleValue, values);
    } else {
      isValid = validators[ruleName](value, ruleValue);
    }

    if (!isValid) {
      if (ruleName === "custom" && fieldRule.customMessage) {
        return typeof fieldRule.customMessage === "function"
          ? fieldRule.customMessage(value, values)
          : fieldRule.customMessage;
      }

      return getRuleMessage({ ruleName, ruleValue, fieldRule, label });
    }
  }

  return "";
}

export function validateForm(values, rules = {}) {
  const errors = {};

  Object.keys(rules).forEach((name) => {
    const message = validateField(name, values?.[name], values, rules);
    if (message) errors[name] = message;
  });

  return {
    errors,
    isValid: Object.keys(errors).length === 0,
  };
}
