# WinPilot-USB — دليل إنشاء ونشر تحديث جديد

هذا الدليل يشرح دورة الإصدار كاملة بدءًا من تعديل المشروع وحتى وصول التحديث إلى المستخدمين. يفترض أن جهاز الإصدار يحتوي على Git وGitHub CLI و.NET SDK، وأن `gh auth status` يعرض اتصالًا صالحًا بحساب GitHub الذي يملك المستودع.

## 1. كيف يصل التحديث إلى المستخدم

الإصدار `1.1.0` وما بعده يفحص الرابط التالي عند التشغيل أو عند الضغط على `Check updates`:

`https://api.github.com/repos/weeshi/WinPilot-USB/releases/latest`

يقارن التطبيق رقم الإصدار الحالي مع tag أحدث Release منشور. يجب أن يكون tag بالصيغة `vMAJOR.MINOR.PATCH` مثل `v1.1.1`.

عند وجود إصدار أحدث يبحث التطبيق عن أصلين بالاسم الثابت:

- `WinPilot-USB-Update.zip`
- `WinPilot-USB-Update.sha256`

يستخدم SHA-256 الذي يعرضه GitHub للأصل، أو ملف البصمة كخيار احتياطي. بعد نجاح التحقق يشغّل `WinPilot-Updater.exe`، يغلق التطبيق، يستبدل الملفات، ثم يعيد تشغيله. عند فشل النسخ يستعيد المحدث الملفات السابقة.

النسخ `1.0` وما قبلها لا تحتوي على هذا النظام. يجب ترقيتها يدويًا مرة واحدة بنسخ `WinPilot-USB.exe` و`WinPilot-Updater.exe` من إصدار `1.1.0` أو أحدث.

## 2. اختيار رقم الإصدار

استخدم Semantic Versioning:

- `PATCH`: إصلاح لا يغير طريقة الاستخدام، مثل `1.1.0` إلى `1.1.1`.
- `MINOR`: ميزة جديدة متوافقة، مثل `1.1.0` إلى `1.2.0`.
- `MAJOR`: تغيير غير متوافق أو بنية جديدة، مثل `1.2.0` إلى `2.0.0`.

لا تعِد استخدام tag أو رقم إصدار سبق نشره. التطبيق يعتمد على ترتيب الأرقام، وليس تاريخ النشر فقط.

حدّث الرقم نفسه في الملفات التالية:

1. `WinPilot-USB.csproj`: خصائص `Version` و`FileVersion` و`InformationalVersion`.
2. `WinPilot-Updater.csproj`: الخصائص نفسها.
3. `.work/app.manifest`: قيمة `assemblyIdentity version` بأربعة أجزاء.
4. `.work/updater.manifest`: قيمة `assemblyIdentity version` بأربعة أجزاء.
5. `docs/VERSION.txt`: الرقم وتاريخ الإصدار.

مثال للإصدار `1.1.1`:

```xml
<Version>1.1.1.0</Version>
<FileVersion>1.1.1.0</FileVersion>
<InformationalVersion>1.1.1</InformationalVersion>
```

وفي manifest:

```xml
<assemblyIdentity version="1.1.1.0" name="WinPilotUSB.Installer" />
```

أضف قسمًا جديدًا في أعلى `docs/CHANGELOG.md` يشرح الإصلاحات والمزايا بلغة المستخدم.

## 3. تجهيز التعديلات

نفّذ التعديلات في المصدر ثم راجع حالة Git:

```powershell
git status --short
git diff --check
git diff
```

لا تضف مجلدات التطوير أو المخرجات إلى حزمة USB التشغيلية:

- `.git`
- `.github`
- `.work`
- `bin`
- `obj`
- `release-local`

## 4. تحديث عادي بدون برامج جديدة

استخدم هذا المسار عند تعديل الواجهة أو السكربتات أو الأصول أو إصلاح خطأ.

### 4.1 البناء المحلي

```powershell
.\scripts\Build-WinPilot.ps1 -Clean
```

يجب أن ينتج الملفان التاليان في جذر المشروع:

- `WinPilot-USB.exe`
- `WinPilot-Updater.exe`

تحقق من الإصدار والبصمات:

```powershell
Get-Item .\WinPilot-USB.exe,.\WinPilot-Updater.exe |
  Select-Object Name,@{Name='Version';Expression={$_.VersionInfo.FileVersion}},Length

Get-FileHash .\WinPilot-USB.exe,.\WinPilot-Updater.exe -Algorithm SHA256
```

### 4.2 الاختبارات المطلوبة

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-Updater.ps1

& "$env:WINDIR\SysWOW64\WindowsPowerShell\v1.0\powershell.exe" `
  -NoProfile -STA -ExecutionPolicy Bypass -File .\scripts\Test-Search.ps1
```

يجب أن ينجح اختبار المحدث في استبدال الملفات، والحفاظ على ملفات المستخدم، وإجراء rollback، ورفض ZIP traversal.

شغّل `WinPilot-USB.exe` وتحقق بصريًا من الواجهة، رقم الإصدار، زر `Check updates`، قائمة البرامج وبطاقات الأدوات.

