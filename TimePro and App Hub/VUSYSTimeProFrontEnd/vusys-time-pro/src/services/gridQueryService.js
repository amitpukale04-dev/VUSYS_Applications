const asSearchText = (value) => {
  if (value === null || value === undefined) return "";
  if (typeof value === "boolean")
    return value ? "yes active true" : "no inactive false";
  return String(value).toLowerCase();
};

/**
 * Simulates the shape of a server-side grid endpoint.
 * Replace this function with an Axios API call in production.
 */
export async function queryGridData({
  source = [],
  query,
  searchableColumns = [],
  delay = 250,
}) {
  const {
    pageNumber = 1,
    pageSize = 20,
    searchText = "",
    sortColumn = "",
    sortDirection = "asc",
    filters = {},
  } = query || {};

  await new Promise((resolve) => window.setTimeout(resolve, delay));

  const normalizedSearch = searchText.trim().toLowerCase();
  let rows = source.filter((row) => {
    const globalMatch =
      !normalizedSearch ||
      searchableColumns.some((column) =>
        asSearchText(row[column]).includes(normalizedSearch),
      );

    if (!globalMatch) return false;

    return Object.entries(filters).every(([column, filterValue]) => {
      if (filterValue === undefined || filterValue === "") return true;
      const rowValue = row[column];
      if (filterValue === "true" || filterValue === "false") {
        return Boolean(rowValue) === (filterValue === "true");
      }
      return asSearchText(rowValue).includes(String(filterValue).toLowerCase());
    });
  });

  if (sortColumn) {
    rows = [...rows].sort((leftRow, rightRow) => {
      const left = leftRow[sortColumn] ?? "";
      const right = rightRow[sortColumn] ?? "";
      const result =
        typeof left === "number" && typeof right === "number"
          ? left - right
          : String(left).localeCompare(String(right), undefined, {
              numeric: true,
              sensitivity: "base",
            });
      return sortDirection === "desc" ? -result : result;
    });
  }

  const totalRecords = rows.length;
  const showAllRecords = Number(pageSize) <= 0;
  const effectivePageSize = showAllRecords
    ? Math.max(1, totalRecords)
    : Math.max(1, Number(pageSize) || 30);
  const effectivePageNumber = showAllRecords
    ? 1
    : Math.max(1, Number(pageNumber) || 1);
  const start = Math.max(0, (effectivePageNumber - 1) * effectivePageSize);

  return {
    data: showAllRecords ? rows : rows.slice(start, start + effectivePageSize),
    totalRecords,
    pageNumber: effectivePageNumber,
    pageSize: showAllRecords ? 0 : effectivePageSize,
    totalPages: showAllRecords
      ? 1
      : Math.max(1, Math.ceil(totalRecords / effectivePageSize)),
  };
}
