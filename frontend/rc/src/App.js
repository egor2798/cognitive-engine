import "./App.css";
import { BrowserRouter, Routes, Route, Link } from "react-router-dom";

import DashboardPage from "./pages/DashboardPage.js";
import PatientsListPage from "./pages/PatientsListPage.js";
import PatientCardPage from "./pages/PatientCardPage.js";

function App() {
  return (
    <BrowserRouter>
      <div>
        <div
          style={{
            display: "flex",
            gap: "12px",
            padding: "16px 24px",
            background: "#ffffff",
            borderBottom: "1px solid #e5e7eb",
          }}
        >
          <Link to="/" className="secondary-btn">Дашборд</Link>
          <Link to="/patients" className="secondary-btn">Пациенты</Link>
          <Link to="/patient" className="secondary-btn">Карточка пациента</Link>
        </div>

        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/patients" element={<PatientsListPage />} />
          <Route path="/patient" element={<PatientCardPage />} />
        </Routes>
      </div>
    </BrowserRouter>
  );
}

export default App;