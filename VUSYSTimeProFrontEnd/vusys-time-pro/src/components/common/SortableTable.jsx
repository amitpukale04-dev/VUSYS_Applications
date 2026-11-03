import { createPortal } from "react-dom";
import {
  useCallback,
  useDeferredValue,
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import SvgIcon from "./SvgIcon";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";

const asSearchText = (value) => {
  if (value === null || value === undefined) return "";
  if (typeof value === "boolean")
    return value ? "yes active true" : "no inactive false";
  return String(value).toLowerCase();
};

const normalizeOptionValue = (option) =>
  typeof option === "object" ? option.value : option;
const normalizeOptionLabel = (option) =>
  typeof option === "object" ? option.label : option;
const ALL_RECORDS = "all";

const createAutomaticPageSizeOptions = (defaultSize, totalRecords) => {
  const baseSize = Math.max(1, Number(defaultSize) || 30);
  const normalizedTotal = Math.max(0, Number(totalRecords) || 0);
  const options = [];

  for (let multiplier = 1; multiplier <= 5; multiplier += 1) {
    const size = baseSize * multiplier;
    if (multiplier === 1 || normalizedTotal === 0 || size < normalizedTotal) {
      options.push(size);
    }
  }

  return [...new Set(options)];
};

function RowActionMenu({
  anchorRect,
  row,
  onClose,
  onEdit,
  onDelete,
  showEditAction = true,
  showDeleteAction = true,
}) {
  useEffect(() => {
    const close = () => onClose();
    window.addEventListener("resize", close);
    window.addEventListener("scroll", close, true);
    return () => {
      window.removeEventListener("resize", close);
      window.removeEventListener("scroll", close, true);
    };
  }, [onClose]);

  if (!anchorRect) return null;

  const style = {
    position: "fixed",
    top: Math.min(anchorRect.bottom + 7, window.innerHeight - 112),
    left: Math.min(Math.max(12, anchorRect.left - 48), window.innerWidth - 150),
    zIndex: 4000,
  };

  return createPortal(
    <>
      <button
        type="button"
        className="tp-action-menu-backdrop"
        aria-label="Close action menu"
        onClick={onClose}
      />
      <div className="tp-row-action-menu" style={style} role="menu">
        {showEditAction && (
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              onEdit?.(row);
              onClose();
            }}
          >
            <SvgIcon name="edit" size={16} />
            <span>Edit</span>
          </button>
        )}
        {showDeleteAction && (
          <button
            type="button"
            role="menuitem"
            className="danger"
            onClick={() => {
              onDelete?.(row);
              onClose();
            }}
          >
            <SvgIcon name="delete" size={16} />
            <span>Delete</span>
          </button>
        )}
      </div>
    </>,
    document.body,
  );
}

