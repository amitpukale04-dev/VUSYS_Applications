// src/services/otpService.js

import axios from "axios";
import { otp } from "../utils/url";
import { handleApiError } from "./axiosSetup";
import {
  getApiResponseFirstRow,
  getApiResponseStatus,
} from "../utils/apiResponseUtils";
import { COMMON_CONSTANTS, API_RESPONSE_TABLE } from "../constants/constants";

export const generateOrResendOtp = async (emailID) => {
  try {
    const response = await axios.post(otp.generate_or_resend_otp, {
      emailID,
    });
    const payload = response?.data || {};
    const details = getApiResponseFirstRow(
      payload,
      API_RESPONSE_TABLE.OTP_SENT_RESPONSE,
    );
    const status = getApiResponseStatus(payload);

    const message =
      details?.Message ||
      details?.message ||
      payload?.Message ||
      payload?.message ||
      "";

    const encodedOtp = details?.otp || details?.OTP || "";

    if (status !== COMMON_CONSTANTS.SUCCESS) {
      const error = new Error(message || "Failed To Send OTP !!!");
      error.responseData = payload;
      error.otpResponse = details;
      throw error;
    }

    return {
      success: true,
      emailID: details?.ID || details?.EmailID || emailID,
      otp: encodedOtp,
      message: message || "OTP Has Been Sent To Your Email ID !!!",
      raw: payload,
    };
  } catch (error) {
    /*
     * axiosSetup.js already handles:
     *
     * - ValidationErrors
     * - Network/CORS errors
     * - GLOBAL_ERROR_RESPONSE
     *
     * Business errors thrown above are passed back
     * to the page so the page can display the API message.
     */
    if (error?.isApiValidationError || error?.__globalToastHandled) {
      throw error;
    }

    /*
     * If this is our business-response error,
     * preserve it instead of replacing it.
     */
    if (error?.responseData || error?.otpResponse) {
      throw error;
    }

    handleApiError(error);
  }
};
