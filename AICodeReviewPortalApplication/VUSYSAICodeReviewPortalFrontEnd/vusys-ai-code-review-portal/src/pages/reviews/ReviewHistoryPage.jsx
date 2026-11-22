import { useEffect, useState } from "react";
import { listReviews } from "../../services/codeReviewService";
import { showApiError } from "../../utils/showApiError";
import CodeReviewPageHeader from "../../components/common/CodeReviewPageHeader";
import CodeReviewTable from "../../components/common/CodeReviewTable";
export default function ReviewHistoryPage() {
  const [rows, setRows] = useState([]);
  const [selected, setSelected] = useState(null);
  const [loading, setLoading] = useState(true);
  useEffect(() => { let active = true; listReviews().then((data) => { if (active) setRows(Array.isArray(data) ? data : []); }).catch((error) => { if (active) showApiError(error); }).finally(() => { if (active) setLoading(false); }); return () => { active = false; }; }, []);
  return <><CodeReviewPageHeader title="Review History" description="Previous Gemini reviews, findings, and publication status." icon="bi-clock-history" /><CodeReviewTable rows={rows} empty={loading ? "Loading review history..." : "No reviews yet."} columns={[{ key: "id", label: "Review ID", render: (row) => <strong>#{row.id}</strong> }, { key: "repository", label: "Repository" }, { key: "pullNumber", label: "PR #", render: (row) => `#${row.pullNumber}` }, { key: "published", label: "Status", render: (row) => <span className={`tp-cr-status ${row.published ? "tp-cr-status-success" : "tp-cr-status-warning"}`}>{row.published ? "Published" : "Draft"}</span> }, { key: "action", label: "Action", render: (row) => <button type="button" className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm" onClick={() => setSelected(row)}><i className="bi bi-eye" aria-hidden="true"/> View Findings</button> }]} />{selected && <section className="tp-cr-panel"><div className="tp-cr-finding-heading"><h3 className="tp-cr-section-title">{selected.repository} — PR #{selected.pullNumber}</h3><button className="tp-cr-icon-btn" type="button" onClick={() => setSelected(null)} aria-label="Close findings"><i className="bi bi-x-lg" /></button></div><p>{selected.summary}</p>{(selected.findings || []).map((finding, index) => <div className="tp-cr-finding" key={index}><div className="tp-cr-finding-heading"><strong>{finding.title}</strong><span className="tp-cr-status tp-cr-status-neutral">{finding.severity}</span></div><p>{finding.description}</p><p><strong>Recommendation:</strong> {finding.recommendation}</p></div>)}</section>}</>;
}
