import { createReadStream, existsSync, statSync } from "node:fs";
import { createServer } from "node:http";
import { extname, join, normalize } from "node:path";

const port = Number(process.env.PORT) || 3000;
const publicDirectory = join(process.cwd(), "public");
const mimeTypes = {
  ".css": "text/css; charset=utf-8",
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".svg": "image/svg+xml",
};

function sendJson(response, statusCode, body) {
  response.writeHead(statusCode, {
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store",
    "X-Content-Type-Options": "nosniff",
  });
  response.end(JSON.stringify(body));
}

function serveStatic(requestPath, response) {
  const requestedFile = requestPath === "/" ? "portfolio.html" : requestPath.slice(1);
  const filePath = normalize(join(publicDirectory, requestedFile));

  if (!filePath.startsWith(publicDirectory) || !existsSync(filePath) || statSync(filePath).isDirectory()) {
    response.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" });
    response.end("Not found");
    return;
  }

  response.writeHead(200, {
    "Content-Type": mimeTypes[extname(filePath)] || "application/octet-stream",
    "X-Content-Type-Options": "nosniff",
    "Referrer-Policy": "strict-origin-when-cross-origin",
  });
  createReadStream(filePath).pipe(response);
}

function readBody(request) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let size = 0;
    request.on("data", (chunk) => {
      size += chunk.length;
      if (size > 1_000_000) {
        reject(new Error("Request too large"));
        request.destroy();
        return;
      }
      chunks.push(chunk);
    });
    request.on("end", () => resolve(Buffer.concat(chunks).toString("utf8")));
    request.on("error", reject);
  });
}

function analyseResumeText(text) {
  const normalizedText = String(text || "").trim();
  const keywords = ["JavaScript", "Node.js", "Python", "HTML", "CSS", "Git", "SQL", "React", "API"];
  const matchedSkills = keywords.filter((skill) => normalizedText.toLowerCase().includes(skill.toLowerCase()));

  return {
    notice: "تحليل تجريبي للتدريب فقط. لا يتم حفظ النص أو الملف.",
    length: normalizedText.length,
    matchedSkills,
    suggestions: [
      normalizedText.length < 250 ? "أضف ملخصًا أو تفاصيل أكثر عن المشاريع والخبرات." : "مستوى التفاصيل مناسب للمراجعة الأولية.",
      matchedSkills.length < 3 ? "أضف المهارات التقنية الأساسية بصياغة واضحة." : "المهارات التقنية ظاهرة بشكل جيد.",
      "في النسخة الفعلية يُستبدل هذا المنطق بخدمة تحليل مصرح بها وسياسة خصوصية واضحة.",
    ],
  };
}

const server = createServer(async (request, response) => {
  const requestUrl = new URL(request.url, `http://${request.headers.host}`);

  if (request.method === "GET" && requestUrl.pathname === "/health") {
    sendJson(response, 200, { status: "ok", service: "ryan-portfolio" });
    return;
  }

  if (request.method === "POST" && requestUrl.pathname === "/api/resume-preview") {
    try {
      const body = JSON.parse(await readBody(request));
      sendJson(response, 200, analyseResumeText(body.text));
    } catch {
      sendJson(response, 400, { error: "تعذّر قراءة النص المرسل. جرّب نصًا مختصرًا فقط." });
    }
    return;
  }

  if (request.method === "GET") {
    serveStatic(requestUrl.pathname, response);
    return;
  }

  sendJson(response, 405, { error: "Method not allowed" });
});

server.listen(port, () => {
  console.log(`Portfolio is running at http://localhost:${port}`);
});
