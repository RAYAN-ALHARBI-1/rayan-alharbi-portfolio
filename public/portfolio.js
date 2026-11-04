const menuToggle = document.querySelector(".menu-toggle");
const primaryNavigation = document.querySelector("#primary-navigation");
const projectList = document.querySelector("#project-list");
const certificateList = document.querySelector("#certificate-list");
const profileEyebrow = document.querySelector("#profile-eyebrow");
const profileBio = document.querySelector("#profile-bio");
const profileEmailButton = document.querySelector("#profile-email-button");
const profilePhoto = document.querySelector("#profile-photo");
const profileNameEn = document.querySelector("#profile-name-en");
const profileGraduation = document.querySelector("#profile-graduation");
const contactEmailHeading = document.querySelector("#contact-email-heading");
const contactEmail = document.querySelector("#contact-email");
const contactLinkedin = document.querySelector("#contact-linkedin");
const languageToggle = document.querySelector("#language-toggle");
const publicPortfolioCacheKey = "rayan-portfolio-public-cache-v2";
const languageStorageKey = "rayan-portfolio-language";
const translations = window.PortfolioTranslations || {};
let currentLanguage = "ar";
let currentPublicPortfolio = null;

function setMenuState(isOpen) {
  menuToggle.setAttribute("aria-expanded", String(isOpen));
  primaryNavigation.classList.toggle("is-open", isOpen);
}

if (menuToggle && primaryNavigation) {
  document.documentElement.classList.add("has-mobile-menu");

  menuToggle.addEventListener("click", () => {
    setMenuState(menuToggle.getAttribute("aria-expanded") !== "true");
  });

  primaryNavigation.addEventListener("click", (event) => {
    if (event.target.closest("a")) {
      setMenuState(false);
    }
  });

  window.addEventListener("resize", () => {
    if (window.innerWidth > 640) {
      setMenuState(false);
    }
  });
}

function isNonEmptyText(value) {
  return typeof value === "string" && value.trim().length > 0;
}

function isSupportedLanguage(value) {
  return value === "ar" || value === "en";
}

function getInitialLanguage() {
  try {
    const savedLanguage = window.localStorage.getItem(languageStorageKey);
    return isSupportedLanguage(savedLanguage) ? savedLanguage : "ar";
  } catch {
    return "ar";
  }
}

function translate(key, replacements = {}) {
  const dictionary = translations[currentLanguage] || translations.ar || {};
  const fallbackDictionary = translations.ar || {};
  const value = dictionary[key] || fallbackDictionary[key] || "";

  return Object.entries(replacements).reduce(
    (text, [placeholder, replacement]) => text.replaceAll(`{${placeholder}}`, replacement),
    value
  );
}

function getLocalizedText(item, key, legacyKey = "") {
  if (!item || typeof item !== "object") {
    return "";
  }

  const preferredValue = item[`${key}_${currentLanguage}`];
  const arabicValue = item[`${key}_ar`];
  const legacyValue = legacyKey ? item[legacyKey] : "";

  return [preferredValue, arabicValue, legacyValue].find(isNonEmptyText) || "";
}

function getProfileDisplayName(profile) {
  return getLocalizedText(profile, "full_name") || `${translate("brand.first")} ${translate("brand.last")}`;
}

function applyStaticTranslations() {
  document.documentElement.lang = currentLanguage;
  document.documentElement.dir = currentLanguage === "ar" ? "rtl" : "ltr";
  document.title = translate("meta.title");

  for (const element of document.querySelectorAll("[data-i18n]")) {
    const text = translate(element.dataset.i18n);
    if (isNonEmptyText(text)) {
      element.textContent = text;
    }
  }

  for (const element of document.querySelectorAll("[data-i18n-aria-label]")) {
    const label = translate(element.dataset.i18nAriaLabel);
    if (isNonEmptyText(label)) {
      element.setAttribute("aria-label", label);
    }
  }

  for (const element of document.querySelectorAll("[data-i18n-content]")) {
    const content = translate(element.dataset.i18nContent);
    if (isNonEmptyText(content)) {
      element.setAttribute("content", content);
    }
  }

  for (const element of document.querySelectorAll("[data-i18n-alt]")) {
    const name = getProfileDisplayName(currentPublicPortfolio?.profile);
    const altText = translate(element.dataset.i18nAlt, { name });
    if (isNonEmptyText(altText)) {
      element.setAttribute("alt", altText);
    }
  }

  if (languageToggle) {
    const languageText = translate("language.switch");
    if (isNonEmptyText(languageText)) {
      languageToggle.textContent = languageText;
    }
  }
}

