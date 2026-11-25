import { useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import CodeReviewPageHeader from "../../components/common/CodeReviewPageHeader";
import ConfirmModal from "../../components/common/ConfirmModal";
import {
  analyzePullRequest,
  publishReview,
} from "../../services/codeReviewService";
import { showApiError } from "../../utils/showApiError";
import ToastService from "../../services/toastService";
import PullRequestChanges from "./PullRequestChanges";
import ManualReview from "./ManualReview";
import PullRequestActions from "./PullRequestActions";
import ReviewConversations from "./ReviewConversations";
import ReviewSelectionDialog from "../../components/common/ReviewSelectionDialog";

const validate = (repo, number) => {
  const validRepo =
    /^(?!\.{1,2}\/)[A-Za-z0-9_.-]{1,100}\/(?!\.{1,2}$)[A-Za-z0-9_.-]{1,100}$/.test(
      repo.trim(),
    );
  const validNumber =
    /^[1-9]\d*$/.test(number.trim()) && Number(number) <= 2147483647;
  return {
    repo: !repo.trim()
      ? "Enter the GitHub Repository."
      : validRepo
        ? ""
        : "Enter a valid repository in owner/repository format.",
    number: !number.trim()
      ? "Enter the Pull Request Number."
      : validNumber
        ? ""
        : "Enter a valid positive Pull Request Number.",
  };
};

function ReviewWorkspace({ initialRepo, initialNumber, onSelectPull }) {
  const [repo, setRepo] = useState(initialRepo);
  const [number, setNumber] = useState(initialNumber);
  const [errors, setErrors] = useState({});
  const [selection, setSelection] = useState(() =>
    Object.values(validate(initialRepo, initialNumber)).some(Boolean)
      ? null
      : { repo: initialRepo.trim(), number: Number(initialNumber) },
  );
  const [result, setResult] = useState(null);
  const [activity, setActivity] = useState("");
  const [statusRevision, setStatusRevision] = useState(0);
  const reviewChanged = () => setStatusRevision((previous) => previous + 1);
  const [view, setView] = useState("changes");
  const changedFiles = useRef(null);
  const openChangedFiles = () => {
    setView("changes");
    requestAnimationFrame(() => changedFiles.current?.focus());
  };
  const [pull, setPull] = useState(null);
  const [commentTarget, setTarget] = useState(null);
  const target =
    commentTarget?.headSha === pull?.headSha &&
    commentTarget?.baseSha === pull?.baseSha
      ? commentTarget?.line
      : null;
  const [confirm, setConfirm] = useState(false);
  const [pickerOpen, setPickerOpen] = useState(false);

  const select = () => {
    const nextErrors = validate(repo, number);
    setErrors(nextErrors);
    if (Object.values(nextErrors).some(Boolean)) return null;
    const selected = { repo: repo.trim(), number: Number(number) };
    setSelection(selected);
    return selected;
  };

  const analyze = async () => {
    const selected = select();
    if (!selected) return;
    setView("ai");
    setActivity("analyze");
    setResult(null);
    try {
      const [owner, name] = selected.repo.split("/");
      setResult(await analyzePullRequest(owner, name, selected.number));
      ToastService.success("Gemini AI code review completed.");
    } catch (exception) {
      showApiError(exception);
    } finally {
      setActivity("");
    }
  };

  const publish = async () => {
    setActivity("publish");
    try {
      const [owner, name] = result.repository.split("/");
      const publication = await publishReview(result.id, owner, name, result.pullNumber);
      setResult((previous) => ({ ...previous, ...publication }));
      reviewChanged();
      ToastService.success("COMMENT review published to GitHub.");
    } catch (exception) {
      showApiError(exception);
    } finally {
      setConfirm(false);
      setActivity("");
    }
  };

  return (
    <>
      <CodeReviewPageHeader title="Review Workspace" icon="bi-code-slash" />
      <details className="tp-cr-panel tp-pr-selector" open={!selection}>
        <summary>
          {selection
            ? `${selection.repo} · PR #${selection.number}`
            : "Select a pull request"}
        </summary>
        <form
          noValidate
          onSubmit={(event) => {
            event.preventDefault();
            setPickerOpen(true);
          }}
        >
          <div className="row g-3">
            <div className="col-12 col-lg-8">
              <label className="tp-cr-label" htmlFor="tp-review-repo">
                GitHub Repository <span className="tp-cr-required">*</span>
              </label>
              <input
                id="tp-review-repo"
                className={`tp-cr-input ${errors.repo ? "is-invalid" : ""}`}
                value={repo}
                disabled={Boolean(activity)}
                aria-invalid={Boolean(errors.repo)}
                aria-describedby={
                  errors.repo ? "tp-review-repo-error" : undefined
                }
                onChange={(event) => {
                  setRepo(event.target.value);
                  setErrors((previous) => ({ ...previous, repo: "" }));
                  setSelection(null);
                  setPull(null);
                  setTarget(null);
                  setResult(null);
                }}
                placeholder="owner/repository"
              />
              {errors.repo && (
                <div className="tp-cr-field-error" id="tp-review-repo-error">
                  {errors.repo}
                </div>
              )}
            </div>
            <div className="col-12 col-lg-4">
              <label className="tp-cr-label" htmlFor="tp-review-number">
                Pull Request Number <span className="tp-cr-required">*</span>
              </label>
              <input
                id="tp-review-number"
                className={`tp-cr-input ${errors.number ? "is-invalid" : ""}`}
                inputMode="numeric"
                value={number}
                disabled={Boolean(activity)}
                aria-invalid={Boolean(errors.number)}
                aria-describedby={
                  errors.number ? "tp-review-number-error" : undefined
                }
                onChange={(event) => {
                  setNumber(event.target.value);
                  setErrors((previous) => ({ ...previous, number: "" }));
                  setSelection(null);
                  setPull(null);
                  setTarget(null);
                  setResult(null);
                }}
                placeholder="e.g. 1"
              />
              {errors.number && (
                <div className="tp-cr-field-error" id="tp-review-number-error">
                  {errors.number}
                </div>
              )}
            </div>
          </div>
          <div className="tp-cr-form-actions">
            <button
              className="tp-cr-btn tp-cr-btn-outline"
              type="submit"
              disabled={Boolean(activity)}
            >
              <i className="bi bi-code-slash" aria-hidden="true" /> View Changed
              Files
            </button>
          </div>
        </form>
      </details>
      {pickerOpen && (
        <ReviewSelectionDialog
          choosePullRequest
          onClose={() => setPickerOpen(false)}
          onSelect={(name, pullNumber) => {
            if (initialRepo !== name || initialNumber !== String(pullNumber)) {
              onSelectPull(name, pullNumber);
            } else {
              setRepo(name);
              setNumber(String(pullNumber));
              setErrors({});
              if (!selection) setSelection({ repo: name, number: pullNumber });
            }
            setView("changes");
            setPickerOpen(false);
          }}
        />
      )}
      {selection && (
        <nav
          aria-label="Review workspace views"
          className="tp-pr-workspace-nav"
        >
          {[
            ["changes", "Changed Files"],
            ["ai", "AI Review"],
            ["manual", "Manual Review"],
            ["conversations", "Review Conversations"],
          ].map(([id, label]) => (
            <button
              key={id}
              type="button"
              aria-pressed={view === id}
              className={`tp-cr-btn ${view === id ? "tp-cr-btn-primary" : "tp-cr-btn-outline"}`}
              onClick={() => setView(id)}
            >
              {label}
            </button>
          ))}
          <button type="button" className="tp-cr-btn tp-cr-btn-outline"
            disabled={Boolean(activity) || !pull} onClick={reviewChanged}>
            <i className="bi bi-arrow-clockwise" aria-hidden="true" /> Refresh PR Status
          </button>
        </nav>
      )}
      {selection && (
        <PullRequestActions key={`actions:${selection.repo}#${selection.number}`} selection={selection} pull={pull}
          busy={Boolean(activity)} setActivity={setActivity} statusRevision={statusRevision} />
      )}
      {selection && (
        <div ref={changedFiles} tabIndex={-1} aria-label="Changed files" hidden={view !== "changes"}>
          <PullRequestChanges
            key={`${selection.repo}#${selection.number}`}
            {...selection}
            onSnapshot={setPull}
            onComment={(line) => {
              if (!target) setTarget({ line, headSha: pull.headSha, baseSha: pull.baseSha });
              setView("manual");
            }}
          />
        </div>
      )}
      {selection && (
        <div hidden={view !== "manual"}>
          <ManualReview
            key={`${selection.repo}#${selection.number}:${pull?.headSha}:${pull?.baseSha}`}
            selection={selection}
            pull={pull}
            target={target}
            setTarget={setTarget}
            onPublished={reviewChanged}
            onChooseFile={openChangedFiles}
            active={view === "manual"}
          />
        </div>
      )}
      {selection && view === "conversations" && (
        <ReviewConversations key={`conversations:${selection.repo}#${selection.number}`} selection={selection} onUpdated={reviewChanged} />
      )}
      {view === "ai" && (
        <section className="tp-cr-panel tp-pr-ai-actions">
          <h3 className="tp-cr-section-title">AI Review</h3>
          <button
            className="tp-cr-btn tp-cr-btn-primary"
            type="button"
            disabled={Boolean(activity)}
            onClick={analyze}
          >
            <i className="bi bi-stars" aria-hidden="true" />{" "}
            {activity === "analyze" ? "Analyzing…" : "Run Gemini AI Review"}
          </button>
          {result && !result.published && (
            <button
              className="tp-cr-btn tp-cr-btn-outline"
              type="button"
              disabled={Boolean(activity)}
              onClick={() => setConfirm(true)}
            >
              Publish COMMENT Review
            </button>
          )}
        </section>
      )}
      {view === "ai" && result && (
        <section className="tp-cr-panel">
          <h3 className="tp-cr-section-title">Review Summary</h3>
          <p>{result.summary}</p>
          {result.emailMessage && <p role="status">{result.emailMessage}</p>}
          {result.publicationRecorded === false && <p role="alert">GitHub published the review. The portal could not update review history.</p>}
          <div className="tp-cr-summary-tags">
            <span className="tp-cr-status tp-cr-status-neutral">
              Review #{result.id}
            </span>
            <span
              className={`tp-cr-status ${result.published ? "tp-cr-status-success" : "tp-cr-status-warning"}`}
            >
              {result.published ? "Published" : "Not published"}
            </span>
          </div>
          {result.coverage && (
            <div>
              <p>
                Reviewed {result.coverage.reviewedFiles} of{" "}
                {result.coverage.totalFiles} files. Partial patches:{" "}
                {result.coverage.partialFiles}.
              </p>
              {result.coverage.limitations?.length > 0 && (
                <ul>
                  {result.coverage.limitations.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ul>
              )}
            </div>
          )}
          <h3 className="tp-cr-section-title">
            Findings ({result.findings?.length || 0})
          </h3>
          {(result.findings || []).map((finding, index) => (
            <div className="tp-cr-finding" key={index}>
              <div className="tp-cr-finding-heading">
                <strong>
                  {index + 1}. {finding.title}
                </strong>
                <span className="tp-cr-status tp-cr-status-neutral">
                  {finding.severity}
                </span>
              </div>
              <small>
                {finding.file}
                {finding.line ? ` : ${finding.line}` : ""}
                {finding.endLine > finding.line ? `–${finding.endLine}` : ""}
              </small>
              <p>{finding.description}</p>
              <p>
                <strong>Recommendation:</strong> {finding.recommendation}
              </p>
            </div>
          ))}
        </section>
      )}
      <ConfirmModal
        show={confirm}
        title="Confirm GitHub Review"
        message="Publish these AI findings as a COMMENT review on GitHub? This will not approve, request changes, or merge the PR."
        onCancel={() => setConfirm(false)}
        onConfirm={publish}
        loading={activity === "publish"}
        confirmText="Yes, Publish"
      />
    </>
  );
}

export default function ReviewWorkspacePage() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const initialRepo = state?.repository || "";
  const initialNumber = String(state?.pr ?? "");
  return (
    <ReviewWorkspace
      key={JSON.stringify([initialRepo, initialNumber])}
      initialRepo={initialRepo}
      initialNumber={initialNumber}
      onSelectPull={(repository, pr) => navigate("/reviews/new", { state: { repository, pr } })}
    />
  );
}
