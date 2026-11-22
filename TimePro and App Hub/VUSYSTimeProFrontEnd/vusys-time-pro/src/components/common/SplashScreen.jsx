export default function SplashScreen() {
  return (
    <div className="tp-splash">
      <div className="tp-splash-orb tp-splash-orb-one" />
      <div className="tp-splash-orb tp-splash-orb-two" />
      <div className="tp-splash-content">
        <div className="tp-brand-mark tp-brand-mark-xl">
          <img src="/images/timepro-brand.png" alt="VUT Time Pro" />
        </div>
        <h1>VUSYS Time Pro</h1>
        <p>Plan better. Track smarter. Deliver faster.</p>
        <div className="tp-splash-loader">
          <span />
          <span />
          <span />
        </div>
      </div>
    </div>
  );
}
