// src/utils/fileExportUtils.js

const DEFAULT_FILE_NAME = "download";
const DEFAULT_CONTENT_TYPE = "application/octet-stream";

const CONTENT_TYPE_EXTENSION_MAP = Object.freeze({
  "application/pdf": ".pdf",
  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": ".xlsx",
  "application/vnd.ms-excel": ".xls",
  "text/csv": ".csv",
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document": ".docx",
  "application/msword": ".doc",
  "application/vnd.openxmlformats-officedocument.presentationml.presentation": ".pptx",
  "application/vnd.ms-powerpoint": ".ppt",
  "application/zip": ".zip",
  "application/x-zip-compressed": ".zip",
  "application/json": ".json",
  "text/plain": ".txt",
  "text/html": ".html",
  "image/png": ".png",
  "image/jpeg": ".jpg",
  "image/jpg": ".jpg",
  "image/gif": ".gif",
  "image/webp": ".webp",
  "image/bmp": ".bmp",
  "image/svg+xml": ".svg",
  "video/mp4": ".mp4",
  "video/mpeg": ".mpeg",
  "video/webm": ".webm",
  "audio/mpeg": ".mp3",
  "audio/wav": ".wav",
});

const normalizeContentType = (contentType = "") =>
  String(contentType || "")
    .split(";")[0]
    .trim()
    .toLowerCase();

const hasFileExtension = (fileName = "") => {
  const normalizedFileName = String(fileName || "").trim();
  const lastSlashIndex = Math.max(
    normalizedFileName.lastIndexOf("/"),
    normalizedFileName.lastIndexOf("\\"),
  );
  const lastDotIndex = normalizedFileName.lastIndexOf(".");

  return lastDotIndex > lastSlashIndex + 0 && lastDotIndex < normalizedFileName.length - 1;
};

const getDataUrlParts = (value = "") => {
  const normalizedValue = String(value || "").trim();

  if (!normalizedValue.toLowerCase().startsWith("data:")) {
    return null;
  }

  const commaIndex = normalizedValue.indexOf(",");

  if (commaIndex < 0) {
    return null;
  }

  const metadata = normalizedValue.slice(5, commaIndex);
  const base64Content = normalizedValue.slice(commaIndex + 1);
  const metadataParts = metadata.split(";");
  const contentType = normalizeContentType(metadataParts[0]);
  const isBase64 = metadataParts.some(
    (part) => String(part || "").trim().toLowerCase() === "base64",
  );

  return {
    contentType,
    base64Content,
    isBase64,
  };
};

/**
 * Returns the common file extension for a MIME/content type.
 *
 * Examples:
 * application/pdf => .pdf
 * application/vnd.openxmlformats-officedocument.spreadsheetml.sheet => .xlsx
 * image/png => .png
 */
export const getFileExtensionFromContentType = (contentType = "") => {
  const normalizedContentType = normalizeContentType(contentType);

  return CONTENT_TYPE_EXTENSION_MAP[normalizedContentType] || "";
};

/**
 * Builds a file name and appends an extension based on contentType only when
 * the supplied file name does not already contain an extension.
 */
export const getExportFileName = (
  fileName = DEFAULT_FILE_NAME,
  contentType = DEFAULT_CONTENT_TYPE,
) => {
  const normalizedFileName = String(fileName || "").trim() || DEFAULT_FILE_NAME;

  if (hasFileExtension(normalizedFileName)) {
    return normalizedFileName;
  }

  return `${normalizedFileName}${getFileExtensionFromContentType(contentType)}`;
};

/**
 * Exports/downloads a Base64 file in the browser.
 *
 * Supported inputs:
 * - raw Base64 content
 * - Base64 data URL (data:application/pdf;base64,...)
 *
 * The supplied contentType controls the Blob MIME type and is also used to
 * append the appropriate file extension when fileName has no extension.
 * When a data URL is supplied, its MIME type is used if contentType is blank.
 *
 * Example:
 * exportBase64File({
 *   base64Content: response.FileContent,
 *   contentType: response.ContentType,
 *   fileName: response.FileName,
 * });
 *
 * @param {Object} params
 * @param {string} params.base64Content Base64 string or Base64 data URL.
 * @param {string} [params.contentType] MIME/content type of the file.
 * @param {string} [params.fileName] Desired download file name.
 * @returns {{ fileName: string, contentType: string, size: number }}
 */
export const exportBase64File = ({
  base64Content,
  contentType = "",
  fileName = DEFAULT_FILE_NAME,
} = {}) => {
  const source = String(base64Content || "").trim();

  if (!source) {
    throw new Error("Base64 file content is required.");
  }

  if (typeof window === "undefined" || typeof document === "undefined") {
    throw new Error("File export is available only in the browser.");
  }

  const dataUrlParts = getDataUrlParts(source);

  if (dataUrlParts && !dataUrlParts.isBase64) {
    throw new Error("The supplied data URL is not Base64 encoded.");
  }

  const resolvedContentType =
    normalizeContentType(contentType) ||
    dataUrlParts?.contentType ||
    DEFAULT_CONTENT_TYPE;

  const resolvedFileName = getExportFileName(fileName, resolvedContentType);

  const normalizedBase64 = String(
    dataUrlParts?.base64Content ?? source,
  ).replace(/\s/g, "");

  let binaryString;

  try {
    binaryString = window.atob(normalizedBase64);
  } catch {
    throw new Error("Invalid Base64 file content.");
  }

  const byteLength = binaryString.length;
  const bytes = new Uint8Array(byteLength);

  for (let index = 0; index < byteLength; index += 1) {
    bytes[index] = binaryString.charCodeAt(index);
  }

  const blob = new Blob([bytes], {
    type: resolvedContentType,
  });

  const objectUrl = URL.createObjectURL(blob);
  const anchor = document.createElement("a");

  anchor.href = objectUrl;
  anchor.download = resolvedFileName;
  anchor.style.display = "none";

  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();

  window.setTimeout(() => {
    URL.revokeObjectURL(objectUrl);
  }, 0);

  return {
    fileName: resolvedFileName,
    contentType: resolvedContentType,
    size: blob.size,
  };
};
