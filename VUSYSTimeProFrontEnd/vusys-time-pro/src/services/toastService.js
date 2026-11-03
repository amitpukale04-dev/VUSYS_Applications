import { createElement } from "react";
import { toast } from "react-toastify";
import { COMMON_CONSTANTS, TOAST_POSITION } from "../constants/constants";

const createToastContent = (message, title = "") => {
  // If title is not passed, preserve the existing toast behavior.
  if (!title) {
    return message;
  }

  // Since this file is toastService.js and not .jsx,
  // use createElement instead of JSX.
  return createElement(
    "div",
    {
      className: "tp-toast-content",
    },

    createElement(
      "div",
      {
        className: "tp-toast-title",
      },
      title,
    ),

    createElement(
      "div",
      {
        className: "tp-toast-message",
      },
      message,
    ),
  );
};

const show = (type, message, titleOrOptions = "", options = {}) => {
  let title = "";
  let finalOptions = options;

  /*
   * Backward compatibility:
   *
   * Existing usage:
   *
   * ToastService.error(
   *   "Something went wrong.",
   *   {
   *     autoClose: 5000
   *   }
   * );
   *
   * New usage:
   *
   * ToastService.error(
   *   "1. Enter The Email ID.",
   *   "API Validations"
   * );
   */

  if (
    titleOrOptions &&
    typeof titleOrOptions === "object" &&
    !Array.isArray(titleOrOptions)
  ) {
    finalOptions = titleOrOptions;
  } else {
    title = titleOrOptions || "";
  }

  const position = finalOptions.position || TOAST_POSITION.TOP_CENTER;

  const className = [
    "tp-toast",
    `tp-toast-${type}`,

    title ? "tp-toast-with-title" : "",

    position === TOAST_POSITION.CENTER_CENTER ? "tp-toast-center" : "",
  ]
    .filter(Boolean)
    .join(" ");

  const toastOptions = {
    autoClose: finalOptions.autoClose ?? 3000,
    closeOnClick: finalOptions.closeOnClick ?? true,
    pauseOnHover: finalOptions.pauseOnHover ?? true,
    draggable: finalOptions.draggable ?? true,
    hideProgressBar: finalOptions.hideProgressBar ?? false,
    className,
    progressClassName: `tp-toast-progress tp-toast-progress-${type}`,
    ...finalOptions,

    position:
      position === TOAST_POSITION.CENTER_CENTER
        ? TOAST_POSITION.TOP_CENTER
        : position,
  };

  const toastContent = createToastContent(message, title);
  const toastMethod = type === "information" ? toast.info : toast[type];
  return toastMethod(toastContent, toastOptions);
};

export const ToastService = {
  success: (message, titleOrOptions = "Time-Pro", options = {}) =>
    show(COMMON_CONSTANTS.SUCCESS, message, titleOrOptions, options),

  error: (message, titleOrOptions = "Time-Pro", options = {}) =>
    show(COMMON_CONSTANTS.ERROR, message, titleOrOptions, options),

  warning: (message, titleOrOptions = "Time-Pro", options = {}) =>
    show(COMMON_CONSTANTS.WARNING, message, titleOrOptions, options),

  information: (message, titleOrOptions = "Time-Pro", options = {}) =>
    show(COMMON_CONSTANTS.INFORMATION, message, titleOrOptions, options),

  dismiss: (toastId) => {
    toast.dismiss(toastId);
  },

  dismissAll: () => {
    toast.dismiss();
  },
};

export default ToastService;
