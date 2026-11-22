import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import SortableTable from "./SortableTable";
import apiClient from "../../services/apiClient";

/**
 * Server-driven grid with the TimePro SortableTable UI.
 *
 * Supply fetchData(payload, { signal }) or apiUrl for a real backend.
 * pageNumber is zero-based in the API payload; SortableTable uses one-based UI.
 * API response: data.TOTAL_RECORDS[0].TotalRecords and data[responseKey].
 * All => pageSize: 0; the backend must explicitly support this convention.
 */
export default function DynamicSortableTable({
  columns = [],
  fetchData,
  apiUrl,
  responseKey = "TRANSACTION_HISTORY_LIST",
  defaultPageSize = 30,
  searchDebounceMs = 350,
  onError,
  onRequestPayload,
  refreshKey,
  ...tableProps
}) {
  const [rows, setRows] = useState([]);
  const [totalRecords, setTotalRecords] = useState(0);
  const [loading, setLoading] = useState(false);
  const [query, setQuery] = useState(null);
  const [refreshCounter, setRefreshCounter] = useState(0);
  const latestRequestRef = useRef(0);
  const requestAbortRef = useRef(null);

  const fetcher = useCallback(
    (payload, { signal }) => {
      if (typeof fetchData === "function") return fetchData(payload, { signal });
      if (!apiUrl) throw new Error("DynamicSortableTable requires fetchData or apiUrl.");
      return apiClient.post(apiUrl, payload, { signal }).then((result) => result.data);
    },
    [apiUrl, fetchData],
  );

  const handleQueryChange = useCallback((nextQuery) => {
    setQuery((previous) => {
      const serialized = JSON.stringify(nextQuery);
      return previous && JSON.stringify(previous) === serialized ? previous : nextQuery;
    });
  }, []);

  const payload = useMemo(() => {
    if (!query) return null;
    const sortColumn = query.sortColumn || "";
    const sortOrder = query.sortDirection || "asc";
    const activeFilters = Object.entries(query.filters || {})
      .filter(([, value]) => value !== "" && value !== undefined && value !== null)
      .map(([columnName, value]) => ({
        columnName,
        searchKeyword: String(value),
        sortOrder,
        columnSequence: columnName === sortColumn ? 1 : 0,
      }));

    // Include the sorted column even if no column search is active.
    if (sortColumn && !activeFilters.some((entry) => entry.columnName === sortColumn)) {
      activeFilters.push({
        columnName: sortColumn,
        searchKeyword: "",
        sortOrder,
        columnSequence: 1,
      });
    }

    return {
      pageSize: query.pageSize,
      pageNumber: Math.max(0, query.pageNumber - 1),
      isGlobalSearch: query.searchText ? 1 : 0,
      sortOrder,
      searchFilter: activeFilters,
      columnName: "",
      searchKeyword: query.searchText || "",
    };
  }, [query]);

  useEffect(() => {
    if (!payload) return undefined;
    const requestId = ++latestRequestRef.current;
    requestAbortRef.current?.abort();
    const controller = new AbortController();
    requestAbortRef.current = controller;
    let active = true;

    const load = async () => {
      setLoading(true);
      onRequestPayload?.(payload);
      try {
        const response = await fetcher(payload, { signal: controller.signal });
        if (!active || requestId !== latestRequestRef.current) return;
        const body = response?.data?.data || response?.data || response;
        const status = response?.data?.Status || response?.Status;
        if (status && String(status).toLowerCase() !== "success") {
          throw new Error(response?.data?.message || response?.message || "Unable to load records.");
        }
        const resultRows = body?.[responseKey];
        setRows(Array.isArray(resultRows) ? resultRows : []);
        const count = Number(body?.TOTAL_RECORDS?.[0]?.TotalRecords);
        setTotalRecords(Number.isFinite(count) ? count : (resultRows?.length || 0));
      } catch (error) {
        if (!active || controller.signal.aborted || requestId !== latestRequestRef.current) return;
        setRows([]);
        setTotalRecords(0);
        onError?.(error);
      } finally {
        if (active && requestId === latestRequestRef.current) setLoading(false);
      }
    };
    load();
    return () => {
      active = false;
      controller.abort();
    };
  }, [fetcher, onError, onRequestPayload, payload, refreshCounter, refreshKey, responseKey]);

  const refresh = useCallback(async () => {
    setRefreshCounter((value) => value + 1);
  }, []);

  return (
    <SortableTable
      {...tableProps}
      columns={columns}
      data={rows}
      serverSide
      totalRecords={totalRecords}
      loading={loading || Boolean(tableProps.loading)}
      defaultPageSize={defaultPageSize}
      searchDebounceMs={searchDebounceMs}
      onQueryChange={handleQueryChange}
      onRefresh={refresh}
    />
  );
}
