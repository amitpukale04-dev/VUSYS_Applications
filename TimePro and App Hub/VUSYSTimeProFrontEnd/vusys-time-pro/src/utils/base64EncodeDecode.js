/**
 * Generic Base64 utility (UTF-8 safe, URL-safe, modern)
 */
export const b64Encode = (input) => {
  try {
    if (input == null) return "";

    // Convert string → UTF-8 bytes
    const utf8Bytes = new TextEncoder().encode(String(input));

    // Convert bytes → base64
    let binary = "";
    utf8Bytes.forEach((b) => (binary += String.fromCharCode(b)));
    const base64 = btoa(binary);

    // Make URL-safe and remove padding
    return base64.replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
  } catch {
    return "";
  }
};

export const b64Decode = (maybeUrlSafe) => {
  try {
    if (!maybeUrlSafe) return "";

    // Normalize URL-safe to standard Base64
    const normalized = maybeUrlSafe.replace(/-/g, "+").replace(/_/g, "/");
    const padLen = normalized.length % 4;
    const padded = padLen ? normalized + "=".repeat(4 - padLen) : normalized;

    // Decode base64 → binary string
    const binary = atob(padded);

    // Convert binary → UTF-8 bytes
    const bytes = Uint8Array.from(binary, (c) => c.charCodeAt(0));

    // Decode UTF-8 bytes → string
    return new TextDecoder().decode(bytes);
  } catch {
    return "";
  }
};
