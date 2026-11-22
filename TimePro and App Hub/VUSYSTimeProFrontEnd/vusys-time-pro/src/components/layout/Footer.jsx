import { APP_CONFIG } from "../../utils/environmentConfig";

export default function Footer() {
  return (
    <footer className="tp-footer">
      <span>
        <strong>{APP_CONFIG.environment}</strong> © {new Date().getFullYear()}{" "}
         VUSYS Time Pro Management
      </span>
      <span className="d-none d-sm-inline">Version {APP_CONFIG.versionNo}</span>
    </footer>
  );
}
