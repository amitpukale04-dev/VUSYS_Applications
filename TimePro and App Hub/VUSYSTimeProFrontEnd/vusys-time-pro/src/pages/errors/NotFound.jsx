import { Link } from "react-router-dom";
import ErrorIllustration from "./ErrorIllustration";

export default function NotFound() {
  return (
    <div className="tp-error-screen">
      <div className="tp-error-screen-card">
        <ErrorIllustration type="404" />
        <span className="tp-error-code">404</span>
        <h1>Page not found</h1>
        <p>The page you requested does not exist, has been moved, or is no longer available.</p>
        <Link to="/" className="tp-primary-button"><i className="bi bi-house-door-fill" /> Back to Home</Link>
      </div>
    </div>
  );
}
