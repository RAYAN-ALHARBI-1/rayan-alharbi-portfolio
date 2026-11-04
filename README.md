# موقع ريان الحربي الشخصي

موقع شخصي يعرض الملف الشخصي والمشاريع والشهادات، مع لوحة إدارة خاصة. الواجهة موجودة في `public` والخادم الحالي مبني بـ **ASP.NET Core 10**.

## البنية

```text
public/                         الواجهة العامة ولوحة الإدارة
src/Portfolio.Server/           خادم ASP.NET Core وواجهات API
src/Portfolio.Server/Data/      نماذج البيانات وترحيلات PostgreSQL
database/seed.sql               بيانات المحتوى الأولية
docs/                           أدلة الإعداد والحماية
server.js                       نسخة قديمة بـ Node.js للرجوع إليها فقط وليست مسار تشغيل أو نشر معتمد
```

## تشغيل الخادم محليًا

إذا كان `.NET 10` مضافًا إلى النظام:

```powershell
dotnet run --project src/Portfolio.Server/Portfolio.Server.csproj --urls http://127.0.0.1:5088
```

أو استخدم النسخة المحلية التي ثُبتت لهذا الجهاز:

```powershell
& "$HOME\.dotnet10\dotnet.exe" run --project src/Portfolio.Server/Portfolio.Server.csproj --urls http://127.0.0.1:5088
```

ثم افتح `http://127.0.0.1:5088`. فحص الخدمة متاح عبر `/health`.

> مسار التشغيل المعتمد للموقع هو خادم ASP.NET Core أعلاه. لا تستخدم `npm start`
> للنشر أو للتشغيل اليومي؛ ملف Node.js موجود مرجعًا لنسخة مبكرة فقط.

## إعداد PostgreSQL

لا تحفظ رابط القاعدة أو أسرار Google داخل الملفات أو Git. في بيئة التطوير استخدم **User Secrets**:

```powershell
dotnet user-secrets --project src/Portfolio.Server set "ConnectionStrings:PortfolioDb" "رابط_اتصال_PostgreSQL"
dotnet tool run dotnet-ef database update --project src/Portfolio.Server --startup-project src/Portfolio.Server
```

بعد إنشاء الجداول، نفّذ `database/seed.sql` مرة واحدة من لوحة مزود قاعدة البيانات أو أداة PostgreSQL المناسبة.

## إعداد دخول Google

أنشئ OAuth Client من Google وحدد عنوان العودة المحلي التالي أثناء التطوير:

```text
http://127.0.0.1:5088/signin-google
```

ثم خزّن الإعدادات محليًا فقط:

```powershell
dotnet user-secrets --project src/Portfolio.Server set "Authentication:Google:ClientId" "CLIENT_ID"
dotnet user-secrets --project src/Portfolio.Server set "Authentication:Google:ClientSecret" "CLIENT_SECRET"
dotnet user-secrets --project src/Portfolio.Server set "AdminAccess:AllowedEmails:0" "YOUR_GOOGLE_EMAIL"
dotnet user-secrets --project src/Portfolio.Server set "AdminAccess:OwnerEmails:0" "YOUR_GOOGLE_EMAIL"
```

يمكن إضافة حسابات إدارة أخرى بفهارس إضافية مثل `AllowedEmails:1`. الخادم هو من يتحقق من الصلاحية؛ إخفاء واجهة الإدارة لا يعد حماية.

## ملاحظات الحماية

- تسجيل الدخول يستخدم Google OAuth وجلسة Cookie محمية من JavaScript.
- عمليات الحفظ تتطلب رمزًا مضادًا للتزوير `CSRF`.
- لا تُنفذ عملية رفع ملفات أو تحليل AI قبل اختيار التخزين السحابي ومفتاح المزود.
- لا ترفع ملفات `.env` أو `appsettings.Development.json` أو أي مفاتيح إلى GitHub.

راجع `docs/server-setup-ar.md` للخطوات التفصيلية وحالة ما تم إنجازه.
