import { useNavigate } from "react-router-dom";
import ErrorIllustration from "../../pages/errors/ErrorIllustration";

export default function AccessDenied({
  title = "Access Denied",
  message = "You don't have permission to access this page.",
  returnPath = "/",
  returnLabel = "Back to Home",
}) {
  const navigate = useNavigate();

  return (
    <div className="tp-error-screen tp-error-screen-contained">
      <div className="tp-error-screen-card">
        <ErrorIllustration type="error" />
        <span className="tp-error-code warning">403</span>
        <h1>{title}</h1>
        <p>{message}</p>
        <button
          type="button"
          className="tp-primary-button"
          onClick={() => navigate(returnPath, { replace: true })}
        >
          <i className="bi bi-house-door-fill" /> {returnLabel}
        </button>
      </div>
    </div>
  );
}
