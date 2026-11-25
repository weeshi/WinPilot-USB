# Weeshi USB Installer

أداة Windows محمولة لتثبيت البرامج المحلية من USB وإدارة أدوات الصيانة بدون الاعتماد على الإنترنت.

## الحالة الحالية

- الإصدار البرمجي: `1.2.0.0`
- البنية: Windows x86/32-bit، وتعمل على Windows 10 وWindows 11 عبر WoW64.
- المصدر المفتوح: [GitHub](https://github.com/weeshi/WinPilot-USB)
- التوقيع الرقمي: مجهز في Workflow، لكنه ينتظر قبول SignPath Foundation.

## البناء والتوقيع

يُقرأ رقم الإصدار المركزي من `Directory.Build.props`، وتستخدمه نسخة Windows والمحدث وكتالوج الويب. أداة `scripts\Build-WinPilot.ps1` تزامن بيانات الويب تلقائيًا ثم تعيد بناء التطبيق والمحدث ببنية x86:

```powershell
.\scripts\Build-WinPilot.ps1 -Clean
```

لمزامنة قائمة الويب والإصدار دون بناء الملفات التنفيذية:

```powershell
.\scripts\Sync-WebCatalog.ps1
```

لتوقيع الملف بشهادة Code Signing حقيقية مثبتة مع مفتاح خاص:

```powershell
.\scripts\Build-WinPilot.ps1 -SignCertificateThumbprint "THUMBPRINT"
```

يتطلب التوقيع Windows SDK و`signtool.exe`. لا تستخدم شهادة اختبار للتوزيع العام.

لتحديث SHA-256 بعد تغيير أي مثبت:

```powershell
.\scripts\Update-InstallerHashes.ps1
```
## المستندات

- `web-app/index.html`: نسخة ويب مطابقة لواجهة التطبيق، مع البحث والتحديد وبطاقات الأدوات وروابط التنزيل.
- `docs/USER-GUIDE.md`: دليل الاستخدام.
- `docs/CHANGELOG.md`: سجل الإصدارات والتغييرات.
- `docs/UPDATE-GUIDE.md`: طريقة تحديث الحزمة.
- `docs/update-guide/index.html`: دليل ويب تفاعلي ومرئي لإنشاء التحديث ونشره والتحقق منه.
- `docs/PROJECT-STATUS.md`: سجل شامل للتعديلات والمزايا وحالة كل ميزة.
- `docs/SECURITY-AND-RELEASE.md`: الأمن والبناء والإصدار والتوقيع.
- `docs/VERSION.txt`: معلومات الإصدار الحالية.

## الملفات المهمة

- `WinPilot-USB.exe`: الملف التنفيذي المبني.
- `WinPilot-Updater.exe`: المحدث المستقل الذي يستبدل الملفات بعد إغلاق التطبيق.
- `WinPilot-USB.csproj`: مشروع البناء القابل لإعادة الإنتاج.
- `.github/workflows/release.yml`: البناء وSHA-256 وGitHub Releases وSignPath.
- إصدارات GitHub تنتج `WinPilot-USB-Update.zip` و`WinPilot-USB-Update.sha256` للتحديث الذاتي.
- `scripts/Build-WinPilot.ps1`: البناء والتوقيع الاختياري.
- `scripts/Update-InstallerHashes.ps1`: تحديث SHA-256 للمثبتات.
- `scripts/Install-LocalApps.ps1`: اكتشاف وتشغيل التثبيت الصامت.
- `scripts/Disable-Windows-Updates.ps1`: تعطيل/استعادة Windows Update مع حفظ الحالة.
- `Apps/`: مثبتات محلية اختيارية، مستبعدة من GitHub لأسباب الحجم وحقوق التوزيع.
