import { useLocation, useNavigate } from "react-router-dom";
import ErrorIllustration from "./ErrorIllustration";

export default function ErrorPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const state = location.state || {};
  const originPath = state.originPath || "/";
  const originLabel = state.originLabel || "Return to Previous Page";

  return (
    <div className="tp-error-screen tp-error-screen-contained">
      <div className="tp-error-screen-card">
        <ErrorIllustration type="error" />
        <span className="tp-error-code warning">{state.status || "Error"}</span>
        <h1>{state.title || "Something went wrong"}</h1>
        <p>{state.message || "An unexpected error occurred while processing your request."}</p>
        <button type="button" className="tp-primary-button" onClick={() => navigate(originPath, { replace: true })}>
          <i className="bi bi-arrow-left-circle-fill" /> {originLabel}
        </button>
      </div>
    </div>
  );
}
