import ToastService from "../services/toastService";
export function showApiError(error,fallback="Unable to complete request."){if(error?.__globalToastHandled)return;ToastService.error(error?.userMessage||error?.response?.data?.error||error?.message||fallback);}
