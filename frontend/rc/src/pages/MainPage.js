import "./MainPage.css";

export default function MainPage() {
  return (
    <main className="main-page">
      <header className="main-header">
        <div className="main-logo">
          <div className="main-logo-mark" />
          <div>
            <div className="main-logo-title">Cognitive</div>
            <div className="main-logo-subtitle">Engine</div>
          </div>
        </div>

        <nav className="main-nav">
          <a href="#home" className="active">Главная</a>
          <a href="#products">Продукты</a>
          <a href="#about">О компании</a>
          <a href="#login">Вход</a>
          <a href="#lang">RU</a>
        </nav>
      </header>

      <section className="main-hero" id="home">
        <div className="main-hero-content">
          <h1>
            ПАК <span>«Когнитивные двигательные системы»</span> — платформа для
            диагностики, реабилитации и исследований
          </h1>

          <p>
            Разрабатываемый комплекс представляет собой 100-дюймовую вертикальную
            панель с ударопрочным покрытием, оснащенную датчиками движения для
            отслеживания позиции тела по 11 точкам. Взаимодействие происходит
            путем прямого контакта или дистанционно — с помощью датчиков.
          </p>

          <div className="main-actions">
            <button>Документация</button>
            <button className="secondary">О компании</button>
          </div>
        </div>

        <img
          className="main-hero-image"
          src="https://www.figma.com/api/mcp/asset/c8893458-81bf-4835-a60e-bb63890546fd"
          alt="Когнитивная двигательная система"
        />
      </section>
            <section className="analytics-section">
        <h2>Детальная аналитика каждой тренировки</h2>

        <div className="analytics-cards">
          <article className="analytics-card">
            <img
              src="https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=900&q=80"
              alt="Sensomode"
            />
            <div className="analytics-card-label">
              Для режима Sensomode
            </div>
          </article>

          <article className="analytics-card">
            <img
              src="https://images.unsplash.com/photo-1559757148-5c350d0d3c56?auto=format&fit=crop&w=900&q=80"
              alt="CognitiveEngine"
            />
            <div className="analytics-card-label">
              Для режима CognitiveEngine
            </div>
          </article>
        </div>

        <p className="analytics-report">
          <strong>Формат выгрузки отчетов:</strong> XLS, PDF, CSV.
        </p>

        <div className="analytics-line" />
      </section>
            <section className="protocols-section">
        <div className="section-line" />

        <h2>Базовые упражнения (10 протоколов)</h2>

        <div className="protocol-list">
          <article className="protocol-item">
            <div className="protocol-arrow">&gt;</div>
            <div className="protocol-icon">♿</div>
            <div className="protocol-content">
              <h3>Статические упражнения</h3>
              <ul>
                <li>Удержание точки в середине мишени</li>
                <li>Удержание одновременно 2-3 точек в середине мишени</li>
                <li>Удержание точек в пульсирующем круге (эффект сжатия/расширения)</li>
              </ul>
            </div>
          </article>

          <article className="protocol-item">
            <div className="protocol-arrow">&gt;</div>
            <div className="protocol-icon">🏃</div>
            <div className="protocol-content">
              <h3>Динамические упражнения</h3>
              <ul>
                <li>Прохождение по фигурам (квадрат, треугольник, пятиугольник, звезда)</li>
                <li>Движение по дуге с поиском кругов и без</li>
                <li>«Трицикл» — прохождение трех вложенных окружностей</li>
                <li>«Архимедова кривая» — движение по спирали</li>
              </ul>
            </div>
          </article>

          <article className="protocol-item">
            <div className="protocol-arrow">&gt;</div>
            <div className="protocol-icon">🏃</div>
            <div className="protocol-content">
              <h3>Объемные упражнения</h3>
              <ul>
                <li>Следование за нейсмикером в трехмерном кубе (для 3D-очков)</li>
                <li>Работа с эллипсами в 3D-пространстве</li>
              </ul>
            </div>
          </article>

          <article className="protocol-item">
            <div className="protocol-arrow">&gt;</div>
            <div className="protocol-icon">🛡</div>
            <div className="protocol-content">
              <h3>Сенсомоторные упражнения</h3>
              <ul>
                <li>Выбор фигуры по цвету и форме в сетке 10×10</li>
                <li>Тесты рабочей памяти (1-back, 2-back, 3-back)</li>
              </ul>
            </div>
          </article>
        </div>

        <div className="section-line" />

        <section className="usage-section">
          <h2>
            Cognitive Engine уже используется в реальных тренировках и исследованиях
          </h2>

          <div className="usage-cards">
            <article className="usage-card">
              <div className="usage-card-title">
                <span className="doc-icon">▤</span>
                Результаты,<br />подтверждённые практикой
              </div>
              <p>
                Система Cognitive Engine уже применяется в работе специалистов,
                исследовательских центров и спортивных организаций. Тысячи тренировочных
                сессий позволяют не только улучшать когнитивные и двигательные функции,
                но и формируют базу данных для развития новых методов диагностики и
                реабилитации.
              </p>
              <p>
                Каждое движение, каждая реакция и каждый результат становятся частью
                интеллектуальной системы, которая делает тренировки точнее, эффективнее
                и персонализированнее.
              </p>
              <div className="usage-arrow">→</div>
            </article>

            <article className="usage-card">
              <div className="usage-card-title">
                <span className="doc-icon">▤</span>
                Нам доверяют<br />результаты
              </div>
              <p>
                Уже сегодня Cognitive Engine используется в тренировках,
                реабилитации и научных исследованиях. Пользователи проходят сотни
                сессий, улучшая скорость реакции, внимание и координацию.
              </p>
              <p>
                Система анализирует данные в реальном времени и помогает специалистам
                принимать более точные решения — от коррекции программ до построения
                индивидуальных протоколов развития.
              </p>
              <div className="usage-arrow">→</div>
            </article>

            <article className="usage-card">
              <div className="usage-card-title">
                <span className="doc-icon">▤</span>
                Технология, которая<br />развивается вместе с Вами
              </div>
              <p>
                Каждая тренировка в Cognitive Engine — это не просто упражнение,
                а вклад в развитие системы. Обезличенные данные помогают формировать
                новые подходы к тренировкам, реабилитации и исследованию когнитивных
                функций.
              </p>
              <p>
                Чем больше пользователей — тем точнее алгоритмы, тем эффективнее
                результаты.
              </p>
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
            <h2>Связаться с нами</h2>

            <select>
              <option>Тема</option>
              <option>Документация</option>
              <option>Демо</option>
              <option>Сотрудничество</option>
            </select>

            <input type="email" placeholder="E-mail для обратной связи*" />
            <input type="text" placeholder="Stassy08@gma" />

            <select>
              <option>Сразу после получения заявки</option>
              <option>В течение дня</option>
              <option>На этой неделе</option>
            </select>

            <textarea placeholder="Введите текст обращения" />

            <button type="button">Отправить обращение</button>
          </form>
        </section>
      </section>
    </main>
  );
}