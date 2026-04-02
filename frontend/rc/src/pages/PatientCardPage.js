import "../App.css";

function PatientCardPage() {

  const patient = {
    name: "Иван Петров",
    age: 12,
    gender: "Мужской",
    diagnosis: "Наблюдение",
    status: "Активен",
  };

  const trainings = [
    { id: 1, title: "Тренировка координации", date: "27.03.2026" },
    { id: 2, title: "Тест реакции", date: "29.03.2026" },
    { id: 3, title: "Упражнение внимания", date: "31.03.2026" },
  ];

  return (
    <div className="page">
      <div className="container">

        <div className="card" style={{ marginBottom: "24px" }}>
          <h1 className="page-title">Карточка пациента</h1>
        </div>

        <div className="card" style={{ marginBottom: "24px" }}>
          <h2>Личные данные</h2>

          <p><b>ФИО:</b> {patient.name}</p>
          <p><b>Возраст:</b> {patient.age}</p>
          <p><b>Пол:</b> {patient.gender}</p>
          <p><b>Статус:</b> {patient.status}</p>
          <p><b>Диагноз:</b> {patient.diagnosis}</p>

        </div>

        <div className="card">
          <h2>Назначенные тренировки</h2>

          <ul>
            {trainings.map((t) => (
              <li key={t.id}>
                {t.title} — {t.date}
              </li>
            ))}
          </ul>

        </div>

      </div>
    </div>
  );
}

export default PatientCardPage;