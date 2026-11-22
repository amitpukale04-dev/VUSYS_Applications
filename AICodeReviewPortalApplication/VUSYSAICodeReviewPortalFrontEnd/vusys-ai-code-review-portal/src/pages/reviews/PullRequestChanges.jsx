import { useEffect, useId, useRef, useState } from "react";
import { getPullRequestFileContent, getPullRequestFiles } from "../../services/codeReviewService";
import "./PullRequestChanges.css";
import FileComparisonDialog from "./FileComparisonDialog";

const patchMessages = {
  Partial: "GitHub returned a partial patch. Some changes may be missing.",
  Malformed: "GitHub returned a malformed patch. Some changes may be missing.",
  Unavailable: "GitHub did not provide a text patch for this file.",
};

const apiMessage = (error, fallback) => {
  const message = error.response?.data?.error;
  return typeof message === "string" && message ? message : fallback;
};

function ChangedFile({ file, owner, repo, number, headSha, baseSha, onComment }) {
  const [open, setOpen] = useState(false);
  const [content, setContent] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const pending = useRef(null);
  const panelId = useId();
  useEffect(() => () => pending.current?.abort(), []);

  const loadContent = async () => {
    const request = new AbortController();
    pending.current?.abort();
    pending.current = request;
    setLoading(true);
    setError("");
    try {
      const comparison = await getPullRequestFileContent(owner, repo, number, file.fileName, request.signal);
      if (request.signal.aborted) return;
      const comparedHead = comparison.headSha ?? comparison.after.commitSha;
      if ((comparedHead && comparedHead !== headSha) || (comparison.baseSha && comparison.baseSha !== baseSha)) {
        setError("The PR changed since this diff was loaded. Refresh changed files before viewing file versions.");
      } else {
        setContent(comparison);
      }
    } catch (exception) {
      if (!request.signal.aborted) setError(apiMessage(exception, "Could not load file versions."));
    } finally {
      if (!request.signal.aborted) setLoading(false);
    }
  };

  return <article className="tp-pr-file">
    <button type="button" className="tp-pr-file-toggle" aria-controls={panelId} aria-expanded={open}
      onClick={() => setOpen(!open)}>
      <i className={`bi ${open ? "bi-chevron-down" : "bi-chevron-right"}`} aria-hidden="true" />
      <span className="tp-pr-file-name">
        {file.previousFileName && <>{file.previousFileName} → </>}{file.fileName}
      </span>
      <span className="tp-cr-status tp-cr-status-neutral">{file.fileStatus}</span>
      <span className="tp-pr-counts"><span>+{file.additions}</span> <span>−{file.deletions}</span></span>
    </button>
    <div id={panelId} hidden={!open}>
      {open && <>
        {file.isBinary
          ? <p className="tp-pr-notice">This is a binary file. A text diff is unavailable.</p>
          : patchMessages[file.diffCompleteness] && <p className="tp-pr-notice">{patchMessages[file.diffCompleteness]}</p>}
        {file.hunks?.length > 0 && <div className="tp-pr-code-scroll" tabIndex={0} aria-label={`Diff for ${file.fileName}`}>
          <table className="tp-pr-code">
            <thead className="visually-hidden"><tr><th>Old line</th><th>New line</th><th>Change</th><th>Code</th></tr></thead>
            {file.hunks.map((hunk, index) => <tbody key={index}>
              <tr className="tp-pr-hunk"><td colSpan={4}>{hunk.header}</td></tr>
              {hunk.lines.map((line, lineIndex) => <tr key={lineIndex} className={`tp-pr-line-${line.lineType.toLowerCase()}`}>
                <td className="tp-pr-line-number">{line.oldLineNumber}</td>
                <td className="tp-pr-line-number">{line.newLineNumber}
                  {line.newLineNumber && file.diffCompleteness === "Complete" && <button type="button" className="tp-pr-add-comment"
                    aria-label={`Comment on line ${line.newLineNumber}`}
                    onClick={() => onComment({ fileName: file.fileName, line: line.newLineNumber })}>+</button>}
                </td>
                <td className="tp-pr-marker">{line.lineType === "Added" ? "+" : line.lineType === "Removed" ? "−" : ""}</td>
                <td className="tp-pr-source">{line.content}</td>
              </tr>)}
            </tbody>)}
          </table>
        </div>}
        <div className="tp-pr-file-actions">
          <button type="button" className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm" disabled={loading}
            onClick={content ? () => setContent(null) : loadContent}>
            {loading ? "Loading file versions…" : content ? "Hide before and after" : "View before and after"}
          </button>
          {error && <span role="alert">{error}</span>}
        </div>
        {content && <FileComparisonDialog file={file} comparison={content} close={() => setContent(null)} />}
      </>}
    </div>
  </article>;
}

