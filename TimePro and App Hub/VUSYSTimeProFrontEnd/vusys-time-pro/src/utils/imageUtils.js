// src/utils/imageUtils.js

const getImageContentType = (fileName = "") => {
  const normalized = String(fileName || "")
    .trim()
    .toLowerCase();

  if (normalized.endsWith(".png")) return "image/png";
  if (normalized.endsWith(".gif")) return "image/gif";
  if (normalized.endsWith(".webp")) return "image/webp";
  if (normalized.endsWith(".bmp")) return "image/bmp";
  if (normalized.endsWith(".svg")) return "image/svg+xml";
  if (normalized.endsWith(".jpg") || normalized.endsWith(".jpeg")) {
    return "image/jpeg";
  }

  return "image/jpeg";
};

/**
 * Converts a base64 string or URL into a valid <img src> value.
 *
 * Supported inputs:
 * - data URLs (data:image/...)
 * - http / https URLs
 * - raw base64 strings
 *
 * When a file name is supplied, its extension is used to select the
 * appropriate MIME type for raw base64 content.
 */
export const toImgSrcFromBase64 = (val, fileName = "") => {
  if (!val) return "";

  const s = String(val).trim();
  if (!s) return "";

  if (s.startsWith("data:") || s.startsWith("http")) {
    return s;
  }

  return `data:${getImageContentType(fileName)};base64,${s}`;
};