function FilterPopover({
  anchorRect,
  anchorElement,
  column,
  value,
  onChange,
  onClear,
  onClose,
}) {
  const popoverRef = useRef(null);
  const [positionRect, setPositionRect] = useState(anchorRect);

  useLayoutEffect(() => {
    setPositionRect(anchorElement?.getBoundingClientRect() || anchorRect);
  }, [anchorElement, anchorRect]);

  useEffect(() => {
    const updatePosition = () => {
      if (!anchorElement?.isConnected) return;
      const nextRect = anchorElement.getBoundingClientRect();
      setPositionRect((current) =>
        current &&
        current.left === nextRect.left &&
        current.top === nextRect.top &&
        current.right === nextRect.right &&
        current.bottom === nextRect.bottom
          ? current
          : nextRect,
      );
    };

    // Keep the popup open and anchored to its column while the page or grid scrolls.
    window.addEventListener("scroll", updatePosition, true);
    return () => window.removeEventListener("scroll", updatePosition, true);
  }, [anchorElement]);

  useEffect(() => {
    const handlePointerDown = (event) => {
      if (!popoverRef.current?.contains(event.target)) onClose();
    };

    /*
     * On mobile, focusing the filter input opens the software keyboard.
     * That changes window.visualViewport (and in some browsers also fires a
     * normal resize event). Closing the popup on those events caused the
     * filter to appear and immediately disappear.
     *
     * Keep resize-close behavior only for desktop. On phones/tablets the
     * popup stays open while the keyboard/visual viewport changes and closes
     * only when the user taps outside it or explicitly presses Close.
     */
    const handleDesktopViewportChange = () => {
      const viewportWidth = window.visualViewport?.width || window.innerWidth;

      if (viewportWidth > 767.98) {
        onClose();
      }
    };

    document.addEventListener("mousedown", handlePointerDown);
    window.addEventListener("resize", handleDesktopViewportChange);

    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      window.removeEventListener("resize", handleDesktopViewportChange);
    };
  }, [onClose]);

  if (!positionRect || !column) return null;

  const viewportWidth = window.visualViewport?.width || window.innerWidth;
  const viewportHeight = window.visualViewport?.height || window.innerHeight;
  const isMobileViewport = viewportWidth <= 767.98;

  const desktopWidth = 230;
  const mobileWidth = Math.min(300, Math.max(240, viewportWidth - 28));
  const width = isMobileViewport ? mobileWidth : desktopWidth;

  const estimatedHeight =
    column.filterType === "select" || column.filterType === "checkbox"
      ? 98
      : 102;

  const preferredLeft = positionRect.right + 10;
  const fallbackLeft = positionRect.left - width - 10;

  const left = isMobileViewport
    ? Math.max(14, (viewportWidth - width) / 2)
    : preferredLeft + width <= viewportWidth - 12
      ? preferredLeft
      : Math.max(12, fallbackLeft);

  const top = isMobileViewport
    ? Math.max(14, (viewportHeight - estimatedHeight) / 2)
    : Math.min(
        Math.max(12, positionRect.top - 8),
        Math.max(12, viewportHeight - estimatedHeight - 12),
      );

  const options = column.filterOptions || column.options || [];

  return createPortal(
    <div
      ref={popoverRef}
      className="tp-column-filter-popover tp-column-filter-popover-fixed"
      style={{ position: "fixed", left, top, width, zIndex: 100100 }}
      onMouseDown={(event) => event.stopPropagation()}
    >
      <div className="tp-column-filter-title">
        <span>Filter {column.label}</span>
        <button type="button" onClick={onClose} aria-label="Close filter">
          <SvgIcon name="close" size={13} />
        </button>
      </div>
      <div className="tp-filter-input-wrap">
        {column.filterType === "select" || column.filterType === "checkbox" ? (
          <select
            value={value}
            onChange={(event) => onChange(event.target.value)}
            autoFocus
          >
            <option value="">All</option>
            {(column.filterType === "checkbox"
              ? [
                  { value: "true", label: "Yes" },
                  { value: "false", label: "No" },
                ]
              : options
            ).map((option) => (
              <option
                key={normalizeOptionValue(option)}
                value={normalizeOptionValue(option)}
              >
                {normalizeOptionLabel(option)}
              </option>
            ))}
          </select>
        ) : (
          <input
            value={value}
            placeholder={`Filter ${column.label}`}
            onChange={(event) => onChange(event.target.value)}
            autoFocus
          />
        )}
        {value !== "" && (
          <button
            type="button"
            className="tp-filter-clear"
            onClick={onClear}
            title="Clear filter"
            aria-label={`Clear ${column.label} filter`}
          >
            <SvgIcon name="close" size={13} />
          </button>
        )}
      </div>
    </div>,
    document.body,
  );
}

