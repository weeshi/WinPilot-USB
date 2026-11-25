---
version: alpha
colors:
  canvas: "#060d19"
  panel: "#0d1a2d"
  panelRaised: "#101f35"
  border: "#30455e"
  text: "#f2f7fc"
  muted: "#9fb0c2"
  cyan: "#28d7e8"
  success: "#25a767"
  danger: "#ef5a69"
typography:
  display:
    fontFamily: '"Segoe UI Variable Display", "Segoe UI", Tahoma, sans-serif'
  body:
    fontFamily: '"Segoe UI Variable Text", "Segoe UI", Tahoma, sans-serif'
  data:
    fontFamily: '"Cascadia Code", Consolas, monospace'
rounded:
  DEFAULT: "3px"
  dialog: "4px"
spacing:
  compact: "6px"
  control: "10px"
  section: "30px"
components:
  toolCard:
    radius: "3px"
    border: "#40566d"
  primaryButton:
    background: "#25a767"
    foreground: "#ffffff"
---

# WinPilot-USB Design

## Overview

واجهة أدوات تقنية عربية لمستخدمي Windows. المرجع البصري هو التطبيق المكتبي نفسه: لوحة صيانة محمولة كثيفة وواضحة، وليست صفحة SaaS عامة. العلامة المميزة هي شبكة الأدوات 2×3 المدعومة بصور التطبيق الأصلية. النسخة المكتبية والويب تشتركان في الهوية، بينما الويب يصرّح بحدود المتصفح بدل محاكاة تشغيل ملفات محلية.

## Colors

الكحلي هو مساحة العمل، السماوي للمعلومات والحالة، الأخضر للإجراء الآمن، والأحمر للتحذير. لا تستخدم تدرجات زخرفية على عناصر التحكم.

## Typography

Segoe UI للنص العربي والواجهة، Cascadia Code للنسخ والإصدارات والعدادات، وGeorgia لعناوين بطاقات الأدوات للحفاظ على شخصية التطبيق الأصلية.

## Layout

سطح المكتب عمودان متوازنان: البرامج والأدوات. تحت 980px يتحولان إلى مسار رأسي، وتحت 640px تصبح شبكة الأدوات عمودًا واحدًا. القوائم هي مالك التمرير الداخلي الوحيد.

## Elevation & Depth

الحدود الرفيعة هي وسيلة الفصل الأساسية. الظلال محصورة في الحوارات؛ البطاقات الثابتة مسطحة.

## Shapes

زوايا شبه مربعة تعكس أدوات Windows. لا تستخدم حبوبًا أو دوائر كبيرة إلا لمؤشر الحالة.

## Components

جميع الإجراءات أزرار أصلية، والروابط تنقل فقط. البحث يملك زر مسح، والقائمة تغطي حالات النتائج والفراغ، والحوارات تعيد التركيز إلى مصدرها.

## Do's and Don'ts

- استخدم صور ونسخ التطبيق الأصلية.
- حافظ على كثافة معلومات عملية.
- وضّح أن تثبيت EXE وصلاحيات Administrator تتطلب تطبيق Windows.
- لا تستخدم alert أو confirm من المتصفح.
- لا توحِ بأن المتصفح نفّذ تغييرًا في Windows.

