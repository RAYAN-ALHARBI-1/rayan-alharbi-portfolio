const googleSignIn = document.querySelector("#google-sign-in");
const loginState = document.querySelector("#login-state");
const csrfStorageKey = "portfolio-admin-csrf";

function setLoginState(message) {
  loginState.textContent = message;
}

function startGoogleSignIn() {
  googleSignIn.disabled = true;
  setLoginState("جارٍ تحويلك إلى Google…");
  window.location.assign("/api/v1/auth/google?returnUrl=/admin.html");
}

async function initialiseLogin() {
  try {
    const sessionResponse = await fetch("/api/v1/admin/session", { credentials: "same-origin" });
    if (sessionResponse.ok) {
      const session = await sessionResponse.json();
      sessionStorage.setItem(csrfStorageKey, session.csrfToken);
      window.location.replace("admin.html");
      return;
    }

    const configResponse = await fetch("/api/v1/auth/config", { credentials: "same-origin" });
    const config = await configResponse.json();
    if (!config.googleEnabled) {
      setLoginState("دخول Google غير مهيأ بعد.");
      return;
    }

    googleSignIn.disabled = false;
    googleSignIn.addEventListener("click", startGoogleSignIn, { once: true });

    if (new URLSearchParams(window.location.search).get("error")) {
      setLoginState("تعذّر تسجيل الدخول. تأكد من الحساب المصرح له ثم حاول مرة أخرى.");
    }
  } catch (error) {
    setLoginState(error.message);
  }
}

initialiseLogin();
