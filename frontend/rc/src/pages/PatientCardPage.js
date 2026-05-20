import { useNavigate, useParams } from "react-router-dom";
import { useEffect, useState } from "react";
import { useLanguage } from "../LanguageContext";
import { t } from "../translations";
import { cognitiveApi } from "../api";

function PatientCardPage() {
  const navigate = useNavigate();
  const { id } = useParams();
  const { language } = useLanguage();
  const [patient, setPatient] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    async function loadPatient() {
      setLoading(true);
      try {
        const data = await cognitiveApi.getPatient(id);
        setPatient({
          id: data.id,
          code: data.code,
          name: data.name,
          age: data.age,
          diagnosis: data.diagnosis || "",
          phone: data.phone || "",
          status: "Активен",
          gender: data.gender === "Male" ? "Мужской" : data.gender === "Female" ? "Женский" : data.gender || "",
          email: data.email || "",
          heightCm: data.heightCm,
          weightKg: data.weightKg,
          restrictions: data.restrictions || "",
          isDepersonalized: data.isDepersonalized,
        });
      } catch (err) {
        console.error(err);
        setError(language === "ru" ? "Пациент не найден или API недоступен" : "Patient not found or API unavailable");
      } finally {
        setLoading(false);
      }
    }

    loadPatient();
  }, [id, language]);

  const trainings = [
    { id: 1, title: t(language, 'trainingCoordination'), date: "27.03.2026", status: t(language, 'assigned') },
    { id: 2, title: t(language, 'trainingAttention'), date: "29.03.2026", status: t(language, 'assigned') },
    { id: 3, title: t(language, 'trainingReaction'), date: "31.03.2026", status: t(language, 'inProgress') }
  ];

  const pageStyle = { minHeight: "100vh", background: "linear-gradient(135deg, #05141a 0%, #0b242c 50%, #061c24 100%)", padding: "40px 20px", fontFamily: "Galdeano, Arial, sans-serif" };
  const cardStyle = { background: "rgba(6, 28, 36, 0.85)", backdropFilter: "blur(12px)", borderRadius: "24px", padding: "24px", border: "1px solid rgba(51, 193, 204, 0.25)", marginBottom: "24px" };
  const titleStyle = { fontFamily: "Jersey 10, sans-serif", fontSize: "32px", letterSpacing: "2px", background: "linear-gradient(135deg, #33c1cc, #196066)", WebkitBackgroundClip: "text", backgroundClip: "text", color: "transparent", marginBottom: "8px" };
  const sectionTitleStyle = { fontFamily: "Jersey 10, sans-serif", fontSize: "22px", letterSpacing: "1px", color: "#33c1cc", marginBottom: "20px", paddingBottom: "8px", borderBottom: "1px solid rgba(51, 193, 204, 0.3)" };
  const backBtnStyle = { background: "none", border: "none", color: "#33c1cc", cursor: "pointer", fontSize: "16px", marginBottom: "16px", display: "inline-flex", alignItems: "center", gap: "4px" };

  if (loading) {
    return <div style={{ ...pageStyle, display: "flex", alignItems: "center", justifyContent: "center", color: "white", fontSize: "24px" }}>Загрузка карточки пациента...</div>;
  }

  if (!patient) {
    return <div style={{ ...pageStyle, display: "flex", alignItems: "center", justifyContent: "center", color: "white" }}>{error || "Пациент не найден"}</div>;
  }

  return (
    <div style={pageStyle}>
      <div className="container">
        <div style={cardStyle}>
          <button style={backBtnStyle} onClick={() => navigate("/patients")}>← {t(language, 'backToPatientsList')}</button>
          <h1 style={titleStyle}>{t(language, 'patientCard')}</h1>
          <p style={{ color: "#33c1cc", marginBottom: "4px" }}>{patient.code}</p>
          <p style={{ color: "rgba(255,255,255,0.6)" }}>{patient.name}</p>
        </div>

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "24px", marginBottom: "24px" }}>
          <div style={cardStyle}>
            <h2 style={sectionTitleStyle}>{t(language, 'personalInfo')}</h2>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: "16px" }}>
              <Info label={t(language, 'name')} value={patient.name} />
              <Info label={t(language, 'age')} value={patient.age ? `${patient.age} ${t(language, 'yearsOld')}` : "—"} />
              <Info label={t(language, 'gender')} value={patient.gender || "—"} />
              <Info label={t(language, 'status')} value={patient.status} />
              <Info label={t(language, 'diagnosis')} value={patient.diagnosis || "—"} />
              <Info label={t(language, 'phone')} value={patient.phone || "—"} />
              <Info label="Email" value={patient.email || "—"} />
              <Info label="Обезличивание" value={patient.isDepersonalized ? "Да" : "Нет"} />
            </div>
          </div>

          <div style={cardStyle}>
            <h2 style={sectionTitleStyle}>Антропометрия и ограничения</h2>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: "16px" }}>
              <Info label="Рост" value={patient.heightCm ? `${patient.heightCm} см` : "—"} />
              <Info label="Вес" value={patient.weightKg ? `${patient.weightKg} кг` : "—"} />
              <Info label="Ограничения" value={patient.restrictions || "—"} wide />
            </div>
          </div>
        </div>

        <div style={cardStyle}>
          <h2 style={sectionTitleStyle}>{t(language, 'assignedTrainings')}</h2>
          <div style={{ display: "flex", flexDirection: "column", gap: "12px" }}>
            {trainings.map(training => (
              <div key={training.id} style={{ padding: "16px", background: "rgba(255,255,255,0.05)", borderRadius: "16px", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <div>
                  <p style={{ color: "white", fontWeight: "bold", marginBottom: "4px" }}>{training.title}</p>
                  <p style={{ color: "rgba(255,255,255,0.5)", fontSize: "14px" }}>{training.date}</p>
                </div>
                <span style={{ color: "#33c1cc", background: "rgba(51, 193, 204, 0.2)", padding: "6px 12px", borderRadius: "20px", fontSize: "12px" }}>{training.status}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

function Info({ label, value, wide }) {
  return (
    <div style={wide ? { gridColumn: "1 / -1" } : undefined}>
      <p style={{ color: "rgba(255,255,255,0.5)", fontSize: "12px", marginBottom: "4px" }}>{label}</p>
      <p style={{ color: "white" }}>{value}</p>
    </div>
  );
}

export default PatientCardPage;
