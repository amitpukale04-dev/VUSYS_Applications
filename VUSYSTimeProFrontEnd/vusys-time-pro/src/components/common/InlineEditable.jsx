import { useEffect, useMemo, useRef, useState } from "react";
import SortableTable from "./SortableTable";
import SvgIcon from "./SvgIcon";
import DirtyStateModal from "./DirtyStateModal";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useLocation } from "react-router-dom";
import { useTabs } from "../../context/TabsContext";
import ToastService from "../../services/toastService";
import { COMMON_MESSAGE } from "../../constants/constants";

const emptyValueForType = (type) => (type === "checkbox" ? false : "");

const resolveInlineColumnDisabled = (column, row, context) => {
  if (!context.isEditing) return true;

  if (typeof column.disabled === "function") {
    return Boolean(column.disabled(row, context));
  }

  return Boolean(column.disabled);
};

export default function InlineEditable({
  value,
  type = "text",
  options = [],
  min,
  max,
  step,
  placeholder = "Click to edit",
  disabled = false,
  editMode = false,
  saveOnBlur = true,
  validate,
  onSave,
  onChange,
  displayValue,
  externalError = "",
}) {
  const { runWithFullPageLoader } = useFullPageLoader();
  const [editing, setEditing] = useState(editMode);
  const [draft, setDraft] = useState(value ?? emptyValueForType(type));
  const [error, setError] = useState("");
  const inputRef = useRef(null);

  const normalizeValue = (current) => {
    if (type === "number" && current !== "") return Number(current);
    if (type === "checkbox") return Boolean(current);
    return current;
  };

  useEffect(() => {
    const nextDraft = value ?? emptyValueForType(type);
    setDraft(nextDraft);
    setEditing(editMode);
    setError(
      editMode && type !== "checkbox"
        ? validate?.(normalizeValue(nextDraft)) || ""
        : "",
    );
  }, [value, type, validate, editMode]);

  useEffect(() => {
    if (!disabled && (editing || editMode) && type !== "checkbox") {
      inputRef.current?.focus();
    }
  }, [disabled, editing, editMode, type]);

  const updateDraft = (nextValue) => {
    const normalized = normalizeValue(nextValue);
    setDraft(nextValue);
    setError(validate?.(normalized) || "");
    onChange?.(normalized);
  };

  const save = async (nextValue = draft) => {
    const normalized = normalizeValue(nextValue);
    const message = validate?.(normalized);
    if (message) {
      setError(message);
      return false;
    }
    if (onSave) {
      await runWithFullPageLoader(
        () => onSave(normalized),
        "Saving, please wait...",
      );
    }
    setError("");
    if (!editMode) setEditing(false);
    return true;
  };

  const cancel = () => {
    setDraft(value ?? emptyValueForType(type));
    setError("");
    if (!editMode) setEditing(false);
  };

  if (type === "checkbox") {
    return (
      <label
        className="tp-inline-checkbox"
        title={disabled ? "Read only" : "Click to update"}
      >
        <input
          type="checkbox"
          checked={Boolean(draft)}
          disabled={disabled}
          onChange={(event) => {
            const checked = event.target.checked;
            setDraft(checked);
            if (editMode) onChange?.(checked);
            else save(checked);
          }}
        />
        <span aria-hidden="true" />
      </label>
    );
  }

  const isEditing = editMode || editing;

  if (!isEditing) {
    const optionLabel =
      type === "select"
        ? options.find((option) => String(option.value) === String(value))
            ?.label
        : undefined;

    /*
     * A select value often arrives from the List API before its dropdown
     * options finish loading. Never flash the raw database ID (for example
     * CountryID = 1) while waiting for the display text.
     */
    const isSelectTextLoading =
      type === "select" &&
      value !== null &&
      value !== undefined &&
      value !== "" &&
      !optionLabel &&
      options.length === 0;

    const shownValue = isSelectTextLoading
      ? ""
      : (displayValue ?? optionLabel ?? value);

    return (
      <div className="tp-inline-display-wrap">
        <button
          type="button"
          className="tp-inline-display"
          disabled={disabled}
          onClick={() => !disabled && setEditing(true)}
        >
          {isSelectTextLoading ? (
            <span
              className="tp-inline-select-loading"
              aria-label="Loading option text"
            >
              <span
                className="tp-inline-select-loading-bar"
                aria-hidden="true"
              />
            </span>
          ) : shownValue !== null &&
            shownValue !== undefined &&
            shownValue !== "" ? (
            shownValue
          ) : (
            <span>{placeholder}</span>
          )}
          {!disabled && <i className="bi bi-pencil-square" />}
        </button>
      </div>
    );
  }

  const displayedError = externalError || error;

  return (
    <div className={`tp-inline-editor ${displayedError ? "is-invalid" : ""}`}>
      {type === "select" ? (
        <select
          ref={inputRef}
          value={draft}
          disabled={disabled}
          className={draft === "" ? "is-placeholder" : ""}
          onChange={(event) => {
            updateDraft(event.target.value);
            if (!editMode) save(event.target.value);
          }}
          onBlur={() => !editMode && saveOnBlur && save()}
          onKeyDown={(event) => {
            if (event.key === "Escape") cancel();
          }}
        >
          <option value="">Select</option>
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      ) : type === "textarea" ? (
        <textarea
          ref={inputRef}
          rows={3}
          value={draft}
          disabled={disabled}
          onChange={(event) => updateDraft(event.target.value)}
          onBlur={() => !editMode && saveOnBlur && save()}
          onKeyDown={(event) => {
            if (event.key === "Escape") cancel();
          }}
        />
      ) : (
        <input
          ref={inputRef}
          type={type}
          min={min}
          disabled={disabled}
          max={max}
          step={step}
          value={draft}
          onChange={(event) => updateDraft(event.target.value)}
          onBlur={() => !editMode && saveOnBlur && save()}
          onKeyDown={(event) => {
            if (!editMode && event.key === "Enter") save();
            if (event.key === "Escape") cancel();
          }}
        />
      )}
      {displayedError && (
        <small className="tp-inline-error">{displayedError}</small>
      )}
    </div>
  );
}

