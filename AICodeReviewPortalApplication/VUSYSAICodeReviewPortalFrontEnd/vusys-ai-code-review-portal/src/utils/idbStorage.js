// src/utils/idbStorage.js
// IndexedDB Key-Value storage helpers (Option C)

const DB_NAME = "time_pro_app";
const STORE_NAME = "kv1";
const DB_VERSION = 1;

function openDb() {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open(DB_NAME, DB_VERSION);

    req.onupgradeneeded = () => {
      const db = req.result;
      if (!db.objectStoreNames.contains(STORE_NAME)) {
        db.createObjectStore(STORE_NAME);
      }
    };

    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

export async function idbSet(key, value) {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(STORE_NAME, "readwrite");
    tx.objectStore(STORE_NAME).put(value, key);

    tx.oncomplete = () => {
      try {
        db.close();
      } catch (e) {
        void e;
      }
      resolve(true);
    };

    tx.onerror = () => {
      try {
        db.close();
      } catch (e) {
        void e;
      }
      reject(tx.error);
    };
  });
}

export async function idbGet(key) {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(STORE_NAME, "readonly");
    const req = tx.objectStore(STORE_NAME).get(key);

    req.onsuccess = () => {
      try {
        db.close();
      } catch (e) {
        void e;
      }
      resolve(req.result ?? null);
    };

    req.onerror = () => {
      try {
        db.close();
      } catch (e) {
        void e;
      }
      reject(req.error);
    };
  });
}

export async function idbDel(key) {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(STORE_NAME, "readwrite");
    tx.objectStore(STORE_NAME).delete(key);

    tx.oncomplete = () => {
      try {
        db.close();
      } catch (e) {
        void e;
      }
      resolve(true);
    };

    tx.onerror = () => {
      try {
        db.close();
      } catch (e) {
        void e;
      }
      reject(tx.error);
    };
  });
}

// Optional: clear everything (not used by default)
export async function idbClearAll() {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(STORE_NAME, "readwrite");
    tx.objectStore(STORE_NAME).clear();

    tx.oncomplete = () => {
      try {
        db.close();
      } catch (e) {
        void e;
      }
      resolve(true);
    };

    tx.onerror = () => {
      try {
        db.close();
      } catch (e) {
        void e;
      }
      reject(tx.error);
    };
  });
}
