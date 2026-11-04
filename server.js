import { createReadStream, existsSync, statSync } from "node:fs";
import { createServer } from "node:http";
import { createHash, randomBytes, randomUUID, timingSafeEqual } from "node:crypto";
import { extname, isAbsolute, relative, resolve } from "node:path";

const port = Number(process.env.PORT) || 3000;
const publicDirectory = resolve(process.cwd(), "public");
const databaseUrl = process.env.DATABASE_URL;
const googleClientId = process.env.GOOGLE_CLIENT_ID;
const publicOrigin = process.env.PUBLIC_ORIGIN;
const isProduction = process.env.NODE_ENV === "production";
const allowedAdminEmails = new Set(
  String(process.env.ADMIN_ALLOWED_EMAILS || "")
    .split(",")
    .map((email) => email.trim().toLowerCase())
    .filter(Boolean),
);
const ownerAdminEmails = new Set(
  String(process.env.ADMIN_OWNER_EMAILS || "")
    .split(",")
    .map((email) => email.trim().toLowerCase())
    .filter(Boolean),
);
let databasePool;
const mimeTypes = {
  ".css": "text/css; charset=utf-8",
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".jpeg": "image/jpeg",
  ".jpg": "image/jpeg",
  ".pdf": "application/pdf",
  ".svg": "image/svg+xml",
};

function sendJson(response, statusCode, body, headers = {}) {
  response.writeHead(statusCode, {
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store",
    "X-Content-Type-Options": "nosniff",
    "X-Frame-Options": "DENY",
    ...headers,
  });
  response.end(JSON.stringify(body));
}

function hashValue(value) {
  return createHash("sha256").update(value).digest("hex");
}

function parseCookies(request) {
  return Object.fromEntries(
    String(request.headers.cookie || "")
      .split(";")
      .map((value) => value.trim())
      .filter(Boolean)
      .map((value) => {
        const separatorIndex = value.indexOf("=");
        return separatorIndex === -1
          ? [value, ""]
          : [value.slice(0, separatorIndex), decodeURIComponent(value.slice(separatorIndex + 1))];
      }),
  );
}

function createSessionCookie(token, maxAgeSeconds) {
  const attributes = [
    `admin_session=${encodeURIComponent(token)}`,
    "HttpOnly",
    "Path=/",
    "SameSite=Strict",
    `Max-Age=${maxAgeSeconds}`,
  ];
  if (isProduction) {
    attributes.push("Secure");
  }
  return attributes.join("; ");
}

function clearSessionCookie() {
  const attributes = ["admin_session=", "HttpOnly", "Path=/", "SameSite=Strict", "Max-Age=0"];
  if (isProduction) {
    attributes.push("Secure");
  }
  return attributes.join("; ");
}

async function getDatabasePool() {
  if (!databaseUrl) {
    throw new Error("Database is not configured.");
  }

  if (!databasePool) {
    const { Pool } = await import("pg");
    databasePool = new Pool({
      connectionString: databaseUrl,
      max: 5,
      idleTimeoutMillis: 30_000,
    });
  }

  return databasePool;
}

async function getPortfolioData() {
  const pool = await getDatabasePool();
  const [profileResult, projectsResult, certificatesResult] = await Promise.all([
    pool.query(
      `SELECT full_name_ar, full_name_en, headline_ar, location_ar, email, linkedin_url, expected_graduation_year
       FROM profiles
       WHERE id = 1`,
    ),
    pool.query(
      `SELECT slug, title_ar, category_ar, summary_ar, technologies, external_url
       FROM projects
       WHERE is_published = TRUE
       ORDER BY sort_order ASC, created_at ASC`,
    ),
    pool.query(
      `SELECT slug, title, issuer, description_ar, issued_on, credential_url
       FROM certificates
       WHERE is_published = TRUE
       ORDER BY sort_order ASC, created_at ASC`,
    ),
  ]);

  return {
    profile: profileResult.rows[0] || null,
    projects: projectsResult.rows,
    certificates: certificatesResult.rows,
  };
}