function setLanguage(language, shouldPersist = true) {
  currentLanguage = isSupportedLanguage(language) ? language : "ar";
  applyStaticTranslations();

  if (shouldPersist) {
    try {
      window.localStorage.setItem(languageStorageKey, currentLanguage);
    } catch {}
  }

  if (currentPublicPortfolio) {
    renderPublicPortfolio(currentPublicPortfolio);
  }
}

function isPublicPortfolio(value) {
  return value
    && typeof value === "object"
    && value.profile
    && typeof value.profile === "object"
    && Array.isArray(value.projects)
    && Array.isArray(value.certificates);
}

function hasBilingualPublicPortfolio(value) {
  return isPublicPortfolio(value)
    && isNonEmptyText(value.profile?.headline_en)
    && isNonEmptyText(value.profile?.location_en)
    && value.projects.every((project) =>
      isNonEmptyText(project?.title_en)
      && isNonEmptyText(project?.category_en)
      && isNonEmptyText(project?.summary_en)
    )
    && value.certificates.every((certificate) =>
      isNonEmptyText(certificate?.title_en)
      && isNonEmptyText(certificate?.issuer_en)
      && isNonEmptyText(certificate?.description_en)
    );
}

function createFallbackPortfolio() {
  return {
    profile: {
      full_name_ar: "ريان صالح الحربي",
      full_name_en: "Rayan Saleh Alharbi",
      headline_ar: "مطور تطبيقات ويب وحلول برمجية متكاملة",
      headline_en: "Full-Stack Web Developer",
      bio_ar: "أطوّر تطبيقات ويب متكاملة وأعمل على قواعد البيانات والخدمات الخلفية، مع اهتمام بتحويل المتطلبات إلى تجربة عملية واضحة قابلة للتوسع.",
      bio_en: "I build integrated web applications and work with databases and backend services, focusing on turning requirements into clear, scalable practical experiences.",
      location_ar: "المدينة المنورة، المملكة العربية السعودية",
      location_en: "Madinah, Saudi Arabia",
      email: "Rayanalharbi272@gmail.com",
      linkedin_url: "https://www.linkedin.com/in/rayan-alharbi-617b1a3a6",
      photo_url: "/rayan-alharbi-portrait.jpeg",
      expected_graduation_year: 2027,
    },
    projects: [
      {
        slug: "student-performance-analytics",
        title_ar: "تحليلات الأداء الأكاديمي للطلاب",
        title_en: "Student Academic Performance Analytics",
        category_ar: "تحليل بيانات وتنقيب بيانات",
        category_en: "Data Analytics & Data Mining",
        summary_ar: "مشروع مستودع بيانات وتنقيب بيانات حلّل محددات الأداء الأكاديمي اعتمادًا على 6,607 سجلات و20 خاصية، مع معالجة البيانات الناقصة وعدم توازن الفئات.",
        summary_en: "A data warehouse and data mining project that analysed factors affecting academic performance using 6,607 records and 20 features, including missing-data handling and class-imbalance treatment.",
        technologies: ["Python", "Scikit-learn", "Pandas", "Data Visualization"],
        external_url: "https://drive.google.com/drive/folders/1F4AhHa_Bn77G_ZPqcFpe4QJtS0hIYTox?usp=drive_link",
      },
      {
        slug: "car-sales-booking-platform",
        title_ar: "منصة مبيعات وحجز السيارات",
        title_en: "Car Sales and Booking Platform",
        category_ar: "تطبيق ويب وجوال متكامل",
        category_en: "Integrated Web & Mobile Application",
        summary_ar: "تطبيق ويب وجوال متكامل لإدارة مبيعات السيارات والحجوزات، مع خلفية .NET Core وقاعدة SQL Server وتطبيق .NET MAUI متصل بالبيانات والخدمات نفسها.",
        summary_en: "An integrated web and mobile application for managing car sales and bookings, with a .NET Core backend, SQL Server database, and a .NET MAUI client connected to the same data and services.",
        technologies: [".NET Core", "SQL Server", ".NET MAUI", "APIs"],
        external_url: "https://drive.google.com/drive/folders/1WDSQWvHcaYM8iul6sgvw9m6q7Xuvx9Wm?usp=drive_link",
      },
    ],
    certificates: [
      {
        slug: "ibm-lifelong-professional-skills",
        title_ar: "المهارات المهنية المستمرة",
        title_en: "Lifelong Professional Skills",
        issuer_ar: "IBM SkillsBuild",
        issuer_en: "IBM SkillsBuild",
        description_ar: "إتمام برنامج المهارات المهنية المستمرة، الذي يركز على تطوير مهارات العمل والتواصل والتعلم المهني.",
        description_en: "Completed a professional skills program focused on workplace, communication, and lifelong-learning skills.",
        issued_on: "2026-07-19",
        credential_url: null,
      },
      {
        slug: "robotics-bootcamp",
        title_ar: "شهادات معسكر الروبوتات",
        title_en: "Robotics Bootcamp Certificates",
        issuer_ar: "معسكر الروبوتات",
        issuer_en: "Robotics Bootcamp",
        description_ar: "مجموعة شهادات متخصصة في أساسيات الروبوتات والأنظمة الذكية وتطبيقاتها العملية.",
        description_en: "A set of specialized certificates covering robotics fundamentals, intelligent systems, and practical applications.",
        issued_on: null,
        credential_url: "/certificates/Robotics%20Bootcamp%20Certificates%20-%20RAYAN%20SALEH%20ALHARBI.pdf",
      },
      {
        slug: "introduction-to-robotics",
        title_ar: "مقدمة في الروبوتات",
        title_en: "Introduction to Robotics",
        issuer_ar: "الروبوتات",
        issuer_en: "Robotics",
        description_ar: "مدخل إلى مفاهيم الروبوتات ومكوّناتها وطريقة عمل الأنظمة الروبوتية.",
        description_en: "An introduction to robotics concepts, components, and the operation of robotic systems.",
        issued_on: null,
        credential_url: "/certificates/Introduction%20to%20robotics%20-%20Rayan%20Saleh%20Alharbi.pdf",
      },
      {
        slug: "generative-ai-agents-robotics",
        title_ar: "من الذكاء الاصطناعي التوليدي إلى الوكلاء والروبوتات",
        title_en: "From Generative AI to Agents & Robotics",
        issuer_ar: "الذكاء الاصطناعي والروبوتات",
        issuer_en: "AI & Robotics",
        description_ar: "استكشاف انتقال الذكاء الاصطناعي التوليدي إلى الوكلاء والأنظمة الروبوتية.",
        description_en: "Explored the progression from generative AI to agents and robotic systems.",
        issued_on: null,
        credential_url: "/certificates/From%20Generative%20AI%20to%20Agents%20%26%20Robotics%20-%20Rayan%20Saleh%20Alharbi.pdf",
      },
      {
        slug: "robotics-security",
        title_ar: "أمن الروبوتات",
        title_en: "Robotics Security",
        issuer_ar: "أمن الروبوتات",
        issuer_en: "Robotics Security",
        description_ar: "مبادئ حماية الأنظمة الروبوتية والاعتبارات الأمنية في بيئاتها المتصلة.",
        description_en: "Learned principles for protecting robotic systems and addressing security considerations in connected environments.",
        issued_on: null,
        credential_url: "/certificates/Robotics%20security%20-%20Rayan%20Saleh%20Alharbi.pdf",
      },
      {
        slug: "uav-swarm-deployment",
        title_ar: "نشر أسراب الطائرات المسيّرة",
        title_en: "UAV Swarm Deployment",
        issuer_ar: "أنظمة الطائرات المسيّرة",
        issuer_en: "UAV Systems",
        description_ar: "تطبيقات نشر أسراب الطائرات المسيّرة والتنسيق بين الأنظمة ذاتية التشغيل.",
        description_en: "Explored UAV swarm deployment and coordination across autonomous systems.",
        issued_on: null,
        credential_url: "/certificates/UAV%20Swarm%20Deployment%20-%20RAYAN%20SALEH%20ALHARBI.pdf",
      },
      {
        slug: "bio-inspired-swarms",
        title_ar: "من روبوت واحد إلى أسراب مستوحاة من الأنظمة الحيوية",
        title_en: "From One Robot to Bio-Inspired Swarms",
        issuer_ar: "أنظمة مستوحاة من الطبيعة",
        issuer_en: "Bio-Inspired Systems",
        description_ar: "مفاهيم الانتقال من الروبوتات الفردية إلى أسراب مستوحاة من الأنظمة الحيوية.",
        description_en: "Explored the transition from individual robots to swarms inspired by biological systems.",
        issued_on: null,
        credential_url: "/certificates/from%20one%20robot%20to%20bio%20inspired%20swarms%20-%20Rayan%20Saleh%20Alharbi.pdf",
      },
    ],
  };
}

