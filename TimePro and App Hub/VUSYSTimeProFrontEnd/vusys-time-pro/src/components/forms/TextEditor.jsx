import { useEffect, useMemo, useRef } from "react";
import FormLabel from "./FormLabel";

const createChangeEvent = (name, value) => ({
  target: {
    name,
    value,
  },
});

export default function TextEditor({
  id,
  name,
  label,
  value = "",
  onChange,
  onBlur,
  error,
  required = false,
  placeholder = "",
  disabled = false,
  readOnly = false,
  minHeight = 180,
  maxHeight,
  className = "",
  toolbar = true,
  showBold = true,
  showItalic = true,
  showUnderline = true,
  showBulletedList = true,
  showNumberedList = true,
  showAlignLeft = true,
  showAlignCenter = true,
  showAlignRight = true,
  showUndo = true,
  showRedo = true,
  showClearFormatting = true,
  defaultListType = "",
}) {
  const editorRef = useRef(null);
  const inputId = id || name;
  const isReadOnly = disabled || readOnly;

  const toolbarItems = useMemo(() => {
    const groups = [
      [
        ...(showBold
          ? [{ command: "bold", icon: "bi-type-bold", title: "Bold" }]
          : []),
        ...(showItalic
          ? [{ command: "italic", icon: "bi-type-italic", title: "Italic" }]
          : []),
        ...(showUnderline
          ? [
              {
                command: "underline",
                icon: "bi-type-underline",
                title: "Underline",
              },
            ]
          : []),
      ],
      [
        ...(showBulletedList
          ? [
              {
                command: "insertUnorderedList",
                icon: "bi-list-ul",
                title: "Bulleted List",
              },
            ]
          : []),
        ...(showNumberedList
          ? [
              {
                command: "insertOrderedList",
                icon: "bi-list-ol",
                title: "Numbered List",
              },
            ]
          : []),
      ],
      [
        ...(showAlignLeft
          ? [
              {
                command: "justifyLeft",
                icon: "bi-text-left",
                title: "Align Left",
              },
            ]
          : []),
        ...(showAlignCenter
          ? [
              {
                command: "justifyCenter",
                icon: "bi-text-center",
                title: "Align Center",
              },
            ]
          : []),
        ...(showAlignRight
          ? [
              {
                command: "justifyRight",
                icon: "bi-text-right",
                title: "Align Right",
              },
            ]
          : []),
      ],
      [
        ...(showUndo
          ? [
              {
                command: "undo",
                icon: "bi-arrow-counterclockwise",
                title: "Undo",
              },
            ]
          : []),
        ...(showRedo
          ? [
              {
                command: "redo",
                icon: "bi-arrow-clockwise",
                title: "Redo",
              },
            ]
          : []),
        ...(showClearFormatting
          ? [
              {
                command: "removeFormat",
                icon: "bi-eraser",
                title: "Clear Formatting",
              },
            ]
          : []),
      ],
    ].filter((group) => group.length > 0);

    return groups.flatMap((group, groupIndex) =>
      groupIndex === 0
        ? group
        : [{ type: "separator", key: `separator-${groupIndex}` }, ...group],
    );
  }, [
    showAlignCenter,
    showAlignLeft,
    showAlignRight,
    showBold,
    showBulletedList,
    showClearFormatting,
    showItalic,
    showNumberedList,
    showRedo,
    showUnderline,
    showUndo,
  ]);

  // ============================================================
  // SYNC EXTERNAL VALUE
  // ============================================================

  useEffect(() => {
    const editor = editorRef.current;

    if (!editor) {
      return;
    }

    const nextValue = String(value ?? "");

    if (editor.innerHTML !== nextValue) {
      editor.innerHTML = nextValue;
    }
  }, [value]);

  // ============================================================
  // APPLY DEFAULT LIST STYLE FOR A NEW/EMPTY EDITOR
  // ============================================================

  useEffect(() => {
    const editor = editorRef.current;

    if (!editor || isReadOnly || String(value ?? "").trim()) {
      return;
    }

    const listType = String(defaultListType || "").toLowerCase();

    if (listType !== "ordered" && listType !== "unordered") {
      return;
    }

    // Show the requested list marker without changing the controlled value.
    // Until the user types actual text, the field still remains empty for
    // required-field validation.
    editor.innerHTML =
      listType === "ordered"
        ? "<ol><li><br></li></ol>"
        : "<ul><li><br></li></ul>";
  }, [defaultListType, isReadOnly, value]);

  // ============================================================
  // EMIT VALUE
  // ============================================================

  const emitValue = () => {
    const editor = editorRef.current;

    if (!editor) {
      return;
    }

    const html = editor.innerHTML;
    const plainText = editor.textContent?.trim() || "";
    const normalizedValue = plainText ? html : "";

    onChange?.(createChangeEvent(name, normalizedValue));
  };

  // ============================================================
  // EXECUTE EDITOR COMMAND
  // ============================================================

  const executeCommand = (command) => {
    if (isReadOnly) {
      return;
    }

    const editor = editorRef.current;

    if (!editor) {
      return;
    }

    editor.focus();
    document.execCommand(command, false);
    emitValue();
  };

  // ============================================================
  // HANDLE INPUT / BLUR
  // ============================================================

  const handleInput = () => {
    if (isReadOnly) {
      return;
    }

    emitValue();
  };

  const handleBlur = () => {
    emitValue();

    onBlur?.(
      createChangeEvent(name, editorRef.current?.innerHTML || ""),
    );
  };

  // ============================================================
  // UI
  // ============================================================

  return (
    <div className={`tp-form-field tp-text-editor-field ${className}`.trim()}>
      {label && (
        <FormLabel htmlFor={inputId} required={required}>
          {label}
        </FormLabel>
      )}

      <div
        className={`tp-text-editor-shell ${error ? "is-invalid" : ""} ${
          isReadOnly ? "is-readonly" : ""
        }`}
      >
        {toolbar && !isReadOnly && (
          <div
            className="tp-editor-toolbar"
            role="toolbar"
            aria-label={`${label || name || "Text"} editor toolbar`}
          >
            {toolbarItems.map((item, index) =>
              item.type === "separator" ? (
                <span
                  key={`separator-${index}`}
                  className="tp-editor-toolbar-separator"
                  aria-hidden="true"
                />
              ) : (
                <button
                  key={item.command}
                  type="button"
                  onMouseDown={(event) => {
                    event.preventDefault();
                    executeCommand(item.command);
                  }}
                  title={item.title}
                  aria-label={item.title}
                >
                  <i className={`bi ${item.icon}`} aria-hidden="true" />
                </button>
              ),
            )}
          </div>
        )}

        <div
          ref={editorRef}
          id={inputId}
          role="textbox"
          aria-multiline="true"
          aria-required={required}
          aria-invalid={Boolean(error)}
          aria-describedby={error ? `${inputId}-error` : undefined}
          contentEditable={!isReadOnly}
          suppressContentEditableWarning
          data-placeholder={placeholder}
          className="tp-text-editor"
          style={{
            minHeight,
            maxHeight: maxHeight || undefined,
          }}
          onInput={handleInput}
          onBlur={handleBlur}
        />
      </div>

      {error && (
        <div id={`${inputId}-error`} className="tp-field-error" role="alert">
          {typeof error === "string" ? error : ""}
        </div>
      )}
    </div>
  );
}
