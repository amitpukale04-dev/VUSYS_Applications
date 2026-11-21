// src/utils/fileExportUtils.js
import { CONTENT_TYPE_CONSTANTS } from "../constants/ContentTypeConstants";
import { FILE_EXTENSION_CONSTANTS } from "../constants/FileExtensionConstants";

const DEFAULT_FILE_NAME = "download";
const DEFAULT_CONTENT_TYPE = CONTENT_TYPE_CONSTANTS.DEFAULT;

const CONTENT_TYPE_EXTENSION_MAP = Object.freeze({
  [CONTENT_TYPE_CONSTANTS.PDF]: FILE_EXTENSION_CONSTANTS.PDF,
  [CONTENT_TYPE_CONSTANTS.EXCEL_XLSX]: FILE_EXTENSION_CONSTANTS.EXCEL_XLSX,
  [CONTENT_TYPE_CONSTANTS.EXCEL_XLS]: FILE_EXTENSION_CONSTANTS.EXCEL_XLS,
  [CONTENT_TYPE_CONSTANTS.CSV]: FILE_EXTENSION_CONSTANTS.CSV,
  [CONTENT_TYPE_CONSTANTS.WORD_DOCX]: FILE_EXTENSION_CONSTANTS.WORD_DOCX,
  [CONTENT_TYPE_CONSTANTS.WORD_DOC]: FILE_EXTENSION_CONSTANTS.WORD_DOC,
  [CONTENT_TYPE_CONSTANTS.POWERPOINT_PPTX]:
    FILE_EXTENSION_CONSTANTS.POWERPOINT_PPTX,
  [CONTENT_TYPE_CONSTANTS.POWERPOINT_PPT]:
    FILE_EXTENSION_CONSTANTS.POWERPOINT_PPT,
  [CONTENT_TYPE_CONSTANTS.ZIP]: FILE_EXTENSION_CONSTANTS.ZIP,
  [CONTENT_TYPE_CONSTANTS.ZIP_COMPRESSED]: FILE_EXTENSION_CONSTANTS.ZIP,
  [CONTENT_TYPE_CONSTANTS.JSON]: FILE_EXTENSION_CONSTANTS.JSON,
  [CONTENT_TYPE_CONSTANTS.TEXT]: FILE_EXTENSION_CONSTANTS.TEXT,
  [CONTENT_TYPE_CONSTANTS.HTML]: FILE_EXTENSION_CONSTANTS.HTML,
  [CONTENT_TYPE_CONSTANTS.PNG]: FILE_EXTENSION_CONSTANTS.PNG,
  [CONTENT_TYPE_CONSTANTS.JPEG]: FILE_EXTENSION_CONSTANTS.JPEG,
  [CONTENT_TYPE_CONSTANTS.JPG]: FILE_EXTENSION_CONSTANTS.JPEG,
  [CONTENT_TYPE_CONSTANTS.GIF]: FILE_EXTENSION_CONSTANTS.GIF,
  [CONTENT_TYPE_CONSTANTS.WEBP]: FILE_EXTENSION_CONSTANTS.WEBP,
  [CONTENT_TYPE_CONSTANTS.BMP]: FILE_EXTENSION_CONSTANTS.BMP,
  [CONTENT_TYPE_CONSTANTS.SVG]: FILE_EXTENSION_CONSTANTS.SVG,
  [CONTENT_TYPE_CONSTANTS.MP4]: FILE_EXTENSION_CONSTANTS.MP4,
  [CONTENT_TYPE_CONSTANTS.MPEG_VIDEO]: FILE_EXTENSION_CONSTANTS.MPEG_VIDEO,
  [CONTENT_TYPE_CONSTANTS.WEBM]: FILE_EXTENSION_CONSTANTS.WEBM,
  [CONTENT_TYPE_CONSTANTS.MP3]: FILE_EXTENSION_CONSTANTS.MP3,
  [CONTENT_TYPE_CONSTANTS.WAV]: FILE_EXTENSION_CONSTANTS.WAV,
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

  return (
    lastDotIndex > lastSlashIndex + 0 &&
    lastDotIndex < normalizedFileName.length - 1
  );
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
    (part) =>
      String(part || "")
        .trim()
        .toLowerCase() === "base64",
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
