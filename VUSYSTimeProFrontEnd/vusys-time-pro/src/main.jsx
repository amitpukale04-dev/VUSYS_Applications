import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { ToastContainer } from "react-toastify";
import "bootstrap/dist/css/bootstrap.min.css";
import "bootstrap/dist/js/bootstrap.bundle.min.js";
import "bootstrap-icons/font/bootstrap-icons.css";
import "react-toastify/dist/ReactToastify.css";
import "./services/axiosSetup";
import "./styles.css";
import App from "./App";
import { AuthProvider } from "./context/AuthContext";
import { ThemeProvider } from "./context/ThemeContext";
import { TabsProvider } from "./context/TabsContext";
import { SessionTimeoutProvider } from "./context/SessionTimeoutContext";
import { FullPageLoaderProvider } from "./context/FullPageLoaderContext";
import SessionTimeoutModal from "./components/common/SessionTimeoutModal";
import FullPageLoader from "./components/common/FullPageLoader";
import registerTimeProPwa from "./pwaRegister";

registerTimeProPwa();

ReactDOM.createRoot(document.getElementById("root")).render(
  <React.StrictMode>
    <BrowserRouter>
      <ThemeProvider>
        <AuthProvider>
          <TabsProvider>
            <FullPageLoaderProvider>
              <SessionTimeoutProvider>
                <App />
                <FullPageLoader />
                <SessionTimeoutModal />
                <ToastContainer
                  position="top-right"
                  autoClose={3000}
                  newestOnTop
                  closeOnClick
                  pauseOnHover
                />
              </SessionTimeoutProvider>
            </FullPageLoaderProvider>
          </TabsProvider>
        </AuthProvider>
      </ThemeProvider>
    </BrowserRouter>
  </React.StrictMode>,
);
