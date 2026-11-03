import { useCallback, useEffect, useMemo, useState } from "react";
import SortableTable from "../../components/common/SortableTable";
import DynamicSortableTable from "../../components/common/DynamicSortableTable";

const COLUMN_KEYS = [
  "transactionId", "transactionDate", "transactionType", "referenceNumber", "itemCode",
  "itemName", "itemDescription", "category", "subCategory", "warehouse", "location",
  "department", "projectCode", "employeeCode", "customerCode", "supplierCode", "unit",
  "quantity", "unitPrice", "grossAmount", "discount", "taxRate", "taxAmount", "netAmount",
  "currency", "paymentMode", "paymentStatus", "approvalStatus", "transactionStatus",
  "createdBy", "createdDate", "updatedBy", "updatedDate", "batchNumber", "serialNumber",
  "lotNumber", "costCenter", "financialYear", "fiscalMonth", "region", "country", "state",
  "city", "remarks", "openingBalance", "closingBalance", "debitAmount", "creditAmount",
  "stockBalance", "priority",
];
const NUMBER_KEYS = new Set([
  "quantity", "unitPrice", "grossAmount", "discount", "taxRate", "taxAmount", "netAmount",
  "fiscalMonth", "openingBalance", "closingBalance", "debitAmount", "creditAmount", "stockBalance",
]);
const COLUMNS = COLUMN_KEYS.map((key, index) => ({
  key,
  label: key.replace(/([A-Z])/g, " $1").replace(/^./, (character) => character.toUpperCase()),
  width: index === 6 ? "270px" : "155px",
}));

const compareValues = (left, right) => {
  if (typeof left === "number" && typeof right === "number") return left - right;
  return String(left ?? "").localeCompare(String(right ?? ""), undefined, {
    numeric: true, sensitivity: "base",
  });
};

export default function TransactionHistory() {
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [mode, setMode] = useState("client");
  const [lastPayload, setLastPayload] = useState(null);

  useEffect(() => {
    const controller = new AbortController();
    fetch(`${import.meta.env.BASE_URL}data/TransactionHistory.json`, { signal: controller.signal })
      .then((response) => {
        if (!response.ok) throw new Error(`Static data could not be loaded (${response.status}).`);
        return response.json();
      })
      .then((result) => {
        if (!controller.signal.aborted) setRows(result);
      })
      .catch((error) => {
        if (!controller.signal.aborted) setLoadError(error.message);
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, []);

  // DEMO adapter: simulates the server using the static JSON dataset.
  // In production replace this callback with a real POST API using apiUrl
  // or fetchData. DynamicSortableTable itself never sorts/filters locally.
  const fetchDemoPage = useCallback(async (payload, { signal }) => {
    if (signal.aborted) throw new DOMException("Aborted", "AbortError");
    let filtered = rows;
    const globalKeyword = String(payload.searchKeyword || "").toLowerCase();
    if (payload.isGlobalSearch && globalKeyword) {
      filtered = filtered.filter((row) =>
        COLUMN_KEYS.some((key) => String(row[key] ?? "").toLowerCase().includes(globalKeyword)),
      );
    }
    for (const filter of payload.searchFilter || []) {
      const keyword = String(filter.searchKeyword || "").toLowerCase();
      if (keyword) {
        filtered = filtered.filter((row) =>
          String(row[filter.columnName] ?? "").toLowerCase().includes(keyword),
        );
      }
    }
    const sortColumn = payload.searchFilter?.find((entry) => entry.columnSequence > 0)?.columnName;
    if (sortColumn) {
      filtered = [...filtered].sort((a, b) =>
        compareValues(a[sortColumn], b[sortColumn]) * (payload.sortOrder === "desc" ? -1 : 1),
      );
    }
    const start = payload.pageNumber * payload.pageSize;
    const pageRows = payload.pageSize === 0 ? filtered : filtered.slice(start, start + payload.pageSize);
    if (signal.aborted) throw new DOMException("Aborted", "AbortError");
    return {
      message: "OK",
      Status: "Success",
      data: {
        TOTAL_RECORDS: [{ TotalRecords: filtered.length }],
        TRANSACTION_HISTORY_LIST: pageRows,
      },
    };
  }, [rows]);

  const columnList = useMemo(() => COLUMNS.map((column) => ({
    ...column,
    filterType: NUMBER_KEYS.has(column.key) ? "text" : undefined,
  })), []);

  return (
    <div>
      <div className="tp-page-heading">
        <div>
          <span className="tp-eyebrow">PERFORMANCE DEMONSTRATION</span>
          <h1>Transaction History</h1>
          <p>50 columns and 100,000 static transactions. Select All to test virtual scrolling.</p>
        </div>
      </div>
      <div className="tp-card" style={{ padding: 16 }}>
        <div className="d-flex flex-wrap align-items-center gap-3 mb-3">
          <label htmlFor="transaction-grid-mode" className="fw-semibold">Grid mode</label>
          <select
            id="transaction-grid-mode"
            className="form-select"
            style={{ width: "min(100%, 320px)" }}
            value={mode}
            onChange={(event) => setMode(event.target.value)}
          >
            <option value="client">SortableTable (client-side + virtual All)</option>
            <option value="server">DynamicSortableTable (simulated API)</option>
          </select>
          <span className="text-muted">{rows.length.toLocaleString()} records · {COLUMN_KEYS.length} columns</span>
        </div>
        {loadError && <div className="alert alert-danger">{loadError}</div>}
        {loading && <p role="status">Loading 100,000 static transactions. This may take a few seconds...</p>}
        {!loading && !loadError && (mode === "client" ? (
          <SortableTable
            key="transaction-client"
            EnableLargeDataSet={true}
            columns={columnList}
            data={rows}
            keyField="transactionId"
            defaultPageSize={30}
            maxHeight={530}
            minTableHeight={250}
            showRefreshAction
            onRefresh={() => {}}
          />
        ) : (
          <>
            <p className="text-muted small">
              This demo simulates the API contract using local JSON. Connect the real API with
              the <code>apiUrl</code> or <code>fetchData</code> prop in DynamicSortableTable.
            </p>
            <DynamicSortableTable
              key="transaction-server"
              columns={columnList}
              fetchData={fetchDemoPage}
              onRequestPayload={setLastPayload}
              keyField="transactionId"
              defaultPageSize={30}
              maxHeight={530}
              minTableHeight={250}
              showRefreshAction
            />
            {lastPayload && (
              <details className="mt-3">
                <summary>Last server-side request payload</summary>
                <pre className="mt-2 p-3" style={{ overflowX: "auto" }}>
                  {JSON.stringify(lastPayload, null, 2)}
                </pre>
              </details>
            )}
          </>
        ))}
      </div>
    </div>
  );
}
