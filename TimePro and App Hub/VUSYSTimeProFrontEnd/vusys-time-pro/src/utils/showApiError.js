import ToastService from "../services/toastService";

// ============================================================
// COMMON API ERROR TOAST HANDLER
// ============================================================

export const showApiError = (error, fallback) => {
  if (
    !error?._globalToastHandled &&
    !error?.__globalToastHandled &&
    !error?.isApiValidationError
  ) {
    ToastService.error(error?.message || fallback);
  }
};