function readJsonBody(request) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let size = 0;

    request.on("data", (chunk) => {
      size += chunk.length;
      if (size > 64_000) {
        reject(new Error("Request body is too large."));
        request.destroy();
        return;
      }
      chunks.push(chunk);
    });
    request.on("end", () => {
      try {
        resolve(JSON.parse(Buffer.concat(chunks).toString("utf8")));
      } catch {
        reject(new Error("Request body must be valid JSON."));
      }
    });
    request.on("error", reject);
  });
}

async function verifyGoogleCredential(credential) {
  if (!googleClientId) {
    throw new Error("Google authentication is not configured.");
  }

  const { OAuth2Client } = await import("google-auth-library");
  const googleClient = new OAuth2Client(googleClientId);
  const ticket = await googleClient.verifyIdToken({
    idToken: credential,
    audience: googleClientId,
  });
  const payload = ticket.getPayload();

  if (!payload?.sub || !payload.email || !payload.email_verified) {
    throw new Error("Google account verification failed.");
  }

  return {
    googleSub: payload.sub,
    email: payload.email.toLowerCase(),
    displayName: payload.name || payload.email,
  };
}

async function findOrCreateAdminUser(identity) {
  if (!allowedAdminEmails.has(identity.email)) {
    throw new Error("This account is not allowed to access the admin area.");
  }

  const pool = await getDatabasePool();
  const existingUser = await pool.query(
    `SELECT id, email, display_name, role, is_active
     FROM admin_users
     WHERE google_sub = $1`,
    [identity.googleSub],
  );

  if (existingUser.rows[0]) {
    return existingUser.rows[0];
  }

  const createdUser = await pool.query(
    `INSERT INTO admin_users (google_sub, email, display_name, role)
     VALUES ($1, $2, $3, $4)
     RETURNING id, email, display_name, role, is_active`,
    [identity.googleSub, identity.email, identity.displayName, ownerAdminEmails.has(identity.email) ? "owner" : "editor"],
  );
  return createdUser.rows[0];
}

async function createAdminSession(user) {
  const sessionToken = randomBytes(32).toString("base64url");
  const csrfToken = randomBytes(32).toString("base64url");
  const maxAgeSeconds = 8 * 60 * 60;
  const pool = await getDatabasePool();

  await pool.query(
    `INSERT INTO admin_sessions (admin_user_id, token_hash, csrf_hash, expires_at)
     VALUES ($1, $2, $3, NOW() + INTERVAL '8 hours')`,
    [user.id, hashValue(sessionToken), hashValue(csrfToken)],
  );

  return { sessionToken, csrfToken, maxAgeSeconds };
}

async function getAdminSession(request) {
  const sessionToken = parseCookies(request).admin_session;
  if (!sessionToken) {
    return null;
  }

  const pool = await getDatabasePool();
  const result = await pool.query(
    `SELECT sessions.id, sessions.csrf_hash, users.id AS user_id, users.email, users.display_name, users.role, users.is_active
     FROM admin_sessions AS sessions
     JOIN admin_users AS users ON users.id = sessions.admin_user_id
     WHERE sessions.token_hash = $1 AND sessions.expires_at > NOW()`,
    [hashValue(sessionToken)],
  );
  return result.rows[0] || null;
}

async function rotateCsrfToken(sessionId) {
  const csrfToken = randomBytes(32).toString("base64url");
  const pool = await getDatabasePool();
  await pool.query(
    "UPDATE admin_sessions SET csrf_hash = $1, last_seen_at = NOW() WHERE id = $2",
    [hashValue(csrfToken), sessionId],
  );
  return csrfToken;
}

function csrfTokenMatches(session, request) {
  const submittedToken = String(request.headers["x-csrf-token"] || "");
  const submittedHash = hashValue(submittedToken);
  const expected = Buffer.from(session.csrf_hash);
  const received = Buffer.from(submittedHash);
  return expected.length === received.length && timingSafeEqual(expected, received);
}

