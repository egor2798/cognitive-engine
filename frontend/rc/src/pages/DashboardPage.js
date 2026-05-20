import { useNavigate } from "react-router-dom";
import { useState, useEffect } from "react";
import { useLanguage } from "../LanguageContext";
import { t } from "../translations";

function DashboardPage() {
  const navigate = useNavigate();
  const { language } = useLanguage();
  
  const [patientsCount, setPatientsCount] = useState(0);

  const updateCount = () => {
    const saved = localStorage.getItem('patients');
    const patients = saved ? JSON.parse(saved) : [];
    setPatientsCount(patients.length);
  };

  useEffect(() => {
    updateCount();
    window.addEventListener('storage', updateCount);
    return () => window.removeEventListener('storage', updateCount);
  }, []);

  const stats = [
    { title: t(language, 'patients'), value: patientsCount, onClick: () => navigate("/patients") },
    { title: t(language, 'activeTrainings'), value: 42, onClick: null },
    { title: t(language, 'completedTrainings'), value: 120, onClick: null },
    { title: t(language, 'averageResult'), value: "81%", onClick: null },
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
  };

  const subtitleStyle = {
    color: "rgba(255, 255, 255, 0.5)",
    fontSize: "16px",
  };

  const statCardStyle = {
    ...cardStyle,
    textAlign: "center",
    cursor: "pointer",
    marginBottom: 0,
  };

  const statValueStyle = {
    fontSize: "42px",
    margin: "8px 0 0 0",
    color: "#33c1cc",
    fontFamily: "Jersey 10, sans-serif",
  };

  return (
    <div style={pageStyle}>
      <div className="container">
        <div style={cardStyle}>
          <h1 style={titleStyle}>{t(language, 'specialistDashboard')}</h1>
          <p style={subtitleStyle}>{t(language, 'statistics')}</p>
        </div>

        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))", gap: "20px" }}>
          {stats.map((item) => (
            <div 
              key={item.title} 
              style={statCardStyle}
              onClick={item.onClick}
            >
              <p style={{ color: "rgba(255,255,255,0.5)", fontSize: "12px", textTransform: "uppercase" }}>{item.title}</p>
              <h2 style={statValueStyle}>{item.value}</h2>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

export default DashboardPage;