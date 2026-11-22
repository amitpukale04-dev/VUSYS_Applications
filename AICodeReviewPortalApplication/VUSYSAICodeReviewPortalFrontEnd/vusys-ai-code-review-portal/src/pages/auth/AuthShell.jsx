export default function AuthShell({ title, subtitle, children }) {
  return (
    <div className="tp-auth-page">
      <section className="tp-auth-visual">
        <div className="tp-auth-brand">
          <img
            src="/images/code-review-brand.svg"
            alt="VUSYS AI Code Review"
            className="tp-auth-brand-image"
          />
        </div>

        <div className="tp-auth-copy">
          <span className="tp-eyebrow">AI-POWERED CODE QUALITY</span>
          <h1>Review smarter. Ship better code.</h1>
          <p>
            Review GitHub pull requests, identify potential issues, and publish feedback from one beautiful workspace.
          </p>
        </div>

        <div className="tp-floating-card tp-float-one">
          <i className="bi bi-check-circle-fill" />
          <div>
            <strong>AI Code Review</strong>
            <span>Review completed</span>
          </div>
        </div>
      </section>

      <section className="tp-auth-form-section">
        <div className="tp-auth-card">
          <div className="tp-auth-mobile-brand">
            <img
              src="/images/code-review-brand.svg"
              alt="VUSYS AI Code Review"
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