function hasExpectedOrigin(request) {
  return Boolean(publicOrigin) && request.headers.origin === publicOrigin;
}

function createSlug(value) {
  const normalized = String(value || "")
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");

  return normalized || `certificate-${randomUUID().slice(0, 8)}`;
}

function normaliseOptionalUrl(value, label) {
  const url = String(value || "").trim();
  if (!url) {
    return null;
  }

  if (!url.startsWith("/") && !/^https:\/\//i.test(url)) {
    throw new Error(`${label} يجب أن يبدأ بـ https:// أو /.`);
  }

  return url;
}

function validateProfilePayload(payload) {
  const fullNameAr = String(payload.fullNameAr || "").trim();
  const fullNameEn = String(payload.fullNameEn || "").trim();
  const headlineAr = String(payload.headlineAr || "").trim();
  const bioAr = String(payload.bioAr || "").trim();
  const locationAr = String(payload.locationAr || "").trim();
  const email = String(payload.email || "").trim().toLowerCase();
  const expectedGraduationYear = Number(payload.expectedGraduationYear || 0);

  if (fullNameAr.length < 3 || fullNameAr.length > 120 || fullNameEn.length < 3 || fullNameEn.length > 120) {
    throw new Error("الاسم العربي والإنجليزي مطلوبان وبصياغة صحيحة.");
  }
  if (headlineAr.length < 5 || headlineAr.length > 180 || locationAr.length < 2 || locationAr.length > 160) {
    throw new Error("المسمى والموقع مطلوبان وبصياغة صحيحة.");
  }
  if (bioAr.length > 1_200) {
    throw new Error("النبذة طويلة جدًا.");
  }
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    throw new Error("البريد الإلكتروني غير صحيح.");
  }
  if (expectedGraduationYear && (expectedGraduationYear < 2000 || expectedGraduationYear > 2100)) {
    throw new Error("سنة التخرج غير صحيحة.");
  }

  return {
    fullNameAr,
    fullNameEn,
    headlineAr,
    bioAr: bioAr || null,
    locationAr,
    email,
    linkedinUrl: normaliseOptionalUrl(payload.linkedinUrl, "رابط LinkedIn"),
    photoUrl: normaliseOptionalUrl(payload.photoUrl, "رابط الصورة"),
    expectedGraduationYear: expectedGraduationYear || null,
  };
}

function validateProjectPayload(payload) {
  const titleAr = String(payload.titleAr || "").trim();
  const categoryAr = String(payload.categoryAr || "").trim();
  const summaryAr = String(payload.summaryAr || "").trim();
  const technologies = Array.isArray(payload.technologies) ? payload.technologies : [];
  const normalisedTechnologies = technologies
    .map((technology) => String(technology).trim())
    .filter(Boolean)
    .slice(0, 16);

  if (titleAr.length < 3 || titleAr.length > 200 || categoryAr.length < 2 || categoryAr.length > 120) {
    throw new Error("عنوان المشروع وتصنيفه مطلوبان وبصياغة صحيحة.");
  }
  if (summaryAr.length < 20 || summaryAr.length > 1_500) {
    throw new Error("وصف المشروع يجب أن يتراوح بين 20 و1500 حرف.");
  }
  if (normalisedTechnologies.some((technology) => technology.length > 60)) {
    throw new Error("إحدى التقنيات المدخلة طويلة جدًا.");
  }

  return {
    titleAr,
    categoryAr,
    summaryAr,
    technologies: normalisedTechnologies,
    externalUrl: normaliseOptionalUrl(payload.externalUrl, "رابط المشروع"),
    coverImageUrl: normaliseOptionalUrl(payload.coverImageUrl, "رابط صورة المشروع"),
    isPublished: payload.isPublished === true,
  };
}

