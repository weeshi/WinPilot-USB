# WinPilot-USB Web UX Contract

## Scope

النسخة الويب واجهة مرافق وتنزيل للتطبيق المكتبي. المتصفح لا يشغّل المثبتات ولا سكربتات Administrator.

## Canonical UI Map

| Capability | Canonical owner | Source of truth | Allowed variants | Verification |
|---|---|---|---|---|
| Search | `web-app/app.js` | هذا العقد | local catalog | query, clear, no-results |
| Selection | `web-app/app.js` | هذا العقد | visible-results only | count and select-all state |
| Toast | `#toast` | هذا العقد | info only | live-region browser check |
| Dialog | `#dialogBackdrop` | هذا العقد | restricted Windows action | Escape, trap, restore focus |
| Scrollbar | `web-app/styles.css` | `DESIGN.md` | list and dialog geometry | computed style |

## Interaction Ledger

- البحث لا يغير الاختيارات المخفية.
- تحديد الكل يطبّق على نتائج البحث الحالية فقط.
- زر التثبيت معطل عند عدم وجود اختيار.
- الضغط على التثبيت أو أداة Windows يشرح القيد ويوفر رابط الإصدار الرسمي.
- روابط التواصل الخارجية تفتح في تبويب منفصل مع `noopener`.
- Facebook يعرض حالة معلومات لأن الرابط غير محدد في المشروع.

## States

- Empty search: رسالة داخلية مع إرشاد لمسح البحث.
- Selected: عداد وحالة في الشريط السفلي.
- Restricted action: حوار يشرح ضرورة نسخة Windows.
- Narrow screen: ترتيب رأسي دون حذف أي إجراء.
- Reduced motion: تعطيل الحركات الانتقالية.

## Accessibility

الهدف WCAG 2.2 AA: عناصر أصلية، أسماء عربية، تركيز ظاهر، live regions، رابط تخطٍ، وحوار قابل للإغلاق بلوحة المفاتيح.

