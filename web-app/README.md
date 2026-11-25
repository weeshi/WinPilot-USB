# WinPilot-USB Web

نسخة ويب متجاوبة تحاكي واجهة تطبيق Windows وتعيد استخدام موارده الأصلية.

تُولّد قائمة البرامج ورقم الإصدار في `catalog-data.js` من `scripts/local-apps.json` و`Directory.Build.props`. لا تعدّل الملف المولّد يدويًا؛ استخدم:

```powershell
.\scripts\Sync-WebCatalog.ps1
```

## التشغيل

يمكن فتح `index.html` مباشرة، أو تشغيل خادم محلي من جذر المشروع:

```powershell
python -m http.server 8765 --bind 127.0.0.1
```

ثم افتح:

```text
http://127.0.0.1:8765/web-app/
```

## حدود المتصفح

البحث والتحديد والإعلانات وروابط التواصل تعمل داخل الويب. تثبيت ملفات EXE وتشغيل Office أو سكربتات تحديث Windows والتفعيل تحتاج تطبيق Windows لأنها تتطلب الوصول إلى الملفات المحلية وصلاحيات Administrator.

بعد تحديد البرامج، يتيح الحوار تنزيل `WinPilot-Selection.json` واستيراده من زر `Import` في تطبيق Windows. زر `فتح في WinPilot` يحتاج تسجيل البروتوكول المحلي اختياريًا:

```powershell
.\scripts\Register-WinPilotProtocol.ps1
```
