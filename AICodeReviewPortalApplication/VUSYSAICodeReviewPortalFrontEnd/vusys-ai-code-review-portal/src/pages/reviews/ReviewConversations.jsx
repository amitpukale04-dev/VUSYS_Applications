import { useState } from "react";
import ConfirmModal from "../../components/common/ConfirmModal";
import { getReviewThreads, getReviewThreadComments, updateReviewThread } from "../../services/codeReviewService";

const errorMessage = (error, fallback) => error.response?.data?.error || fallback;

function Conversation({ path, thread, onUpdated }) {
  const [comments, setComments] = useState(thread.comments);
  const [resolved, setResolved] = useState(thread.isResolved);
  const [reply, setReply] = useState("");
  const [busy, setBusy] = useState(false);
  const [confirm, setConfirm] = useState(false);
  const [error, setError] = useState("");

  const loadComments = async (more) => {
    const page = await getReviewThreadComments(path, thread.id, more ? comments.pageInfo.endCursor : undefined);
    setComments(more ? { ...page, nodes: [...comments.nodes, ...page.nodes] } : page);
  };

  const post = async (resolve) => {
    setBusy(true);
    setError("");
    try {
      await updateReviewThread(path, thread.id, resolve ? undefined : reply.trim());
      if (resolve) setResolved(true);
      else setReply("");
      onUpdated();
      if (!resolve) {
        try {
          await loadComments(false);
        } catch {
          setError("Reply posted. Refresh conversations to see it.");
        }
      }
    } catch (exception) {
      setError(errorMessage(exception, "GitHub did not confirm the update. Refresh the thread before retrying."));
    } finally {
      setBusy(false);
      setConfirm(false);
    }
  };

  return (
    <article className="tp-cr-finding">
      <div className="tp-cr-finding-heading">
        <strong className="text-break">{thread.path}{(thread.line ?? thread.originalLine) && ` · Line ${thread.line ?? thread.originalLine}`}</strong>
        <span className={`tp-cr-status ${resolved ? "tp-cr-status-success" : "tp-cr-status-warning"}`}>
          {resolved ? "Resolved" : "Unresolved"}{thread.isOutdated ? " · Outdated" : ""}
        </span>
      </div>
      {comments.nodes.map((comment) => (
        <div key={comment.id} className="mb-2 mt-2">
          <strong>{comment.author?.login ?? "Unknown GitHub user"}</strong>{" "}
          <small><time dateTime={comment.createdAt}>{new Date(comment.createdAt).toLocaleString()}</time></small>
          <p className="mb-2 text-break" style={{ whiteSpace: "pre-wrap" }}>{comment.body}</p>
        </div>
      ))}
      {comments.pageInfo.hasNextPage && (
        <button className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm" type="button" disabled={busy} onClick={async () => {
          setBusy(true);
          setError("");
          try { await loadComments(true); }
          catch (exception) { setError(errorMessage(exception, "Could not load more replies.")); }
          finally { setBusy(false); }
        }}>Load more replies</button>
      )}
      {!resolved && (
        <>
          <label className="tp-cr-label mt-2" htmlFor={`review-reply-${thread.id}`}>Reply</label>
          <textarea id={`review-reply-${thread.id}`} className="tp-cr-input" rows={3} maxLength={10000}
            value={reply} onChange={(event) => setReply(event.target.value)} disabled={busy} />
          <div className="tp-cr-form-actions">
            <button type="button" className="tp-cr-btn tp-cr-btn-outline" disabled={busy || !reply.trim()}
              onClick={() => post(false)}>Post reply</button>
            <button type="button" className="tp-cr-btn tp-cr-btn-outline" disabled={busy}
              onClick={() => setConfirm(true)}>Resolve conversation</button>
          </div>
        </>
      )}
      {error && <p role="alert">{error}</p>}
      <ConfirmModal show={confirm} title="Resolve conversation" message="Mark this review conversation as resolved on GitHub?"
        onCancel={() => setConfirm(false)} onConfirm={() => post(true)} loading={busy} confirmText="Resolve conversation" />
    </article>
  );
}

export default function ReviewConversations({ selection, onUpdated }) {
  const [threads, setThreads] = useState(null);
  const [revision, setRevision] = useState(0);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const path = [...selection.repo.split("/"), selection.number];

  const load = async (more) => {
    setBusy(true);
    setError("");
    try {
      const page = await getReviewThreads(path, more ? threads.pageInfo.endCursor : undefined);
      setThreads(more ? { ...page, nodes: [...threads.nodes, ...page.nodes] } : page);
      if (!more) setRevision((previous) => previous + 1);
    } catch (exception) {
      setError(errorMessage(exception, "Could not load review conversations."));
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="tp-cr-panel" aria-busy={busy}>
      <div className="tp-pr-heading">
        <h3 className="tp-cr-section-title">Review Conversations</h3>
        <button className="tp-cr-btn tp-cr-btn-outline" type="button" disabled={busy} onClick={() => load(false)}>
          {threads ? "Refresh conversations" : "Load conversations"}
        </button>
      </div>
      {error && <p role="alert">{error}</p>}
      {threads?.nodes.length === 0 && <p role="status">No review conversations on this PR.</p>}
      {threads?.nodes.map((thread) => (
        <Conversation key={`${revision}:${thread.id}`} path={path} thread={thread} onUpdated={onUpdated} />
      ))}
      {threads?.pageInfo.hasNextPage && (
        <button className="tp-cr-btn tp-cr-btn-outline" type="button" disabled={busy} onClick={() => load(true)}>
          Load more conversations
        </button>
      )}
    </section>
  );
}
