import { useEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import FormLabel from "./FormLabel";
import { getHolidayList } from "../../services/holidayService";
import { showApiError } from "../../utils/showApiError";
import { getCurrentDate } from "../../utils/dateUtils";

const dateKey = (year, month, day) =>
  `${year}-${String(month + 1).padStart(2, "0")}-${String(day).padStart(2, "0")}`;

const parseDate = (value) => {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(String(value || ""))) return null;
  const [year, month, day] = value.split("-").map(Number);
  const date = new Date(year, month - 1, day);
  return date.getFullYear() === year &&
    date.getMonth() === month - 1 &&
    date.getDate() === day
    ? date
    : null;
};

// Follow the browser's system locale for the displayed date order and month names.
const CLIENT_LOCALE =
  typeof navigator !== "undefined" ? navigator.language : "en-US";
const DATE_PARTS = new Intl.DateTimeFormat(CLIENT_LOCALE, {
  day: "2-digit",
  month: "short",
  year: "numeric",
}).formatToParts(new Date(2026, 9, 16));
const DATE_ORDER = DATE_PARTS.filter((part) =>
  ["day", "month", "year"].includes(part.type),
).map((part) => part.type);
const DATE_SEPARATOR =
  DATE_PARTS.find(
    (part) => part.type === "literal" && /[/.-]/.test(part.value),
  )?.value.trim() || "-";
