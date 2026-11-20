# مثبت البرامج المحلي من مجلد WinPilot-USB

## الهدف

يقرأ السكربت `scripts\Install-LocalApps.ps1` ملفات التثبيت الموجودة في مجلد المشروع الحالي تلقائيًا، ويطابقها مع `scripts\local-apps.json`، ثم يشغل المثبتات بمعاملات صامتة قابلة للمراجعة.

السكربت لا يثبت شيئاً عند استخدام `-List` أو `-DryRun`، ولا ينفذ تثبيتاً إلا مع اختيار صريح.

## عرض البرامج المكتشفة

افتح PowerShell وشغّل:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\scripts\Install-LocalApps.ps1" -List
```

## محاكاة التثبيت دون تنفيذ

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\scripts\Install-LocalApps.ps1" -Name chrome,vlc -DryRun -SkipElevation
```

## تثبيت برامج محددة بصمت

افتح PowerShell **كمسؤول** ثم شغّل:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\App\WinPilot\scripts\Install-LocalApps.ps1" -Name chrome,vlc
```

يمكن استخدام أسماء المعرفات الموجودة في العمود `Id` أو أسماء البرامج. أمثلة:

```powershell
-Name chrome
-Name firefox,telegram,zoom
-Name libreoffice
```

## تثبيت كل البرامج المدعومة

هذا الأمر يثبت كل العناصر التي عثر عليها السكربت في `D:\App`:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\App\WinPilot\scripts\Install-LocalApps.ps1" -All
```

راجع نتيجة `-List` وجرّب `-DryRun` قبل استخدام `-All`. لا تستخدم `-All` على جهاز إنتاجي دون اختبار. يفضّل إضافة SHA256 لكل مثبت في manifest قبل التوزيع.

## إعداد المعاملات الصامتة

عدّل الملف:

```text
D:\App\WinPilot\scripts\local-apps.json
```

كل عنصر يحدد:

- `Id`: معرف يستخدم مع `-Name`.
- `Name`: الاسم الظاهر.
- `Pattern`: نمط اسم الملف في `D:\App`.
- `Type`: `exe` أو `msi` أو `msixbundle`.
- `SilentArgs`: معاملات التثبيت الصامت الخاصة بالمثبت.

المعاملات تختلف بين الشركات. يجب التأكد من توثيق كل مثبت قبل اعتماده، خصوصاً AnyDesk وKaspersky وبرامج الحماية.

## السجلات

السجلات موجودة في:

```text
D:\App\WinPilot\scripts\logs
```

## ملاحظات أمنية

- شغّل PowerShell كمسؤول فقط عند الحاجة.
- لا تستخدم `-AllowUnknown` إلا بعد مراجعة معاملات المثبت.
- الملفات المضغوطة مثل ZIP وRAR لا تثبت تلقائياً.
- حزم MSIX تستخدم `Add-AppxPackage` وقد تتطلب سياسة AppX مناسبة.
- السكربت يستبعد مجلد `D:\App\WinPilot` من البحث حتى لا يعامل ملفات المشروع كمثبتات.
- وضع `DryRun` لا ينفذ أي برنامج.
