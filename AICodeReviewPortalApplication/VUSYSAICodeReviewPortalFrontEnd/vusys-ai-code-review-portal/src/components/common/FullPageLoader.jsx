import { createPortal } from "react-dom";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";

export default function FullPageLoader() {
  const { isLoading, message } = useFullPageLoader();

  if (!isLoading || typeof document === "undefined") {
    return null;
  }

  return createPortal(
    <div
      className="tp-full-page-loader"
      role="status"
      aria-live="polite"
      aria-busy="true"
    >
      <div className="tp-full-page-loader-backdrop" />

      <div className="tp-full-page-loader-card">
        <div className="tp-full-page-loader-visual" aria-hidden="true">
          <span className="tp-full-page-loader-orbit tp-full-page-loader-orbit-one" />
          <span className="tp-full-page-loader-orbit tp-full-page-loader-orbit-two" />
          <span className="tp-full-page-loader-core">
            <img src="/images/timepro-brand.png" alt="" />
          </span>
        </div>

        <div className="tp-full-page-loader-content">
          <span className="tp-full-page-loader-message">{message}</span>
          <span className="tp-full-page-loader-subtext">Please wait a moment</span>
          <span className="tp-full-page-loader-dots" aria-hidden="true">
            <i />
            <i />
            <i />
          </span>
        </div>
      </div>
    </div>,
    document.body,
  );
}
