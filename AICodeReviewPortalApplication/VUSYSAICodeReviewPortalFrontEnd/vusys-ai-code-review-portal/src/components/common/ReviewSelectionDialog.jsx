import { useEffect, useId, useRef, useState } from "react";
import { listPullRequests, listRepositories } from "../../services/codeReviewService";
import "./ReviewSelectionDialog.css";

export default function ReviewSelectionDialog({ choosePullRequest = false, onSelect, onClose }) {
  const dialog = useRef(null);
  const titleId = useId();
  const [repository, setRepository] = useState("");
  const [query, setQuery] = useState("");
  const [items, setItems] = useState(null);
  const [error, setError] = useState("");
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    const element = dialog.current;
    element.showModal();
    return () => element.close();
  }, []);

  useEffect(() => {
    const request = new AbortController();
    const options = { signal: request.signal, skipGlobalErrorToast: true };
    const result = repository
      ? listPullRequests(...repository.split("/"), options)
      : listRepositories(options);
    result.then((data) => {
      if (request.signal.aborted) return;
      const rows = Array.isArray(data) ? data : [];
      setItems(repository
        ? rows.filter((row) => Number.isInteger(row.number) && row.number > 0)
          .map((row) => ({ value: row.number, label: `#${row.number} · ${row.title || "Untitled pull request"}` }))
        : rows.map((row) => row.fullName || row.full_name)
          .filter((name) => typeof name === "string" && /^[^/]+\/[^/]+$/.test(name))
          .map((name) => ({ value: name, label: name })));
    }).catch((exception) => {
      if (!request.signal.aborted) setError(exception.userMessage || "Could not load the available choices.");
    });
    return () => request.abort();
  }, [repository, attempt]);

  const changeStep = (next) => {
    setItems(null);
    setError("");
    setQuery("");
    setRepository(next);
  };

  const select = (value) => {
    if (repository) onSelect(repository, value);
    else if (choosePullRequest) changeStep(value);
    else onSelect(value);
  };

  const filtered = items?.filter((item) => item.label.toLowerCase().includes(query.trim().toLowerCase()));

  return <dialog ref={dialog} className="tp-review-picker" aria-labelledby={titleId}
    onCancel={(event) => { event.preventDefault(); onClose(); }}>
    <header>
      <h2 id={titleId}>{repository ? "Select a Pull Request" : "Select a Repository"}</h2>
      <button type="button" className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm" onClick={onClose}>Cancel</button>
    </header>
    {repository && <p className="tp-review-picker-repository">{repository}</p>}
    <input type="search" className="tp-cr-input" value={query} onChange={(event) => setQuery(event.target.value)}
      aria-label={repository ? "Search pull requests" : "Search repositories"}
      placeholder={repository ? "Search pull requests" : "Search repositories"} />
    {error && <div className="tp-review-picker-status">
      <p role="alert">{error}</p>
      <button type="button" className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm"
        onClick={() => { setError(""); setAttempt((current) => current + 1); }}>Retry</button>
    </div>}
    {!items && !error && <p className="tp-review-picker-status" role="status">Loading…</p>}
    {filtered?.length === 0 && <p className="tp-review-picker-status" role="status">
      {items.length ? "No matches found." : repository ? "No open pull requests found." : "No repositories found."}
    </p>}
    {filtered?.length > 0 && <ul className="tp-review-picker-list">
      {filtered.map((item) => <li key={item.value}>
        <button type="button" onClick={() => select(item.value)}>{item.label}</button>
      </li>)}
    </ul>}
    {repository && <button type="button" className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm"
      onClick={() => changeStep("")}>Back to repositories</button>}
  </dialog>;
}
