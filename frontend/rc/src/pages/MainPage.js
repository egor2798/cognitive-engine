import "./MainPage.css";
import { Link } from "react-router-dom";
import { useLanguage } from "../LanguageContext";
import { t } from "../translations";

export default function MainPage() {
  const { language, toggleLanguage } = useLanguage();

  return (
    <main className="main-page">
      <header className="main-header">
        <div className="main-logo">
          <img 
            src="/Group 329.png" 
            alt="Cognitive Engine Logo" 
            style={{ 
              width: "64px", 
              height: "64px", 
              borderRadius: "18px", 
              objectFit: "cover"
            }}
          />
          <div>
            <div className="main-logo-title">Cognitive</div>
            <div className="main-logo-subtitle">Engine</div>
          </div>
        </div>

        <nav className="main-nav">
          <a href="#home">{t(language, 'home')}</a>
          <a href="#products">{t(language, 'products')}</a>
          <a href="#about">{t(language, 'about')}</a>
          <Link to="/api-panel">API/БД</Link>
          <Link to="/auth">{t(language, 'login')}</Link>
          <button 
            onClick={toggleLanguage}
            style={{ 
              background: 'none', 
              border: 'none', 
              color: 'white', 
              cursor: 'pointer',
              fontSize: '24px',
              fontFamily: 'inherit'
            }}
          >
            {language === 'ru' ? 'EN' : 'RU'}
          </button>
        </nav>
      </header>

      <section className="main-hero" id="home">
        <div className="main-hero-content">
          <h1>
            {t(language, 'heroTitle1')}
            <span>{t(language, 'heroTitleSpan')}</span>
            {t(language, 'heroTitle2')}
          </h1>

          <p>
            {t(language, 'heroDescription')}
          </p>

          <div className="main-actions">
            <button>{t(language, 'documentation')}</button>
            <button className="secondary">{t(language, 'aboutCompany')}</button>
          </div>
        </div>

        <img
          className="main-hero-image"
          src="https://www.figma.com/api/mcp/asset/c8893458-81bf-4835-a60e-bb63890546fd"
          alt="Cognitive motor system"
        />
      </section>

      <section className="analytics-section">
        <h2>{t(language, 'analyticsTitle')}</h2>

        <div className="analytics-cards">
          <article className="analytics-card">
            <img
              src="https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=900&q=80"
              alt="Sensomode"
            />
            <div className="analytics-card-label">
              {t(language, 'sensomode')}
            </div>
          </article>

          <article className="analytics-card">
            <img
              src="https://images.unsplash.com/photo-1559757148-5c350d0d3c56?auto=format&fit=crop&w=900&q=80"
              alt="CognitiveEngine"
            />
            <div className="analytics-card-label">
              {t(language, 'cognitiveEngine')}
            </div>
          </article>
        </div>

        <p className="analytics-report">
          <strong>{t(language, 'reportFormat')}</strong> XLS, PDF, CSV.
        </p>

        <div className="analytics-line" />
      </section>

      <section className="protocols-section">
        <div className="section-line" />

        <h2>{t(language, 'protocolsTitle')}</h2>

        <div className="protocol-list">
          <article className="protocol-item">
            <div className="protocol-arrow">&gt;</div>
            <div className="protocol-icon">♿</div>
            <div className="protocol-content">
              <h3>{t(language, 'staticExercises')}</h3>
              <ul>
                <li>{t(language, 'static1')}</li>
                <li>{t(language, 'static2')}</li>
                <li>{t(language, 'static3')}</li>
              </ul>
            </div>
          </article>

          <article className="protocol-item">
            <div className="protocol-arrow">&gt;</div>
            <div className="protocol-icon">🏃</div>
            <div className="protocol-content">
              <h3>{t(language, 'dynamicExercises')}</h3>
              <ul>
                <li>{t(language, 'dynamic1')}</li>
                <li>{t(language, 'dynamic2')}</li>
                <li>{t(language, 'dynamic3')}</li>
                <li>{t(language, 'dynamic4')}</li>
              </ul>
            </div>
          </article>

          <article className="protocol-item">
            <div className="protocol-arrow">&gt;</div>
            <div className="protocol-icon">🏃</div>
            <div className="protocol-content">
              <h3>{t(language, 'volumetricExercises')}</h3>
              <ul>
                <li>{t(language, 'volumetric1')}</li>
                <li>{t(language, 'volumetric2')}</li>
              </ul>
            </div>
          </article>

          <article className="protocol-item">
            <div className="protocol-arrow">&gt;</div>
            <div className="protocol-icon">🛡</div>
            <div className="protocol-content">
              <h3>{t(language, 'sensorimotorExercises')}</h3>
              <ul>
                <li>{t(language, 'sensorimotor1')}</li>
                <li>{t(language, 'sensorimotor2')}</li>
              </ul>
            </div>
          </article>
        </div>

        <div className="section-line" />

        <section className="usage-section">
          <h2>{t(language, 'usageTitle')}</h2>

          <div className="usage-cards">
            <article className="usage-card">
              <div className="usage-card-title">
                <span className="doc-icon">▤</span>
                {t(language, 'usageCard1Title')}
              </div>
              <p>{t(language, 'usageCard1Text1')}</p>
              <p>{t(language, 'usageCard1Text2')}</p>
              <div className="usage-arrow">→</div>
            </article>

            <article className="usage-card">
              <div className="usage-card-title">
                <span className="doc-icon">▤</span>
                {t(language, 'usageCard2Title')}
              </div>
              <p>{t(language, 'usageCard2Text1')}</p>
              <p>{t(language, 'usageCard2Text2')}</p>
              <div className="usage-arrow">→</div>
            </article>

            <article className="usage-card">
              <div className="usage-card-title">
                <span className="doc-icon">▤</span>
                {t(language, 'usageCard3Title')}
              </div>
              <p>{t(language, 'usageCard3Text1')}</p>
              <p>{t(language, 'usageCard3Text2')}</p>
              <div className="usage-arrow">→</div>
            </article>
          </div>
        </section>

        <div className="section-line contact-line" />

        <section className="contact-section">
          <div className="map-box">
            <iframe
              title="map"
              src="https://yandex.ru/map-widget/v1/?ll=37.617700%2C55.755864&z=15"
              frameBorder="0"
            />
          </div>

          <form className="contact-form">
            <span className="form-small">all</span>
            <h2>{t(language, 'contactTitle')}</h2>

            <select>
              <option>{t(language, 'subject')}</option>
              <option>{t(language, 'documentation2')}</option>
              <option>{t(language, 'demo')}</option>
              <option>{t(language, 'partnership')}</option>
            </select>

            <input type="email" placeholder={t(language, 'emailPlaceholder')} />
            <input type="text" placeholder="Stassy08@gma" />

            <select>
              <option>{t(language, 'whenContact')}</option>
              <option>{t(language, 'duringDay')}</option>
              <option>{t(language, 'thisWeek')}</option>
            </select>

            <textarea placeholder={t(language, 'messagePlaceholder')} />

            <button type="button">{t(language, 'sendButton')}</button>
          </form>
        </section>
      </section>
    </main>
  );
}