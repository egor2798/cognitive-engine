import React, { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import cognitiveApi from "../api";
import "./AuthPage.css";

function AuthPage() {
  const navigate = useNavigate();

  const [mode, setMode] = useState("login");
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [role, setRole] = useState("methodist");
  const [password, setPassword] = useState("");
  const [message, setMessage] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const saveAuth = (data) => {
    if (!data) {
      throw new Error("Пустой ответ от сервера");
    }

    // Поддержка разных форматов ответа API:
    // { token, user } или { accessToken, user } или просто объект пользователя.
    const token = data.token || data.accessToken || data.jwt || "local-demo-token";
    const user = data.user || {
      id: data.id,
      login: data.login || data.username || username,
      email: data.email,
      firstName: data.firstName,
      lastName: data.lastName,
      role: data.role || role,
    };

    localStorage.setItem("token", token);
    localStorage.setItem("user", JSON.stringify(user));

    const userRole = String(user.role || "").toLowerCase();

    if (userRole === "patient") {
      navigate("/patient-dashboard");
    } else {
      // Для методиста/оператора/админа сразу открываем страницу результатов Unity/API/БД.
      navigate("/api-panel");
    }
  };

  const handleLogin = async (event) => {
    event.preventDefault();
    setMessage("");
    setIsLoading(true);

    try {
      const loginValue = username.trim();

      // Отправляем сразу несколько имён поля, чтобы попасть в DTO backend,
      // даже если там используется Login, Username или LoginOrEmail.
      const data = await cognitiveApi.login({
        username: loginValue,
        login: loginValue,
        loginOrEmail: loginValue,
        email: loginValue,
        password,
      });

      saveAuth(data);
    } catch (error) {
      console.error(error);
      setMessage(error.message || "Неверный логин или пароль");
    } finally {
      setIsLoading(false);
    }
  };

  const handleRegister = async (event) => {
    event.preventDefault();
    setMessage("");
    setIsLoading(true);

    try {
      const loginValue = username.trim();

      const data = await cognitiveApi.register({
        username: loginValue,
        login: loginValue,
        loginOrEmail: loginValue,
        email: email.trim(),
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        role,
        password,
      });

      saveAuth(data);
    } catch (error) {
      console.error(error);
      setMessage(error.message || "Не удалось зарегистрировать пользователя");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className="auth-page">
      <section className="auth-card">
        <Link className="auth-back" to="/">
          ← На главную
        </Link>

        <div className="auth-logo">CE</div>
        <h1>Cognitive Engine</h1>
        <p className="auth-subtitle">Вход через API/БД</p>

        <div className="auth-tabs">
          <button
            type="button"
            className={mode === "login" ? "active" : ""}
            onClick={() => {
              setMode("login");
              setMessage("");
            }}
          >
            Вход
          </button>
          <button
            type="button"
            className={mode === "register" ? "active" : ""}
            onClick={() => {
              setMode("register");
              setMessage("");
            }}
          >
            Регистрация
          </button>
        </div>

        <form onSubmit={mode === "login" ? handleLogin : handleRegister}>
          <label>
            <span>Логин или email</span>
            <input
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              placeholder="methodist"
              autoComplete="username"
              required
            />
          </label>

          {mode === "register" && (
            <>
              <label>
                <span>Email</span>
                <input
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  placeholder="methodist@test.com"
                  autoComplete="email"
                  type="email"
                  required
                />
              </label>

              <div className="auth-row">
                <label>
                  <span>Имя</span>
                  <input
                    value={firstName}
                    onChange={(event) => setFirstName(event.target.value)}
                    placeholder="Мария"
                    required
                  />
                </label>

                <label>
                  <span>Фамилия</span>
                  <input
                    value={lastName}
                    onChange={(event) => setLastName(event.target.value)}
                    placeholder="Методистова"
                    required
                  />
                </label>
              </div>

              <label>
                <span>Роль</span>
                <select value={role} onChange={(event) => setRole(event.target.value)}>
                  <option value="methodist">Методист</option>
                  <option value="operator">Оператор</option>
                  <option value="admin">Администратор</option>
                  <option value="researcher">Исследователь</option>
                  <option value="patient">Пациент</option>
                </select>
              </label>
            </>
          )}

          <label>
            <span>Пароль</span>
            <input
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              placeholder="123456"
              autoComplete={mode === "login" ? "current-password" : "new-password"}
              type="password"
              required
            />
          </label>

          {message && <div className="auth-error">{message}</div>}

          <button className="auth-submit" type="submit" disabled={isLoading}>
            {isLoading ? "Подождите..." : mode === "login" ? "Войти" : "Зарегистрироваться"}
          </button>
        </form>

        <div className="auth-hint">
          Демо-вход: <b>methodist</b> / <b>123456</b>
        </div>
      </section>
    </main>
  );
}

export default AuthPage;
