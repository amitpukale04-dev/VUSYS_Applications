export async function detectNetworkOrCorsError(apiUrl) {
  let networkOk = false;

  try {
    await fetch(apiUrl, {
      method: "GET",
      mode: "no-cors",
    });

    // fetch() didn't throw → reachable
    networkOk = true;
  } catch (err) {
    // Detect SSL Certificate problems (Chrome)
    if (
      err?.message?.includes("SSL") ||
      err?.message?.includes("certificate") ||
      err?.message?.includes("ERR_CERT")
    ) {
      return {
        isCors: false,
        isSsl: true,
        message:
          "SSL Certificate error. Please check if the API's HTTPS certificate is valid.",
      };
    }

    // fetch() threw → Network down or service offline
    networkOk = false;
  }

  if (networkOk) {
    const origin = window.location.origin;
    return {
      isCors: true,
      isSsl: false,
      message: `The Request Was Blocked Due To CORS policy. Ensure The API Allows This '${origin}' Origin.`,
    };
  }

  return {
    isCors: false,
    isSsl: false,
    message: "API Service is not running or unable to connect.",
  };
}
