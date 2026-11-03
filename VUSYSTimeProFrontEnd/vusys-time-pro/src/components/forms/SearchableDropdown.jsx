import { useEffect, useMemo, useRef, useState } from "react";
import FormLabel from "./FormLabel";

export default function SearchableDropdown({
  id,
  name,
  label,
  value,
  onChange,
  options = [],
  columns = [{ key: "label", label: "Name" }],
  multiple = false,
  error,
  required,
  placeholder = "Search and select...",
  disabled,
}) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const rootRef = useRef(null);
  const selectedValues = multiple
    ? Array.isArray(value)
      ? value
      : []
    : [value].filter(Boolean);
  const selected = options.filter((option) =>
    selectedValues.includes(option.value),
  );
  const filtered = useMemo(
    () =>
      options.filter((option) =>
        columns.some((column) =>
          String(option[column.key] ?? "")
            .toLowerCase()
            .includes(query.toLowerCase()),
        ),
      ),
    [columns, options, query],
  );

  useEffect(() => {
    const outside = (event) => {
      if (!rootRef.current?.contains(event.target)) setOpen(false);
    };
    document.addEventListener("mousedown", outside);
    return () => document.removeEventListener("mousedown", outside);
  }, []);

  const select = (option) => {
    if (multiple) {
      const exists = selectedValues.includes(option.value);
      onChange?.({
        target: {
          name,
          value: exists
            ? selectedValues.filter((item) => item !== option.value)
            : [...selectedValues, option.value],
        },
      });
    } else {
      onChange?.({ target: { name, value: option.value } });
      setOpen(false);
    }
  };

  return (
    <div className="tp-form-field" ref={rootRef}>
      {label && (
        <FormLabel htmlFor={id || name} required={required}>
          {label}
        </FormLabel>
      )}
      <button
        id={id || name}
        type="button"
        disabled={disabled}
        className={`tp-searchable-select ${error ? "is-invalid" : ""} ${selected.length ? "has-value" : "is-placeholder"}`}
        onClick={() => setOpen((current) => !current)}
      >
        <span
          className={
            selected.length
              ? "tp-searchable-value"
              : "tp-searchable-placeholder"
          }
        >
          {selected.length
            ? selected.map((item) => item.label).join(", ")
            : placeholder}
        </span>
        <i className="bi bi-chevron-down" />
      </button>
      {open && (
        <div className="tp-searchable-dropdown">
          <div className="tp-searchable-input">
            <i className="bi bi-search" />
            <input
              autoFocus
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search..."
            />
          </div>
          <div className="tp-searchable-header">
            {columns.map((column) => (
              <span key={column.key}>{column.label}</span>
            ))}
          </div>
          <div className="tp-searchable-options">
            {filtered.map((option) => (
              <button
                type="button"
                key={option.value}
                onClick={() => select(option)}
                className={
                  selectedValues.includes(option.value) ? "selected" : ""
                }
              >
                {multiple && (
                  <i
                    className={`bi ${selectedValues.includes(option.value) ? "bi-check-square-fill" : "bi-square"}`}
                  />
                )}
                {columns.map((column) => (
                  <span key={column.key}>{option[column.key]}</span>
                ))}
              </button>
            ))}
            {!filtered.length && (
              <div className="tp-searchable-empty">No records found</div>
            )}
          </div>
        </div>
      )}
      {error && <div className="tp-field-error">{error}</div>}
    </div>
  );
}
