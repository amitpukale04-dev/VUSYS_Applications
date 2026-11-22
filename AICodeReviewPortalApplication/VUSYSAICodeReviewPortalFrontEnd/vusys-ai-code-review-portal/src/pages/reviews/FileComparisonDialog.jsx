import { useEffect, useRef, useState } from "react";

const statusText = {
  NotApplicable: "This version does not exist for this change.",
  MergeBaseUnavailable: "GitHub could not resolve the merge base.",
  TooLarge: "This file version exceeds the 512 KiB display limit.",
  Binary: "This version is binary.",
  BinaryOrUnsupportedEncoding: "This version is binary or uses an unsupported encoding.",
  UnsupportedFileType: "This file type cannot be displayed.",
};

function scrollPosition(pane) {
  return { top: pane.scrollTop, left: pane.scrollLeft };
}

function Version({ label, version, changes, kind, fallbackLine, scroller, positions, onScroll }) {
  const lines = version.content ? version.content.split(/\r\n|\n|\r/) : [];
  if (lines.at(-1) === "") lines.pop();
  const size = 300;
  const first = changes.values().next().value ?? fallbackLine;
  const anchor = Math.min(Math.max(first, 1), Math.max(lines.length, 1));
  const initial = Math.floor((anchor - 1) / size);
  const [page, setPage] = useState(initial);
  const [jump, setJump] = useState(0);
  useEffect(() => {
    const element = scroller.current;
    if (!element) return;
    const row = element.querySelector("[data-comparison-anchor]");
    element.scrollTop = row ? Math.max(0, row.offsetTop - 44) : 0;
    positions.current.set(element, scrollPosition(element));
  }, [page, jump, anchor, scroller, positions]);
  const offset = page * size;

  return <section className="tp-pr-version">
    <h5>{label}</h5>
    <p className="tp-pr-version-path">{version.path}</p>
    {version.status === "Available" ? <>
      {lines.length === 0 ? <p>Empty file.</p> :
        <div className="tp-pr-code-scroll tp-pr-version-scroll" ref={scroller} onScroll={onScroll} tabIndex={0} aria-label={`${label} file content`}>
          <table className="tp-pr-code"><tbody>
            {lines.slice(offset, offset + size).map((line, index) => {
              const number = offset + index + 1;
              const changed = changes.has(number);
              return <tr key={number} className={changed ? `tp-pr-line-${kind}` : undefined}
                data-changed={changed || undefined} data-comparison-anchor={number === anchor || undefined}>
                <td className="tp-pr-line-number">{number}</td>
                <td className="tp-pr-marker">{changed ? (kind === "added" ? "+" : "−") : ""}</td>
                <td className="tp-pr-source">{line || " "}</td>
              </tr>;
            })}
          </tbody></table>
        </div>}
      {lines.length > size && <div className="tp-pr-file-actions">
        <button type="button" className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm" disabled={!page}
          onClick={() => setPage(page - 1)}>Previous lines</button>
        <span>{offset + 1}–{Math.min(lines.length, offset + size)} of {lines.length}</span>
        <button type="button" className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm" disabled={offset + size >= lines.length}
          onClick={() => setPage(page + 1)}>Next lines</button>
        {fallbackLine > 0 && <button type="button" className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm"
          onClick={() => { setPage(initial); setJump((current) => current + 1); }}>
          First change
        </button>}
      </div>}
    </> : <p>{statusText[version.status] ?? "GitHub could not retrieve this version."}</p>}
  </section>;
}

export default function FileComparisonDialog({ file, comparison, close }) {
  const ref = useRef(null);
  const before = useRef(null);
  const after = useRef(null);
  const positions = useRef(new WeakMap());

  const syncScroll = (source, target) => {
    const current = scrollPosition(source);
    const previous = positions.current.get(source) ?? { top: 0, left: 0 };
    positions.current.set(source, current);
    if (!target) return;

    if (current.top !== previous.top) target.scrollTop = current.top;
    if (current.left !== previous.left) target.scrollLeft = current.left;
    // Record the actual position after clamping so the reflected event cannot move the source.
    positions.current.set(target, scrollPosition(target));
  };
  const removed = new Set();
  const added = new Set();
  for (const hunk of file.hunks ?? []) {
    for (const line of hunk.lines) {
      if (line.lineType === "Added") added.add(line.newLineNumber);
      if (line.lineType === "Removed") removed.add(line.oldLineNumber);
    }
  }
  const firstChangedHunk = file.hunks?.find((hunk) =>
    hunk.lines.some((line) => line.lineType === "Added" || line.lineType === "Removed"));
  useEffect(() => {
    const dialog = ref.current;
    dialog.showModal();
    return () => dialog.close();
  }, []);
  return <dialog className="tp-pr-compare-dialog" ref={ref} aria-labelledby="comparison-title"
    onCancel={(event) => { event.preventDefault(); close(); }}>
    <div className="tp-pr-compare-header">
      <h4 id="comparison-title">Before and After</h4>
      <button type="button" className="tp-cr-btn tp-cr-btn-outline tp-cr-btn-sm" onClick={close}>Close comparison</button>
    </div>
    <p className="tp-pr-version-path">{file.fileName}</p>
    <p><span className="tp-pr-line-removed">− Removed</span>{" · "}<span className="tp-pr-line-added">+ Added</span></p>
    {file.diffCompleteness !== "Complete" && <p>Highlights are limited to the lines GitHub returned in the patch.</p>}
    <div className="tp-pr-versions">
      <Version label="Before" version={comparison.before} changes={removed} kind="removed" fallbackLine={firstChangedHunk?.oldStartLine ?? 0}
        scroller={before} positions={positions} onScroll={(event) => syncScroll(event.currentTarget, after.current)} />
      <Version label="After" version={comparison.after} changes={added} kind="added" fallbackLine={firstChangedHunk?.newStartLine ?? 0}
        scroller={after} positions={positions} onScroll={(event) => syncScroll(event.currentTarget, before.current)} />
    </div>
  </dialog>;
}
