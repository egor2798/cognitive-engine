import "./App.css";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import DashboardPage from "./pages/DashboardPage.js";
import PatientsListPage from "./pages/PatientsListPage.js";
import PatientCardPage from "./pages/PatientCardPage.js";
import PatientDashboardPage from "./pages/PatientDashboardPage.js";
import MainPage from "./pages/MainPage.js";
import AuthPage from "./pages/AuthPage.js";
import ApiPanelPage from "./pages/ApiPanelPage.js";

const PrivateRoute = ({ children }) => {
  const user = localStorage.getItem("user");
  return user ? children : <Navigate to="/auth" replace />;
};

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<MainPage />} />
        <Route path="/auth" element={<AuthPage />} />
        <Route path="/dashboard" element={<PrivateRoute><DashboardPage /></PrivateRoute>} />
        <Route path="/patients" element={<PrivateRoute><PatientsListPage /></PrivateRoute>} />
        <Route path="/patient/:id" element={<PrivateRoute><PatientCardPage /></PrivateRoute>} />
        <Route path="/patient-dashboard" element={<PrivateRoute><PatientDashboardPage /></PrivateRoute>} />
        <Route path="/api-panel" element={<PrivateRoute><ApiPanelPage /></PrivateRoute>} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;