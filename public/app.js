const form = document.querySelector("#resume-form");
const textArea = document.querySelector("#resume-text");
const result = document.querySelector("#resume-result");

function createResult(title, items) {
  const heading = document.createElement("h3");
  heading.textContent = title;
  const list = document.createElement("ul");
  items.forEach((item) => {
    const listItem = document.createElement("li");
    listItem.textContent = item;
    list.append(listItem);
  });
  result.replaceChildren(heading, list);
  result.hidden = false;
}

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  const text = textArea.value.trim();

  if (!text) {
    createResult("أضف نصًا أولًا", ["استخدم نصًا تدريبيًا فقط، ولا تلصق معلومات شخصية أو سيرة حقيقية."]);
    return;
  }

  try {
    const response = await fetch("/api/resume-preview", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ text }),
    });
    const analysis = await response.json();

    if (!response.ok) throw new Error(analysis.error);

    const skills = analysis.matchedSkills.length ? `مهارات ظهرت في النص: ${analysis.matchedSkills.join("، ")}.` : "لم تظهر كلمات تقنية من القائمة التجريبية.";
    createResult("نتيجة المعاينة", [analysis.notice, `عدد الأحرف: ${analysis.length}.`, skills, ...analysis.suggestions]);
  } catch (error) {
    createResult("تعذّر التحليل", [error.message || "حاول مجددًا لاحقًا."]);
  }
});