### 4.3 فحص الحزمة محليًا

```powershell
.\scripts\New-UpdatePackage.ps1
```

ينتج داخل `release-local`:

- `WinPilot-USB-Update.zip`
- `WinPilot-USB-Update.sha256`

تحقق من البصمة:

```powershell
$expected = (Get-Content .\release-local\WinPilot-USB-Update.sha256 -Raw).Split()[0]
$actual = (Get-FileHash .\release-local\WinPilot-USB-Update.zip -Algorithm SHA256).Hash
if ($expected -ne $actual) { throw 'Update package hash mismatch' }
```

## 5. إضافة برنامج جديد إلى التحديث

### 5.1 إضافة تعريف البرنامج

1. ضع المثبت داخل `Apps`.
2. أضف عنصرًا إلى `scripts/local-apps.json` يحتوي على `Id` فريد و`Name` و`Pattern` و`Type` و`SilentArgs` و`SHA256`.
3. حدّث البصمات:

```powershell
.\scripts\Update-InstallerHashes.ps1
```

4. اختبر معاملات التثبيت الصامت على جهاز أو آلة افتراضية منفصلة.
5. تأكد أن لديك حق إعادة توزيع المثبت قبل نشره في GitHub Release.

### 5.2 إنشاء حزمة تتضمن البرامج الجديدة

مرّر قيم `Id` التي أضفتها إلى أداة الحزمة:

```powershell
.\scripts\New-UpdatePackage.ps1 -AppIds "new-program-id"
```

لأكثر من برنامج:

```powershell
.\scripts\New-UpdatePackage.ps1 -AppIds "program-one","program-two"
```

تنسخ الأداة المثبتات المحددة فقط إلى `Apps` داخل ZIP، وتضيف دائمًا `scripts/local-apps.json` الجديد.

### 5.3 تحديث مثبت برنامج موجود

المحدث لا يحذف ملفات المستخدم تلقائيًا. لتجنب بقاء نسختين يطابقهما `Pattern`:

- استخدم اسم ملف ثابت للمثبت، مثل `Firefox-Offline.exe`، واستبدله في كل إصدار؛ أو
- اجعل `Pattern` يطابق اسم الملف الجديد وحده، وتأكد أن الملف القديم لن يتم اختياره؛ أو
- اطلب من المستخدم إزالة الملف القديم إذا لم يكن بالإمكان استخدام اسم ثابت.

الأفضل للتحديثات المستقبلية هو اعتماد أسماء ثابتة للمثبتات داخل `Apps` وعدم وضع رقم الإصدار في اسم الملف.

## 6. الالتزام ورفع المصدر

بعد نجاح الاختبارات:

```powershell
git status --short
git diff --check
git add <الملفات-المعدلة>
git commit -m "Release V1.1.1"
git push origin master
```

لا تستخدم `git add .` دون مراجعة، لأن `Ads` أو ملفات محلية أخرى قد تكون معدلة ولا تريد نشرها.

تحقق أن فرع `master` على GitHub يحتوي كل تعديلات الإصدار قبل إنشاء tag.

## 7. إنشاء tag وتشغيل الإصدار الآلي

```powershell
git tag -a v1.1.1 -m "WinPilot USB V1.1.1"
git push origin v1.1.1
```

يدفع tag الملف `.github/workflows/release.yml` إلى تنفيذ الخطوات التالية:

1. بناء التطبيق والمحدث.
2. إنشاء حزمة ZIP.
3. حساب SHA-256.
4. رفع artifact داخلي للبناء.
5. إنشاء GitHub Release ونشر الملفات الخمسة.
6. إرسال الملف إلى SignPath فقط إذا كان `SIGNPATH_ENABLED=true` وإعدادات SignPath مكتملة.

راقب البناء:

```powershell
gh run list --repo weeshi/WinPilot-USB --workflow release.yml --limit 5
gh run watch RUN_ID --repo weeshi/WinPilot-USB --exit-status
```

لا تعتبر الإصدار منشورًا بنجاح قبل ظهور جميع خطوات Job `build` باللون الأخضر.

## 8. إضافة ملاحظات الإصدار

أنشئ ملفًا محليًا مثل `release-notes.md` يوضح المشكلة السابقة والسلوك بعد الإصلاح، والمزايا والبرامج الجديدة، وأي خطوة يدوية مطلوبة، وحالة التوقيع الرقمي.

ثم حدّث Release:

```powershell
gh release edit v1.1.1 `
  --repo weeshi/WinPilot-USB `
  --title "WinPilot USB V1.1.1" `
  --notes-file .\release-notes.md
```

## 9. استبدال الحزمة الآلية بحزمة تحتوي برامج

GitHub Actions لا يضمّن `Apps` لأن المثبتات مستبعدة من Git. بعد نجاح Release، ارفع الحزمة المحلية التي أنشأتها في القسم 5 بدل الحزمة الآلية:

```powershell
gh release upload v1.1.1 `
  .\release-local\WinPilot-USB-Update.zip `
  .\release-local\WinPilot-USB-Update.sha256 `
  --repo weeshi/WinPilot-USB `
  --clobber
