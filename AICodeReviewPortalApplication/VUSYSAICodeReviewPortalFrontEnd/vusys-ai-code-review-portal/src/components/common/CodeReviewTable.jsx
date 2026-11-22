import { useEffect, useMemo, useState } from "react";
import SvgIcon from "./SvgIcon";

export default function CodeReviewTable({ columns = [], rows = [], empty = "No records found." }) {
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const [size, setSize] = useState(30);
  const filtered = useMemo(() => rows.filter((row) => JSON.stringify(row).toLowerCase().includes(query.trim().toLowerCase())), [rows, query]);
  const pages = Math.max(1, Math.ceil(filtered.length / (size === "all" ? Math.max(filtered.length, 1) : size)));
  const current = Math.min(page, pages);
  useEffect(() => setPage(1), [rows]);
  const visible = size === "all" ? filtered : filtered.slice((current - 1) * size, current * size);
  return (
    <section className="tp-cr-table-wrap">
      <div className="tp-cr-table-tools">
        <div className="tp-cr-search"><i className="bi bi-search" aria-hidden="true" /><input className="tp-cr-input" aria-label="Search records" placeholder="Search records..." value={query} onChange={(event) => { setQuery(event.target.value); setPage(1); }} /></div>
        <span className="tp-cr-record-count">{filtered.length} {filtered.length === 1 ? "record" : "records"}</span>
      </div>
      <div className="tp-cr-table-scroll"><table className="tp-cr-table"><thead><tr>{columns.map((column) => <th key={column.key} scope="col">{column.label}</th>)}</tr></thead><tbody>{visible.length ? visible.map((row, index) => <tr key={row.id ?? `${current}-${index}`}>{columns.map((column) => <td key={column.key} data-label={column.label}>{column.render ? column.render(row) : String(row[column.key] ?? "—")}</td>)}</tr>) : <tr><td colSpan={columns.length} className="tp-cr-empty"><i className="bi bi-inbox" aria-hidden="true" /><span>{empty}</span></td></tr>}</tbody></table></div>
      <div className="tp-cr-pagination"><span className="tp-cr-page-count">Page <strong>{current}</strong> of <strong>{pages}</strong></span><button type="button" className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm" disabled={current === 1} onClick={() => setPage((value) => value - 1)}><i className="bi bi-chevron-left" aria-hidden="true"/> Previous</button><button type="button" className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm" disabled={current === pages} onClick={() => setPage((value) => value + 1)}>Next <i className="bi bi-chevron-right" aria-hidden="true"/></button><select aria-label="Rows per page" className="tp-cr-select" value={size} onChange={(event) => { setSize(event.target.value === "all" ? "all" : Number(event.target.value)); setPage(1); }}><option value={30}>30 rows</option><option value={50}>50 rows</option><option value={100}>100 rows</option><option value="all">All</option></select></div>
    </section>
  );
}
