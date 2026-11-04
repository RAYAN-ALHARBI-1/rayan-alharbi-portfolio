# انتقال الموقع الشخصي إلى ASP.NET Core

## القرار الحالي

أصبح **ASP.NET Core 10** هو الخادم المعتمد للموقع الشخصي؛ لأنه الأقرب لتقنيات مشروع الموارد البشرية. يبقى `server.js` كنموذج Node.js سابق للرجوع إليه فقط، ولا ينبغي تشغيل الخادمين معًا في الإنتاج.

## ما تم إنجازه

- تثبيت .NET SDK 10.0.403 محليًا في `C:\Users\zdzd6\.dotnet10`.
- إنشاء الحل `RayanPortfolio.slnx` ومشروع `src/Portfolio.Server`.
- تقديم كل محتوى `public` من خادم ASP.NET Core.
- بناء واجهات API للملف الشخصي والمشاريع والشهادات.
- إضافة EF Core مع موفر PostgreSQL وترحيل أول للجداول.
- تجهيز تسجيل Google OAuth وجلسة Cookie محمية.
- إضافة حماية CSRF ورؤوس حماية وحدّ لمحاولات الواجهات الحساسة.

## ما يحتاج إعدادًا خارجيًا

1. قاعدة PostgreSQL سحابية ورابط اتصالها.
2. OAuth Client من Google مع عنوان العودة النهائي.
3. البريد أو عناوين البريد المصرح لها بالإدارة.
4. مزود تخزين خاص للصور وملفات PDF قبل بناء الرفع الفعلي.
5. مفتاح Gemini API عند بدء اقتراح المحتوى بالذكاء الاصطناعي.

## لماذا لا نكمل الرفع والذكاء الاصطناعي الآن؟

رفع الملفات يحتاج تخزينًا خاصًا وموقعًا واضحًا للملفات وسياسة وصول، وتحليلها يحتاج مفتاحًا سريًا. بناء واجهة شكلية تحفظ الملفات محليًا ثم حذفها لاحقًا ليس تجربة نشر صحيحة ولا آمنة.

## تشغيل المشروع

اتبع `README.md` أو شغّل:

```powershell
& "$HOME\.dotnet10\dotnet.exe" run --project src/Portfolio.Server/Portfolio.Server.csproj --urls http://127.0.0.1:5088
```

## التحقق

```powershell
& "$HOME\.dotnet10\dotnet.exe" build RayanPortfolio.slnx
& "$HOME\.dotnet10\dotnet.exe" tool run dotnet-ef migrations script --project src/Portfolio.Server --startup-project src/Portfolio.Server
```
