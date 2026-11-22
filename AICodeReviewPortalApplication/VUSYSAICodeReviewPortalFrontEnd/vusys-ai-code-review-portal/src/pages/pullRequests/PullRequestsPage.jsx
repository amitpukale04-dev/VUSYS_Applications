import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { listPullRequests } from "../../services/codeReviewService";
import { showApiError } from "../../utils/showApiError";
import CodeReviewPageHeader from "../../components/common/CodeReviewPageHeader";
import CodeReviewTable from "../../components/common/CodeReviewTable";

import ReviewSelectionDialog from "../../components/common/ReviewSelectionDialog";

const validateRepository = (value) => {
  const trimmed = value.trim();
  if (!trimmed) return "Enter the GitHub Repository.";
  if (!/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(trimmed)) return "Enter a valid repository in owner/repository format.";
  return "";
};
export default function PullRequestsPage() {
  const [params, setParams] = useSearchParams();
  const selectedRepository = params.get("repository") || "";
  const [repository, setRepository] = useState(selectedRepository);
  const [error, setError] = useState("");
  const [rows, setRows] = useState([]);
  const [loadedRepository, setLoadedRepository] = useState("");
  const [loading, setLoading] = useState(false);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [reload, setReload] = useState(0);
  const navigate = useNavigate();
  useEffect(() => {
    setRepository(selectedRepository);
    const message = selectedRepository ? validateRepository(selectedRepository) : "";
    setError(message);
    setRows([]);
    setLoadedRepository("");
    if (!selectedRepository || message) {
      setLoading(false);
      return;
    }

    const request = new AbortController();
    setLoading(true);
    listPullRequests(...selectedRepository.trim().split("/"), { signal: request.signal })
      .then((result) => {
        if (request.signal.aborted) return;
        setRows(Array.isArray(result) ? result : []);
        setLoadedRepository(selectedRepository.trim());
      })
      .catch((exception) => { if (!request.signal.aborted) showApiError(exception); })
      .finally(() => { if (!request.signal.aborted) setLoading(false); });
    return () => request.abort();
  }, [selectedRepository, reload]);
  return <>
    <CodeReviewPageHeader title="Pull Requests" description="Find open pull requests and inspect their changed files." icon="bi-git"><button type="button" className="tp-cr-btn tp-cr-btn-primary" onClick={() => navigate("/reviews/new")}><i className="bi bi-stars" aria-hidden="true"/> New AI Review</button></CodeReviewPageHeader>
    <section className="tp-cr-panel"><form noValidate onSubmit={(event) => { event.preventDefault(); setPickerOpen(true); }}><label className="tp-cr-label" htmlFor="tp-pr-repository">GitHub Repository <span className="tp-cr-required">*</span></label><div className="tp-cr-input-action"><div className="tp-cr-field"><input id="tp-pr-repository" className={`tp-cr-input ${error ? "is-invalid" : ""}`} value={repository} aria-invalid={Boolean(error)} aria-describedby={error ? "tp-pr-repository-error" : undefined} onChange={(event) => { setRepository(event.target.value); if (error) setError(""); }} onBlur={() => { if (repository.trim()) setError(validateRepository(repository)); }} placeholder="owner/repository" />{error && <div className="tp-cr-field-error" id="tp-pr-repository-error">{error}</div>}</div><button type="submit" className="tp-cr-btn tp-cr-btn-primary" disabled={loading}><i className={`bi ${loading ? "bi-arrow-repeat" : "bi-arrow-clockwise"}`} aria-hidden="true"/> {loading ? "Loading..." : "Load Pull Requests"}</button></div></form></section>
    {pickerOpen && <ReviewSelectionDialog onClose={() => setPickerOpen(false)} onSelect={(name) => {
      setRepository(name);
      setPickerOpen(false);
      if (name === selectedRepository) setReload((current) => current + 1);
      else setParams((current) => { const next = new URLSearchParams(current); next.set("repository", name); return next; });
    }} />}
    <CodeReviewTable rows={rows} empty={loading ? "Loading pull requests..." : "No pull requests loaded."} columns={[{ key: "number", label: "PR #", render: (row) => <strong>#{row.number}</strong> }, { key: "title", label: "Title" }, { key: "state", label: "Status", render: (row) => <span className={`tp-cr-status ${row.state === "open" ? "tp-cr-status-success" : "tp-cr-status-neutral"}`}>{row.state}</span> }, { key: "action", label: "Action", render: (row) => <button type="button" className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm" onClick={() => navigate(`/reviews/new?repository=${encodeURIComponent(loadedRepository)}&pr=${row.number}`)}><i className="bi bi-code-slash" aria-hidden="true"/> View Changes</button> }]} />
  </>;
}
