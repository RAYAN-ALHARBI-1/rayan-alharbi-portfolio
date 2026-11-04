const certificateForm = document.querySelector("#certificate-form");
const certificateList = document.querySelector("#certificate-list");
const certificateState = document.querySelector("#certificate-state");
const certificateFormState = document.querySelector("#certificate-form-state");
const certificateFormTitle = document.querySelector("#certificate-form-title");
const certificateFormLabel = document.querySelector("#certificate-form-label");
const certificateSubmitButton = document.querySelector("#certificate-submit-button");
const certificateCancelButton = document.querySelector("#certificate-cancel-button");
const profileForm = document.querySelector("#profile-form");
const profileState = document.querySelector("#profile-state");
const projectForm = document.querySelector("#project-form");
const projectState = document.querySelector("#project-state");
const projectFormTitle = document.querySelector("#project-form-title");
const projectFormLabel = document.querySelector("#project-form-label");
const projectSubmitButton = document.querySelector("#project-submit-button");
const projectCancelButton = document.querySelector("#project-cancel-button");
const projectList = document.querySelector("#project-list");
const projectListState = document.querySelector("#project-list-state");
const signOutButton = document.querySelector("#sign-out-button");
const deleteDialog = document.querySelector("#delete-dialog");
const deleteDialogMessage = document.querySelector("#delete-dialog-message");
const csrfStorageKey = "portfolio-admin-csrf";
let pendingDeletion = null;

function setState(message) {
  certificateState.textContent = message;
}

function setProfileState(message) {
  profileState.textContent = message;
}

function setCertificateFormState(message) {
  certificateFormState.textContent = message;
}

function setProjectState(message) {
  projectState.textContent = message;
}

function createActionButton(label, action, itemName, handler) {
  const button = document.createElement("button");
  button.className = action === "delete" ? "danger-button" : "secondary-button";
  button.type = "button";
  button.textContent = label;
  button.setAttribute("aria-label", `${label}: ${itemName}`);
  button.addEventListener("click", handler);
  return button;
}

function createCertificateItem(certificate) {
  const item = document.createElement("article");
  item.className = "certificate-item";

  const details = document.createElement("div");
  details.className = "certificate-item__details";
  const title = document.createElement("h3");
  title.textContent = certificate.title_ar;
  const issuer = document.createElement("p");
  issuer.textContent = `${certificate.title_en} · ${certificate.issuer_ar}`;
  details.append(title, issuer);

  const status = document.createElement("span");
  status.className = certificate.is_published ? "status" : "status status--draft";
  status.textContent = certificate.is_published ? "منشورة" : "مسودة";
  const actions = document.createElement("div");
  actions.className = "item-actions";
  actions.append(
    createActionButton("تعديل", "edit", certificate.title_ar, () => startCertificateEdit(certificate)),
    createActionButton("حذف", "delete", certificate.title_ar, () => openDeleteDialog("certificate", certificate))
  );
  item.append(details, status, actions);
  return item;
}

function createProjectItem(project) {
  const item = document.createElement("article");
  item.className = "certificate-item";

  const details = document.createElement("div");
  details.className = "certificate-item__details";
  const title = document.createElement("h3");
  title.textContent = project.title_ar;
  const category = document.createElement("p");
  category.textContent = `${project.title_en} · ${project.category_ar}`;
  details.append(title, category);

  const status = document.createElement("span");
  status.className = project.is_published ? "status" : "status status--draft";
  status.textContent = project.is_published ? "منشور" : "مسودة";
  const actions = document.createElement("div");
  actions.className = "item-actions";
  actions.append(
    createActionButton("تعديل", "edit", project.title_ar, () => startProjectEdit(project)),
    createActionButton("حذف", "delete", project.title_ar, () => openDeleteDialog("project", project))
  );
  item.append(details, status, actions);
  return item;
}

async function requestAdmin(path, options = {}) {
  const method = options.method || "GET";
  const headers = new Headers(options.headers || {});
  if (method !== "GET") {
    headers.set("X-CSRF-Token", sessionStorage.getItem(csrfStorageKey) || "");
  }

  const response = await fetch(path, {
    ...options,
    credentials: "same-origin",
    headers,
  });
  const contentType = response.headers.get("content-type") || "";
  const body = contentType.includes("application/json") ? await response.json() : null;
  if (!response.ok) {
    throw new Error(body?.error || "تعذّر إكمال الطلب.");
  }
  return body;
}