export default function SortableTable({
  columns = [],
  data = [],
  keyField = "id",
  emptyText = "No records found",
  enablePagination = true,
  EnableLargeDataSet = false,
  enableSorting = true,
  enableGlobalSearch = true,
  enableColumnFilters = true,
  stickyHeader = true,
  freezeColumns = 0,
  responsiveCards = false,
  pageSizeOptions = null,
  defaultPageSize = 30,
  maxHeight = 430,
  minTableHeight = 235,
  globalSearchPlaceholder = "Search all columns...",
  showActions = false,
  actionColumnWidth = "112px",
  showRowMore = true,
  renderActions,
  onView,
  onEdit,
  onDelete,
  toolbarExtra = null,
  toolbarExtraPlacement = "start",
  showViewAction = true,
  showEditAction = true,
  showDeleteAction = true,
  showAddAction = false,
  addActionLabel = "Add",
  addActionIconOnly = false,
  addActionIconName = "add",
  addActionDisabled = false,
  onAdd,
  showRefreshAction = false,
  onRefresh,
  refreshTrigger,
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
  const { runWithFullPageLoader, showFullPageLoader, hideFullPageLoader } =
    useFullPageLoader();
  const [sort, setSort] = useState({ key: "", direction: "asc" });
  const [globalSearch, setGlobalSearch] = useState("");
  const deferredGlobalSearch = useDeferredValue(globalSearch);
  const [largeSearchResult, setLargeSearchResult] = useState(null);
  const largeSearchCache = useRef(new WeakMap());
  const [filters, setFilters] = useState({});
  const [filterState, setFilterState] = useState({ key: "", rect: null });
  const [page, setPage] = useState(initialPageNumber);
  const [pageInput, setPageInput] = useState(String(initialPageNumber));
  const [pageSize, setPageSize] = useState(defaultPageSize);
  const [actionState, setActionState] = useState({ row: null, rect: null });
  const initialServerRequestSkipped = useRef(false);

  // Opt-in virtual rendering. With the default flag disabled the original
  // SortableTable rendering, paging, and scrolling behavior is unchanged.
  const tableScrollRef = useRef(null);
  const [virtualScrollTop, setVirtualScrollTop] = useState(0);
  const [virtualViewportHeight, setVirtualViewportHeight] = useState(430);
  const VIRTUAL_ROW_HEIGHT = 44;
  const VIRTUAL_OVERSCAN = 8;

  // Keep navigation / initial grid loading consistent with Save and Refresh.
  // Any page that renders SortableTable (including InlineEditableTable, which
  // delegates to SortableTable) automatically gets the common full-page
  // loader while its `loading` prop is true.
  useLayoutEffect(() => {
    if (!loading) return undefined;

    const loaderRequestId = showFullPageLoader(
      refreshing ? "Refreshing records..." : loadingText,
    );

    return () => {
      hideFullPageLoader(loaderRequestId);
    };
  }, [
    hideFullPageLoader,
    loading,
    loadingText,
    refreshing,
    showFullPageLoader,
  ]);

  const actionColumn = useMemo(
    () =>
      showActions
        ? [
            {
              key: "__actions",
              label: "Action",
              width: actionColumnWidth || "112px",
              sortable: false,
              filterable: false,
            },
          ]
        : [],
    [actionColumnWidth, showActions],
  );
  const visibleColumns = useMemo(
    () => [
      ...actionColumn,
      ...columns.filter((column) => column.hidden !== true),
    ],
    [actionColumn, columns],
  );
  const dataColumns = useMemo(
    () => visibleColumns.filter((column) => column.key !== "__actions"),
    [visibleColumns],
  );

  const frozenOffsets = useMemo(() => {
    let offset = 0;
    return visibleColumns.map((column, index) => {
      const current = offset;
      if (index < freezeColumns)
        offset += Number.parseInt(column.width, 10) || 140;
      return current;
    });
  }, [freezeColumns, visibleColumns]);

  // Reset to the first page only when the user changes the grid query.
  // Do not reset when `data` changes because inline editing (for example,
  // toggling Active/Default) updates the row collection and must preserve
  // the user's current page.
  useEffect(() => {
    if (!serverSide) setPage(1);
  }, [globalSearch, filters, pageSize, serverSide]);

  // Large client-side searches run in small batches, keeping the input responsive.
  // When disabled, retain the original synchronous SortableTable behavior.
  useEffect(() => {
    largeSearchCache.current = new WeakMap();
  }, [data, dataColumns]);

  const useBatchedSearch =
    EnableLargeDataSet &&
    !serverSide &&
    data.length > 200 &&
    deferredGlobalSearch.trim() !== "";

  useEffect(() => {
    if (!useBatchedSearch) return undefined;

    let cancelled = false;
    let timer;
    let position = 0;
    const matches = [];
    const keyword = deferredGlobalSearch.trim().toLowerCase();
    const activeFilters = dataColumns.filter(
      (column) =>
        filters[column.key] !== undefined && filters[column.key] !== "",
    );
    const cache = largeSearchCache.current;
    const processBatch = () => {
      const end = Math.min(position + 200, data.length);
      for (; position < end; position += 1) {
        const row = data[position];
        let text = cache.get(row);
        if (text === undefined) {
          text = dataColumns
            .map((column) => asSearchText(row[column.key]))
            .join("\u0001");
          cache.set(row, text);
        }
        if (!text.includes(keyword)) continue;
        if (
          !activeFilters.every((column) => {
            const filterValue = filters[column.key];
            const rowValue = row[column.key];
            if (column.filterType === "select")
              return String(rowValue) === String(filterValue);
            if (column.filterType === "checkbox")
              return Boolean(rowValue) === (filterValue === "true");
            return asSearchText(rowValue).includes(
              String(filterValue).toLowerCase(),
            );
          })
        )
          continue;
        matches.push(row);
      }
      if (cancelled) return;
      if (position < data.length) {
        timer = window.setTimeout(processBatch, 0);
      } else {
        setLargeSearchResult({
          rows: matches,
          keyword: deferredGlobalSearch,
          data,
          dataColumns,
          filters,
        });
      }
    };

    timer = window.setTimeout(processBatch, 0);
    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [data, dataColumns, deferredGlobalSearch, filters, useBatchedSearch]);

  const isLargeSearchPending =
    useBatchedSearch &&
    (deferredGlobalSearch !== globalSearch ||
      largeSearchResult?.keyword !== deferredGlobalSearch ||
      largeSearchResult?.data !== data ||
      largeSearchResult?.dataColumns !== dataColumns ||
      largeSearchResult?.filters !== filters);

  const processedRows = useMemo(() => {
    if (serverSide) return data;

    const search = (EnableLargeDataSet ? deferredGlobalSearch : globalSearch)
      .trim()
      .toLowerCase();
    let result = useBatchedSearch
      ? isLargeSearchPending
        ? []
        : largeSearchResult.rows
      : data.filter((row) => {
          const globalMatch =
            !search ||
            dataColumns.some((column) =>
              asSearchText(row[column.key]).includes(search),
            );
          if (!globalMatch) return false;
          return dataColumns.every((column) => {
            const filterValue = filters[column.key];
            if (filterValue === undefined || filterValue === "") return true;
            const rowValue = row[column.key];
            if (column.filterType === "select")
              return String(rowValue) === String(filterValue);
            if (column.filterType === "checkbox")
              return Boolean(rowValue) === (filterValue === "true");
            return asSearchText(rowValue).includes(
              String(filterValue).toLowerCase(),
            );
          });
        });

    if (enableSorting && sort.key) {
      result = [...result].sort((a, b) => {
        const left = a[sort.key] ?? "";
        const right = b[sort.key] ?? "";
        const comparison =
          typeof left === "number" && typeof right === "number"
            ? left - right
            : String(left).localeCompare(String(right), undefined, {
                numeric: true,
                sensitivity: "base",
              });
        return sort.direction === "asc" ? comparison : -comparison;
      });
    }
    return result;
  }, [
    data,
    dataColumns,
    deferredGlobalSearch,
    EnableLargeDataSet,
    largeSearchResult,
    isLargeSearchPending,
    useBatchedSearch,
    enableSorting,
    filters,
    globalSearch,
    serverSide,
    sort,
  ]);

  const effectiveTotalRecords = serverSide
    ? Math.max(0, Number(totalRecords) || 0)
    : processedRows.length;
  const isAllRecords = pageSize === ALL_RECORDS;
  const numericPageSize = isAllRecords
    ? Math.max(1, effectiveTotalRecords || processedRows.length || data.length)
    : Math.max(1, Number(pageSize) || 30);
  const totalPages =
    enablePagination && !isAllRecords
      ? Math.max(1, Math.ceil(effectiveTotalRecords / numericPageSize))
      : 1;
  const currentPage = Math.min(page, totalPages);
  const displayedRows = serverSide
    ? processedRows
    : enablePagination && !isAllRecords
      ? processedRows.slice(
          (currentPage - 1) * numericPageSize,
          currentPage * numericPageSize,
        )
      : processedRows;
  const useVirtualRows =
    EnableLargeDataSet &&
    isAllRecords &&
    !responsiveCards &&
    displayedRows.length > 200;

  // Measure the actual scroll container so the visible window follows its size.
  useEffect(() => {
    if (!useVirtualRows) return undefined;
    const element = tableScrollRef.current;
    if (!element) return undefined;
    const measure = () => setVirtualViewportHeight(element.clientHeight || 430);
    measure();
    const observer =
      typeof ResizeObserver !== "undefined"
        ? new ResizeObserver(measure)
        : null;
    observer?.observe(element);
    window.addEventListener("resize", measure);
    return () => {
      observer?.disconnect();
      window.removeEventListener("resize", measure);
    };
  }, [useVirtualRows]);

  // The browser scroll height is based on all rows, but only a small window
  // is mounted in the DOM. All records remain available for search and sort.
  const virtualStart = useVirtualRows
    ? Math.max(
        0,
        Math.floor(virtualScrollTop / VIRTUAL_ROW_HEIGHT) - VIRTUAL_OVERSCAN,
      )
    : 0;
  const virtualVisibleCount = useVirtualRows
    ? Math.ceil(virtualViewportHeight / VIRTUAL_ROW_HEIGHT) +
      VIRTUAL_OVERSCAN * 2
    : displayedRows.length;
  const virtualEnd = useVirtualRows
    ? Math.min(displayedRows.length, virtualStart + virtualVisibleCount)
    : displayedRows.length;
  const renderedRows = useVirtualRows
    ? displayedRows.slice(virtualStart, virtualEnd)
    : displayedRows;
  useEffect(() => {
    if (!EnableLargeDataSet) return;
    if (tableScrollRef.current) tableScrollRef.current.scrollTop = 0;
    setVirtualScrollTop(0);
  }, [EnableLargeDataSet, pageSize, sort, globalSearch, filters]);

  const virtualTopSpace = useVirtualRows
    ? virtualStart * VIRTUAL_ROW_HEIGHT
    : 0;
  const virtualBottomSpace = useVirtualRows
    ? (displayedRows.length - virtualEnd) * VIRTUAL_ROW_HEIGHT
    : 0;

  const firstRecord = effectiveTotalRecords
    ? (currentPage - 1) * numericPageSize + 1
    : 0;
  const lastRecord = effectiveTotalRecords
    ? Math.min(firstRecord + displayedRows.length - 1, effectiveTotalRecords)
    : 0;
  const calculatedPageSizeOptions = useMemo(() => {
    if (Array.isArray(pageSizeOptions) && pageSizeOptions.length > 0) {
      return [
        ...new Set(
          pageSizeOptions
            .map(Number)
            .filter((size) => Number.isFinite(size) && size > 0),
        ),
      ];
    }
    return createAutomaticPageSizeOptions(
      defaultPageSize,
      effectiveTotalRecords,
    );
  }, [defaultPageSize, effectiveTotalRecords, pageSizeOptions]);

  useEffect(() => {
    setPageInput(String(currentPage));
  }, [currentPage]);

  const query = useMemo(
    () => ({
      pageNumber: currentPage,
      pageSize: isAllRecords ? 0 : numericPageSize,
      searchText: globalSearch.trim(),
      sortColumn: sort.key || "",
      sortDirection: sort.direction,
      filters: { ...filters },
    }),
    [currentPage, filters, globalSearch, isAllRecords, numericPageSize, sort],
  );

  useEffect(() => {
    if (!serverSide) return undefined;
    const timer = window.setTimeout(
      () => {
        if (!initialServerRequestSkipped.current)
          initialServerRequestSkipped.current = true;
        onQueryChange?.(query);
      },
      globalSearch ? searchDebounceMs : 0,
    );
    return () => window.clearTimeout(timer);
  }, [globalSearch, onQueryChange, query, searchDebounceMs, serverSide]);

  const changeSort = (column) => {
    if (!enableSorting || column.sortable === false) return;
    const nextSort = {
      key: column.key,
      direction:
        sort.key === column.key && sort.direction === "asc" ? "desc" : "asc",
    };
    setSort(nextSort);
    if (serverSide) {
      setPage(1);
      onSortChange?.({
        sortColumn: nextSort.key,
        sortDirection: nextSort.direction,
      });
    }
  };

  const goToPage = (nextPage) => {
    const parsedPage = Number.parseInt(nextPage, 10);
    const normalizedPage = Math.min(
      Math.max(Number.isFinite(parsedPage) ? parsedPage : 1, 1),
      totalPages,
    );
    setPage(normalizedPage);
    setPageInput(String(normalizedPage));
    if (serverSide) onPageChange?.(normalizedPage);
  };

  const commitPageInput = () => goToPage(pageInput);

  const changePageSize = (nextPageSize) => {
    const normalizedPageSize =
      nextPageSize === ALL_RECORDS ? ALL_RECORDS : Number(nextPageSize);
    setPageSize(normalizedPageSize);
    setPage(1);
    setPageInput("1");
    if (serverSide)
      onPageSizeChange?.(
        normalizedPageSize === ALL_RECORDS ? 0 : normalizedPageSize,
      );
  };

  /*
   * Grid toolbar actions use these helpers so refresh/add behavior is
   * consistent everywhere SortableTable is consumed, including grids that
   * are rendered through InlineEditable.
   *
   * Refresh:
   * - clears Search All Columns text
   * - clears every active column filter
   * - closes an open column-filter popover
   * - returns to the initial page
   * - restores the configured defaultPageSize
   *
   * Add:
   * - always moves to the first page so a newly inserted first row is visible
   */
  const resetPaginationForRefresh = () => {
    const nextPage = Math.max(1, Number(initialPageNumber) || 1);
    const nextPageSize =
      defaultPageSize === ALL_RECORDS
        ? ALL_RECORDS
        : Math.max(1, Number(defaultPageSize) || 30);

    setPage(nextPage);
    setPageInput(String(nextPage));
    setPageSize(nextPageSize);
  };

  const moveToFirstPageForAdd = () => {
    setPage(1);
    setPageInput("1");
  };

  const resetFiltersForRefresh = () => {
    setGlobalSearch("");
    setFilters({});
    setFilterState({ key: "", rect: null });
    // Restore the original row order when Refresh Grid is clicked.
    setSort({ key: "", direction: "asc" });

    // Keep consumers that use the optional server-side callbacks in sync
    // with the filter state cleared by the common Refresh Grid action.
    if (serverSide) {
      onSearchChange?.("");
      onFilterChange?.({});
      onSortChange?.({ sortColumn: "", sortDirection: "asc" });
    }
  };

  // External Refresh Grid buttons can request the same shared reset.
  // Ignore the initial prop value so mounting the table does not reset it.
  const initialRefreshTrigger = useRef(true);
  useEffect(() => {
    if (initialRefreshTrigger.current) {
      initialRefreshTrigger.current = false;
      return;
    }
    resetFiltersForRefresh();
    resetPaginationForRefresh();
    // Only changes to refreshTrigger should reset the grid.
  }, [refreshTrigger]);

  const executeToolbarRefresh = async () => {
    resetFiltersForRefresh();
    resetPaginationForRefresh();

    await runWithFullPageLoader(() => onRefresh?.(), "Refreshing records...");
  };

  const executeToolbarAdd = async () => {
    moveToFirstPageForAdd();
    await onAdd?.();
  };

  const changeGlobalSearch = (value) => {
    setGlobalSearch(value);
    setPage(1);
    if (serverSide) onSearchChange?.(value);
  };

  const changeFilter = useCallback(
    (key, value) => {
      setFilters((current) => {
        const nextFilters = { ...current, [key]: value };
        if (serverSide) onFilterChange?.(nextFilters);
        return nextFilters;
      });
      setPage(1);
    },
    [onFilterChange, serverSide],
  );

  const hasToolbar =
    enableGlobalSearch ||
    enablePagination ||
    toolbarExtra ||
    showAddAction ||
    showRefreshAction;

  return (
    <div className="tp-data-grid" aria-busy={loading}>
      {hasToolbar && (
        <div className="tp-grid-toolbar tp-grid-toolbar-modern">
          <div className="tp-grid-toolbar-right">
            {toolbarExtra && toolbarExtraPlacement !== "beforeSearch" && (
              <div className="tp-grid-toolbar-extra">{toolbarExtra}</div>
            )}
            <div className="tp-grid-toolbar-controls">
              {enablePagination && effectiveTotalRecords > 0 && (
                <div className="tp-grid-pagination tp-grid-pagination-modern tp-grid-pagination-input-mode tp-grid-pagination-toolbar">
                  <button
                    type="button"
                    disabled={currentPage === 1 || loading}
                    onClick={() => goToPage(1)}
                    title="First page"
                    aria-label="First page"
                  >
                    <SvgIcon name="first" size={16} />
                  </button>
                  <button
                    type="button"
                    disabled={currentPage === 1 || loading}
                    onClick={() => goToPage(currentPage - 1)}
                    title="Previous page"
                    aria-label="Previous page"
                  >
                    <SvgIcon name="previous" size={16} />
                  </button>
                  <span className="tp-grid-page-jump-label">Page</span>
                  <input
                    type="number"
                    min="1"
                    max={totalPages}
                    value={pageInput}
                    disabled={loading || isAllRecords}
                    className="tp-grid-page-number-input"
                    aria-label="Page number"
                    onChange={(event) =>
                      setPageInput(event.target.value.replace(/[^0-9]/g, ""))
                    }
                    onBlur={commitPageInput}
                    onKeyDown={(event) => {
                      if (event.key === "Enter") {
                        event.preventDefault();
                        commitPageInput();
                        event.currentTarget.blur();
                      }
                      if (event.key === "Escape")
                        setPageInput(String(currentPage));
                    }}
                  />
                  <span className="tp-grid-page-jump-total">
                    of <strong>{totalPages}</strong>
                  </span>
                  <button
                    type="button"
                    disabled={currentPage === totalPages || loading}
                    onClick={() => goToPage(currentPage + 1)}
                    title="Next page"
                    aria-label="Next page"
                  >
                    <SvgIcon name="next" size={16} />
                  </button>
                  <button
                    type="button"
                    disabled={currentPage === totalPages || loading}
                    onClick={() => goToPage(totalPages)}
                    title="Last page"
                    aria-label="Last page"
                  >
                    <SvgIcon name="last" size={16} />
                  </button>
                </div>
              )}
              {toolbarExtra && toolbarExtraPlacement === "beforeSearch" && (
                <div className="tp-grid-toolbar-extra tp-grid-toolbar-extra-before-search">
                  {toolbarExtra}
                </div>
              )}
              {enableGlobalSearch && (
                <div className="tp-grid-search tp-grid-search-toolbar">
                  <SvgIcon name="search" size={17} />
                  <input
                    value={globalSearch}
                    onChange={(event) => changeGlobalSearch(event.target.value)}
                    placeholder={globalSearchPlaceholder}
                  />
                  {globalSearch && (
                    <button
                      type="button"
                      className="tp-grid-search-clear"
                      onClick={() => changeGlobalSearch("")}
                      title="Clear search"
                      aria-label="Clear search"
                    >
                      <SvgIcon name="close" size={13} />
                    </button>
                  )}
                </div>
              )}
              {enablePagination && (
                <label
                  className="tp-grid-page-size tp-grid-page-size-compact"
                  title="Rows per page"
                >
                  <span className="visually-hidden">Rows per page</span>
                  <select
                    value={pageSize}
                    onChange={(event) => changePageSize(event.target.value)}
                    aria-label="Rows per page"
                  >
                    {calculatedPageSizeOptions.map((size) => (
                      <option key={size} value={size}>
                        {size}
                      </option>
                    ))}
                    <option value={ALL_RECORDS}>All</option>
                  </select>
                </label>
              )}
              {(showAddAction || showRefreshAction) && (
                <div className="tp-grid-feature-flags tp-grid-toolbar-actions">
                  {showRefreshAction && (
                    <button
                      type="button"
                      className="tp-grid-icon-action"
                      onClick={executeToolbarRefresh}
                      disabled={refreshing || loading}
                      title="Refresh grid"
                      aria-label="Refresh grid"
                    >
                      <SvgIcon
                        name="refresh"
                        size={17}
                        className={refreshing ? "tp-spin" : ""}
                      />
                    </button>
                  )}
                  {showAddAction && (
                    <button
                      type="button"
                      className={`tp-grid-add-action ${addActionIconOnly ? "tp-grid-add-action-icon-only" : ""}`}
                      onClick={executeToolbarAdd}
                      disabled={addActionDisabled || loading}
                      title={addActionLabel}
                      aria-label={addActionLabel}
                    >
                      <SvgIcon name={addActionIconName} size={18} />
                      {!addActionIconOnly && <span>{addActionLabel}</span>}
                    </button>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      <div
        ref={tableScrollRef}
        onScroll={
          EnableLargeDataSet
            ? (event) => {
                if (useVirtualRows)
                  setVirtualScrollTop(event.currentTarget.scrollTop);
              }
            : undefined
        }
        className="tp-table-wrap"
        style={{
          maxHeight: stickyHeader ? maxHeight : undefined,
          minHeight: minTableHeight,
        }}
      >
        {(loading || isLargeSearchPending) && (
          <div
            className="tp-grid-loading-layer"
            role="status"
            aria-live="polite"
          >
            <div className="tp-grid-loading-card">
              <span className="tp-grid-loading-spinner" aria-hidden="true" />
              <span>
                {isLargeSearchPending ? "Searching records..." : loadingText}
              </span>
            </div>
          </div>
        )}
        <table
          className={`table tp-table tp-table-modern align-middle mb-0 ${responsiveCards ? "responsive-cards" : ""}`}
        >
          <thead className={stickyHeader ? "tp-sticky-header" : ""}>
            <tr>
              {visibleColumns.map((column, index) => {
                const isFilterable =
                  enableColumnFilters && column.filterable !== false;
                const isFiltered =
                  filters[column.key] !== undefined &&
                  filters[column.key] !== "";
                return (
                  <th
                    key={column.key}
                    className={index < freezeColumns ? "tp-frozen-column" : ""}
                    title={column.tooltip || column.label}
                    style={{
                      width: column.width,
                      minWidth: column.width,
                      left:
                        index < freezeColumns
                          ? frozenOffsets[index]
                          : undefined,
                      zIndex: index < freezeColumns ? 8 : undefined,
                    }}
                  >
                    <div className="tp-column-head">
                      <button
                        type="button"
                        className="tp-column-sort"
                        disabled={!enableSorting || column.sortable === false}
                        onClick={() => changeSort(column)}
                      >
                        <span>{column.label}</span>
                        {enableSorting && column.sortable !== false && (
                          <SvgIcon
                            name={
                              sort.key === column.key
                                ? sort.direction === "asc"
                                  ? "sortUp"
                                  : "sortDown"
                                : "sort"
                            }
                            size={14}
                          />
                        )}
                      </button>
                      {isFilterable && (
                        <button
                          type="button"
                          className={`tp-column-filter-button ${isFiltered ? "active" : ""}`}
                          title={`Filter ${column.label}`}
                          aria-label={`Filter ${column.label}`}
                          onClick={(event) => {
                            event.stopPropagation();
                            const anchorElement = event.currentTarget;
                            const anchorRect =
                              anchorElement.getBoundingClientRect();
                            setFilterState((current) =>
                              current.key === column.key
                                ? { key: "", rect: null, anchorElement: null }
                                : {
                                    key: column.key,
                                    rect: anchorRect,
                                    anchorElement,
                                  },
                            );
                          }}
                        >
                          <SvgIcon
                            name="filter"
                            size={15}
                            strokeColor="#FFFFFF"
                          />
                        </button>
                      )}
                    </div>
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody>
            {useVirtualRows && virtualTopSpace > 0 && (
              <tr aria-hidden="true" className="tp-virtual-spacer-row">
                <td
                  colSpan={visibleColumns.length}
                  style={{ height: virtualTopSpace, padding: 0, border: 0 }}
                />
              </tr>
            )}
            {displayedRows.length ? (
              renderedRows.map((row) => (
                <tr
                  key={row[keyField]}
                  className={useVirtualRows ? "tp-virtual-data-row" : undefined}
                >
                  {visibleColumns.map((column, index) => (
                    <td
                      key={column.key}
                      data-label={column.label}
                      className={`${index < freezeColumns ? "tp-frozen-column" : ""} ${column.key === "__actions" ? "tp-action-cell" : ""}`}
                      style={{
                        left:
                          index < freezeColumns
                            ? frozenOffsets[index]
                            : undefined,
                      }}
                    >
                      {column.key === "__actions" ? (
                        renderActions ? (
                          renderActions(row, (event) => {
                            event.stopPropagation();
                            setActionState({
                              row,
                              rect: event.currentTarget.getBoundingClientRect(),
                            });
                          })
                        ) : (
                          <div className="tp-row-actions">
                            {showViewAction && (
                              <button
                                type="button"
                                className="tp-row-view"
                                title="View"
                                aria-label="View"
                                onClick={() => onView?.(row)}
                              >
                                <SvgIcon name="eye" size={19} />
                              </button>
                            )}
                            {showRowMore &&
                              (showEditAction || showDeleteAction) && (
                                <button
                                  type="button"
                                  className="tp-row-more"
                                  title="More actions"
                                  aria-label="More actions"
                                  onClick={(event) => {
                                    event.stopPropagation();
                                    setActionState({
                                      row,
                                      rect: event.currentTarget.getBoundingClientRect(),
                                    });
                                  }}
                                >
                                  <SvgIcon name="more" size={20} />
                                </button>
                              )}
                          </div>
                        )
                      ) : column.render ? (
                        column.render(row[column.key], row)
                      ) : (
                        row[column.key]
                      )}
                    </td>
                  ))}
                </tr>
              ))
            ) : (
              <tr>
                <td colSpan={visibleColumns.length} className="tp-table-empty">
                  <i className="bi bi-inbox" />
                  {emptyText}
                </td>
              </tr>
            )}
            {useVirtualRows && virtualBottomSpace > 0 && (
              <tr aria-hidden="true" className="tp-virtual-spacer-row">
                <td
                  colSpan={visibleColumns.length}
                  style={{ height: virtualBottomSpace, padding: 0, border: 0 }}
                />
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="tp-grid-footer">
        <div className="tp-grid-count">
          Showing <strong>{firstRecord}</strong> to{" "}
          <strong>{lastRecord}</strong> of{" "}
          <strong>{effectiveTotalRecords}</strong> records
        </div>
      </div>

      {showRowMore && (showEditAction || showDeleteAction) && (
        <RowActionMenu
          anchorRect={actionState.rect}
          row={actionState.row}
          onClose={() => setActionState({ row: null, rect: null })}
          onEdit={onEdit}
          onDelete={onDelete}
          showEditAction={showEditAction}
          showDeleteAction={showDeleteAction}
        />
      )}
      {filterState.key && (
        <FilterPopover
          anchorRect={filterState.rect}
          anchorElement={filterState.anchorElement}
          column={visibleColumns.find(
            (column) => column.key === filterState.key,
          )}
          value={filters[filterState.key] ?? ""}
          onChange={(value) => changeFilter(filterState.key, value)}
          onClear={() => changeFilter(filterState.key, "")}
          onClose={() => setFilterState({ key: "", rect: null })}
        />
      )}
    </div>
  );
}