function getCachedPublicPortfolio() {
  try {
    const cachedValue = window.localStorage.getItem(publicPortfolioCacheKey);
    if (!cachedValue) {
      return null;
    }

    const portfolio = JSON.parse(cachedValue);
    return hasBilingualPublicPortfolio(portfolio) ? portfolio : null;
  } catch {
    window.localStorage.removeItem(publicPortfolioCacheKey);
    return null;
  }
}

function cachePublicPortfolio(portfolio) {
  if (!hasBilingualPublicPortfolio(portfolio)) {
    return;
  }

  try {
    window.localStorage.setItem(publicPortfolioCacheKey, JSON.stringify(portfolio));
  } catch {
    return;
  }
}

function getSafeUrl(value) {
  if (!isNonEmptyText(value)) {
    return null;
  }

  try {
    const url = new URL(value, window.location.origin);
    return url.protocol === "https:" || url.origin === window.location.origin ? url : null;
  } catch {
    return null;
  }
}

function addExternalLink(parent, label, value) {
  const url = getSafeUrl(value);
  if (!url) {
    return;
  }

  const link = document.createElement("a");
  link.href = url.href;
  link.target = "_blank";
  link.rel = "noopener noreferrer";
  link.textContent = label;

  const arrow = document.createElement("span");
  arrow.setAttribute("aria-hidden", "true");
  arrow.textContent = " ↗";
  link.append(arrow);
  parent.append(link);
}

