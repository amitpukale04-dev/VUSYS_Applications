import { isCancel } from "axios";
import apiClient from "./apiClient";
import ToastService from "./toastService";
apiClient.interceptors.response.use(r=>r,error=>{
 if (isCancel(error) || error?.code === "ERR_CANCELED") return Promise.reject(error);
 const message=error?.response?.data?.error||error?.response?.data?.message||(!error?.response?"Unable to connect to the API server.":error.response.status===401?"Your session is invalid or expired.":error.response.status===429?"AI review rate limit reached. Please try later.":"Unable to process your request.");
 error.userMessage=message;
 if(error?.response?.status===401&&!String(error?.config?.url||"").includes("auth/login")){window.dispatchEvent(new Event("vusys:session-expired"));}
 if(!error?.config?.skipGlobalErrorToast){ToastService.error(message);error.__globalToastHandled=true;}
 return Promise.reject(error);
});
