import { useState } from "react";

export default function OfflinePage() {
  const [checking, setChecking] = useState(false);

  const retryConnection = () => {
    setChecking(true);

    window.setTimeout(() => {
      if (navigator.onLine) {
        window.location.reload();
        return;
      }

      setChecking(false);
    }, 450);
  };

  return (
    <div className="tp-offline-page" role="alert" aria-live="assertive">
      <div className="tp-offline-page-glow tp-offline-page-glow-one" />
      <div className="tp-offline-page-glow tp-offline-page-glow-two" />

      <section className="tp-offline-page-card">
        <div className="tp-offline-page-icon" aria-hidden="true">
          <i className="bi bi-wifi-off" />
        </div>

        <span className="tp-offline-page-badge">
          <span className="tp-offline-page-status-dot" />
          Offline
        </span>

        <h1>No Internet Connection</h1>

        <p>
          TimePro cannot connect to the internet right now. Check your Wi-Fi
          or mobile data connection and try again.
        </p>

        <button
          type="button"
          className="tp-offline-page-retry"
          onClick={retryConnection}
          disabled={checking}
        >
          {checking ? (
            <span
              className="spinner-border spinner-border-sm"
              aria-hidden="true"
            />
          ) : (
            <i className="bi bi-arrow-clockwise" aria-hidden="true" />
          )}
          <span>{checking ? "Checking Connection..." : "Try Again"}</span>
        </button>

        <small>
          Your current TimePro screen will be restored automatically when the
          connection returns.
        </small>
      </section>
    </div>
  );
}
