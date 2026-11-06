const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5080";

const TOKEN_KEY = "authToken";
const EXPIRES_KEY = "authExpiresAt";
const USER_KEY = "user";

export function getAuthToken() {
  const token = localStorage.getItem(TOKEN_KEY);
  if (!token) {
    return null;
  }

  const expiresAt = localStorage.getItem(EXPIRES_KEY);
  if (expiresAt && Date.parse(expiresAt) <= Date.now()) {
    clearAuthSession();
    return null;
  }

  return token;
}

export function getAuthUser() {
  const token = getAuthToken();
  if (!token) {
    return null;
  }

  try {
    // UI hints come from the JWT, never the editable cached user.roles value.
    // Signature validation and current database permissions remain server-side.
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    const payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const bytes = Uint8Array.from(atob(payload.padEnd(Math.ceil(payload.length / 4) * 4, '=')), (c) => c.charCodeAt(0));
    const claims = JSON.parse(new TextDecoder().decode(bytes));
    if (!Number.isFinite(claims.exp) || claims.exp * 1000 <= Date.now()) return null;
    if (claims.nbf && claims.nbf * 1000 > Date.now()) return null;
    const roles = claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? claims.role ?? [];
    const raw = localStorage.getItem(USER_KEY);
    let cached = {};
    try { cached = raw ? JSON.parse(raw) : {}; } catch { /* Ignore corrupt display metadata. */ }
    return {
      ...cached,
      id: claims.sub || claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
      email: claims.email || claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || '',
      roles: (Array.isArray(roles) ? roles : [roles]).filter((role) => typeof role === 'string'),
      assignedHotelId: Number(claims.assignedHotelId) || null,
    };
  } catch {
    return null;
  }
}

const STAFF_ROLES = ["SuperAdmin", "Admin", "HotelManager", "Receptionist"];

export function isAdminSession() {
  const roles = getAuthUser()?.roles;
  return Array.isArray(roles) && roles.some((role) => STAFF_ROLES.includes(role));
}

export function hasRole(...allowed) {
  const roles = getAuthUser()?.roles;
  return Array.isArray(roles) && roles.some((role) => allowed.includes(role));
}

export function canManageUser(target) {
  if (!target || !hasRole('SuperAdmin', 'Admin')) return false;
  const roles = Array.isArray(target.roles) ? target.roles : [target.role];
  return hasRole('SuperAdmin') || !roles.includes('SuperAdmin');
}

export function getPostLoginPath(roles) {
  const list = Array.isArray(roles) ? roles : [];
  const isReceptionOnly =
    list.includes("Receptionist") &&
    !list.some((role) => ["SuperAdmin", "Admin", "HotelManager"].includes(role));
  if (isReceptionOnly) return "/admin/resepsiyon";
  if (list.some((role) => ["SuperAdmin", "Admin", "HotelManager"].includes(role))) {
    return "/admin";
  }
  return "/dashboard";
}

export function getSafeAdminPath(next) {
  if (typeof next !== "string" || !next.startsWith("/admin")) {
    return null;
  }
  if (next.startsWith("//") || next.includes("://")) {
    return null;
  }
  return next;
}

export function clearAuthSession() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(EXPIRES_KEY);
  localStorage.removeItem(USER_KEY);
  localStorage.removeItem("isAuthenticated");
  localStorage.removeItem("rememberMe");
  
  // Notify App.jsx to reload favorites for guest
  window.dispatchEvent(new Event("authChange"));
}

export function saveAuthSession(auth, { rememberMe = false } = {}) {
  if (!auth?.token) {
    throw new Error("Token tapılmadı.");
  }

  localStorage.setItem(TOKEN_KEY, auth.token);
  if (auth.expiresAt) {
    localStorage.setItem(EXPIRES_KEY, auth.expiresAt);
  }

  localStorage.setItem(
    USER_KEY,
    JSON.stringify({
      email: auth.email,
      name: auth.name,
      roles: Array.isArray(auth.roles) ? auth.roles : [],
      assignedHotelId: auth.assignedHotelId || null,
    })
  );

  // Header UI hint only — API auth uses the JWT, not this flag.
  localStorage.setItem("isAuthenticated", "true");
  if (rememberMe) {
    localStorage.setItem("rememberMe", "true");
  } else {
    localStorage.removeItem("rememberMe");
  }

  window.dispatchEvent(new Event("user-updated"));
  // Notify App.jsx to reload favorites for logged-in user
  window.dispatchEvent(new Event("authChange"));
}

async function postAuth(path, body) {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: "POST",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
  });

  const data = await response.json().catch(() => ({}));
  if (!response.ok) {
    throw new Error(data.message || `Sorğu alınmadı (${response.status})`);
  }

  return data;
}

export function loginRequest(email, password) {
  return postAuth("/api/auth/login", { email, password });
}

export function registerRequest(payload) {
  return postAuth("/api/auth/register", payload);
}

export function forgotPasswordRequest(email) {
  return postAuth("/api/auth/forgot-password", { email });
}

export function resetPasswordRequest({ email, token, newPassword }) {
  return postAuth("/api/auth/reset-password", { email, token, newPassword });
}
