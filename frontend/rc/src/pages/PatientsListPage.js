import { useNavigate } from "react-router-dom";
import { useEffect, useState } from "react";
import { useLanguage } from "../LanguageContext";
import { t } from "../translations";
import { cognitiveApi } from "../api";

function PatientsListPage() {
  const navigate = useNavigate();
  const { language } = useLanguage();

  const [patients, setPatients] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [loading, setLoading] = useState(false);
  const [status, setStatus] = useState("");
  const [newPatient, setNewPatient] = useState({
    code: "",
    firstName: "",
    lastName: "",
    age: "",
    diagnosis: "",
    phone: "",
    gender: "",
    email: "",
    heightCm: "",
    weightKg: "",
    restrictions: "",
    isDepersonalized: false,
  });

  async function loadPatients() {
    setLoading(true);
    setStatus(language === "ru" ? "Загрузка пациентов из БД..." : "Loading patients from DB...");
    try {
      const data = await cognitiveApi.getPatients();
      setPatients(data || []);
      localStorage.setItem("patients", JSON.stringify((data || []).map(mapToLocalPatient)));
      setStatus(language === "ru" ? "Пациенты загружены из API/БД" : "Patients loaded from API/DB");
    } catch (err) {
      console.error(err);
      setStatus(`${language === "ru" ? "Ошибка API" : "API error"}: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadPatients();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function mapToLocalPatient(patient) {
    return {
      id: patient.id,
      name: patient.name,
      code: patient.code,
      age: patient.age,
      diagnosis: patient.diagnosis || "",
      phone: patient.phone || "",
      status: "Активен",
      gender: patient.gender === "Male" ? "Мужской" : patient.gender === "Female" ? "Женский" : patient.gender || "",
      email: patient.email || "",
      heightCm: patient.heightCm,
      weightKg: patient.weightKg,
      restrictions: patient.restrictions || "",
      trainings: [
        { id: 1, title: t(language, 'trainingCoordination'), date: "27.03.2026", status: t(language, 'assigned') },
        { id: 2, title: t(language, 'trainingAttention'), date: "29.03.2026", status: t(language, 'assigned') },
        { id: 3, title: t(language, 'trainingReaction'), date: "31.03.2026", status: t(language, 'inProgress') }
      ]
    };
  }

  const handleInputChange = (e) => {
    const { name, value, type, checked } = e.target;
    setNewPatient({ ...newPatient, [name]: type === "checkbox" ? checked : value });
  };

  async function handleAddPatient() {
    if (!newPatient.firstName || !newPatient.lastName || !newPatient.age || !newPatient.diagnosis) {
      alert(language === 'ru' ? 'Заполните ФИО, возраст и диагноз/цель' : 'Please fill name, age and diagnosis');
      return;
    }

    if (newPatient.email && (!newPatient.email.includes('@') || !newPatient.email.includes('.'))) {
      alert(language === 'ru' ? 'Введите корректный email' : 'Enter a valid email');
      return;
    }

    const payload = {
      code: newPatient.code || null,
      firstName: newPatient.firstName,
      lastName: newPatient.lastName,
      age: Number(newPatient.age),
      diagnosis: newPatient.diagnosis,
      phone: newPatient.phone || null,
      gender: newPatient.gender || null,
      email: newPatient.email || null,
      heightCm: newPatient.heightCm ? Number(newPatient.heightCm) : null,
      weightKg: newPatient.weightKg ? Number(newPatient.weightKg) : null,
      restrictions: newPatient.restrictions || null,
      isDepersonalized: newPatient.isDepersonalized,
    };

    setLoading(true);
    try {
      await cognitiveApi.createPatient(payload);
      setNewPatient({ code: "", firstName: "", lastName: "", age: "", diagnosis: "", phone: "", gender: "", email: "", heightCm: "", weightKg: "", restrictions: "", isDepersonalized: false });
      setShowForm(false);
      await loadPatients();
      setStatus(language === "ru" ? "Пациент добавлен в БД" : "Patient saved to DB");
    } catch (err) {
      console.error(err);
      setStatus(`${language === "ru" ? "Не удалось добавить пациента" : "Failed to add patient"}: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function handleDeletePatient(id, e) {
    e.stopPropagation();
    if (!window.confirm(language === 'ru' ? 'Удалить пациента из БД?' : 'Delete patient from DB?')) return;

    setLoading(true);
    try {
      await cognitiveApi.deletePatient(id);
      await loadPatients();
      setStatus(language === "ru" ? "Пациент удалён" : "Patient deleted");
    } catch (err) {
      console.error(err);
      setStatus(`${language === "ru" ? "Не удалось удалить" : "Delete failed"}: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  const pageStyle = { minHeight: "100vh", background: "linear-gradient(135deg, #05141a 0%, #0b242c 50%, #061c24 100%)", padding: "40px 20px", fontFamily: "Galdeano, Arial, sans-serif" };
  const cardStyle = { background: "rgba(6, 28, 36, 0.85)", backdropFilter: "blur(12px)", borderRadius: "24px", padding: "24px", border: "1px solid rgba(51, 193, 204, 0.25)", marginBottom: "24px" };
  const formStyle = { background: "rgba(6, 28, 36, 0.95)", backdropFilter: "blur(12px)", borderRadius: "24px", padding: "24px", border: "1px solid rgba(51, 193, 204, 0.5)", marginBottom: "24px" };
  const inputStyle = { width: "100%", padding: "12px 16px", borderRadius: "12px", border: "1px solid rgba(51, 193, 204, 0.3)", background: "rgba(255, 255, 255, 0.08)", color: "white", fontSize: "16px", fontFamily: "Galdeano, Arial, sans-serif", boxSizing: "border-box" };
  const selectStyle = { ...inputStyle, background: "rgba(6, 28, 36, 0.95)", cursor: "pointer" };
  const labelStyle = { display: "block", color: "rgba(255, 255, 255, 0.7)", marginBottom: "8px", fontSize: "14px" };
  const buttonStyle = { background: "linear-gradient(135deg, #33c1cc, #196066)", border: "none", borderRadius: "50px", padding: "12px 24px", color: "white", fontSize: "16px", cursor: "pointer", fontFamily: "Jersey 10, sans-serif", letterSpacing: "1px" };
  const secondaryButtonStyle = { background: "rgba(255, 255, 255, 0.1)", border: "1px solid rgba(51, 193, 204, 0.4)", borderRadius: "50px", padding: "12px 24px", color: "white", fontSize: "16px", cursor: "pointer", marginLeft: "12px" };
  const deleteButtonStyle = { background: "rgba(255, 80, 80, 0.2)", border: "1px solid rgba(255, 80, 80, 0.5)", borderRadius: "50px", padding: "8px 16px", color: "#ff6b6b", fontSize: "14px", cursor: "pointer", marginLeft: "12px" };

  return (
    <div style={pageStyle}>
      <div className="container">
        <div style={cardStyle}>
          <button onClick={() => navigate("/dashboard")} style={{ background: "none", border: "none", color: "#33c1cc", cursor: "pointer", fontSize: "16px", marginBottom: "16px", display: "inline-flex", alignItems: "center", gap: "4px" }}>
            ← {t(language, 'backToDashboard')}
          </button>
          <h1 style={{ fontFamily: "Jersey 10, sans-serif", fontSize: "32px", letterSpacing: "2px", background: "linear-gradient(135deg, #33c1cc, #196066)", WebkitBackgroundClip: "text", backgroundClip: "text", color: "transparent", marginBottom: "8px" }}>{t(language, 'patientsList')}</h1>
          <p style={{ color: "rgba(255,255,255,0.5)" }}>Всего пациентов в БД: {patients.length}</p>
          {status && <p style={{ color: status.includes("Ошибка") || status.includes("Failed") || status.includes("Не удалось") ? "#ff8b8b" : "#66e5ec", marginTop: "8px" }}>{status}</p>}

          <button onClick={() => setShowForm(!showForm)} style={{ ...buttonStyle, marginTop: "16px" }} disabled={loading}>+ {t(language, 'addPatient')}</button>
          <button onClick={loadPatients} style={secondaryButtonStyle} disabled={loading}>Обновить из БД</button>
        </div>

        {showForm && (
          <div style={formStyle}>
            <h2 style={{ fontFamily: "Jersey 10, sans-serif", fontSize: "24px", color: "#33c1cc", marginBottom: "20px" }}>Новая карточка пациента</h2>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "16px" }}>
              <div><label style={labelStyle}>Код пациента</label><input name="code" value={newPatient.code} onChange={handleInputChange} placeholder="P-0002 / можно пусто" style={inputStyle} /></div>
              <div><label style={labelStyle}>Фамилия *</label><input name="lastName" value={newPatient.lastName} onChange={handleInputChange} placeholder="Иванов" style={inputStyle} /></div>
              <div><label style={labelStyle}>Имя *</label><input name="firstName" value={newPatient.firstName} onChange={handleInputChange} placeholder="Иван" style={inputStyle} /></div>
              <div><label style={labelStyle}>Возраст *</label><input type="number" name="age" value={newPatient.age} onChange={handleInputChange} placeholder="34" style={inputStyle} /></div>
              <div><label style={labelStyle}>Пол</label><select name="gender" value={newPatient.gender} onChange={handleInputChange} style={selectStyle}><option value="">Не указан</option><option value="Male">Мужской</option><option value="Female">Женский</option><option value="Other">Другое</option></select></div>
              <div><label style={labelStyle}>Email</label><input type="email" name="email" value={newPatient.email} onChange={handleInputChange} placeholder="email@example.com" style={inputStyle} /></div>
              <div><label style={labelStyle}>Телефон</label><input name="phone" value={newPatient.phone} onChange={handleInputChange} placeholder="+7..." style={inputStyle} /></div>
              <div><label style={labelStyle}>Рост, см</label><input type="number" name="heightCm" value={newPatient.heightCm} onChange={handleInputChange} placeholder="178" style={inputStyle} /></div>
              <div><label style={labelStyle}>Вес, кг</label><input type="number" name="weightKg" value={newPatient.weightKg} onChange={handleInputChange} placeholder="75" style={inputStyle} /></div>
              <div><label style={labelStyle}>Диагноз / цель *</label><input name="diagnosis" value={newPatient.diagnosis} onChange={handleInputChange} placeholder="Наблюдение / ЦВБ / реабилитация" style={inputStyle} /></div>
              <div style={{ gridColumn: "1 / -1" }}><label style={labelStyle}>Ограничения движения</label><input name="restrictions" value={newPatient.restrictions} onChange={handleInputChange} placeholder="Например: правая рука до уровня плеча" style={inputStyle} /></div>
              <label style={{ color: "rgba(255,255,255,0.75)", display: "flex", gap: "10px", alignItems: "center" }}><input type="checkbox" name="isDepersonalized" checked={newPatient.isDepersonalized} onChange={handleInputChange} /> Обезличить отображение пациента</label>
            </div>
            <div style={{ marginTop: "20px", display: "flex", justifyContent: "flex-end" }}>
              <button onClick={() => setShowForm(false)} style={secondaryButtonStyle}>Отмена</button>
              <button onClick={handleAddPatient} style={buttonStyle} disabled={loading}>Сохранить в БД</button>
            </div>
          </div>
        )}

        {patients.map((patient) => (
          <div key={patient.id} onClick={() => navigate(`/patient/${patient.id}`)} style={{ ...cardStyle, cursor: "pointer", transition: "all 0.3s", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <div style={{ flex: 1 }}>
              <h2 style={{ color: "white", fontSize: "24px", marginBottom: "8px", fontFamily: "Jersey 10, sans-serif", letterSpacing: "1px" }}>{patient.name}</h2>
              <p style={{ color: "#33c1cc", marginBottom: "8px" }}>{patient.code} · {patient.age ? `${patient.age} лет` : "возраст не указан"} · {patient.gender || "пол не указан"}</p>
              <p style={{ color: "rgba(255,255,255,0.65)", marginBottom: "8px" }}>Диагноз/цель: {patient.diagnosis || "—"}</p>
              <p style={{ color: "rgba(255,255,255,0.45)" }}>Рост: {patient.heightCm || "—"} см · Вес: {patient.weightKg || "—"} кг · Ограничения: {patient.restrictions || "—"}</p>
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
              <span style={{ padding: "8px 16px", borderRadius: "20px", background: "rgba(51, 193, 204, 0.2)", color: "#33c1cc", fontSize: "14px" }}>Активен</span>
              <button onClick={(e) => handleDeletePatient(patient.id, e)} style={deleteButtonStyle}>Удалить</button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

export default PatientsListPage;
