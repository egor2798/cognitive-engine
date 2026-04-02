import "../App.css";

function PatientCardPage() {
  const patient = {
    name: "Иван Петров",
    age: 12,
    gender: "Мужской",
    diagnosis: "Наблюдение",
    status: "Активен",
    phone: "+7 (999) 123-45-67",
    email: "ivan.petrov@example.com",
  };

  const trainings = [
    { id: 1, title: "Тренировка на координацию", date: "27.03.2026", status: "Назначена" },
    { id: 2, title: "Упражнение на внимание", date: "29.03.2026", status: "Назначена" },
    { id: 3, title: "Тест скорости реакции", date: "31.03.2026", status: "В процессе" },
  ];

  return (
    <div className="page">
      <div className="container">
        <div className="card" style={{ marginBottom: "24px" }}>
          <h1 className="page-title">Карточка пациента</h1>
          <p className="page-subtitle">Личная информация, прогресс и назначенные тренировки</p>
        </div>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            gap: "24px",
            marginBottom: "24px",
          }}
        >
          <div className="card">
            <h2 className="section-title">Личные данные</h2>
            <div className="patient-info-grid">
              <div className="info-item">
                <span className="info-label">ФИО</span>
                <span className="info-value">{patient.name}</span>
              </div>
              <div className="info-item">
                <span className="info-label">Возраст</span>
                <span className="info-value">{patient.age}</span>
              </div>
              <div className="info-item">
                <span className="info-label">Пол</span>
                <span className="info-value">{patient.gender}</span>
              </div>
              <div className="info-item">
                <span className="info-label">Статус</span>
                <span className="info-value">{patient.status}</span>
              </div>
              <div className="info-item">
                <span className="info-label">Диагноз</span>
                <span className="info-value">{patient.diagnosis}</span>
              </div>
              <div className="info-item">
                <span className="info-label">Телефон</span>
                <span className="info-value">{patient.phone}</span>
              </div>
              <div className="info-item">
                <span className="info-label">Email</span>
                <span className="info-value">{patient.email}</span>
              </div>
            </div>
          </div>

          <div className="card">
            <h2 className="section-title">График прогресса</h2>
            <div className="chart-image-box">
              <span className="chart-placeholder-text">Здесь будет график прогресса</span>
            </div>
          </div>
        </div>

        <div className="card">
          <h2 className="section-title">Назначенные тренировки</h2>
          <div className="training-list">
            {trainings.map((training) => (
              <div key={training.id} className="training-item">
                <div>
                  <h3 className="training-title">{training.title}</h3>
                  <p className="training-meta">{training.date}</p>
                </div>
                <div className="training-side">
                  <span className="status-badge">{training.status}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

export default PatientCardPage;