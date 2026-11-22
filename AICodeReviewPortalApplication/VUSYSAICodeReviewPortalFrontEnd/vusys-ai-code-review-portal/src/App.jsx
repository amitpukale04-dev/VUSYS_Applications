import { useEffect, useState } from "react";
import AppRoutes from "./routes/AppRoutes";
import SplashScreen from "./components/common/SplashScreen";
import OfflinePage from "./pages/errors/OfflinePage";

export default function App() {
  const [ready, setReady] = useState(false);
  const [online, setOnline] = useState(() => navigator.onLine);

  useEffect(() => {
    const timer = window.setTimeout(() => setReady(true), 1100);
    return () => window.clearTimeout(timer);
  }, []);

  useEffect(() => {
    const handleOnline = () => setOnline(true);
    const handleOffline = () => setOnline(false);

    window.addEventListener("online", handleOnline);
    window.addEventListener("offline", handleOffline);

    return () => {
      window.removeEventListener("online", handleOnline);
      window.removeEventListener("offline", handleOffline);
    };
  }, []);

  if (!online) {
    return <OfflinePage />;
  }

  return ready ? <AppRoutes /> : <SplashScreen />;
}
