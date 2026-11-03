export default function AuthShell({ title, subtitle, children }) {
  return (
    <div className="tp-auth-page">
      <section className="tp-auth-visual">
        <div className="tp-auth-brand">
          <img
            src="/images/timepro-brand.png"
            alt="VUT Time Pro"
            className="tp-auth-brand-image"
          />
        </div>

        <div className="tp-auth-copy">
          <span className="tp-eyebrow">TIME INTELLIGENCE</span>
          <h1>Turn every hour into meaningful progress.</h1>
          <p>
            Track work, simplify approvals and understand team productivity
            from one beautiful workspace.
          </p>
        </div>

        <div className="tp-floating-card tp-float-one">
          <i className="bi bi-check-circle-fill" />
          <div>
            <strong>Monthly Timesheet</strong>
            <span>Submitted successfully</span>
          </div>
        </div>
      </section>

      <section className="tp-auth-form-section">
        <div className="tp-auth-card">
          <div className="tp-auth-mobile-brand">
            <img
              src="/images/timepro-brand.png"
              alt="VUT Time Pro"
              className="tp-auth-mobile-brand-image"
            />
          </div>

          <h2>{title}</h2>
          <p className="tp-auth-subtitle">{subtitle}</p>
          {children}
          <div className="tp-auth-footer">
          </div>
        </div>
      </section>
    </div>
  );
}