function validateCertificatePayload(payload) {
  const title = String(payload.title || "").trim();
  const issuer = String(payload.issuer || "").trim();
  const descriptionAr = String(payload.descriptionAr || "").trim();
  const credentialUrl = String(payload.credentialUrl || "").trim();
  const issuedOn = String(payload.issuedOn || "").trim();

  if (title.length < 2 || title.length > 200 || issuer.length < 2 || issuer.length > 120) {
    throw new Error("العنوان والجهة المانحة مطلوبان وبصياغة صحيحة.");
  }

  if (descriptionAr.length < 10 || descriptionAr.length > 800) {
    throw new Error("وصف الشهادة يجب أن يتراوح بين 10 و800 حرف.");
  }

  if (issuedOn && !/^\d{4}-\d{2}-\d{2}$/.test(issuedOn)) {
    throw new Error("تاريخ الإصدار غير صحيح.");
  }

  return {
    title,
    issuer,
    descriptionAr,
    credentialUrl: normaliseOptionalUrl(credentialUrl, "رابط الملف"),
    issuedOn: issuedOn || null,
    isPublished: payload.isPublished === true,
  };
}

async function getAdminProfile() {
  const pool = await getDatabasePool();
  const result = await pool.query(
    `SELECT full_name_ar, full_name_en, headline_ar, bio_ar, location_ar, email, linkedin_url, photo_url, expected_graduation_year
     FROM profiles
     WHERE id = 1`,
  );
  return result.rows[0] || null;
}

async function updateProfile(payload) {
  const profile = validateProfilePayload(payload);
  const pool = await getDatabasePool();
  const result = await pool.query(
    `UPDATE profiles
     SET full_name_ar = $1, full_name_en = $2, headline_ar = $3, bio_ar = $4, location_ar = $5,
         email = $6, linkedin_url = $7, photo_url = $8, expected_graduation_year = $9, updated_at = NOW()
     WHERE id = 1
     RETURNING full_name_ar, full_name_en, headline_ar, bio_ar, location_ar, email, linkedin_url, photo_url, expected_graduation_year`,
    [
      profile.fullNameAr,
      profile.fullNameEn,
      profile.headlineAr,
      profile.bioAr,
      profile.locationAr,
      profile.email,
      profile.linkedinUrl,
      profile.photoUrl,
      profile.expectedGraduationYear,
    ],
  );
  return result.rows[0] || null;
}

async function listAdminProjects() {
  const pool = await getDatabasePool();
  const result = await pool.query(
    `SELECT slug, title_ar, category_ar, summary_ar, technologies, external_url, cover_image_url, is_published, sort_order
     FROM projects
     ORDER BY sort_order ASC, created_at ASC`,
  );
  return result.rows;
}

async function createProject(payload) {
  const project = validateProjectPayload(payload);
  const pool = await getDatabasePool();
  const result = await pool.query(
    `INSERT INTO projects (slug, title_ar, category_ar, summary_ar, technologies, external_url, cover_image_url, is_published, sort_order)
     VALUES ($1, $2, $3, $4, $5, $6, $7, $8, (SELECT COALESCE(MAX(sort_order), 0) + 10 FROM projects))
     RETURNING slug, title_ar, category_ar, summary_ar, technologies, external_url, cover_image_url, is_published, sort_order`,
    [
      createSlug(payload.slug || project.titleAr),
      project.titleAr,
      project.categoryAr,
      project.summaryAr,
      JSON.stringify(project.technologies),
      project.externalUrl,
      project.coverImageUrl,
      project.isPublished,
    ],
  );
  return result.rows[0];
}

async function listAdminCertificates() {
  const pool = await getDatabasePool();
  const result = await pool.query(
    `SELECT slug, title, issuer, description_ar, issued_on, credential_url, is_published, sort_order, created_at, updated_at
     FROM certificates
     ORDER BY sort_order ASC, created_at ASC`,
  );
  return result.rows;
}