const MONTH_NAMES = Array.from({ length: 12 }, (_, index) =>
  new Intl.DateTimeFormat(CLIENT_LOCALE, { month: "short" }).format(
    new Date(2026, index, 1),
  ),
);
const ENGLISH_MONTHS = [
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
const DATE_PLACEHOLDER = DATE_ORDER.map((part) =>
  part === "year" ? "yyyy" : part === "month" ? "MMM" : "dd",
).join(DATE_SEPARATOR);

const formatDisplayDate = (value) => {
  const date = parseDate(value);
  if (!date) return "";
  const parts = {
    day: String(date.getDate()).padStart(2, "0"),
    month: MONTH_NAMES[date.getMonth()],
    year: String(date.getFullYear()),
  };
  return DATE_ORDER.map((part) => parts[part]).join(DATE_SEPARATOR);
};

// Accept locale-formatted, ISO, compact numeric and compact month-name dates.
// The input is deliberately not rewritten on each keystroke: doing so would
// prevent typing "16oct2026" or editing an existing date in the middle.
const parseTypedDate = (value) => {
  const text = String(value || "").trim();
  if (!text) return null;

  const toIso = (year, month, day) => {
    const iso = `${year}-${String(month).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
    return parseDate(iso) ? iso : null;
  };

  const resolveMonth = (textValue) => {
    const token = String(textValue)
      .toLocaleLowerCase(CLIENT_LOCALE)
      .replace(/\./g, "");
    if (/^\d{1,2}$/.test(token)) return Number(token);

    const localeIndex = MONTH_NAMES.findIndex(
      (item) =>
        item.toLocaleLowerCase(CLIENT_LOCALE).replace(/\./g, "") === token,
    );
    if (localeIndex >= 0) return localeIndex + 1;

    const englishIndex = ENGLISH_MONTHS.findIndex(
      (item) => item.toLowerCase() === token.slice(0, 3),
    );
    return englishIndex + 1;
  };

  // ISO is unambiguous regardless of the client's locale.
  const isoMatch = text.match(/^(\d{4})[-/.](\d{1,2})[-/.](\d{1,2})$/);
  if (isoMatch)
    return toIso(isoMatch[1], Number(isoMatch[2]), Number(isoMatch[3]));

  // 16oct2026, 16-Oct-2026, 16/Oct/2026 and 16 Oct 2026.
  const dayMonthName = text.match(
    /^(\d{1,2})[\s/.,-]*([\p{L}]{3,})[\s/.,-]*(\d{4})$/u,
  );
  if (dayMonthName)
    return toIso(
      dayMonthName[3],
      resolveMonth(dayMonthName[2]),
      Number(dayMonthName[1]),
    );

  // Also accept the month-first form used by some system locales.
  const monthNameDay = text.match(
    /^([\p{L}]{3,})[\s/.,-]*(\d{1,2})[\s/.,-]*(\d{4})$/u,
  );
  if (monthNameDay)
    return toIso(
      monthNameDay[3],
      resolveMonth(monthNameDay[1]),
      Number(monthNameDay[2]),
    );

  // Compact eight-digit input follows the client's displayed date order.
  if (/^\d{8}$/.test(text)) {
    const lengths = DATE_ORDER.map((part) => (part === "year" ? 4 : 2));
    let offset = 0;
    const parts = {};
    DATE_ORDER.forEach((part, index) => {
      parts[part] = text.slice(offset, offset + lengths[index]);
      offset += lengths[index];
    });
    return toIso(parts.year, Number(parts.month), Number(parts.day));
  }

  const tokens = text.split(/[\s/.,-]+/).filter(Boolean);
  if (tokens.length !== 3) return null;

  const parts = Object.fromEntries(
    DATE_ORDER.map((part, index) => [part, tokens[index]]),
  );
  if (!/^\d{4}$/.test(parts.year) || !/^\d{1,2}$/.test(parts.day)) return null;
  return toIso(parts.year, resolveMonth(parts.month), Number(parts.day));
};

const isNoHolidayRecords = (error) =>
  /no record|not found|no data|holiday list is empty/i.test(
    String(error?.message || ""),
  );

// ============================================================
// HOLIDAY AND TASK CALENDAR DATE - REUSABLE DATE CONTROL
// ============================================================
export default function HolidayAndTaskDatePicker({
  id,
  name,
  label,
  value = "",
  onChange,
  onBlur,
  error,
  required = false,
  disabled = false,
  readOnly = false,
  allowFutureDates = true,
  allowPastDates = true,
  disableHolidays = true,
  showHolidays = true,
  showSaturday = true,
  showSunday = true,
  disableSaturday = false,
  disableSunday = false,
  minDate = "",
  maxDate = "",
  className = "",
}) {
  const inputId = id || name;
  const today = getCurrentDate();
  const selectedDate = parseDate(value);
  const [visibleMonth, setVisibleMonth] = useState(
    () => selectedDate || new Date(),
  );
  const [isOpen, setIsOpen] = useState(false);
  const [typedValue, setTypedValue] = useState(() => formatDisplayDate(value));
  const [loadingYear, setLoadingYear] = useState(null);
  const [pendingTypedDate, setPendingTypedDate] = useState(null);
  const [calendarView, setCalendarView] = useState("days");
  const [monthSlide, setMonthSlide] = useState(null);
  const [slideStarted, setSlideStarted] = useState(false);
  const monthSlideTimerRef = useRef(null);
  const [yearPage, setYearPage] = useState(
    () => Math.floor((selectedDate || new Date()).getFullYear() / 10) * 10,
  );
  const [holidaysByYear, setHolidaysByYear] = useState({});
  const [position, setPosition] = useState({ top: 0, left: 0 });
  const wrapperRef = useRef(null);
  const popupRef = useRef(null);
  const requestedYearsRef = useRef(new Set());
  const year = visibleMonth.getFullYear();
  const pendingYear = pendingTypedDate
    ? Number(pendingTypedDate.slice(0, 4))
    : null;
  const month = visibleMonth.getMonth();
  const resolvedMinDate =
    [minDate, !allowPastDates ? today : ""].filter(Boolean).sort().at(-1) || "";
  const resolvedMaxDate =
    [maxDate, !allowFutureDates ? today : ""].filter(Boolean).sort()[0] || "";

  useEffect(() => {
    setTypedValue(formatDisplayDate(value));
  }, [value]);

  useEffect(() => {
    if (selectedDate) {
      setVisibleMonth(selectedDate);
      setYearPage(Math.floor(selectedDate.getFullYear() / 10) * 10);
    }
  }, [value]);

  useEffect(() => {
    const holidayYear = pendingYear || (isOpen ? year : null);
    if (!showHolidays || !holidayYear || requestedYearsRef.current.has(holidayYear)) return;
    requestedYearsRef.current.add(holidayYear);
    setLoadingYear(holidayYear);
    getHolidayList(holidayYear)
      .then((rows) => {
        setHolidaysByYear((current) => ({ ...current, [holidayYear]: rows }));
      })
      .catch((requestError) => {
        if (isNoHolidayRecords(requestError)) {
          // No holiday records means all dates are available for this year.
          setHolidaysByYear((current) => ({ ...current, [holidayYear]: [] }));
        } else {
          requestedYearsRef.current.delete(holidayYear);
          setPendingTypedDate(null);
          setTypedValue("");
          showApiError(requestError, "Unable to load Holiday List.");
        }
      })
      .finally(() =>
        setLoadingYear((current) => (current === holidayYear ? null : current)),
      );
  }, [isOpen, year, pendingYear, showHolidays]);

  const holidayMap = useMemo(() => {
    const map = new Map();
    (showHolidays ? holidaysByYear[year] || [] : []).forEach((holiday) => {
      if (holiday.isActive && holiday.holidayDate)
        map.set(holiday.holidayDate, holiday);
    });
    return map;
  }, [holidaysByYear, year, showHolidays]);

  // Keep the portal calendar within the visible viewport on desktop, tablet
  // and mobile, including when the mobile on-screen keyboard is displayed.
  useEffect(() => {
    if (!isOpen) return;

    const updatePosition = () => {
      const rect = wrapperRef.current?.getBoundingClientRect();
      if (!rect) return;

      const viewport = window.visualViewport;
      const viewportLeft = viewport?.offsetLeft ?? 0;
      const viewportTop = viewport?.offsetTop ?? 0;
      const viewportWidth = viewport?.width ?? window.innerWidth;
      const viewportHeight = viewport?.height ?? window.innerHeight;
      const edgeGap = 8;
      const popupWidth = popupRef.current?.offsetWidth || Math.min(242, viewportWidth - 16);
      const popupHeight = popupRef.current?.offsetHeight || 315;
      const minLeft = viewportLeft + edgeGap;
      const maxLeft = viewportLeft + viewportWidth - popupWidth - edgeGap;
      const left = Math.max(minLeft, Math.min(rect.left, Math.max(minLeft, maxLeft)));

      const availableBelow = viewportTop + viewportHeight - rect.bottom - edgeGap;
      const availableAbove = rect.top - viewportTop - edgeGap;
      const showAbove = availableBelow < popupHeight + 5 && availableAbove > availableBelow;
      const preferredTop = showAbove
        ? rect.top - popupHeight - 5
        : rect.bottom + 5;
      const minTop = viewportTop + edgeGap;
      const maxTop = viewportTop + viewportHeight - popupHeight - edgeGap;
      const top = Math.max(minTop, Math.min(preferredTop, Math.max(minTop, maxTop)));

      setPosition((current) =>
        current.top === top && current.left === left
          ? current
          : { top, left },
      );
    };

    const handlePointer = (event) => {
      if (
        !wrapperRef.current?.contains(event.target) &&
        !popupRef.current?.contains(event.target)
      )
        setIsOpen(false);
    };
    const handleKey = (event) => {
      if (event.key === "Escape") setIsOpen(false);
    };

    updatePosition();
    const resizeObserver = typeof ResizeObserver !== "undefined"
      ? new ResizeObserver(updatePosition)
      : null;
    if (popupRef.current) resizeObserver?.observe(popupRef.current);
    if (wrapperRef.current) resizeObserver?.observe(wrapperRef.current);

    document.addEventListener("pointerdown", handlePointer);
    document.addEventListener("keydown", handleKey);
    window.addEventListener("resize", updatePosition);
    window.addEventListener("scroll", updatePosition, true);
    window.visualViewport?.addEventListener("resize", updatePosition);
    window.visualViewport?.addEventListener("scroll", updatePosition);

    return () => {
      resizeObserver?.disconnect();
      document.removeEventListener("pointerdown", handlePointer);
      document.removeEventListener("keydown", handleKey);
      window.removeEventListener("resize", updatePosition);
      window.removeEventListener("scroll", updatePosition, true);
      window.visualViewport?.removeEventListener("resize", updatePosition);
      window.visualViewport?.removeEventListener("scroll", updatePosition);
    };
  }, [isOpen, calendarView, year]);

  const selectDate = (date) => {
    setTypedValue(formatDisplayDate(date));
    onChange?.({ target: { name, value: date, type: "text" } });
    onBlur?.({ target: { name, value: date } });
    setIsOpen(false);
  };

  const clearTypedDate = () => {
    setPendingTypedDate(null);
    setTypedValue("");
    if (value) onChange?.({ target: { name, value: "", type: "text" } });
  };

  // Selection restrictions are independent from highlighting flags.
  // Apply the same rules to calendar clicks, manual entry and Today.
  const isUnavailableDate = (date, holidays = []) => {
    const parsed = parseDate(date);
    if (!parsed) return true;

    return Boolean(
      (showHolidays &&
        disableHolidays &&
        holidays.some((item) => item.isActive && item.holidayDate === date)) ||
      (disableSaturday && parsed.getDay() === 6) ||
      (disableSunday && parsed.getDay() === 0) ||
      (resolvedMinDate && date < resolvedMinDate) ||
      (resolvedMaxDate && date > resolvedMaxDate),
    );
  };

  const handleTypedChange = (text) => {
    setPendingTypedDate(null);
    // Keep typed separators, month names and caret position unchanged.
    // Formatting is applied only after the complete date is validated.
    setTypedValue(text);
  };

  const commitTypedDate = () => {
    if (!typedValue.trim()) {
      clearTypedDate();
      return;
    }
    const typedDate = parseTypedDate(typedValue);
    if (!typedDate) {
      clearTypedDate();
      return;
    }
    const typedYear = Number(typedDate.slice(0, 4));
    if (
      showHolidays &&
      !Object.prototype.hasOwnProperty.call(holidaysByYear, typedYear)
    ) {
      setPendingTypedDate(typedDate);
      return;
    }
    if (isUnavailableDate(typedDate, showHolidays ? holidaysByYear[typedYear] : [])) {
      clearTypedDate();
      return;
    }
    setPendingTypedDate(null);
    selectDate(typedDate);
  };

  useEffect(() => {
    if (!pendingTypedDate) return;
    const typedYear = Number(pendingTypedDate.slice(0, 4));
    if (
      showHolidays &&
      !Object.prototype.hasOwnProperty.call(holidaysByYear, typedYear)
    ) return;
    if (isUnavailableDate(
      pendingTypedDate,
      showHolidays ? holidaysByYear[typedYear] : [],
    )) {
      clearTypedDate();
    } else {
      selectDate(pendingTypedDate);
      setPendingTypedDate(null);
    }
  }, [holidaysByYear, pendingTypedDate, showHolidays, disableHolidays,
    disableSaturday, disableSunday, resolvedMinDate, resolvedMaxDate]);

  const moveMonth = (offset) => {
    if (monthSlide) return;
    const next = new Date(year, month + offset, 1);
    setMonthSlide({ direction: offset, next });
    setSlideStarted(false);
    window.requestAnimationFrame(() => {
      window.requestAnimationFrame(() => setSlideStarted(true));
    });
    window.clearTimeout(monthSlideTimerRef.current);
    monthSlideTimerRef.current = window.setTimeout(() => {
      setVisibleMonth(next);
      setMonthSlide(null);
      setSlideStarted(false);
    }, 260);
  };

  useEffect(() => () => window.clearTimeout(monthSlideTimerRef.current), []);

  const changeYear = (selectedYear) => {
    setVisibleMonth(new Date(selectedYear, month, 1));
    setYearPage(Math.floor(selectedYear / 10) * 10);
    setCalendarView("months");
  };

  const changeMonth = (selectedMonth) => {
    setVisibleMonth(new Date(year, selectedMonth, 1));
    setCalendarView("days");
  };

  const selectToday = () => {
    const todayDate = parseDate(today);
    if (!todayDate) return;
    const currentYear = todayDate.getFullYear();
    if (year !== currentYear) {
      setVisibleMonth(todayDate);
      setCalendarView("days");
      return;
    }
    if (
      (showHolidays &&
        !Object.prototype.hasOwnProperty.call(holidaysByYear, currentYear)) ||
      isUnavailableDate(today, showHolidays ? holidaysByYear[currentYear] : [])
    ) {
      setVisibleMonth(todayDate);
      setCalendarView("days");
      return;
    }
    selectDate(today);
  };
  const renderMonthGrid = (gridDate) => {
    const gridYear = gridDate.getFullYear();
    const gridMonth = gridDate.getMonth();
    const firstWeekday = new Date(gridYear, gridMonth, 1).getDay();
    const daysInMonth = new Date(gridYear, gridMonth + 1, 0).getDate();
    const cells = Array.from({ length: 42 }, (_, index) => {
      const day = index - firstWeekday + 1;
      return day >= 1 && day <= daysInMonth ? day : null;
    });
    const gridHolidayMap = new Map();
    (showHolidays ? holidaysByYear[gridYear] || [] : []).forEach((holiday) => {
      if (holiday.isActive && holiday.holidayDate)
        gridHolidayMap.set(holiday.holidayDate, holiday);
    });
    return (
      <div className="tp-holiday-calendar-grid">
        {["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"].map((day) => (
          <span key={day} className="tp-holiday-calendar-weekday">
            {day}
          </span>
        ))}
        {cells.map((day, index) => {
          if (!day) return <span key={`empty-${index}`} />;
          const date = dateKey(gridYear, gridMonth, day);
          const holiday = gridHolidayMap.get(date);
          const outOfRange =
            (resolvedMinDate && date < resolvedMinDate) ||
            (resolvedMaxDate && date > resolvedMaxDate);
          const unavailable = Boolean(
            isUnavailableDate(date, showHolidays ? holidaysByYear[gridYear] : []) ||
            (showHolidays &&
              !Object.prototype.hasOwnProperty.call(holidaysByYear, gridYear)),
          );
          const isWeekend =
            (showSunday && index % 7 === 0) ||
            (showSaturday && index % 7 === 6);
          const description = holiday
            ? holiday.holidayDescription || holiday.holidayName || "Holiday"
            : "";
          return (
            <span
              key={date}
              title={description || (outOfRange ? "Date not available" : "")}
              className="tp-holiday-calendar-cell"
            >
              <button
                type="button"
                className={`tp-holiday-calendar-day ${holiday ? "is-holiday" : isWeekend ? "is-weekend" : ""} ${value === date ? "is-selected" : ""} ${date === today ? "is-today" : ""}`}
                disabled={unavailable}
                title={description || (outOfRange ? "Date not available" : "")}
                aria-label={`${date}${description ? `, Holiday: ${description}` : ""}`}
                onClick={() => selectDate(date)}
              >
                {day}
              </button>
            </span>
          );
        })}
      </div>
    );
  };
  return (
    <div className={`tp-form-field ${className}`} ref={wrapperRef}>
      {label && (
        <FormLabel htmlFor={inputId} required={required}>
          {label}
        </FormLabel>
      )}
      <div
        className={`tp-form-control-wrap tp-holiday-date-input ${error ? "is-invalid" : ""}`}
      >
        <input
          id={inputId}
          name={name}
          type="text"
          value={typedValue}
          placeholder={DATE_PLACEHOLDER}
          autoComplete="off"
          disabled={disabled}
          readOnly={readOnly}
          required={required}
          aria-invalid={Boolean(error)}
          aria-describedby={error ? `${inputId}-error` : undefined}
          aria-haspopup="dialog"
          aria-expanded={isOpen}
          maxLength={18}
          onChange={(event) => handleTypedChange(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              event.preventDefault();
              commitTypedDate();
            }
          }}
          onBlur={(event) => {
            if (!popupRef.current?.contains(event.relatedTarget))
              commitTypedDate();
            onBlur?.(event);
          }}
        />
        <button
          type="button"
          className="tp-holiday-date-trigger"
          aria-label={`Open ${label || name} calendar`}
          disabled={disabled || readOnly}
          onClick={() => {
            setCalendarView("days");
            setIsOpen((open) => !open);
          }}
        >
          <i className="bi bi-calendar-event" aria-hidden="true" />
        </button>
      </div>
      {error && (
        <div id={`${inputId}-error`} className="tp-field-error" role="alert">
          {typeof error === "string" ? error : ""}
        </div>
      )}
      {isOpen &&
        !disabled &&
        !readOnly &&
        createPortal(
          <div
            ref={popupRef}
            className="tp-holiday-calendar"
            role="dialog"
            aria-label={`${label || name} calendar`}
            style={{ top: position.top, left: position.left }}
          >
            <div className="tp-holiday-calendar-header">
              <button
                type="button"
                className="tp-holiday-calendar-heading"
                onClick={() => {
                  setYearPage(Math.floor(year / 10) * 10);
                  setCalendarView(calendarView === "days" ? "months" : "days");
                }}
                aria-label="Choose month and year"
              >
                {visibleMonth.toLocaleString("en-US", { month: "long" })},{" "}
                {year}
                <i className="bi bi-caret-down-fill ms-1" aria-hidden="true" />
              </button>
              <div className="tp-holiday-calendar-nav">
                <button
                  type="button"
                  onClick={() => {
                    if (calendarView === "years")
                      setYearPage((current) => current - 10);
                    else if (calendarView === "months") changeYear(year - 1);
                    else moveMonth(-1);
                  }}
                  aria-label="Show previous month"
                  title="Show previous month"
                >
                  <i className="bi bi-arrow-up" />
                </button>
                <button
                  type="button"
                  onClick={() => {
                    if (calendarView === "years")
                      setYearPage((current) => current + 10);
                    else if (calendarView === "months") changeYear(year + 1);
                    else moveMonth(1);
                  }}
                  aria-label="Show next month"
                  title="Show next month"
                >
                  <i className="bi bi-arrow-down" />
                </button>
              </div>
            </div>
            {calendarView === "months" && (
              <div className="tp-holiday-calendar-month-view">
                <button
                  type="button"
                  className="tp-holiday-calendar-year-heading"
                  onClick={() => setCalendarView("years")}
                >
                  {year} <i className="bi bi-caret-down-fill" />
                </button>
                <div className="tp-holiday-calendar-month-grid">
                  {Array.from({ length: 12 }, (_, index) => (
                    <button
                      key={index}
                      type="button"
                      className={index === month ? "is-active" : ""}
                      onClick={() => changeMonth(index)}
                    >
                      {new Date(year, index, 1).toLocaleString("en-US", {
                        month: "short",
                      })}
                    </button>
                  ))}
                </div>
              </div>
            )}
            {calendarView === "years" && (
              <div className="tp-holiday-calendar-year-view">
                <div className="tp-holiday-calendar-year-range">
                  {yearPage} – {yearPage + 9}
                </div>
                <div className="tp-holiday-calendar-year-grid">
                  {Array.from(
                    { length: 10 },
                    (_, index) => yearPage + index,
                  ).map((item) => (
                    <button
                      key={item}
                      type="button"
                      className={item === year ? "is-active" : ""}
                      onClick={() => changeYear(item)}
                    >
                      {item}
                    </button>
                  ))}
                </div>
              </div>
            )}
            {calendarView === "days" && (
              <div className="tp-holiday-calendar-slide-window">
                {monthSlide ? (
                  <div
                    className={`tp-holiday-calendar-slide-track ${slideStarted ? "is-moving" : ""}`}
                    style={{
                      transform:
                        monthSlide.direction > 0
                          ? `translateY(${slideStarted ? "-50%" : "0%"})`
                          : `translateY(${slideStarted ? "0%" : "-50%"})`,
                    }}
                  >
                    <div className="tp-holiday-calendar-slide-panel">
                      {renderMonthGrid(
                        monthSlide.direction > 0
                          ? visibleMonth
                          : monthSlide.next,
                      )}
                    </div>
                    <div className="tp-holiday-calendar-slide-panel">
                      {renderMonthGrid(
                        monthSlide.direction > 0
                          ? monthSlide.next
                          : visibleMonth,
                      )}
                    </div>
                  </div>
                ) : (
                  renderMonthGrid(visibleMonth)
                )}
              </div>
            )}
            <div className="tp-holiday-calendar-footer">
              <button
                type="button"
                className="tp-holiday-calendar-action"
                onClick={() => selectDate("")}
              >
                Clear
              </button>
              <button
                type="button"
                className="tp-holiday-calendar-action"
                onClick={selectToday}
              >
                Today
              </button>
            </div>
            {showHolidays && calendarView === "days" && loadingYear === year && (
              <div className="tp-holiday-calendar-loading">
                Loading holidays...
              </div>
            )}
          </div>,
          document.body,
        )}
    </div>
  );
}
