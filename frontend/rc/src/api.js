export const API_BASE_URL = process.env.REACT_APP_API_URL || "http://localhost:5278";

async function request(path, options = {}) {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(options.headers || {}),
    },
    ...options,
  });

  if (!response.ok) {
    let message = `HTTP ${response.status}`;

    try {
      const errorData = await response.json();
      message = errorData.message || errorData.error || JSON.stringify(errorData);
    } catch {
      try {
        message = await response.text();
      } catch {
        // ignore
      }
    }

    throw new Error(message || `HTTP ${response.status}`);
  }

  if (response.status === 204) {
    return null;
  }

  const text = await response.text();
  return text ? JSON.parse(text) : null;
}

export const cognitiveApi = {
  baseUrl: API_BASE_URL,

  getDashboard() {
    return request("/api/dashboard");
  },

  getBodyPoints() {
    return request("/api/body-points");
  },

  getExercises() {
    return request("/api/exercises");
  },

  getExerciseTemplates() {
    return request("/api/exercise-templates");
  },

  // алиас для страниц, где функция называется getTemplates
  getTemplates() {
    return request("/api/exercise-templates");
  },

  getSessions() {
    return request("/api/sessions");
  },

  getSession(id) {
    return request(`/api/sessions/${id}`);
  },

  createDemoSession() {
    return request("/api/demo/seed-session", {
      method: "POST",
    });
  },

  importUnitySession(json) {
    return request("/api/sessions/import-json", {
      method: "POST",
      body: JSON.stringify(json),
    });
  },

  login(data) {
    return request("/api/auth/login", {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  register(data) {
    return request("/api/auth/register", {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  me() {
    return request("/api/auth/me");
  },

  getPatients() {
    return request("/api/patients");
  },

  getPatient(id) {
    return request(`/api/patients/${id}`);
  },

  createPatient(data) {
    return request("/api/patients", {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  updatePatient(id, data) {
    return request(`/api/patients/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },

  deletePatient(id) {
    return request(`/api/patients/${id}`, {
      method: "DELETE",
    });
  },
};

export default cognitiveApi;
