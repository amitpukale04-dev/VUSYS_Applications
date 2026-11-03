import { useCallback, useState } from "react";
import { validateField, validateForm } from "../validation/validateForm";

export default function useFormValidation(rules = {}) {
  const [errors, setErrors] = useState({});

  const validate = useCallback(
    (values) => {
      const result = validateForm(values, rules);
      setErrors(result.errors);
      return result.isValid;
    },
    [rules],
  );

  const validateOne = useCallback(
    (name, value, values) => {
      const message = validateField(name, value, values, rules);
      setErrors((current) => {
        if (!message) {
          const next = { ...current };
          delete next[name];
          return next;
        }
        return { ...current, [name]: message };
      });
      return !message;
    },
    [rules],
  );

  const clearError = useCallback((name) => {
    setErrors((current) => {
      if (!current[name]) return current;
      const next = { ...current };
      delete next[name];
      return next;
    });
  }, []);

  const clearErrors = useCallback(() => setErrors({}), []);

  const setFieldError = useCallback((name, message) => {
    setErrors((current) => ({ ...current, [name]: message }));
  }, []);

  return {
    errors,
    validate,
    validateOne,
    clearError,
    clearErrors,
    setFieldError,
    setErrors,
  };
}
