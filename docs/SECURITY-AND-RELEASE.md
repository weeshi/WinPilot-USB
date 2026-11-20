# الأمن والإصدار

## البناء المحلي

```powershell
.\scripts\Build-WinPilot.ps1 -Clean
```

## تحديث hashes

```powershell
.\scripts\Update-InstallerHashes.ps1
```

يجب إعادة مراجعة `scripts/local-apps.json` بعد تغيير أي ملف داخل `Apps`.

## التوقيع

التوقيع المحلي يحتاج شهادة Code Signing حقيقية ومفتاحًا خاصًا:

```powershell
.\scripts\Build-WinPilot.ps1 -SignCertificateThumbprint "THUMBPRINT"
```

لا تُحفظ الشهادة أو المفتاح الخاص داخل GitHub. التوقيع المجاني المقترح للمشروع المفتوح هو SignPath Foundation بعد القبول، عبر الأسرار والمتغيرات في `.github/workflows/release.yml`.

## حدود الثقة

- SHA-256 يثبت سلامة الملف بعد نشر hash موثوق، لكنه لا يثبت أن المصدر آمن.
- شهادة Self-Signed مناسبة للاختبار الداخلي فقط.
- تعطيل Windows Update قد يمنع تحديثات الأمان ويجب استخدامه في بيئة صيانة محددة.
- يجب مراجعة معاملات التثبيت الصامت لكل برنامج قبل استخدام `-All`.
- لا تُرفع المثبتات التجارية إلى المستودع العام دون التحقق من حقوق إعادة التوزيع.
