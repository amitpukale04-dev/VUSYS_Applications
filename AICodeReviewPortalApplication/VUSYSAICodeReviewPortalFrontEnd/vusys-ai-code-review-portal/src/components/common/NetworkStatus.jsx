import { useEffect, useState } from "react";

export default function NetworkStatus() {
  const [online, setOnline] = useState(navigator.onLine);
  useEffect(() => {
    const sync = () => setOnline(navigator.onLine);
    window.addEventListener("online", sync);
    window.addEventListener("offline", sync);
    return () => {
      window.removeEventListener("online", sync);
      window.removeEventListener("offline", sync);
    };
  }, []);
  return online ? null : (
    <div className="tp-offline-banner">
      <i className="bi bi-wifi-off me-2" />
      You are offline. Some actions are unavailable.
    </div>
  );
}