export function InlineEditableTable({
  columns = [],
  data = [],
  keyField = "id",
  onDataChange,
  enablePagination = true,
  enableSorting = true,
  enableGlobalSearch = true,
  globalSearchPlaceholder = "Search all columns...",
  enableColumnFilters = true,
  stickyHeader = true,
  freezeColumns = 0,
  pageSizeOptions = null,
  defaultPageSize = 30,
  maxHeight = 430,
  minTableHeight = 235,
  showActions = false,
  actionColumnWidth = "112px",
  showRowMore = false,
  showEditAction = true,
  showDeleteAction = true,
  onView,
  onEdit,
  onDelete,
  onRowSave,
  onRowCancel,
  rowValidationErrors = {},
  onCellChange,
  newRowFactory,
  toolbarExtra = null,
  showAddAction = false,
  addActionLabel = "Add",
  addActionIconOnly = false,
  onAdd,
  showRefreshAction = false,
  onRefresh,
  refreshing = false,
  loading = false,
  loadingText = "Loading records...",
  serverSide = false,
  totalRecords = 0,
  initialPageNumber = 1,
  searchDebounceMs = 350,
  onQueryChange,
  onPageChange,
  onPageSizeChange,
  onSortChange,
  onSearchChange,
  onFilterChange,
}) {
  const { runWithFullPageLoader } = useFullPageLoader();
  const { registerTabDirtyState } = useTabs();
  const location = useLocation();
  const [rows, setRows] = useState(data);
  const [editingRowKey, setEditingRowKey] = useState(null);
  const [newRowKey, setNewRowKey] = useState(null);
  const [dirtyOpen, setDirtyOpen] = useState(false);
  const originalRowRef = useRef(null);
  const pendingActionRef = useRef(null);

  useEffect(() => setRows(data), [data]);

  // A refresh/loading cycle must never leave a stale row in edit mode.
  useEffect(() => {
    if (!loading) return;
    setEditingRowKey(null);
    setNewRowKey(null);
    originalRowRef.current = null;
    pendingActionRef.current = null;
    setDirtyOpen(false);
  }, [loading]);

  const updateRows = (updatedRows, metadata) => {
    setRows(updatedRows);
    onDataChange?.(updatedRows, metadata);
  };

  const updateCell = (row, column, nextValue) => {
    const updatedRows = rows.map((item) =>
      item[keyField] === row[keyField]
        ? { ...item, [column.key]: nextValue }
        : item,
    );
    updateRows(updatedRows, { row, column, value: nextValue, isDraft: true });
    onCellChange?.(row, column.key, nextValue);
  };

  const validateRow = (row) =>
    columns.every((column) => {
      if (column.editable === false || !column.validate) return true;
      return !column.validate(row[column.key], row);
    });

  const activeRow =
    editingRowKey === null
      ? null
      : rows.find((item) => item[keyField] === editingRowKey) || null;

  const isDirty = useMemo(() => {
    if (!activeRow) return false;
    if (activeRow[keyField] === newRowKey) return true;
    if (!originalRowRef.current) return false;
    return JSON.stringify(activeRow) !== JSON.stringify(originalRowRef.current);
  }, [activeRow, keyField, newRowKey]);

  useEffect(() => {
    registerTabDirtyState?.(location.pathname, isDirty);

    return () => {
      registerTabDirtyState?.(location.pathname, false);
    };
  }, [isDirty, location.pathname, registerTabDirtyState]);

  const beginEdit = (row) => {
    if (editingRowKey !== null) return;
    originalRowRef.current = { ...row };
    setEditingRowKey(row[keyField]);
    setNewRowKey(null);
    onEdit?.(row);
  };

  const saveRow = async (row) => {
    const latestRow =
      rows.find((item) => item[keyField] === row[keyField]) || row;

    const isNewRow = latestRow[keyField] === newRowKey;

    /*
     * Existing InlineEditable rows must not call their save API unless
     * at least one editable value has changed. New rows still save normally.
     */
    if (!isNewRow && !isDirty) {
      ToastService.error(COMMON_MESSAGE.NO_CHANGES_MADE_TO_FORM);
      return false;
    }

    if (!validateRow(latestRow)) {
      setEditingRowKey(latestRow[keyField]);
      return false;
    }

    if (onRowSave) {
      const saved = await runWithFullPageLoader(
        () =>
          onRowSave(latestRow, {
            isNew: latestRow[keyField] === newRowKey,
          }),
        "Saving, please wait...",
      );
      // A failed API validation keeps the draft row open for correction.
      if (saved === false) return false;
    }
    originalRowRef.current = null;
    setEditingRowKey(null);
    setNewRowKey(null);
    return true;
  };

  const performCancel = () => {
    const currentKey = editingRowKey;
    if (currentKey === null) return;

    let updatedRows = rows;
    const currentRow = rows.find((item) => item[keyField] === currentKey);

    if (currentKey === newRowKey) {
      updatedRows = rows.filter((item) => item[keyField] !== currentKey);
    } else if (originalRowRef.current) {
      updatedRows = rows.map((item) =>
        item[keyField] === currentKey ? { ...originalRowRef.current } : item,
      );
    }

    updateRows(updatedRows, {
      row: currentRow,
      cancelled: true,
      isNew: currentKey === newRowKey,
    });
    onRowCancel?.(currentRow, { isNew: currentKey === newRowKey });
    originalRowRef.current = null;
    setEditingRowKey(null);
    setNewRowKey(null);
  };

  const requestCancel = () => {
    if (!isDirty) {
      performCancel();
      return;
    }
    pendingActionRef.current = { type: "cancel" };
    setDirtyOpen(true);
  };

  const handleAddAction = async () => {
    if (newRowKey !== null) {
      const row = rows.find((item) => item[keyField] === newRowKey);
      if (row) await saveRow(row);
      return;
    }

    if (editingRowKey !== null) return;

    const createdRow = newRowFactory?.(rows) ?? (await onAdd?.());
    if (!createdRow) return;

    const updatedRows = [createdRow, ...rows];
    updateRows(updatedRows, { row: createdRow, isNew: true });
    originalRowRef.current = null;
    setNewRowKey(createdRow[keyField]);
    setEditingRowKey(createdRow[keyField]);
  };

  const executeRefresh = async () => {
    // Reset edit/add state before re-binding data so the toolbar always returns to Add.
    setEditingRowKey(null);
    setNewRowKey(null);
    originalRowRef.current = null;
    await runWithFullPageLoader(() => onRefresh?.(), "Refreshing records...");
  };

  const handleRefresh = () => {
    if (editingRowKey !== null && isDirty) {
      pendingActionRef.current = { type: "refresh" };
      setDirtyOpen(true);
      return;
    }
    if (editingRowKey !== null) performCancel();
    executeRefresh();
  };

  const discardPendingChanges = async () => {
    const pending = pendingActionRef.current;
    setDirtyOpen(false);
    pendingActionRef.current = null;
    performCancel();
    if (pending?.type === "refresh") await executeRefresh();
  };

  // ==========================================================
  // EDITABLE COLUMNS
  // ==========================================================

  const editableColumns = useMemo(
    () =>
      columns.map((column) => ({
        ...column,
        render: (cellValue, row) => {
          if (column.editable === false)
            return column.render ? column.render(cellValue, row) : cellValue;

          const rowIsEditing = editingRowKey === row[keyField];
          const controlDisabled = resolveInlineColumnDisabled(column, row, {
            isEditing: rowIsEditing,
            isNew: row[keyField] === newRowKey,
            editingRowKey,
            newRowKey,
          });

          return (
            <InlineEditable
              value={cellValue}
              displayValue={
                typeof column.displayValue === "function"
                  ? column.displayValue(cellValue, row)
                  : column.displayValue
              }
              type={column.inputType || "text"}
              options={column.options || []}
              min={column.min}
              max={column.max}
              step={column.step}
              disabled={controlDisabled}
              editMode={rowIsEditing}
              validate={(nextValue) => column.validate?.(nextValue, row)}
              externalError={
                rowIsEditing ? rowValidationErrors[column.key] || "" : ""
              }
              onChange={(nextValue) => updateCell(row, column, nextValue)}
            />
          );
        },
      })),
    [columns, editingRowKey, keyField, newRowKey, rows, rowValidationErrors],
  );

  const renderActions = (row, openMoreMenu) => {
    const rowIsEditing = editingRowKey === row[keyField];
    return (
      <div className="tp-row-actions tp-inline-row-actions">
        {(rowIsEditing || showEditAction) && (
          <button
            type="button"
            className={rowIsEditing ? "tp-row-save" : "tp-row-edit"}
            title={rowIsEditing ? "Save row" : "Edit row"}
            aria-label={rowIsEditing ? "Save row" : "Edit row"}
            onClick={() => (rowIsEditing ? saveRow(row) : beginEdit(row))}
            disabled={editingRowKey !== null && !rowIsEditing}
          >
            <SvgIcon name={rowIsEditing ? "save" : "edit"} size={18} />
          </button>
        )}

        {rowIsEditing ? (
          <button
            type="button"
            className="tp-row-cancel"
            title="Cancel changes"
            aria-label="Cancel changes"
            onClick={requestCancel}
          >
            <SvgIcon name="close" size={18} strokeColor="#EF4444" />
          </button>
        ) : (
          showDeleteAction && (
            <button
              type="button"
              className="tp-row-delete"
              title="Delete row"
              aria-label="Delete row"
              onClick={() => onDelete?.(row)}
              disabled={editingRowKey !== null}
            >
              <SvgIcon name="delete" size={18} strokeColor="#EF4444" />
            </button>
          )
        )}

        {showRowMore && (
          <button
            type="button"
            className="tp-row-more"
            title="More actions"
            aria-label="More actions"
            onClick={openMoreMenu}
            disabled={editingRowKey !== null}
          >
            <SvgIcon name="more" size={19} />
          </button>
        )}
      </div>
    );
  };

  return (
    <>
      <SortableTable
        columns={editableColumns}
        data={rows}
        keyField={keyField}
        enablePagination={enablePagination}
        enableSorting={enableSorting}
        enableGlobalSearch={enableGlobalSearch}
        globalSearchPlaceholder={globalSearchPlaceholder}
        enableColumnFilters={enableColumnFilters}
        stickyHeader={stickyHeader}
        freezeColumns={freezeColumns}
        pageSizeOptions={pageSizeOptions}
        defaultPageSize={defaultPageSize}
        maxHeight={maxHeight}
        minTableHeight={minTableHeight}
        showActions={showActions}
        actionColumnWidth={actionColumnWidth}
        showRowMore={showRowMore}
        showEditAction={showEditAction}
        showDeleteAction={showDeleteAction}
        renderActions={renderActions}
        onView={onView}
        onEdit={onEdit}
        onDelete={onDelete}
        toolbarExtra={toolbarExtra}
        showAddAction={showAddAction}
        addActionLabel={newRowKey !== null ? "Save" : addActionLabel}
        addActionIconOnly={addActionIconOnly}
        addActionIconName={newRowKey !== null ? "save" : "add"}
        addActionDisabled={editingRowKey !== null && newRowKey === null}
        onAdd={handleAddAction}
        showRefreshAction={showRefreshAction}
        onRefresh={handleRefresh}
        refreshing={refreshing}
        loading={loading}
        loadingText={loadingText}
        serverSide={serverSide}
        totalRecords={totalRecords}
        initialPageNumber={initialPageNumber}
        searchDebounceMs={searchDebounceMs}
        onQueryChange={onQueryChange}
        onPageChange={onPageChange}
        onPageSizeChange={onPageSizeChange}
        onSortChange={onSortChange}
        onSearchChange={onSearchChange}
        onFilterChange={onFilterChange}
      />

      <DirtyStateModal
        open={dirtyOpen}
        onStay={() => {
          setDirtyOpen(false);
          pendingActionRef.current = null;
        }}
        onDiscard={discardPendingChanges}
        message="You have unsaved row changes. Do you want to discard them?"
        discardText="Yes, Discard"
      />
    </>
  );
}