function PullRequestChanges({ repo: repository, number, onSnapshot, onComment }) {
  const [owner, repo] = repository.split("/");
  const [response, setResponse] = useState(null);
  const [error, setError] = useState("");
  const [attempt, setAttempt] = useState(0);
  const [filter, setFilter] = useState("");
  useEffect(() => {
    const request = new AbortController();
    getPullRequestFiles(owner, repo, number, request.signal)
      .then((data) => { if (!request.signal.aborted) { setResponse(data); onSnapshot(data.pullRequest); } })
      .catch((exception) => {
        if (!request.signal.aborted) setError(apiMessage(exception, "Could not load changed files."));
      });
    return () => request.abort();
  }, [owner, repo, number, attempt, onSnapshot]);

  const refresh = () => {
    setResponse(null);
    setError("");
    setAttempt((current) => current + 1);
  };

  const pull = response?.pullRequest;
  const files = response?.files ?? [];
  const search = filter.trim().toLowerCase();
  const visible = files.filter((file) => [file.fileName, file.previousFileName].some((name) => name?.toLowerCase().includes(search)));
  return <section className="tp-cr-panel tp-pr-changes" aria-busy={!response && !error}>
    <div className="tp-pr-heading">
      <h3 className="tp-cr-section-title">Files Changed</h3>
      {(response || error) && <button type="button" className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm" onClick={refresh}>
        {error ? "Retry" : "Refresh changed files"}
      </button>}
    </div>
    {error && <p role="alert">{error}</p>}
    {!response && !error && <p role="status">Loading pull request changes…</p>}
    {pull && <>
      <h4>#{pull.number} {pull.title}</h4>
      <p>{pull.state} · {pull.author?.login || "Unknown author"} · <code>{pull.sourceBranch}</code> → <code>{pull.targetBranch}</code></p>
      {pull.description && <p className="tp-pr-description">{pull.description}</p>}
      <p>
        {pull.commits ?? "—"} commits · {pull.changedFiles ?? response.returnedFileCount} changed files
        {" · "}+{pull.additions ?? "—"} / −{pull.deletions ?? "—"}
        {" · "}Head <code>{pull.headSha?.slice(0, 12)}</code>
      </p>
      {!response.fileListComplete && <p role="status">GitHub returned {response.returnedFileCount} files. The changed-file list may be incomplete. GitHub caps this endpoint at {response.upstreamFileLimit} files.</p>}
      {files.length > 0 && <label className="tp-cr-label tp-pr-filter">Filter changed files
        <input className="tp-cr-input" type="search" value={filter} onChange={(event) => setFilter(event.target.value)} />
      </label>}
      {visible.length === 0 && <p>{files.length ? "No changed files match your search." : "GitHub returned no changed files."}</p>}
      {visible.map((file) => <ChangedFile key={file.fileName} file={file} owner={owner} repo={repo} number={number} headSha={pull.headSha} baseSha={pull.baseSha} onComment={onComment} />)}
    </>}
  </section>;
}

export default PullRequestChanges;
