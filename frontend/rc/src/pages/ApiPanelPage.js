import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { API_BASE_URL, cognitiveApi } from "../api";
import "./ApiPanelPage.css";

function StatCard({ label, value, hint }) {
  return (
    <div className="api-stat-card">
      <div className="api-stat-label">{label}</div>
      <div className="api-stat-value">{value ?? 0}</div>
      {hint ? <div className="api-stat-hint">{hint}</div> : null}
    </div>
  );
}

const METRIC_LABELS = {
  meanDeviationMm: "Среднее отклонение",
  maxDeviationMm: "Максимальное отклонение",
  rmseDeviationMm: "Среднеквадратичное отклонение",
  timeInsideSec: "Время внутри зоны",
  timeOutsideSec: "Время вне зоны",
  outsidePercent: "Процент времени вне зоны",
  outsideEpisodesCount: "Количество выходов за коридор",
  lagTimeSec: "Время отставания от пейсмейкера",
  leadTimeSec: "Время опережения пейсмейкера",
  meanPointerSpeedMmS: "Средняя скорость движения",
  maxPointerSpeedMmS: "Максимальная скорость движения",
  pointerPathLengthMm: "Длина траектории курсора",
  pacemakerPathLengthMm: "Длина пути пейсмейкера",
  meanPacemakerSpeedMmS: "Средняя скорость пейсмейкера",
  maxPacemakerSpeedMmS: "Максимальная скорость пейсмейкера",
  stopCount: "Количество остановок",
  longestStopSec: "Самая длинная остановка",
  totalErrorCount: "Общее количество ошибок",
  totalErrorTimeSec: "Суммарное время ошибок",
  dataQualityScore: "Качество данных",
  movementSmoothnessScore: "Плавность движения",
};

const UNIT_LABELS = {
  mm: "мм",
  "mm/s": "мм/с",
  mmS: "мм/с",
  sec: "с",
  s: "с",
  percent: "%",
  pct: "%",
  count: "раз",
};

const EVENT_TYPE_LABELS = {
  1: "Сессия начата",
  2: "Сессия завершена",
  3: "Пейсмейкер включён",
  4: "Пейсмейкер выключен",
  10: "Курсор вошёл в коридор",
  11: "Курсор вышел из коридора",
  12: "Курсор вернулся в коридор",
  20: "Потеря сигнала",
  21: "Сигнал восстановлен",
  62: "Сессия завершена",
};

function getMetricCode(metric) {
  return metric.code ?? metric.name ?? metric.metricCode ?? "metric";
}

function getMetricLabel(metric) {
  const code = getMetricCode(metric);
  return METRIC_LABELS[code] || code;
}

function getMetricUnit(metric) {
  const rawUnit = metric.unit ?? "";
  return UNIT_LABELS[rawUnit] || rawUnit;
}

function getMetricValue(metric) {
  const value = metric.value ?? metric.currentValue ?? metric.metricValue;
  return value ?? "—";
}

function formatMetric(metric) {
  const value = getMetricValue(metric);
  const unit = getMetricUnit(metric);
  return `${value}${unit ? ` ${unit}` : ""}`;
}

function MetricBadge({ metric }) {
  return (
    <div className="metric-badge">
      <span>{getMetricLabel(metric)}</span>
      <strong>{formatMetric(metric)}</strong>
    </div>
  );
}