function resetCertificateForm() {
  certificateForm.reset();
  certificateForm.elements.isPublished.checked = true;
  delete certificateForm.dataset.editingSlug;
  certificateFormLabel.textContent = "إضافة";
  certificateFormTitle.textContent = "شهادة جديدة";
  certificateSubmitButton.textContent = "حفظ الشهادة";
  certificateCancelButton.classList.add("is-hidden");
  setCertificateFormState("");
}

function startCertificateEdit(certificate) {
  certificateForm.dataset.editingSlug = certificate.slug;
  certificateForm.elements.titleAr.value = certificate.title_ar || "";
  certificateForm.elements.titleEn.value = certificate.title_en || "";
  certificateForm.elements.issuerAr.value = certificate.issuer_ar || "";
  certificateForm.elements.issuerEn.value = certificate.issuer_en || "";
  certificateForm.elements.descriptionAr.value = certificate.description_ar || "";
  certificateForm.elements.descriptionEn.value = certificate.description_en || "";
  certificateForm.elements.issuedOn.value = certificate.issued_on || "";
  certificateForm.elements.credentialUrl.value = certificate.credential_url || "";
  certificateForm.elements.isPublished.checked = Boolean(certificate.is_published);
  certificateFormLabel.textContent = "تعديل";
  certificateFormTitle.textContent = "تعديل الشهادة";
  certificateSubmitButton.textContent = "حفظ التعديلات";
  certificateCancelButton.classList.remove("is-hidden");
  setCertificateFormState(`تعدّل الآن: ${certificate.title_ar}`);
  certificateForm.scrollIntoView({ behavior: "smooth", block: "start" });
  certificateForm.elements.titleAr.focus();
}

function resetProjectForm() {
  projectForm.reset();
  projectForm.elements.isPublished.checked = true;
  delete projectForm.dataset.editingSlug;
  projectFormLabel.textContent = "إضافة";
  projectFormTitle.textContent = "مشروع جديد";
  projectSubmitButton.textContent = "حفظ المشروع";
  projectCancelButton.classList.add("is-hidden");
  setProjectState("");
}

function startProjectEdit(project) {
  projectForm.dataset.editingSlug = project.slug;
  projectForm.elements.titleAr.value = project.title_ar || "";
  projectForm.elements.titleEn.value = project.title_en || "";
  projectForm.elements.categoryAr.value = project.category_ar || "";
  projectForm.elements.categoryEn.value = project.category_en || "";
  projectForm.elements.summaryAr.value = project.summary_ar || "";
  projectForm.elements.summaryEn.value = project.summary_en || "";
  projectForm.elements.technologies.value = (project.technologies || []).join("، ");
  projectForm.elements.externalUrl.value = project.external_url || "";
  projectForm.elements.coverImageUrl.value = project.cover_image_url || "";
  projectForm.elements.isPublished.checked = Boolean(project.is_published);
  projectFormLabel.textContent = "تعديل";
  projectFormTitle.textContent = "تعديل المشروع";
  projectSubmitButton.textContent = "حفظ التعديلات";
  projectCancelButton.classList.remove("is-hidden");
  setProjectState(`تعدّل الآن: ${project.title_ar}`);
  projectForm.scrollIntoView({ behavior: "smooth", block: "start" });
  projectForm.elements.titleAr.focus();
}

function openDeleteDialog(kind, item) {
  pendingDeletion = { kind, slug: item.slug, title: item.title_ar };
  deleteDialogMessage.textContent = `سيُحذف «${pendingDeletion.title}» نهائيًا ولا يمكن التراجع عن ذلك.`;
  deleteDialog.showModal();
}

async function deleteContent({ kind, slug, title }) {
  const isProject = kind === "project";
  const path = isProject ? "/api/v1/admin/projects" : "/api/v1/admin/certificates";

  try {
    await requestAdmin(`${path}/${encodeURIComponent(slug)}`, { method: "DELETE" });
    if (isProject) {
      if (projectForm.dataset.editingSlug === slug) {
        resetProjectForm();
      }
      await loadProjects();
      setProjectState(`تم حذف «${title}».`);
    } else {
      if (certificateForm.dataset.editingSlug === slug) {
        resetCertificateForm();
      }
      await loadCertificates();
      setCertificateFormState(`تم حذف «${title}».`);
    }
  } catch (error) {
    if (isProject) {
      setProjectState(error.message);
    } else {
      setCertificateFormState(error.message);
    }
  }
}

async function loadCertificates() {
  setState("جارٍ تحميل الشهادات…");
  certificateList.replaceChildren();
  const certificates = await requestAdmin("/api/v1/admin/certificates");
  certificateList.replaceChildren(...certificates.map(createCertificateItem));
  setState(certificates.length ? "" : "لا توجد شهادات مضافة بعد.");
}

