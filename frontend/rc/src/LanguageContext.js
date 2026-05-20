import React, { createContext, useState, useContext, useEffect } from 'react';

const LanguageContext = createContext();

export const useLanguage = () => {
  const context = useContext(LanguageContext);
  if (!context) {
    throw new Error('useLanguage must be used within LanguageProvider');
  }
  return context;
};

export const LanguageProvider = ({ children }) => {
  const [language, setLanguage] = useState(() => {
    const saved = localStorage.getItem('language');
    // Если нет сохранённого языка - ставим русский
    if (!saved) {
      localStorage.setItem('language', 'ru');
      return 'ru';
    }
    // Если сохранён английский - меняем на русский (чтобы изначально был русский)
    if (saved === 'en') {
      localStorage.setItem('language', 'ru');
      return 'ru';
    }
    return saved;
  });

  const toggleLanguage = () => {
    const newLang = language === 'ru' ? 'en' : 'ru';
    console.log('Переключение языка на:', newLang);
    setLanguage(newLang);
    localStorage.setItem('language', newLang);
  };

  useEffect(() => {
    console.log('Язык изменился на:', language);
    localStorage.setItem('language', language);
  }, [language]);

  return (
    <LanguageContext.Provider value={{ language, toggleLanguage }}>
      {children}
    </LanguageContext.Provider>
  );
};