function DataTable({ rows, columns, emptyText }) {
  if (!rows || rows.length === 0) return <p className="api-muted">{emptyText}</p>;
  return (
    <div className="table-wrap">
      <table className="api-table">
        <thead>
          <tr>{columns.map((c) => <th key={c.key}>{c.title}</th>)}</tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={row.id ?? `${row.sampleIndex ?? "row"}-${index}`}>
              {columns.map((c) => <td key={c.key}>{c.render ? c.render(row, index) : String(row[c.key] ?? "—")}</td>)}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function pickMetric(metrics, code) {
  const item = metrics?.find((m) => String(m.code).toLowerCase() === code.toLowerCase());
  if (!item) return "—";
  return formatMetric(item);
}


const PREVIEW_LABELS = {
  exercise: "Упражнение",
  duration: "Длительность",
  cursorSamples: "Samples курсора",
  pacerSamples: "Samples пейсмейкера",
  events: "События",
  meanDeviationMm: "Среднее отклонение",
  rmseDeviationMm: "RMSE",
  timeOutsideSec: "Время вне зоны",
  activeBodyPointId: "Активная точка тела",
  activeTrackerId: "Датчик",
  activeCursorId: "Курсор",
};

function previewLabel(key) {
  return PREVIEW_LABELS[key] || key;
}

function eventTypeLabel(type) {
  return EVENT_TYPE_LABELS[type] || String(type ?? "—");
}

function yesNo(value) {
  if (value === true || String(value).toLowerCase() === "true") return "да";
  if (value === false || String(value).toLowerCase() === "false") return "нет";
  return String(value ?? "—");
}

function ApiPanelPage() {
  const [dashboard, setDashboard] = useState(null);
  const [bodyPoints, setBodyPoints] = useState([]);
  const [exercises, setExercises] = useState([]);
  const [templates, setTemplates] = useState([]);
  const [sessions, setSessions] = useState([]);
  const [selectedSessionId, setSelectedSessionId] = useState(null);
  const [sessionDetails, setSessionDetails] = useState(null);
  const [selectedJson, setSelectedJson] = useState(null);
  const [selectedFileName, setSelectedFileName] = useState("");
  const [jsonPreview, setJsonPreview] = useState(null);
  const [status, setStatus] = useState("Готово к подключению API");
  const [loading, setLoading] = useState(false);

  const stats = useMemo(() => {
    const d = dashboard || {};
    return [
      ["Точки тела", d.bodyPointsCount ?? bodyPoints.length, "справочник"],
      ["Пациенты", d.patientsCount ?? 0, "карточки"],
      ["Шаблоны", d.templatesCount ?? templates.length, "exercise_config"],
      ["Упражнения", d.exercisesCount ?? exercises.length, "запуски"],
      ["Сессии", d.sessionsCount ?? sessions.length, "Unity results"],
      ["Samples", d.samplesCount ?? 0, "cursor/pacer"],
      ["События", d.eventsCount ?? 0, "timeline"],
      ["Метрики", d.metricsCount ?? 0, "итоги"],
    ];
  }, [dashboard, bodyPoints.length, templates.length, exercises.length, sessions.length]);

  async function loadAll() {
    setLoading(true);
    setStatus("Загрузка данных из API...");
    try {
      const [dashboardData, bodyPointsData, templatesData, exercisesData, sessionsData] = await Promise.all([
        cognitiveApi.getDashboard(),
        cognitiveApi.getBodyPoints(),
        cognitiveApi.getTemplates().catch(() => []),
        cognitiveApi.getExercises(),
        cognitiveApi.getSessions(),
      ]);
      setDashboard(dashboardData);
      setBodyPoints(bodyPointsData || []);
      setTemplates(templatesData || []);
      setExercises(exercisesData || []);
      setSessions(sessionsData || []);

      const firstId = sessionsData?.[0]?.id;
      if (firstId && !selectedSessionId) {
        setSelectedSessionId(firstId);
        await loadSession(firstId, false);
      }
      setStatus("Данные успешно получены из API/БД");
    } catch (error) {
      console.error(error);
      setStatus(`Ошибка подключения: ${error.message}. Проверь backend: ${API_BASE_URL}/swagger`);
    } finally {
      setLoading(false);
    }
  }

  async function loadSession(id, withLoading = true) {
    if (!id) return;
    if (withLoading) setLoading(true);
    try {
      const details = await cognitiveApi.getSession(id);
      setSelectedSessionId(id);
      setSessionDetails(details);
      setStatus(`Открыта сессия #${id}: samples=${details.samples?.length ?? 0}, events=${details.events?.length ?? 0}, metrics=${details.metrics?.length ?? 0}`);
    } catch (error) {
      console.error(error);
      setStatus(`Не удалось открыть сессию #${id}: ${error.message}`);
    } finally {
      if (withLoading) setLoading(false);
    }
  }

  useEffect(() => { loadAll(); }, []);

  async function createDemoSession() {
    setLoading(true);
    setStatus("Создание демо-сессии...");
    try {
      const created = await cognitiveApi.seedDemoSession();
      await loadAll();
      if (created?.id) await loadSession(created.id, false);
      setStatus("Демо-сессия создана и сохранена в БД");
    } catch (error) {
      console.error(error);
      setStatus(`Не удалось создать демо-сессию: ${error.message}`);
    } finally {
      setLoading(false);
    }
  }

  function handleFileChange(event) {
    const file = event.target.files?.[0];
    if (!file) {
      setSelectedJson(null);
      setSelectedFileName("");
      setJsonPreview(null);
      return;
    }
    const reader = new FileReader();
    reader.onload = () => {
      try {
        const parsed = JSON.parse(reader.result);
        setSelectedJson(parsed);
        setSelectedFileName(file.name);
        setJsonPreview({
          exercise: parsed.exerciseName ?? parsed.exercise?.name ?? parsed.name ?? "—",
          duration: parsed.durationSec ?? parsed.sessionTotalDurationSec ?? parsed.totalDurationSec ?? "—",
          cursorSamples: parsed.cursorSamples?.length ?? parsed.samples?.length ?? 0,
          pacerSamples: parsed.pacerSamples?.length ?? 0,
          events: parsed.events?.length ?? 0,
          meanDeviationMm: parsed.meanDeviationMm ?? "—",
          rmseDeviationMm: parsed.rmseDeviationMm ?? "—",
          timeOutsideSec: parsed.timeOutsideSec ?? "—",
          activeBodyPointId: parsed.activeBodyPointId ?? "—",
          activeTrackerId: parsed.activeTrackerId ?? "—",
          activeCursorId: parsed.activeCursorId ?? "—",
        });
        setStatus(`Выбран Unity JSON: ${file.name}`);
      } catch {
        setSelectedJson(null);
        setSelectedFileName("");
        setJsonPreview(null);
        setStatus("Не удалось прочитать JSON-файл");
      }
    };
    reader.readAsText(file);
  }

  async function importJson() {
    if (!selectedJson) {
      setStatus("Сначала выберите session_result.json из Unity");
      return;
    }
    setLoading(true);
    setStatus("Импорт результата Unity в API/БД...");
    try {
      const result = await cognitiveApi.importSessionResult(selectedJson);
      setSelectedJson(null);
      setSelectedFileName("");
      setJsonPreview(null);
      await loadAll();
      if (result?.id) await loadSession(result.id, false);
      setStatus(`Unity JSON импортирован: sessionId=${result?.id ?? "—"}, samples=${result?.samples ?? "—"}, events=${result?.events ?? "—"}, metrics=${result?.metrics ?? "—"}`);
    } catch (error) {
      console.error(error);
      setStatus(`Ошибка импорта: ${error.message}`);
    } finally {
      setLoading(false);
    }
  }

  const detailMetrics = sessionDetails?.metrics || [];
  const detailEvents = sessionDetails?.events || [];
  const detailSamples = sessionDetails?.samples || [];
  const cursorSamples = detailSamples.filter((x) => String(x.kind) === "1" || String(x.kind).toLowerCase() === "cursor").slice(0, 50);
  const pacerSamples = detailSamples.filter((x) => String(x.kind) === "2" || String(x.kind).toLowerCase() === "pacer").slice(0, 50);

  return (
    <main className="api-panel-page">
      <header className="api-panel-header">
        <div>
          <div className="api-kicker">Cognitive Engine</div>
          <h1>Unity → API → БД → сайт</h1>
          <p>Отображение данных, которые приходят из Unity: сессии, samples, events и метрики. API: {API_BASE_URL}</p>
        </div>
        <div className="api-header-actions">
          <Link className="api-link-button" to="/">На главную</Link>
          <a className="api-link-button" href={`${API_BASE_URL}/swagger`} target="_blank" rel="noreferrer">Swagger</a>
        </div>
      </header>

      <section className="api-status-row"><span className={loading ? "api-dot api-dot-loading" : "api-dot"} />{status}</section>

      <section className="api-stats-grid">
        {stats.map(([label, value, hint]) => <StatCard key={label} label={label} value={value} hint={hint} />)}
      </section>

      <section className="api-card api-wide-card api-import-card">
        <h2>Импорт результата из Unity</h2>
        <p className="api-muted">Выбери <b>session_result.json</b>. Сайт отправит его в API, backend разложит данные в БД: session, metrics, events, cursorSamples и pacerSamples.</p>
        <div className="api-import-row">
          <input type="file" accept="application/json,.json" onChange={handleFileChange} />
          <button onClick={importJson} disabled={loading || !selectedJson}>Импортировать Unity JSON</button>
          <button onClick={createDemoSession} disabled={loading}>Создать демо-сессию</button>
          <button onClick={loadAll} disabled={loading}>Обновить</button>
        </div>
        {jsonPreview ? (
          <div className="json-preview">
            <h3>Предпросмотр файла: {selectedFileName}</h3>
            <div className="preview-grid">
              {Object.entries(jsonPreview).map(([k, v]) => <div key={k}><span>{previewLabel(k)}</span><strong>{String(v)}</strong></div>)}
            </div>
          </div>
        ) : null}
      </section>

      <section className="api-card api-wide-card">
        <div className="api-card-title-row"><h2>Справочник точек тела</h2><button onClick={loadAll} disabled={loading}>Обновить</button></div>
        <div className="body-points-grid">
          {bodyPoints.map((point) => (
            <article className="body-point-item" key={point.id ?? point.code}>
              <strong>{point.code}</strong>
              <h3>{point.name}</h3>
              <p>сторона={point.side}, группа={point.group}, порядок={point.orderIndex}</p>
            </article>
          ))}
        </div>
      </section>

      <div className="api-two-columns">
        <section className="api-card">
          <div className="api-card-title-row"><h2>Упражнения</h2><button onClick={loadAll} disabled={loading}>Обновить</button></div>
          {exercises.length === 0 ? <p className="api-muted">Упражнений пока нет.</p> : (
            <div className="api-list">{exercises.map((exercise) => <article className="api-list-item" key={exercise.id}><h3>{exercise.name}</h3><p>ID={exercise.id}, шаблон={exercise.templateId ?? "—"}, пациент={exercise.patientId ?? "—"}, статус={exercise.status ?? "—"}</p></article>)}</div>
          )}
        </section>
        <section className="api-card">
          <div className="api-card-title-row"><h2>Сессии</h2><button onClick={loadAll} disabled={loading}>Обновить</button></div>
          {sessions.length === 0 ? <p className="api-muted">Сессий пока нет. Создай демо-сессию или импортируй session_result.json.</p> : (
            <div className="api-list sessions-list">{sessions.map((session) => (
              <button className={selectedSessionId === session.id ? "api-list-item active-session" : "api-list-item"} key={session.id} onClick={() => loadSession(session.id)}>
                <h3>{session.exerciseName || session.name || `Сессия #${session.id}`}</h3>
                <p>ID={session.id}, длительность={session.durationSec ?? session.timeSeconds ?? "—"} с, пациент={session.patientCode ?? session.patientId ?? "—"}</p>
                <p>Среднее отклонение: {pickMetric(session.metrics, "meanDeviationMm")}, RMSE: {pickMetric(session.metrics, "rmseDeviationMm")}</p>
              </button>
            ))}</div>
          )}
        </section>
      </div>

      <section className="api-card api-wide-card">
        <div className="api-card-title-row"><h2>Детализация выбранной сессии</h2>{selectedSessionId ? <button onClick={() => loadSession(selectedSessionId)} disabled={loading}>Обновить сессию</button> : null}</div>
        {!sessionDetails ? <p className="api-muted">Выбери сессию справа или импортируй Unity JSON.</p> : (
          <>
            <div className="details-summary">
              <div><span>ID сессии</span><strong>{sessionDetails.session?.id ?? selectedSessionId}</strong></div>
              <div><span>Упражнение</span><strong>{sessionDetails.session?.exerciseName ?? "—"}</strong></div>
              <div><span>Длительность</span><strong>{sessionDetails.session?.durationSec ?? "—"} с</strong></div>
              <div><span>Метрики</span><strong>{detailMetrics.length}</strong></div>
              <div><span>События</span><strong>{detailEvents.length}</strong></div>
              <div><span>Samples</span><strong>{detailSamples.length}</strong></div>
            </div>

            <h3 className="subhead">Итоговые метрики</h3>
            <div className="metrics-grid">
              {detailMetrics.length === 0 ? <p className="api-muted">Метрик нет.</p> : detailMetrics.map((m, i) => <MetricBadge metric={m} key={`${m.code}-${i}`} />)}
            </div>

            <h3 className="subhead">События Unity</h3>
            <DataTable
              rows={detailEvents.slice(0, 80)}
              emptyText="Событий нет."
              columns={[
                { key: "t", title: "t, c" },
                { key: "type", title: "Событие", render: (r) => eventTypeLabel(r.type) },
                { key: "cursorRef", title: "Курсор" },
                { key: "pacerRef", title: "Пейсмейкер" },
                { key: "bodyPointZoneRef", title: "Зона" },
                { key: "payload", title: "Комментарий", render: (r) => typeof r.payload === "string" ? r.payload.slice(0, 80) : JSON.stringify(r.payload).slice(0, 80) },
              ]}
            />

            <h3 className="subhead">Cursor samples из Unity</h3>
            <DataTable
              rows={cursorSamples}
              emptyText="Cursor samples не найдены."
              columns={[
                { key: "sampleIndex", title: "#" },
                { key: "t", title: "t, c" },
                { key: "cursorRef", title: "Курсор" },
                { key: "bodyPointId", title: "Точка тела" },
                { key: "bodyPointZoneRef", title: "Зона" },
                { key: "xmm", title: "x, мм", render: (r) => r.xmm ?? r.xMm ?? "—" },
                { key: "ymm", title: "y, мм", render: (r) => r.ymm ?? r.yMm ?? "—" },
                { key: "distanceToTrackCenterMm", title: "Отклонение, мм" },
                { key: "insideCorridor", title: "В коридоре", render: (r) => yesNo(r.insideCorridor) },
                { key: "confidence", title: "Достоверность" },
              ]}
            />

            <h3 className="subhead">Pacer samples из Unity</h3>
            <DataTable
              rows={pacerSamples}
              emptyText="Pacer samples не найдены или не были импортированы."
              columns={[
                { key: "sampleIndex", title: "#" },
                { key: "t", title: "t, c" },
                { key: "pacerRef", title: "Пейсмейкер" },
                { key: "xmm", title: "x, мм", render: (r) => r.xmm ?? r.xMm ?? "—" },
                { key: "ymm", title: "y, мм", render: (r) => r.ymm ?? r.yMm ?? "—" },
                { key: "speed", title: "Скорость" },
                { key: "trackRef", title: "Трек" },
              ]}
            />
          </>
        )}
      </section>
    </main>
  );
}

export default ApiPanelPage;