function formatCertificateDate(value) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value || "")) {
    return "";
  }

  const date = new Date(`${value}T12:00:00Z`);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  const locale = currentLanguage === "en" ? "en-GB" : "ar-SA-u-ca-gregory";
  return new Intl.DateTimeFormat(locale, {
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(date);
}

function createProjectCard(project, index) {
  const card = document.createElement("article");
  card.className = index === 0 ? "project-card project-card--dark" : "project-card";

  const number = document.createElement("p");
  number.className = "project-number";
  number.textContent = String(index + 1).padStart(2, "0");

  const category = document.createElement("p");
  category.className = "project-kind";
  category.textContent = getLocalizedText(project, "category");

  const title = document.createElement("h3");
  title.textContent = getLocalizedText(project, "title");

  const summary = document.createElement("p");
  summary.textContent = getLocalizedText(project, "summary");

  const technologies = Array.isArray(project.technologies)
    ? project.technologies.filter(isNonEmptyText).slice(0, 16)
    : [];
  const technologyList = document.createElement("ul");
  technologyList.className = "tag-list";
  for (const technology of technologies) {
    const item = document.createElement("li");
    item.textContent = technology;
    technologyList.append(item);
  }

  card.append(number, category, title, summary);
  if (technologies.length > 0) {
    card.append(technologyList);
  }
  addExternalLink(card, translate("projects.view"), project.external_url);
  return card;
}

function createCertificateCard(certificate, index) {
  const card = document.createElement("article");
  card.className = index === 0 ? "certificate-card certificate-card--featured" : "certificate-card";

  const issuer = document.createElement("p");
  issuer.className = "certificate-card__issuer";
  issuer.textContent = getLocalizedText(certificate, "issuer", "issuer");

  const title = document.createElement("h3");
  title.textContent = getLocalizedText(certificate, "title", "title");

  const description = document.createElement("p");
  description.textContent = getLocalizedText(certificate, "description");

  card.append(issuer, title, description);

  const issuedOn = formatCertificateDate(certificate.issued_on);
  if (issuedOn) {
    const date = document.createElement("span");
    date.textContent = issuedOn;
    card.append(date);
  }

  addExternalLink(card, translate("certifications.open"), certificate.credential_url);
  return card;
}

function createEmptyState(message) {
  const state = document.createElement("p");
  state.className = "portfolio-empty-state";
  state.textContent = message;
  return state;
}

function renderProjects(projects) {
  if (!projectList || !Array.isArray(projects)) {
    return;
  }

  const validProjects = projects.filter((project) =>
    isNonEmptyText(getLocalizedText(project, "title")) &&
    isNonEmptyText(getLocalizedText(project, "category")) &&
    isNonEmptyText(getLocalizedText(project, "summary"))
  );

  projectList.replaceChildren(
    ...(validProjects.length > 0
      ? validProjects.map(createProjectCard)
      : [createEmptyState(translate("projects.empty"))])
  );
}

function renderCertificates(certificates) {
  if (!certificateList || !Array.isArray(certificates)) {
    return;
  }

  const validCertificates = certificates.filter((certificate) =>
    isNonEmptyText(getLocalizedText(certificate, "title", "title")) &&
    isNonEmptyText(getLocalizedText(certificate, "issuer", "issuer")) &&
    isNonEmptyText(getLocalizedText(certificate, "description"))
  );

  certificateList.replaceChildren(
    ...(validCertificates.length > 0
      ? validCertificates.map(createCertificateCard)
      : [createEmptyState(translate("certifications.empty"))])
  );
}

function updateEmailLinks(email) {
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email || "")) {
    return;
  }

  const mailto = `mailto:${email}`;
  for (const link of [profileEmailButton, contactEmailHeading, contactEmail]) {
    if (link) {
      link.href = mailto;
    }
  }

  if (contactEmail) {
    contactEmail.textContent = email;
  }
}