async function createCertificate(payload) {
  const certificate = validateCertificatePayload(payload);
  const pool = await getDatabasePool();
  const result = await pool.query(
    `INSERT INTO certificates (slug, title, issuer, description_ar, issued_on, credential_url, is_published, sort_order)
     VALUES ($1, $2, $3, $4, $5, $6, $7, (SELECT COALESCE(MAX(sort_order), 0) + 10 FROM certificates))
     RETURNING slug, title, issuer, description_ar, issued_on, credential_url, is_published, sort_order`,
    [
      createSlug(payload.slug || certificate.title),
      certificate.title,
      certificate.issuer,
      certificate.descriptionAr,
      certificate.issuedOn,
      certificate.credentialUrl,
      certificate.isPublished,
    ],
  );
  return result.rows[0];
}

function serveStatic(requestPath, response) {
  const requestedFile = requestPath === "/" ? "portfolio.html" : requestPath.slice(1);
  const filePath = resolve(publicDirectory, requestedFile);
  const fileRelativePath = relative(publicDirectory, filePath);

  if (
    !fileRelativePath ||
    fileRelativePath.startsWith("..") ||
    isAbsolute(fileRelativePath) ||
    !existsSync(filePath) ||
    statSync(filePath).isDirectory()
  ) {
    response.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" });
    response.end("Not found");
    return;
  }

  response.writeHead(200, {
    "Content-Type": mimeTypes[extname(filePath)] || "application/octet-stream",
    "X-Content-Type-Options": "nosniff",
    "Referrer-Policy": "strict-origin-when-cross-origin",
    "X-Frame-Options": "DENY",
    "Content-Security-Policy": "default-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; img-src 'self' data:; object-src 'none'; style-src 'self'; script-src 'self' https://accounts.google.com; frame-src https://accounts.google.com; connect-src 'self' https://accounts.google.com",
  });
  createReadStream(filePath).pipe(response);
}

