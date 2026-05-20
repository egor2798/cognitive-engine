const api = "";

const metricLabels = {
  meanDeviationMm: "Среднее отклонение",
  maxDeviationMm: "Макс. отклонение",
  rmseDeviationMm: "RMSE",
  timeOutsideSec: "Время вне зоны",
  outsidePercent: "% вне зоны",
  outsideEpisodesCount: "Выходы",
  lagTimeSec: "Отставание",
  leadTimeSec: "Опережение",
  meanPointerSpeedMmS: "Средняя скорость",
  maxPointerSpeedMmS: "Макс. скорость",
  pacemakerPathLengthMm: "Путь пейсмейкера",
  meanPacemakerSpeedMmS: "Скорость пейсмейкера"
};

async function getJson(url) {
  const res = await fetch(api + url);
  if (!res.ok) throw new Error(`${url}: ${res.status}`);
  return await res.json();
}

function fmt(n) {
  if (n === null || n === undefined) return "—";
  const num = Number(n);
  return Number.isFinite(num) ? num.toLocaleString("ru-RU", { maximumFractionDigits: 2 }) : n;
}

async function loadDashboard() {
  const root = document.getElementById("dashboardCards");
  try {
    const d = await getJson("/api/dashboard");
    const cards = [
      ["Точки тела", d.bodyPoints],
      ["Упражнения", d.exercises],
      ["Сессии", d.sessions],
      ["Samples", d.samples],
      ["События", d.events],
      ["Метрики", d.metrics],
      ["Пациенты", d.patients],
      ["Шаблоны", d.templates]
    ];
    root.innerHTML = cards.map(([label, value]) => `
      <div class="card"><div class="label">${label}</div><div class="value">${fmt(value)}</div></div>
    `).join("");
  } catch (e) {
    root.innerHTML = `<div class="card error">Не удалось загрузить dashboard: ${e.message}</div>`;
  }
}

async function loadBodyPoints() {
  const root = document.getElementById("bodyPoints");
  root.innerHTML = "Загрузка...";
  try {
    const items = await getJson("/api/body-points");
    root.innerHTML = items.map(p => `
      <div class="point">
        <div class="point-code">${p.code}</div>
        <div class="point-name">${p.name}</div>
        <div class="point-meta">side=${p.side}, group=${p.group}, order=${p.orderIndex}</div>
      </div>
    `).join("");
  } catch (e) {
    root.innerHTML = `<span class="error">Ошибка: ${e.message}</span>`;
  }
}

async function loadExercises() {
  const root = document.getElementById("exercises");
  root.innerHTML = "Загрузка...";
  try {
    const items = await getJson("/api/exercises");
    root.innerHTML = items.length ? items.map(x => `
      <div class="row">
        <div class="row-title">${x.name}</div>
        <div class="row-meta">exerciseId=${x.id}, templateId=${x.templateId}, patientId=${x.patientId}, status=${x.status}</div>
      </div>
    `).join("") : `<div class="row">Пока нет упражнений. Запусти seed.sql.</div>`;
  } catch (e) {
    root.innerHTML = `<span class="error">Ошибка: ${e.message}</span>`;
  }
}

async function loadSessions() {
  const root = document.getElementById("sessions");
  root.innerHTML = "Загрузка...";
  try {
    const items = await getJson("/api/sessions");
    root.innerHTML = items.length ? items.map(s => `
      <div class="row">
        <div class="row-title">Сессия #${s.id}: ${s.exerciseName}</div>
        <div class="row-meta">patient=${s.patientCode}, duration=${fmt(s.durationSec)} c, status=${s.status}</div>
        <div class="metrics">
          ${(s.metrics || []).slice(0, 8).map(m => `<span class="metric">${metricLabels[m.code] || m.code}: ${fmt(m.value)} ${m.unit || ""}</span>`).join("")}
        </div>
      </div>
    `).join("") : `<div class="row">Сессий пока нет. Нажми “Создать демо-сессию” или импортируй session_result.json.</div>`;
  } catch (e) {
    root.innerHTML = `<span class="error">Ошибка: ${e.message}</span>`;
  }
}

async function importJson() {
  const input = document.getElementById("jsonFile");
  const output = document.getElementById("importResult");
  if (!input.files.length) {
    output.textContent = "Выбери session_result.json";
    return;
  }
  try {
    const text = await input.files[0].text();
    const parsed = JSON.parse(text);
    const res = await fetch("/api/sessions/import-json", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ source: "web-upload", resultJson: parsed })
    });
    const data = await res.json();
    output.textContent = JSON.stringify(data, null, 2);
    await refreshAll();
  } catch (e) {
    output.textContent = "Ошибка импорта: " + e.message;
  }
}

async function createDemoSession() {
  const output = document.getElementById("importResult");
  try {
    const res = await fetch("/api/demo/seed-session", { method: "POST" });
    const data = await res.json();
    output.textContent = JSON.stringify(data, null, 2);
    await refreshAll();
  } catch (e) {
    output.textContent = "Ошибка: " + e.message;
  }
}

async function refreshAll() {
  await Promise.all([loadDashboard(), loadBodyPoints(), loadExercises(), loadSessions()]);
}

refreshAll();