function updateProfile(profile) {
  if (!profile || typeof profile !== "object") {
    return;
  }

  const headline = getLocalizedText(profile, "headline");
  const location = getLocalizedText(profile, "location");
  if (profileEyebrow && isNonEmptyText(headline) && isNonEmptyText(location)) {
    profileEyebrow.textContent = `${headline} · ${location}`;
  }
  const biography = getLocalizedText(profile, "bio");
  if (profileBio && isNonEmptyText(biography)) {
    profileBio.textContent = biography;
  }
  if (profileNameEn && isNonEmptyText(profile.full_name_en)) {
    profileNameEn.textContent = profile.full_name_en.toUpperCase();
  }
  if (profileGraduation && Number.isInteger(profile.expected_graduation_year)) {
    profileGraduation.textContent = String(profile.expected_graduation_year);
  }

  const photoUrl = getSafeUrl(profile.photo_url);
  if (profilePhoto && photoUrl) {
    profilePhoto.src = photoUrl.href;
    profilePhoto.alt = translate("profile.photoAlt", { name: getProfileDisplayName(profile) });
  }

  updateEmailLinks(profile.email);

  const linkedinUrl = getSafeUrl(profile.linkedin_url);
  if (contactLinkedin && linkedinUrl) {
    contactLinkedin.href = linkedinUrl.href;
  }
}

function renderPublicPortfolio(portfolio) {
  if (!isPublicPortfolio(portfolio)) {
    return;
  }

  currentPublicPortfolio = portfolio;
  applyStaticTranslations();
  updateProfile(portfolio.profile);
  renderProjects(portfolio.projects);
  renderCertificates(portfolio.certificates);
}

async function loadPublicPortfolio() {
  try {
    const response = await fetch("/api/v1/portfolio", {
      headers: { Accept: "application/json" },
      cache: "no-store",
    });
    if (!response.ok) {
      return;
    }

    const portfolio = await response.json();
    if (!hasBilingualPublicPortfolio(portfolio)) {
      return;
    }

    renderPublicPortfolio(portfolio);
    cachePublicPortfolio(portfolio);
  } catch {
    return;
  }
}

currentPublicPortfolio = getCachedPublicPortfolio() || createFallbackPortfolio();
setLanguage(getInitialLanguage(), false);

if (languageToggle) {
  languageToggle.addEventListener("click", () => {
    setLanguage(currentLanguage === "ar" ? "en" : "ar");
  });
}

if (currentPublicPortfolio) {
  renderPublicPortfolio(currentPublicPortfolio);
}

void loadPublicPortfolio();
