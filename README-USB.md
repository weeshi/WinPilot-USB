# Weeshi USB Installer
 

## البناء والتوقيع

ملف `WinPilot-USB.csproj` وأداة `scripts\Build-WinPilot.ps1` يعيدان بناء الملف بإصدار `1.0.0.0` وبنية x86:

```powershell
.\scripts\Build-WinPilot.ps1 -Clean
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
الإصدار الحالي: **1.0**

تطبيق محمول لتثبيت البرامج من الفلاش بدون اتصال بالإنترنت.

## المستندات

- `docs\\USER-GUIDE.md`: دليل الاستخدام.
- `docs\\CHANGELOG.md`: سجل الإصدارات والتغييرات.
- `docs\\UPDATE-GUIDE.md`: طريقة تحديث الحزمة.
- `docs\\VERSION.txt`: معلومات الإصدار الحالية.

## الملفات المهمة

- `WinPilot-USB.exe`: الملف التنفيذي.
- `logo.png`: شعار التطبيق المستخدم في الواجهة وأيقونة البرنامج.
- `Apps\\office.iso`: ملف Office الذي تفتحه بطاقة Office بالطريقة العادية.
- `scripts\\Disable-Windows-Updates.ps1`: سكربت إيقاف واستعادة Windows Update.
