import axios from "axios";
import { baseUrl, ContentType } from "../utils/url";

/**
 * Shared Axios instance for all TimePro API calls.
 * Global authentication, refresh-token and error interceptors are installed
 * by axiosSetup.js during application startup.
 */
const apiClient = axios.create({
  baseURL: baseUrl || undefined,
  headers: {
    "Content-Type": ContentType,
  },
  timeout: 30_000,
});

export default apiClient;
