import { useEffect, useRef, useState } from "react";
import ConfirmModal from "../../components/common/ConfirmModal";
import { publishManualReview } from "../../services/codeReviewService";

export default function ManualReview({ selection, pull, target, setTarget, onPublished, onChooseFile, active }) {
  const editor = useRef(null);
  useEffect(() => {
    if (active && target) editor.current?.focus();
  }, [active, target]);

  const [body, setBody] = useState("");
  const [inlineBody, setInlineBody] = useState("");
  const [comments, setComments] = useState([]);
  const [confirm, setConfirm] = useState(false);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState("");
  const [publication, setPublication] = useState(null);

  const inlineCommentLimit = 20;

  const add = () => {
    if (!inlineBody.trim() || comments.length >= inlineCommentLimit) return;
    setComments((previous) => [...previous, { ...target, body: inlineBody.trim() }]);
    setInlineBody("");
    setTarget(null);
  };

  const publish = async () => {
    setSending(true);
    setError("");
    try {
      const [owner, repo] = selection.repo.split("/");
      const response = await publishManualReview({ owner, repo, number: selection.number,
        headSha: pull.headSha, baseSha: pull.baseSha, body: body.trim(), comments });
      setPublication(response);
      onPublished();
      setConfirm(false);
    } catch (exception) {
      setConfirm(false);
      setError(exception.response?.data?.error || "Publication could not be confirmed. Check the GitHub PR before retrying.");
    } finally {
      setSending(false);
    }
  };

  if (publication) return <section className="tp-cr-panel" role="status">
    <p>Manual review published to GitHub. Review ID: {publication.gitHubReviewId}.</p>
    <p>{publication.emailMessage}</p>
    {publication.publicationRecorded === false && <p role="alert">GitHub published the review. The portal could not update review history.</p>}
  </section>;

  return <section className="tp-cr-panel tp-manual-review">
    <div className="tp-pr-heading">
      <h3 className="tp-cr-section-title">Manual Review</h3>
      {pull && <button type="button" className="tp-cr-btn tp-cr-btn-outline"
        disabled={sending || Boolean(target) || comments.length >= inlineCommentLimit} onClick={onChooseFile}>
        {comments.length ? "Add another inline comment" : "Choose a file and line"}
      </button>}
    </div>
    {!pull && <p role="status">Load the pull request before publishing a manual review.</p>}
    <label className="tp-cr-label" htmlFor="manual-body">General review comment</label>
    <textarea id="manual-body" rows={5} maxLength={10000} className="tp-cr-input" value={body} disabled={sending}
      onChange={(event) => setBody(event.target.value)} />
    {target && <div className="tp-cr-finding">
      <label className="tp-cr-label" htmlFor="manual-inline">Comment on {target.fileName}, line {target.line}</label>
      <textarea ref={editor} id="manual-inline" rows={3} maxLength={2000} className="tp-cr-input" value={inlineBody} disabled={sending}
        onChange={(event) => setInlineBody(event.target.value)} />
      <div className="tp-cr-form-actions">
        <button type="button" className="tp-cr-btn tp-cr-btn-outline" disabled={sending || !inlineBody.trim() || comments.length >= inlineCommentLimit}
          onClick={add}>Add inline comment</button>
        <button type="button" className="tp-cr-btn tp-cr-btn-outline" disabled={sending}
          onClick={() => { setTarget(null); setInlineBody(""); }}>Cancel inline comment</button>
      </div>
    </div>}
    <p>Expand any changed file and click <strong>+</strong> beside a new-side line number to draft a comment.</p>
    {comments.length > 0 && <p role="status">
      {comments.length} of {inlineCommentLimit} inline comments drafted across {new Set(comments.map((comment) => comment.fileName)).size} file(s).
    </p>}
    {comments.length > 0 && <ul className="tp-manual-comments">
      {comments.map((comment, index) => <li key={index}>
        <strong>{comment.fileName}, line {comment.line}</strong>
        <p>{comment.body}</p>
        <button type="button" className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm" disabled={sending}
          onClick={() => setComments((current) => current.filter((_, item) => index !== item))}>Remove</button>
      </li>)}
    </ul>}
    {error && <p role="alert">{error}</p>}
    <button type="button" className="tp-cr-btn tp-cr-btn-primary"
      disabled={!pull || sending || Boolean(target) || (!body.trim() && comments.length === 0)}
      onClick={() => setConfirm(true)}>Publish Manual Review</button>
    <ConfirmModal show={confirm} title="Publish Manual Review" message="Post this manual review and its inline comments to GitHub?"
      onCancel={() => setConfirm(false)} onConfirm={publish} loading={sending} confirmText="Yes, Publish" />
  </section>;
}
