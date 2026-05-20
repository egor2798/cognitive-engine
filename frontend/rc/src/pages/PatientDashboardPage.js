import { useLanguage } from "../LanguageContext";
import { t } from "../translations";

function PatientDashboardPage() {
  const { language } = useLanguage();
  const currentUser = JSON.parse(localStorage.getItem("user") || "{}");
  
  const patient = {
    name: currentUser.name || currentUser.username || "Пациент",
    age: 35,
    gender: "Мужской",
    diagnosis: "Реабилитация после инсульта",
    status: "Активен",
    phone: "+7 (999) 123-45-67",
    email: currentUser.email || "patient@example.com",
  };

  const trainings = [
    { id: 1, title: t(language, 'trainingCoordination'), date: "27.03.2026", status: t(language, 'assigned') },
    { id: 2, title: t(language, 'trainingAttention'), date: "29.03.2026", status: t(language, 'assigned') },
    { id: 3, title: t(language, 'trainingReaction'), date: "31.03.2026", status: t(language, 'inProgress') },
  ];

  const pageStyle = {
    minHeight: "100vh",
    background: "linear-gradient(135deg, #05141a 0%, #0b242c 50%, #061c24 100%)",
    padding: "40px 20px",
    fontFamily: "Galdeano, Arial, sans-serif",
  };

  const cardStyle = {
    background: "rgba(6, 28, 36, 0.85)",
    backdropFilter: "blur(12px)",
    borderRadius: "24px",
    padding: "24px",
    border: "1px solid rgba(51, 193, 204, 0.25)",
    marginBottom: "24px",
  };

  const titleStyle = {
    fontFamily: "Jersey 10, sans-serif",
    fontSize: "32px",
    letterSpacing: "2px",
    background: "linear-gradient(135deg, #33c1cc, #196066)",
    WebkitBackgroundClip: "text",
    backgroundClip: "text",
    color: "transparent",
    marginBottom: "8px",
    textAlign: "center",
  };

  const sectionTitleStyle = {
    fontFamily: "Jersey 10, sans-serif",
    fontSize: "22px",
    letterSpacing: "1px",
    color: "#33c1cc",
    marginBottom: "20px",
    paddingBottom: "8px",
    borderBottom: "1px solid rgba(51, 193, 204, 0.3)",
  };

  return (
    <div style={pageStyle}>
      <div className="container">
        <div style={{ ...cardStyle, textAlign: "center" }}>
          <div style={{ width: "80px", height: "80px", margin: "0 auto 16px", background: "linear-gradient(135deg, #33c1cc, #196066)", borderRadius: "24px", display: "flex", alignItems: "center", justifyContent: "center" }}>
            <span style={{ fontSize: "40px" }}>🧠</span>
          </div>
          <h1 style={titleStyle}>{t(language, 'personalCabinet')}</h1>
          <p style={{ color: "rgba(255,255,255,0.5)" }}>{t(language, 'welcome')}, {patient.name}</p>
        </div>

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "24px", marginBottom: "24px" }}>
          <div style={cardStyle}>
            <h2 style={sectionTitleStyle}>{t(language, 'myData')}</h2>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: "16px" }}>
              <div><p style={{ color: "rgba(255,255,255,0.5)", fontSize: "12px" }}>{t(language, 'name')}</p><p style={{ color: "white" }}>{patient.name}</p></div>
              <div><p style={{ color: "rgba(255,255,255,0.5)", fontSize: "12px" }}>{t(language, 'age')}</p><p style={{ color: "white" }}>{patient.age} {t(language, 'yearsOld')}</p></div>
              <div><p style={{ color: "rgba(255,255,255,0.5)", fontSize: "12px" }}>{t(language, 'gender')}</p><p style={{ color: "white" }}>{language === 'ru' ? patient.gender : 'Male'}</p></div>
              <div><p style={{ color: "rgba(255,255,255,0.5)", fontSize: "12px" }}>{t(language, 'status')}</p><p style={{ color: "white" }}>{patient.status}</p></div>
              <div><p style={{ color: "rgba(255,255,255,0.5)", fontSize: "12px" }}>{t(language, 'diagnosis')}</p><p style={{ color: "white" }}>{patient.diagnosis}</p></div>
              <div><p style={{ color: "rgba(255,255,255,0.5)", fontSize: "12px" }}>{t(language, 'phone')}</p><p style={{ color: "white" }}>{patient.phone}</p></div>
              <div><p style={{ color: "rgba(255,255,255,0.5)", fontSize: "12px" }}>Email</p><p style={{ color: "white" }}>{patient.email}</p></div>
            </div>
          </div>

          <div style={cardStyle}>
            <h2 style={sectionTitleStyle}>{t(language, 'myProgress')}</h2>
            <div style={{ background: "rgba(255,255,255,0.05)", borderRadius: "16px", padding: "20px", textAlign: "center", marginBottom: "20px" }}>
              <div style={{ height: "140px", display: "flex", alignItems: "center", justifyContent: "center", color: "rgba(255,255,255,0.4)" }}>
                📊 {t(language, 'progress')}
              </div>
            </div>
            <div>
              <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "8px" }}>
                <span style={{ color: "rgba(255,255,255,0.6)" }}>{t(language, 'overallProgress')}</span>
                <span style={{ color: "#33c1cc" }}>65%</span>
              </div>
              <div style={{ background: "rgba(255,255,255,0.1)", borderRadius: "10px", overflow: "hidden" }}>
                <div style={{ width: "65%", height: "8px", background: "linear-gradient(90deg, #33c1cc, #196066)" }}></div>
              </div>
            </div>
          </div>
        </div>

        <div style={cardStyle}>
          <h2 style={sectionTitleStyle}>{t(language, 'myTrainings')}</h2>
          <div style={{ display: "flex", flexDirection: "column", gap: "12px" }}>
            {trainings.map((training) => (
              <div key={training.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "16px", background: "rgba(255,255,255,0.03)", borderRadius: "12px" }}>
                <div>
                  <h3 style={{ fontSize: "16px", color: "white", marginBottom: "4px" }}>{training.title}</h3>
                  <p style={{ fontSize: "12px", color: "rgba(255,255,255,0.5)" }}>{training.date}</p>
                </div>
                <span style={{ background: "rgba(51,193,204,0.2)", color: "#33c1cc", padding: "4px 12px", borderRadius: "20px", fontSize: "12px" }}>{training.status}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

export default PatientDashboardPage;