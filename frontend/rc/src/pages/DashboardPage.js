import "../App.css";

function DashboardPage() {
  const stats = [
    { title: "Пациентов", value: 18 },
    { title: "Активных тренировок", value: 42 },
    { title: "Завершенных тренировок", value: 120 },
    { title: "Средний результат", value: "81%" },
  ];

  return (
    <div className="page">
      <div className="container">

        <div className="card" style={{ marginBottom: "24px" }}>
          <h1 className="page-title">Дашборд специалиста</h1>
          <p className="page-subtitle">
            Общая статистика по пациентам и тренировкам
          </p>
        </div>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))",
            gap: "16px",
          }}
        >
          {stats.map((item) => (
            <div key={item.title} className="card">
              <p className="info-label">{item.title}</p>
              <h2>{item.value}</h2>
            </div>
          ))}
        </div>

      </div>
    </div>
  );
}

export default DashboardPage;