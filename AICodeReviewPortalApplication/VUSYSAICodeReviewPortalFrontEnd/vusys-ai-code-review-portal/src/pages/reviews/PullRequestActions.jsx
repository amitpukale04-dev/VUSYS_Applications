import { useEffect, useState } from "react";
import ToastService from "../../services/toastService";
import ConfirmModal from "../../components/common/ConfirmModal";
import { getPullActionStatus, runPullAction } from "../../services/codeReviewService";

export default function PullRequestActions({ selection, pull, busy, setActivity, statusRevision }) {
  const [status, setStatus] = useState(null);
  const [confirmation, setConfirmation] = useState(null);
  const [feedback, setFeedback] = useState("");
  const [error, setError] = useState("");
  const [owner, repo] = selection.repo.split("/");

  const head = pull?.headSha;
  const number = selection.number;
  useEffect(() => {
    if (!head) return undefined;
    const request = new AbortController();
    getPullActionStatus(owner, repo, number, request.signal)
      .then((latest) => {
        if (request.signal.aborted) return;
        setStatus(latest);
        setError("");
      })
      .catch(() => { if (!request.signal.aborted) setError("Could not load the current GitHub status."); });
    return () => request.abort();
  }, [owner, repo, number, head, statusRevision]);

  const refreshStatus = async () => {
    const latest = await getPullActionStatus(owner, repo, selection.number);
    setStatus(latest);
    return latest;
  };

  const open = async (action) => {
    setActivity("pull-status");
    setError("");
    setFeedback("");
    try {
      const latest = await refreshStatus();
      if (action) setConfirmation({ action, inspectedHead: pull.headSha, latest });
    } catch (exception) {
      setError(exception.response?.data?.error || "Could not load the current GitHub status.");
    } finally {
      setActivity("");
    }
  };

  const close = () => setConfirmation(null);

  const execute = async () => {
    const { action, inspectedHead } = confirmation;
    setActivity("pull-write");
    setError("");
    try {
      const result = await runPullAction(owner, repo, selection.number, action, inspectedHead);
      setFeedback(`${result.message} GitHub account: ${result.actor}.`);
      if (result.status === "approved" || result.status === "merged") {
        ToastService.success(result.message);
      }
    } catch (exception) {
      setError(exception.response?.data?.error || "GitHub did not confirm the action. Check the PR on GitHub before retrying.");
    } finally {
      setConfirmation(null);
      try {
        await refreshStatus();
      } catch {
        setError((previous) => [previous, "Could not refresh the current GitHub status."].filter(Boolean).join(" "));
      }
      setActivity("");
    }
  };

  const merging = confirmation?.action === "Merge";
  const latest = confirmation?.latest;
  const stale = confirmation && (latest.headSha !== confirmation.inspectedHead || pull?.headSha !== confirmation.inspectedHead);
  const blocked = merging && (latest.targetBranch !== "main" || latest.inMergeQueue || latest.stacked);
  const actionLabel = merging ? "Merge to main" : "Approve PR";
  const visibleStatus = status?.headSha === pull?.headSha ? status : null;
  const isOpen = pull?.state === "open" && visibleStatus?.state !== "merged" && visibleStatus?.state !== "closed";
  const canApprove = isOpen && visibleStatus?.commentPublished && !visibleStatus.approved && !visibleStatus.draft;
  const canMerge = isOpen && visibleStatus?.commentPublished && visibleStatus.approved
    && visibleStatus.targetBranch === "main" && !visibleStatus.draft && !visibleStatus.stacked && !visibleStatus.inMergeQueue;

  return (
    <section className="tp-cr-panel" aria-label="GitHub pull request actions">
      <div className="tp-cr-form-actions" hidden={!canApprove && !canMerge}>
        {canApprove && <button type="button" className="tp-cr-btn tp-cr-btn-outline"
          disabled={busy} onClick={() => open("Approve")}>Approve PR</button>}
        {canMerge && <button type="button" className="tp-cr-btn tp-cr-btn-outline"
          disabled={busy} onClick={() => open("Merge")}>Merge to main</button>}
      </div>
      {status && <p className="tp-pr-github-status" role="status">
        <span className="tp-pr-github-status-label">GitHub status</span>
        <span className={`tp-cr-status tp-cr-status-${status.state === "merged" ? "success" : status.state === "closed" ? "danger" : "neutral"}`}>
          {status.state}
        </span>
        {status.inMergeQueue && <span className="tp-cr-status tp-cr-status-neutral">In merge queue</span>}
        {(status.reviewDecision || status.approved) && <span
          className={`tp-cr-status tp-cr-status-${(status.reviewDecision === "APPROVED" || !status.reviewDecision) ? "success" : status.reviewDecision === "CHANGES_REQUESTED" ? "danger" : "warning"}`}>
          {(status.reviewDecision || "APPROVED").toLowerCase().replaceAll("_", " ")}
        </span>}
      </p>}
      {status && pull && status.headSha !== pull.headSha && <p role="alert">
        The PR head changed. Refresh changed files and review the latest changes.
      </p>}
      {feedback && <p role="status">{feedback}</p>}
      {error && <p role="alert">{error}</p>}
      <ConfirmModal show={Boolean(confirmation)} title={actionLabel}
        message={latest && `${selection.repo} · PR #${selection.number} · Target: ${latest.targetBranch}`}
        onCancel={close} onConfirm={execute} confirmText={actionLabel} loading={busy}
        confirmDisabled={!latest?.commentPublished || (merging && !latest.approved) || stale || blocked || latest?.draft || latest?.state !== "open"}>
        {stale && <p role="alert">The PR head changed. Refresh changed files and review the latest changes.</p>}
        {latest && latest.state !== "open" && <p>The PR is {latest.state}.</p>}
        {!merging && latest?.draft && <p>The PR is a draft.</p>}
        {merging && latest.mergeQueueEnabled && <p>GitHub will use the merge queue for this branch.</p>}
        {merging && (latest.mergeBlockers.length > 0
          ? <ul>{latest.mergeBlockers.map((blocker) => <li key={blocker}>{blocker}</li>)}</ul>
          : <p>GitHub reports no current merge blockers.</p>)}
        {!merging && latest && <p>Approve commit <code>{confirmation.inspectedHead.slice(0, 12)}</code>.</p>}
      </ConfirmModal>
    </section>
  );
}
