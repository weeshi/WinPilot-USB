(() => {
  "use strict";

  const root = document.documentElement;
  const toast = document.getElementById("toast");
  const sections = [...document.querySelectorAll(".searchable")];
  const navLinks = [...document.querySelectorAll("#sectionNav a")];
  let toastTimer;

  const notify = (message) => {
    toast.textContent = message;
    toast.classList.add("show");
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => toast.classList.remove("show"), 1800);
  };

  const savedTheme = localStorage.getItem("winpilot-guide-theme");
  if (savedTheme) root.dataset.theme = savedTheme;

  document.getElementById("themeToggle").addEventListener("click", () => {
    const next = root.dataset.theme === "light" ? "dark" : "light";
    root.dataset.theme = next;
    localStorage.setItem("winpilot-guide-theme", next);
    notify(next === "light" ? "تم تفعيل المظهر الفاتح" : "تم تفعيل المظهر الداكن");
  });

  document.getElementById("printGuide").addEventListener("click", () => window.print());

  const normalizeVersion = (value) => {
    const match = String(value).trim().replace(/^v/i, "").match(/^(\d+)\.(\d+)\.(\d+)$/);
    return match ? match.slice(1).map(Number) : null;
  };

  const versionInput = document.getElementById("versionInput");
  const versionTag = document.getElementById("versionTag");
  const displayVersion = document.getElementById("displayVersion");
  const assemblyVersion = document.getElementById("assemblyVersion");
  const versionTemplates = new Map();

  ["tagCode", "clobberCode", "verifyCode"].forEach((id) => {
    const node = document.getElementById(id);
    if (node) versionTemplates.set(node, node.textContent);
  });

  const renderVersion = () => {
    const parts = normalizeVersion(versionInput.value);
    const valid = Boolean(parts);
    versionInput.setAttribute("aria-invalid", String(!valid));
    if (!valid) return;
    const version = parts.join(".");
    versionTag.textContent = "v" + version;
    displayVersion.textContent = "V" + version;
    assemblyVersion.textContent = version + ".0";
    versionTemplates.forEach((template, node) => {
      node.textContent = template
        .replaceAll("v1.1.1", "v" + version)
        .replaceAll("V1.1.1", "V" + version);
    });
  };

  versionInput.addEventListener("input", renderVersion);
  document.querySelectorAll("[data-bump]").forEach((button) => {
    button.addEventListener("click", () => {
      const parts = normalizeVersion(versionInput.value) || [1, 1, 0];
      const kind = button.dataset.bump;
      if (kind === "patch") parts[2] += 1;
      if (kind === "minor") { parts[1] += 1; parts[2] = 0; }
      if (kind === "major") { parts[0] += 1; parts[1] = 0; parts[2] = 0; }
      versionInput.value = parts.join(".");
      renderVersion();
    });
  });
  renderVersion();

  const copyText = async (text) => {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text);
      return;
    }
    const helper = document.createElement("textarea");
    helper.value = text;
    helper.style.position = "fixed";
    helper.style.opacity = "0";
    document.body.appendChild(helper);
    helper.select();
    document.execCommand("copy");
    helper.remove();
  };

  document.querySelectorAll("[data-copy-target]").forEach((button) => {
    button.addEventListener("click", async () => {
      const target = document.getElementById(button.dataset.copyTarget);
      if (!target) return;
      try {
        await copyText(target.textContent.trim());
        notify("تم نسخ الأمر");
      } catch {
        notify("تعذر النسخ؛ حدّد الأمر يدويًا");
      }
    });
  });

  const search = document.getElementById("guideSearch");
  search.addEventListener("input", () => {
    const query = search.value.trim().toLocaleLowerCase("ar");
    sections.forEach((section) => {
      const haystack = (section.dataset.search + " " + section.textContent).toLocaleLowerCase("ar");
      section.classList.toggle("search-hidden", Boolean(query) && !haystack.includes(query));
    });
    navLinks.forEach((link) => {
      const section = document.querySelector(link.getAttribute("href"));
      link.classList.toggle("search-hidden", section && section.classList.contains("search-hidden"));
    });
  });

  if ("IntersectionObserver" in window) {
    const observer = new IntersectionObserver((entries) => {
      const visible = entries.filter((entry) => entry.isIntersecting)
        .sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];
      if (!visible) return;
      navLinks.forEach((link) => {
        link.classList.toggle("active", link.getAttribute("href") === "#" + visible.target.id);
      });
    }, { rootMargin: "-18% 0px -67% 0px", threshold: [0, .2, .5] });
    sections.forEach((section) => observer.observe(section));
  }

  const checklist = [...document.querySelectorAll("#releaseChecklist input")];
  const checkCount = document.getElementById("checkCount");
  const checkProgress = document.getElementById("checkProgress");
  const storageKey = "winpilot-release-checklist-v1";

  const saveChecklist = () => {
    localStorage.setItem(storageKey, JSON.stringify(checklist.map((item) => item.checked)));
  };
  const renderChecklist = () => {
    const checked = checklist.filter((item) => item.checked).length;
    checkCount.textContent = checked + " من " + checklist.length;
    checkProgress.style.width = (checked / checklist.length * 100) + "%";
    checkProgress.parentElement.setAttribute("aria-label", "اكتمل " + checked + " من " + checklist.length);
    if (checked === checklist.length) notify("الإصدار جاهز للنشر");
  };

  try {
    const stored = JSON.parse(localStorage.getItem(storageKey) || "[]");
    stored.forEach((value, index) => { if (checklist[index]) checklist[index].checked = Boolean(value); });
  } catch { localStorage.removeItem(storageKey); }

  checklist.forEach((item) => item.addEventListener("change", () => {
    saveChecklist();
    renderChecklist();
  }));
  document.getElementById("resetChecklist").addEventListener("click", () => {
    checklist.forEach((item) => { item.checked = false; });
    saveChecklist();
    renderChecklist();
    notify("بدأت قائمة إصدار جديدة");
  });
  renderChecklist();

  document.querySelectorAll('a[target="_blank"]').forEach((link) => {
    link.rel = "noopener noreferrer";
  });
})();

