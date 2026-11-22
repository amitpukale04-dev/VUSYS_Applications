export default function ErrorIllustration({ type = "404" }) {
  const is404 = String(type) === "404";
  return (
    <svg className="tp-error-illustration" viewBox="0 0 560 320" role="img" aria-label={is404 ? "Page not found illustration" : "Application error illustration"}>
      <defs>
        <linearGradient id="tpErrorGradient" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#4f46e5" />
          <stop offset="1" stopColor="#06b6d4" />
        </linearGradient>
        <filter id="tpErrorShadow"><feDropShadow dx="0" dy="10" stdDeviation="12" floodOpacity=".16" /></filter>
      </defs>
      <ellipse cx="280" cy="282" rx="190" ry="20" fill="#dbeafe" opacity=".65" />
      <rect x="112" y="46" width="336" height="210" rx="24" fill="#fff" stroke="#dbe3f1" strokeWidth="4" filter="url(#tpErrorShadow)" />
      <rect x="112" y="46" width="336" height="42" rx="24" fill="url(#tpErrorGradient)" />
      <circle cx="142" cy="67" r="7" fill="#fecaca" /><circle cx="165" cy="67" r="7" fill="#fde68a" /><circle cx="188" cy="67" r="7" fill="#bbf7d0" />
      {is404 ? (
        <>
          <path d="M214 132h-38l-34 55h72v31m-38-31h58" fill="none" stroke="#4f46e5" strokeWidth="18" strokeLinecap="round" strokeLinejoin="round" />
          <circle cx="280" cy="177" r="44" fill="#eef2ff" stroke="#6366f1" strokeWidth="12" />
          <path d="M346 132h-38l-34 55h72v31m-38-31h58" fill="none" stroke="#06b6d4" strokeWidth="18" strokeLinecap="round" strokeLinejoin="round" />
          <path d="M267 177h26M280 164v26" stroke="#6366f1" strokeWidth="8" strokeLinecap="round" />
        </>
      ) : (
        <>
          <path d="M280 118l70 118H210z" fill="#fff7ed" stroke="#f59e0b" strokeWidth="10" strokeLinejoin="round" />
          <path d="M280 158v38" stroke="#f59e0b" strokeWidth="12" strokeLinecap="round" />
          <circle cx="280" cy="217" r="7" fill="#f59e0b" />
        </>
      )}
      <path d="M96 122l-36-18m404 18 36-18M86 208l-38 20m426-20 38 20" stroke="#c7d2fe" strokeWidth="8" strokeLinecap="round" />
    </svg>
  );
}
