export default function SplashScreen() {
  return (
    <div className="tp-splash">
      <div className="tp-splash-orb tp-splash-orb-one" />
      <div className="tp-splash-orb tp-splash-orb-two" />
      <div className="tp-splash-content">
        <div className="tp-brand-mark tp-brand-mark-xl">
          <img src="/images/code-review-brand.svg" alt="VUSYS AI Code Review" />
        </div>
        <h1>VUSYS AI Code Review</h1>
        <p>Review smarter. Ship better code.</p>
        <div className="tp-splash-loader">
          <span />
          <span />
          <span />
        </div>
      </div>
    </div>
  );
}