const server = createServer(async (request, response) => {
  const requestUrl = new URL(request.url, `http://${request.headers.host}`);

  if (request.method === "GET" && requestUrl.pathname === "/health") {
    sendJson(response, 200, {
      status: "ok",
      service: "rayan-portfolio",
      database: databaseUrl ? "configured" : "not_configured",
    });
    return;
  }

  if (request.method === "GET" && requestUrl.pathname === "/api/v1/auth/config") {
    sendJson(response, 200, { googleClientId: googleClientId || null });
    return;
  }

  if (request.method === "POST" && requestUrl.pathname === "/api/v1/auth/google") {
    try {
      const body = await readJsonBody(request);
      const credential = String(body.credential || "").trim();
      if (!credential || credential.length > 8_000) {
        sendJson(response, 400, { error: "تعذّر التحقق من بيانات الدخول." });
        return;
      }

      const identity = await verifyGoogleCredential(credential);
      const user = await findOrCreateAdminUser(identity);
      if (!user.is_active) {
        sendJson(response, 403, { error: "هذا الحساب غير مفعّل للإدارة." });
        return;
      }

      const session = await createAdminSession(user);
      sendJson(
        response,
        200,
        {
          user: {
            email: user.email,
            displayName: user.display_name,
            role: user.role,
          },
          csrfToken: session.csrfToken,
        },
        { "Set-Cookie": createSessionCookie(session.sessionToken, session.maxAgeSeconds) },
      );
    } catch (error) {
      if (error.message === "Google authentication is not configured." || error.message === "Database is not configured.") {
        sendJson(response, 503, { error: "خدمة الدخول غير مهيأة بعد." });
        return;
      }

      if (error.message === "This account is not allowed to access the admin area.") {
        sendJson(response, 403, { error: "هذا الحساب غير مصرح له بالدخول." });
        return;
      }

      console.error("Google sign-in failed.");
      sendJson(response, 401, { error: "تعذّر تسجيل الدخول بواسطة Google." });
    }
    return;
  }

  if (request.method === "POST" && requestUrl.pathname === "/api/v1/auth/logout") {
    try {
      const session = await getAdminSession(request);
      if (session) {
        if (!hasExpectedOrigin(request) || !csrfTokenMatches(session, request)) {
          sendJson(response, 403, { error: "تعذّر التحقق من طلب تسجيل الخروج." });
          return;
        }
        const pool = await getDatabasePool();
        await pool.query("DELETE FROM admin_sessions WHERE id = $1", [session.id]);
      }
    } catch {}
    sendJson(response, 200, { status: "signed_out" }, { "Set-Cookie": clearSessionCookie() });
    return;
  }

  if (request.method === "GET" && requestUrl.pathname === "/api/v1/portfolio") {
    try {
      sendJson(response, 200, await getPortfolioData());
    } catch (error) {
      if (error.message === "Database is not configured.") {
        sendJson(response, 503, { error: "قاعدة البيانات غير مهيأة بعد." });
        return;
      }

      console.error("Portfolio API failed.");
      sendJson(response, 500, { error: "تعذّر جلب البيانات حاليًا." });
    }
    return;
  }

  if (requestUrl.pathname.startsWith("/api/v1/admin/")) {
    try {
      const session = await getAdminSession(request);
      if (!session || !session.is_active) {
        sendJson(response, 401, { error: "يلزم تسجيل الدخول للوصول إلى الإدارة." });
        return;
      }

      if (request.method === "GET" && requestUrl.pathname === "/api/v1/admin/session") {
        const csrfToken = await rotateCsrfToken(session.id);
        sendJson(response, 200, {
          user: {
            email: session.email,
            displayName: session.display_name,
            role: session.role,
          },
          csrfToken,
        });
        return;
      }

      if (request.method === "GET" && requestUrl.pathname === "/api/v1/admin/profile") {
        sendJson(response, 200, await getAdminProfile());
        return;
      }

      if (request.method === "PUT" && requestUrl.pathname === "/api/v1/admin/profile") {
        if (!hasExpectedOrigin(request) || !csrfTokenMatches(session, request)) {
          sendJson(response, 403, { error: "تعذّر التحقق من طلب الحفظ." });
          return;
        }

        sendJson(response, 200, await updateProfile(await readJsonBody(request)));
        return;
      }

      if (request.method === "GET" && requestUrl.pathname === "/api/v1/admin/projects") {
        sendJson(response, 200, await listAdminProjects());
        return;
      }

      if (request.method === "POST" && requestUrl.pathname === "/api/v1/admin/projects") {
        if (!hasExpectedOrigin(request) || !csrfTokenMatches(session, request)) {
          sendJson(response, 403, { error: "تعذّر التحقق من طلب الحفظ." });
          return;
        }

        sendJson(response, 201, await createProject(await readJsonBody(request)));
        return;
      }

      if (request.method === "GET" && requestUrl.pathname === "/api/v1/admin/certificates") {
        sendJson(response, 200, await listAdminCertificates());
        return;
      }

      if (request.method === "POST" && requestUrl.pathname === "/api/v1/admin/certificates") {
        if (!hasExpectedOrigin(request) || !csrfTokenMatches(session, request)) {
          sendJson(response, 403, { error: "تعذّر التحقق من طلب الحفظ." });
          return;
        }

        const certificate = await createCertificate(await readJsonBody(request));
        sendJson(response, 201, certificate);
        return;
      }

      sendJson(response, 404, { error: "مسار الإدارة غير موجود." });
    } catch (error) {
      if (error.message === "Database is not configured.") {
        sendJson(response, 503, { error: "قاعدة البيانات غير مهيأة بعد." });
        return;
      }

      if (
        error.message.startsWith("الاسم") ||
        error.message.startsWith("المسمى") ||
        error.message.startsWith("البريد") ||
        error.message.startsWith("سنة") ||
        error.message.startsWith("عنوان") ||
        error.message.startsWith("وصف") ||
        error.message.startsWith("تاريخ") ||
        error.message.startsWith("رابط") ||
        error.message.startsWith("إحدى")
      ) {
        sendJson(response, 400, { error: error.message });
        return;
      }

      console.error("Admin API failed.");
      sendJson(response, 500, { error: "تعذّر إكمال الطلب حاليًا." });
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
