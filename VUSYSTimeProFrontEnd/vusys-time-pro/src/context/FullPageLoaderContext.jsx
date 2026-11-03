import { createContext, useCallback, useContext, useMemo, useState } from "react";

const FullPageLoaderContext = createContext(null);

const DEFAULT_MESSAGE = "Saving, please wait...";

export function FullPageLoaderProvider({ children }) {
  const [requests, setRequests] = useState([]);

  const showFullPageLoader = useCallback((message = DEFAULT_MESSAGE) => {
    const requestId = `${Date.now()}-${Math.random()}`;

    setRequests((current) => [
      ...current,
      {
        id: requestId,
        message: String(message || DEFAULT_MESSAGE),
      },
    ]);

    return requestId;
  }, []);

  const hideFullPageLoader = useCallback((requestId) => {
    setRequests((current) => {
      if (!current.length) return current;

      if (!requestId) {
        return current.slice(0, -1);
      }

      return current.filter((item) => item.id !== requestId);
    });
  }, []);

  const runWithFullPageLoader = useCallback(
    async (action, message = DEFAULT_MESSAGE) => {
      const requestId = showFullPageLoader(message);

      try {
        return await action();
      } finally {
        hideFullPageLoader(requestId);
      }
    },
    [hideFullPageLoader, showFullPageLoader],
  );

  const value = useMemo(
    () => ({
      isLoading: requests.length > 0,
      message: requests.length
        ? requests[requests.length - 1].message
        : DEFAULT_MESSAGE,
      showFullPageLoader,
      hideFullPageLoader,
      runWithFullPageLoader,
    }),
    [
      hideFullPageLoader,
      requests,
      runWithFullPageLoader,
      showFullPageLoader,
    ],
  );

  return (
    <FullPageLoaderContext.Provider value={value}>
      {children}
    </FullPageLoaderContext.Provider>
  );
}

export const useFullPageLoader = () => {
  const context = useContext(FullPageLoaderContext);

  if (!context) {
    throw new Error(
      "useFullPageLoader must be used inside FullPageLoaderProvider.",
    );
  }

  return context;
};