```

`--clobber` يحذف الأصل الذي يحمل الاسم نفسه ثم يرفع البديل. افحص الحزمة والبصمة محليًا قبل هذا الأمر، لأن فشل الرفع بعد الحذف قد يترك Release بلا حزمة صالحة.

إذا كانت Immutable Releases مفعلة فلن تستطيع استبدال الأصول بعد النشر. في هذه الحالة جهّز الحزمة النهائية قبل نشر Release أو أنشئ رقم إصدار جديد.

وفق GitHub، يمكن أن يحتوي Release حتى 1000 أصل، ويجب أن يكون حجم كل ملف أقل من 2 GiB، بينما لا يوجد حد إجمالي معلن لحجم Release أو bandwidth. إذا تجاوز ZIP حد 2 GiB فقسّم البرامج إلى أصول مستقلة أو لا توزعها عبر GitHub.

## 10. التحقق من Release المنشور

اعرض الملفات:

```powershell
gh release view v1.1.1 `
  --repo weeshi/WinPilot-USB `
  --json url,tagName,isDraft,isPrerelease,assets
```

يجب أن تجد الأسماء التالية حرفيًا:

- `WinPilot-USB.exe`
- `WinPilot-Updater.exe`
- `WinPilot-USB-Update.zip`
- `WinPilot-USB-Update.sha256`
- `SHA256SUMS.txt`

نزّل الحزمة المنشورة إلى مجلد اختبار منفصل:

```powershell
New-Item -ItemType Directory -Force .\release-verify | Out-Null
gh release download v1.1.1 `
  --repo weeshi/WinPilot-USB `
  --pattern "WinPilot-USB-Update*" `
  --dir .\release-verify
```

ثم قارن البصمة:

```powershell
$expected = (Get-Content .\release-verify\WinPilot-USB-Update.sha256 -Raw).Split()[0]
$actual = (Get-FileHash .\release-verify\WinPilot-USB-Update.zip -Algorithm SHA256).Hash
if ($expected -ne $actual) { throw 'Published package hash mismatch' }
```

## 11. اختبار وصول التحديث

اختبر على نسخة أقدم داخل مجلد منفصل أو USB تجريبي:

1. شغّل الإصدار السابق.
2. انتظر الفحص التلقائي أو اضغط `Check updates`.
3. تأكد أن الزر يعرض رقم الإصدار الجديد.
4. وافق على التحديث.
5. تحقق من تقدم التنزيل ورسالة نجاح SHA-256.
6. بعد إعادة التشغيل تحقق من رقم الإصدار الجديد.
7. تأكد أن `Apps` و`Ads` و`info.txt` لم تُحذف.
8. إذا أضفت برنامجًا، تأكد من وجود مثبته وظهوره في القائمة ثم اختبر التثبيت الصامت على جهاز اختبار.

لا تختبر أول تحديث على نسخة المستخدم الوحيدة. استخدم USB تجريبيًا واحتفظ بنسخة احتياطية قبل النشر العام.

## 12. التراجع أو سحب إصدار معيب

إذا اكتشفت مشكلة قبل انتشار الإصدار:

1. لا تعدل tag المنشور ولا تعِد استخدام رقمه.
2. أصلح المشكلة وارفع إصدار PATCH جديدًا، مثل `v1.1.2`.
3. اجعل الإصدار الجديد Latest.
4. اكتب في الملاحظات أن الإصدار السابق معيب وما الذي أصلحه الإصدار الجديد.

يمكن حذف Release أو أصله في الحالات الضرورية، لكن الأجهزة التي نزّلت التحديث بالفعل لن تتراجع تلقائيًا. الإصدار التصحيحي الجديد هو المسار الأكثر وضوحًا للمستخدمين.

## 13. قائمة تحقق مختصرة قبل النشر

- [ ] رقم الإصدار موحّد في المشروعين والـ manifests والوثائق.
- [ ] `CHANGELOG` وملاحظات الإصدار مكتملة.
- [ ] البناء المحلي بلا أخطاء أو تحذيرات.
- [ ] `Test-Updater.ps1` ناجح.
- [ ] `Test-Search.ps1` ناجح.
- [ ] الواجهة فُحصت بصريًا.
- [ ] SHA-256 للمثبتات محدثة.
- [ ] معاملات التثبيت الصامت اختُبرت.
- [ ] حقوق إعادة توزيع البرامج مؤكدة.
- [ ] Git نظيف باستثناء تغييرات مقصودة.
- [ ] GitHub Actions ناجح.
- [ ] الأصول الخمسة موجودة في Release.
- [ ] بصمة ZIP المنشور مطابقة.
- [ ] التحديث اختُبر من إصدار أقدم على USB تجريبي.
- [ ] حالة Authenticode/SignPath موضحة للمستخدمين.

## مراجع GitHub

- [About releases](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases)
- [Managing releases](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository)
- [GitHub CLI: release upload](https://cli.github.com/manual/gh_release_upload)
- [GitHub CLI: release download](https://cli.github.com/manual/gh_release_download)