function fillProfileForm(profile) {
  if (!profile) {
    setProfileState("لم تُهيأ بيانات الملف الشخصي بعد.");
    return;
  }

  profileForm.elements.fullNameAr.value = profile.full_name_ar || "";
  profileForm.elements.fullNameEn.value = profile.full_name_en || "";
  profileForm.elements.headlineAr.value = profile.headline_ar || "";
  profileForm.elements.headlineEn.value = profile.headline_en || "";
  profileForm.elements.bioAr.value = profile.bio_ar || "";
  profileForm.elements.bioEn.value = profile.bio_en || "";
  profileForm.elements.locationAr.value = profile.location_ar || "";
  profileForm.elements.locationEn.value = profile.location_en || "";
  profileForm.elements.email.value = profile.email || "";
  profileForm.elements.linkedinUrl.value = profile.linkedin_url || "";
  profileForm.elements.photoUrl.value = profile.photo_url || "";
  profileForm.elements.expectedGraduationYear.value = profile.expected_graduation_year || "";
}

async function loadProfile() {
  const profile = await requestAdmin("/api/v1/admin/profile");
  fillProfileForm(profile);
}

async function loadProjects() {
  projectListState.textContent = "جارٍ تحميل المشاريع…";
  projectList.replaceChildren();
  const projects = await requestAdmin("/api/v1/admin/projects");
  projectList.replaceChildren(...projects.map(createProjectItem));
  projectListState.textContent = projects.length ? "" : "لا توجد مشاريع مضافة بعد.";
}

async function initialiseDashboard() {
  try {
    const session = await requestAdmin("/api/v1/admin/session");
    sessionStorage.setItem(csrfStorageKey, session.csrfToken);
  } catch {
    sessionStorage.removeItem(csrfStorageKey);
    window.location.replace("admin-login.html");
    return;
  }

  try {
    await Promise.all([loadProfile(), loadCertificates(), loadProjects()]);
  } catch (error) {
    const message = error.message || "تعذّر تحميل المحتوى حاليًا.";
    setProfileState(message);
    setState(message);
    projectListState.textContent = message;
  }
}

certificateForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const formData = new FormData(certificateForm);
  const payload = Object.fromEntries(formData.entries());
  payload.isPublished = formData.get("isPublished") === "on";
  const editingSlug = certificateForm.dataset.editingSlug;

  try {
    await requestAdmin(editingSlug ? `/api/v1/admin/certificates/${encodeURIComponent(editingSlug)}` : "/api/v1/admin/certificates", {
      method: editingSlug ? "PUT" : "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
    const message = editingSlug ? "تم حفظ تعديلات الشهادة." : "تم حفظ الشهادة.";
    resetCertificateForm();
    await loadCertificates();
    setCertificateFormState(message);
  } catch (error) {
    setCertificateFormState(error.message);
  }
});

profileForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const formData = new FormData(profileForm);
  const payload = Object.fromEntries(formData.entries());

  try {
    await requestAdmin("/api/v1/admin/profile", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
    setProfileState("تم حفظ معلومات الملف الشخصي.");
  } catch (error) {
    setProfileState(error.message);
  }
});

projectForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const formData = new FormData(projectForm);
  const payload = Object.fromEntries(formData.entries());
  payload.technologies = String(payload.technologies || "")
    .split(/[,،\n]/)
    .map((technology) => technology.trim())
    .filter(Boolean);
  payload.isPublished = formData.get("isPublished") === "on";
  const editingSlug = projectForm.dataset.editingSlug;

  try {
    await requestAdmin(editingSlug ? `/api/v1/admin/projects/${encodeURIComponent(editingSlug)}` : "/api/v1/admin/projects", {
      method: editingSlug ? "PUT" : "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
    const message = editingSlug ? "تم حفظ تعديلات المشروع." : "تم حفظ المشروع.";
    resetProjectForm();
    await loadProjects();
    setProjectState(message);
  } catch (error) {
    setProjectState(error.message);
  }
});

certificateCancelButton.addEventListener("click", resetCertificateForm);
projectCancelButton.addEventListener("click", resetProjectForm);

deleteDialog.addEventListener("close", () => {
  const deletion = pendingDeletion;
  const shouldDelete = deleteDialog.returnValue === "confirm";
  pendingDeletion = null;

  if (shouldDelete && deletion) {
    void deleteContent(deletion);
  }
});

signOutButton.addEventListener("click", () => {
  requestAdmin("/api/v1/auth/logout", { method: "POST" }).finally(() => {
    sessionStorage.removeItem(csrfStorageKey);
    certificateList.replaceChildren();
    window.location.replace("admin-login.html");
  });
});

initialiseDashboard();
