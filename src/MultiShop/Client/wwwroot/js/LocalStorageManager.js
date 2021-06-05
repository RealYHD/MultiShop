const APP_STORAGE_KEY = "MultiShop"

export function save(key, value) {
    try {
        localStorage.setItem(APP_STORAGE_KEY + "/" + key, JSON.stringify(value));
    } catch (error) {
        console.warn("Failed to save value with key: " + key);
        return false;
    }
    return true;
}

export function retrieve(key) {
    let value = localStorage.getItem(APP_STORAGE_KEY + "/" + key);
    if (value == null) return null;
    try {
        return JSON.parse(value);
    } catch (error) {
        if (error instanceof SyntaxError) {
            console.warn("Unable to parse value for key \"" + key + "\". Removing.");
            remove(key);
            return null;
        }
    }
}

export function remove(key) {
    localStorage.removeItem(APP_STORAGE_KEY + "/" + key);
}

export function clear() {
    for (let i = 0; i < localStorage.length; i++) {
        let name = localStorage.key(i);
        if (name.startsWith(APP_STORAGE_KEY + "/")) {
            localStorage.removeItem(name);
        }
    }
}