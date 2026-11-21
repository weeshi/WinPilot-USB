using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal sealed class AppRow
{
    public string Id;
    public string Name;
    public string Pattern;
    public string Type;
    public string File;
    public string Version;
    public string Category;
    public override string ToString() { return Name + "  |  " + Version; }
}

internal sealed class MainForm : Form
{
    private readonly string root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    private readonly List<AppRow> allApps = new List<AppRow>();
    private readonly Queue<AppRow> installQueue = new Queue<AppRow>();
    private readonly List<string> adFiles = new List<string>();
    private CheckedListBox appList;
    private CheckBox selectAll;
    private Label countLabel;
    private Label status;
    private Label currentLabel;
    private ProgressBar progress;
    private RichTextBox output;
    private Button install;
    private Process currentProcess;
    private AppRow currentApp;
    private Timer progressTimer;
    private Timer adTimer;
    private PictureBox adPicture;
    private int adIndex;
    private int progressValue;
    private bool installing;
    private TextBox search;
    private bool refreshing;
    private readonly HashSet<string> checkedIds = new HashSet<string>();
    private static readonly string[] Categories = { "مكتبية", "الوسائط", "الحماية", "المتصفحات", "التواصل", "أدوات النظام", "مكونات التشغيل", "أخرى" };

    public MainForm(string baseDirectory = null)
    {
        if (baseDirectory != null) root = baseDirectory;
        Text = "Weeshi USB Installer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 800);
        Size = new Size(1290, 840);
        BackColor = Color.FromArgb(10, 17, 32);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);

        // Two equal header columns; the advertisement spans the full header height.
        var header = new TableLayoutPanel {
            Location = new Point(38, 12), Size = new Size(1214, 134),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent,
            Margin = Padding.Empty, Padding = Padding.Empty, AccessibleName = "applicationHeader"
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        adPicture = new PictureBox {
            Dock = DockStyle.Fill, Margin = new Padding(0, 0, 6, 0),
            SizeMode = PictureBoxSizeMode.StretchImage,
            BackColor = Color.FromArgb(14, 24, 45), BorderStyle = BorderStyle.FixedSingle,
            AccessibleName = "advertisement"
        };
        var identity = new Panel { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 0, 0), BackColor = Color.Transparent };
        var topBar = new TableLayoutPanel {
            Dock = DockStyle.Top, Height = 42, ColumnCount = 3, RowCount = 1,
            BackColor = Color.FromArgb(14, 24, 45), BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(8, 0, 8, 0)
        };
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27F));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        topBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        var drive = new Label { Text = "USB  •  Local Apps", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(170, 190, 210), Font = new Font("Consolas", 8F) };
        var engine = new Label { Text = "Offline Engine  •  V1.0", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(67, 220, 200), Font = new Font("Consolas", 8F) };
        var topTitle = new Label { Text = "Weeshi USB Installer", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 9F) };
        topBar.Controls.Add(drive, 0, 0); topBar.Controls.Add(engine, 1, 0); topBar.Controls.Add(topTitle, 2, 0);
        var branding = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(4, 8, 4, 0),
            BackColor = Color.Transparent
        };
        branding.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        branding.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86F));
        branding.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        var copy = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        copy.RowStyles.Add(new RowStyle(SizeType.Percent, 50F)); copy.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        var title = new Label { Text = "Weeshi USB Installer", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 19F), ForeColor = Color.White };
        var subtitle = new Label { Text = "تثبيت البرامج من الفلاش بنقرة واحدة\r\nبدون الحاجة إلى إنترنت", Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopRight, RightToLeft = RightToLeft.Yes, Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(180, 195, 215) };
        copy.Controls.Add(title, 0, 0); copy.Controls.Add(subtitle, 0, 1);
        var logo = new PictureBox { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent, AccessibleName = "applicationLogo", Image = LoadImageUnlocked(Path.Combine(root, "logo.png")) };
        branding.Controls.Add(copy, 0, 0); branding.Controls.Add(logo, 1, 0);
        identity.Controls.Add(branding); identity.Controls.Add(topBar);
        header.Controls.Add(adPicture, 0, 0); header.Controls.Add(identity, 1, 0);
        status = new Label { Text = "جاهز للاستخدام  •  READY", AutoSize = true, Location = new Point(660, 675), ForeColor = Color.FromArgb(91, 215, 164), Font = new Font("Segoe UI", 9F) };
        currentLabel = new Label { Text = "", AutoSize = true, Location = new Point(660, 695), ForeColor = Color.FromArgb(255, 211, 112), Font = new Font("Segoe UI", 9F) };
        Controls.AddRange(new Control[] { header, status, currentLabel });
        LoadAds();
        adTimer = new Timer { Interval = 3000 };
        adTimer.Tick += (s, e) => ShowNextAd();
        adTimer.Start();

        selectAll = new CheckBox { Text = "تحديد الكل  |  Select all", AutoSize = true, Location = new Point(38, 175), ForeColor = Color.White, AccessibleName = "selectAll" };
        selectAll.CheckedChanged += (s, e) => { if (!refreshing) SetAllChecked(selectAll.Checked); };
        var clearTop = MakeButton("إلغاء التحديد  |  Clear", 205, 168, 135, (s, e) => { checkedIds.Clear(); ApplyFilter(); });
        countLabel = new Label { Text = "0 selected", AutoSize = true, Location = new Point(365, 175), ForeColor = Color.FromArgb(180, 190, 198) };
        install = MakeButton("⚡  تثبيت صامت  |  Install silently", 38, 215, 300, (s, e) => StartInstall());
        install.BackColor = Color.FromArgb(36, 150, 92);
        install.FlatAppearance.BorderColor = Color.FromArgb(93, 225, 145);

        appList = new CategorizedCheckedListBox { Location = new Point(660, 178), Size = new Size(592, 386), CheckOnClick = true, BackColor = Color.FromArgb(14, 24, 45), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Font = new Font("Segoe UI", 10F), AccessibleName = "applicationList" };
        appList.ItemCheck += (s, e) => {
            if (refreshing) return;
            var row = (AppRow)appList.Items[e.Index];
            if (e.NewValue == CheckState.Checked) checkedIds.Add(row.Id); else checkedIds.Remove(row.Id);
            BeginInvoke((Action)UpdateSelectionState);
        };

        var appHeader = new Label { Text = "البرامج المتاحة  |  Available applications", AutoSize = true, Location = new Point(660, 154), ForeColor = Color.FromArgb(61, 220, 235), Font = new Font("Segoe UI Semibold", 12F) };
        search = new SearchBox { Text = "", Location = new Point(1010, 150), Size = new Size(242, 25), BackColor = Color.FromArgb(8, 14, 30), ForeColor = Color.FromArgb(150, 170, 195), BorderStyle = BorderStyle.FixedSingle, AccessibleName = "applicationSearch" };
        search.TextChanged += (s, e) => ApplyFilter();
        Controls.AddRange(new Control[] { selectAll, countLabel });
        var applicationPanel = new Panel {
            BackColor = Color.Transparent, AccessibleName = "availableApplicationsPanel"
        };
        appHeader.AutoSize = false;
        appHeader.SetBounds(0, 0, 560, 26);
        appHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        search.SetBounds(0, 30, 560, 25);
        search.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        appList.SetBounds(0, 61, 560, 386);
        appList.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        applicationPanel.Controls.AddRange(new Control[] { appHeader, search, appList });
        Controls.Add(applicationPanel);
        Action alignApplications = () => {
            int columnWidth = header.Width / 2;
            int width = Math.Max(360, Math.Min(560, columnWidth - 36));
            applicationPanel.SetBounds(header.Left + columnWidth + (columnWidth - width) / 2,
                154, width, 447);
        };
        alignApplications();
        header.SizeChanged += (s, e) => alignApplications();
        BuildToolCards();

        Controls.AddRange(new Control[] { clearTop, install });

        progress = new ProgressBar { Location = new Point(38, 715), Size = new Size(1214, 14), Minimum = 0, Maximum = 100, Style = ProgressBarStyle.Continuous, AccessibleName = "installProgress" };
        output = new RichTextBox { Location = new Point(38, 737), Size = new Size(1214, 35), ReadOnly = true, BackColor = Color.FromArgb(6, 11, 22), ForeColor = Color.FromArgb(198, 231, 230), Font = new Font("Consolas", 9F), BorderStyle = BorderStyle.FixedSingle, AccessibleName = "installLog", Text = "[READY]  حدد البرامج التي تريد تثبيتها ثم اضغط تثبيت صامت.\r\n" };
        var versionLabel = new Label { Text = "V1.0   →   WeeshiTech Solutions © 2025", AutoSize = true, Location = new Point(38, 782), ForeColor = Color.FromArgb(125, 145, 170), Font = new Font("Consolas", 8.5F), AccessibleName = "applicationVersion" };
        var footerStatus = new Label { Text = "جاهز للاستخدام  •  READY", AutoSize = true, Location = new Point(1090, 782), ForeColor = Color.FromArgb(91, 215, 164), Font = new Font("Segoe UI", 8.5F) };
        Controls.AddRange(new Control[] { progress, output, versionLabel, footerStatus });
        progressTimer = new Timer { Interval = 350 };
        progressTimer.Tick += (s, e) => UpdateProgressEstimate();
        Load += (s, e) => LoadApps();
    }

    private void BuildToolCards()
    {
        var grid = new Panel { Location = new Point(38, 215), Size = new Size(598, 435), BackColor = Color.FromArgb(18, 30, 50), BorderStyle = BorderStyle.FixedSingle, AccessibleName = "toolsGrid" };
        Controls.Add(grid);
        toolsGrid = grid;
        AddImageCard(10, 10, "Office", "Microsoft Office  •  2019–2021", Path.Combine(root, "assets", "office-card.png"), (s, e) => OpenOfficeInstaller());
        AddImageCard(303, 10, "Drivers", "تعريفات الأجهزة  •  SDI", Path.Combine(root, "assets", "drivers-card.png"), (s, e) => OpenDriverInstaller());
        AddImageCard(10, 145, "Windows Updates Stop", "إيقاف تحديثات Windows", Path.Combine(root, "assets", "windows-updates-stop-card.png"), (s, e) => DisableUpdates());
        AddImageCard(303, 145, "Windows Updates Start", "تشغيل تحديثات Windows", Path.Combine(root, "assets", "windows-updates-start-card.png"), (s, e) => EnableUpdates());
        AddImageCard(10, 280, "Windows + Office Activation", "تفعيل Windows وOffice", Path.Combine(root, "assets", "windows-office-activation-card.png"), (s, e) => ActivateWindowsOffice());
        var contact = new Panel { Location = new Point(303, 280), Size = new Size(285, 125), BackColor = Color.FromArgb(16, 29, 50), BorderStyle = BorderStyle.FixedSingle, AccessibleName = "contactInformation" };
        var contactTitle = new Label { Text = "بيانات التواصل  |  Contact", AutoSize = true, Location = new Point(10, 10), ForeColor = Color.FromArgb(61, 220, 235), Font = new Font("Segoe UI Semibold", 10F) };
        var contactText = new Label { Text = "0915955991   •   0945955991", AutoSize = false, Size = new Size(270, 20), Location = new Point(10, 35), ForeColor = Color.FromArgb(210, 220, 225), Font = new Font("Segoe UI", 8.5F) };
        var facebook = MakeContactButton(Path.Combine(root, "assets", "contact-facebook.png"), "#", "Facebook", 10);
        var whatsapp = MakeContactButton(Path.Combine(root, "assets", "contact-whatsapp.png"), "https://wa.me/218915955991", "WhatsApp", 75);
        var website = MakeContactButton(Path.Combine(root, "assets", "contact-website.png"), "https://www.ly-tech.ly", "Website", 140);
        var email = MakeContactButton(Path.Combine(root, "assets", "contact-email.png"), "mailto:weeshiaia@gmail.com", "Email", 205);
        contact.Controls.AddRange(new Control[] { contactTitle, contactText, facebook, whatsapp, website, email }); toolsGrid.Controls.Add(contact);
    }

    private Panel toolsGrid;

    private Button MakeContactButton(string imagePath, string target, string name, int x)
    {
        var button = new Button { Location = new Point(x, 63), Size = new Size(48, 48), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(16, 29, 50), ForeColor = Color.White, Image = LoadImageUnlocked(imagePath), ImageAlign = ContentAlignment.MiddleCenter, AccessibleName = name, Cursor = Cursors.Hand, TabStop = true };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (s, e) => OpenContactTarget(target, name);
        return button;
    }

    private void OpenContactTarget(string target, string name)
    {
        if (target == "#") { MessageBox.Show("لم يتم تحديد رابط Facebook بعد.", name, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        try { Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show(ex.Message, name, MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void AddImageCard(int x, int y, string title, string subtitle, string imagePath, EventHandler click)
    {
        var card = new Panel { Location = new Point(x, y), Size = new Size(285, 125), BackColor = Color.FromArgb(16, 29, 50), BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand, AccessibleName = title + " tool" };
        var image = new PictureBox { Location = new Point(6, 6), Size = new Size(273, 76), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(9, 14, 18), Image = LoadImageUnlocked(imagePath), Cursor = Cursors.Hand };
        var label = new Label { Text = title, AutoSize = true, Location = new Point(9, 87), ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 10.5F), Cursor = Cursors.Hand };
        var sub = new Label { Text = subtitle, AutoSize = false, Size = new Size(270, 25), Location = new Point(9, 105), ForeColor = Color.FromArgb(180, 190, 198), Cursor = Cursors.Hand };
        card.Click += click; image.Click += click; label.Click += click; sub.Click += click; card.Controls.AddRange(new Control[] { image, label, sub }); if (toolsGrid != null) toolsGrid.Controls.Add(card); else Controls.Add(card);
    }

    private Button MakeButton(string text, int x, int y, int width, EventHandler click)
    {
        var b = new Button { Text = text, Location = new Point(x, y), Width = width, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(31, 53, 60), ForeColor = Color.White, AccessibleName = text };
        b.FlatAppearance.BorderColor = Color.FromArgb(74, 135, 143); b.Click += click; return b;
    }

    private string ScriptsPath() { return Path.Combine(root, "scripts", "Install-LocalApps.ps1"); }
    private string ManifestPath() { return Path.Combine(root, "scripts", "local-apps.json"); }
    private string AppsPath() { return Path.Combine(root, "Apps"); }

    private Image LoadImageUnlocked(string path)
    {
        try { if (!File.Exists(path)) return null; using (var ms = new MemoryStream(File.ReadAllBytes(path))) using (var img = Image.FromStream(ms)) return new Bitmap(img); } catch { return null; }
    }

    private void LoadAds()
    {
        adFiles.Clear(); var dir = Path.Combine(root, "Ads"); if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly)) { try { using (var ms = new MemoryStream(File.ReadAllBytes(file))) using (var img = Image.FromStream(ms)) adFiles.Add(file); } catch { } }
        if (adFiles.Count == 0) { adPicture.Image = null; adPicture.BackColor = Color.FromArgb(28, 36, 43); adPicture.Paint += (s, e) => e.Graphics.DrawString("ضع صور الإعلانات داخل مجلد Ads\r\nضع أسماء مشفّرة مثل dsa1.xxx", Font, Brushes.LightGray, new PointF(20, 35)); } else ShowNextAd();
    }
    private void ShowNextAd() { if (adFiles.Count == 0) return; adPicture.Image = LoadImageUnlocked(adFiles[adIndex % adFiles.Count]); adIndex++; }

    private string ReadContactInfo()
    {
        var path = Path.Combine(root, "info.txt"); if (!File.Exists(path)) return "info.txt غير موجود\r\nأضف بيانات التواصل";
        try { return File.ReadAllText(path).Trim(); } catch { return "تعذر قراءة info.txt"; }
    }

    private string FindFirst(string folder, string[] patterns)
    {
        if (!Directory.Exists(folder)) return null; foreach (var pattern in patterns) { var files = Directory.GetFiles(folder, pattern, SearchOption.AllDirectories); if (files.Length > 0) return files[0]; } return null;
    }
    private void OpenOfficeInstaller()
    {
        var file = Path.Combine(root, "Apps", "office2019-2021", "Setup.exe");
        if (!File.Exists(file)) {
            MessageBox.Show("لم يتم العثور على مثبت Office في:\n" + file, "Office", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        StartNormalInstaller(file);
    }
    private void OpenDriverInstaller()
    {
        var file = FindFirst(Path.Combine(root, "Apps", "SDI_RUS"), new[] { "*.exe" }); if (file == null) { MessageBox.Show("لم يتم العثور على مثبت داخل Apps\\SDI_RUS.", "Drivers", MessageBoxButtons.OK, MessageBoxIcon.Information); return; } StartNormalInstaller(file);
    }
    private void DisableUpdates()
    {
        var script = Path.Combine(root, "scripts", "Disable-Windows-Updates.ps1"); if (!File.Exists(script)) { MessageBox.Show("ضع سكربت Disable-Windows-Updates.ps1 داخل scripts ثم أعد المحاولة.", "Windows Updates", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        try { Process.Start(new ProcessStartInfo { FileName = "powershell.exe", Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" -Force", Verb = "runas", UseShellExecute = true, WindowStyle = ProcessWindowStyle.Normal }); } catch (Exception ex) { MessageBox.Show(ex.Message, "Windows Updates", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void EnableUpdates()
    {
        var script = Path.Combine(root, "scripts", "Disable-Windows-Updates.ps1"); if (!File.Exists(script)) { MessageBox.Show("ضع سكربت Disable-Windows-Updates.ps1 داخل scripts ثم أعد المحاولة.", "Windows Updates", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        try { Process.Start(new ProcessStartInfo { FileName = "powershell.exe", Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" -Restore", Verb = "runas", UseShellExecute = true, WindowStyle = ProcessWindowStyle.Normal }); } catch (Exception ex) { MessageBox.Show(ex.Message, "Windows Updates", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void ActivateWindowsOffice()
    {
        var script = Path.Combine(root, "scripts", "MAS_AIO.cmd"); if (!File.Exists(script)) { MessageBox.Show("ضع سكربت MAS_AIO.cmd داخل scripts ثم أعد المحاولة.", "Activation", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        try { Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = "/c \"\"" + script + "\"\"", WorkingDirectory = root, Verb = "runas", UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show(ex.Message, "Activation", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void StartNormalInstaller(string file)
    {
        try { Process.Start(new ProcessStartInfo { FileName = file, WorkingDirectory = Path.GetDirectoryName(file), UseShellExecute = true, Verb = "runas" }); output.AppendText("\r\nStarted normal installer: " + Path.GetFileName(file) + "\r\n"); } catch (Exception ex) { MessageBox.Show(ex.Message, "Installer", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void LoadApps()
    {
        try
        {
            if (!File.Exists(ManifestPath())) throw new FileNotFoundException("Missing scripts\\local-apps.json"); if (!Directory.Exists(AppsPath())) throw new DirectoryNotFoundException("Missing Apps folder beside WinPilot-USB.exe");
            var data = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(ManifestPath())) as object[]; allApps.Clear(); appList.Items.Clear();
            if (data != null) foreach (var raw in data) { var item = raw as Dictionary<string, object>; if (item == null) continue; var row = new AppRow { Id = Value(item, "Id"), Name = Value(item, "Name"), Pattern = Value(item, "Pattern"), Type = Value(item, "Type") }; var files = Directory.GetFiles(AppsPath(), row.Pattern, SearchOption.AllDirectories); if (files.Length > 0) { row.File = files[0]; row.Version = DetectVersion(row.File); row.Category = CategoryFor(row.Id); allApps.Add(row); } }
            allApps.Sort((a, b) => { int c = Array.IndexOf(Categories, a.Category).CompareTo(Array.IndexOf(Categories, b.Category)); return c != 0 ? c : StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name); });
            ApplyFilter(); progress.Value = 0; status.Text = ""; currentLabel.Text = ""; output.Text = "تم فحص مجلد Apps. اختر البرامج التي تريد تثبيتها.\r\nApps folder scanned. Select the applications to install.\r\n";
        }
        catch (Exception ex) { status.Text = "خطأ في الفحص  —  Scan error"; output.Text = ex.Message; MessageBox.Show(ex.Message, "Weeshi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private string Value(Dictionary<string, object> item, string key) { return item.ContainsKey(key) && item[key] != null ? Convert.ToString(item[key]) : ""; }
    private string DetectVersion(string file) { try { var info = FileVersionInfo.GetVersionInfo(file); if (!string.IsNullOrWhiteSpace(info.ProductVersion)) return info.ProductVersion.Trim(); if (!string.IsNullOrWhiteSpace(info.FileVersion)) return info.FileVersion.Trim(); } catch { } var match = Regex.Match(Path.GetFileName(file), @"\d+(?:\.\d+){1,4}"); return match.Success ? match.Value : "Version unknown"; }
    private static string CategoryFor(string id)
    {
        switch (id) {
            case "libreoffice": case "foxit": case "notepadpp": case "googledrive": return Categories[0];
            case "vlc": case "gom": case "klite": return Categories[1];
            case "360": case "kaspersky-vrt": return Categories[2];
            case "chrome": case "firefox": case "brave": case "chromium": return Categories[3];
            case "telegram": case "whatsapp": case "zoom": return Categories[4];
            case "anydesk": case "crystaldiskinfo": case "poweriso": case "winrar": return Categories[5];
            case "vcredist-x64": case "vcredist-x86": case "dotnet-desktop-x64": case "dotnet-desktop-x86": return Categories[6];
            default: return Categories[7];
        }
    }
    private void ApplyFilter()
    {
        if (appList == null) return;
        string query = search == null ? "" : search.Text.Trim();
        refreshing = true;
        appList.BeginUpdate();
        try {
            appList.Items.Clear();
            foreach (var row in allApps)
                if ((row.Name + " " + row.Id + " " + row.Category).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    appList.Items.Add(row, checkedIds.Contains(row.Id));
        } finally { appList.EndUpdate(); refreshing = false; }
        UpdateSelectionState();
    }
    private void SetAllChecked(bool value)
    {
        if (appList == null) return;
        foreach (AppRow row in appList.Items)
            if (value) checkedIds.Add(row.Id); else checkedIds.Remove(row.Id);
        ApplyFilter();
    }
    private void UpdateSelectionState()
    {
        if (appList == null || countLabel == null) return;
        countLabel.Text = checkedIds.Count + " محدد / selected";
        refreshing = true;
        selectAll.Checked = appList.Items.Count > 0 && appList.CheckedItems.Count == appList.Items.Count;
        refreshing = false;
    }
    private List<AppRow> SelectedApps() { return allApps.FindAll(row => checkedIds.Contains(row.Id)); }
    private void RunDryRun() { var selected = SelectedApps(); if (selected.Count == 0) { MessageBox.Show("حدد برنامجاً واحداً على الأقل أولاً.\nPlease select at least one application.", "Weeshi", MessageBoxButtons.OK, MessageBoxIcon.Information); return; } output.Clear(); foreach (var app in selected) output.AppendText("[DRY-RUN] " + app.Name + "\r\n  File: " + Path.GetFileName(app.File) + "\r\n"); status.Text = "اكتملت المعاينة  —  Dry run complete"; }
    private void StartInstall() { if (installing) return; var selected = SelectedApps(); if (selected.Count == 0) { MessageBox.Show("حدد برنامجاً واحداً على الأقل أولاً.", "Weeshi", MessageBoxButtons.OK, MessageBoxIcon.Information); return; } if (MessageBox.Show("سيتم تثبيت " + selected.Count + " برنامجاً بالتتابع. عند الفشل ستظهر نافذة متابعة أو إلغاء.\n\nContinue?", "تأكيد التثبيت", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return; installQueue.Clear(); foreach (var app in selected) installQueue.Enqueue(app); installing = true; install.Enabled = false; selectAll.Enabled = false; appList.Enabled = false; output.Clear(); output.AppendText("بدء التثبيت الصامت بالتتابع...\r\nSilent installation queue started...\r\n"); StartNextInstall(); }
    private void StartNextInstall() { if (installQueue.Count == 0) { FinishInstall(); return; } currentApp = installQueue.Dequeue(); progressValue = 1; progress.Value = 1; currentLabel.Text = "Installing " + currentApp.Name + " (1%)"; status.Text = "جاري التثبيت  —  Installing"; output.AppendText("\r\n>>> Installing: " + currentApp.Name + "\r\n    File: " + Path.GetFileName(currentApp.File) + "\r\n"); var psi = new ProcessStartInfo { FileName = "powershell.exe", Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + ScriptsPath() + "\" -SourcePath \"" + AppsPath() + "\" -Name \"" + currentApp.Id + "\"", WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8 }; try { currentProcess = new Process { StartInfo = psi, EnableRaisingEvents = true }; currentProcess.OutputDataReceived += ProcessOutput; currentProcess.ErrorDataReceived += ProcessError; currentProcess.Exited += ProcessExited; currentProcess.Start(); currentProcess.BeginOutputReadLine(); currentProcess.BeginErrorReadLine(); progressTimer.Start(); } catch (Exception ex) { HandleFailure("تعذر تشغيل PowerShell: " + ex.Message); } }
    private void ProcessOutput(object sender, DataReceivedEventArgs e) { if (!string.IsNullOrWhiteSpace(e.Data)) BeginInvoke((Action)(() => HandleLine(e.Data))); }
    private void ProcessError(object sender, DataReceivedEventArgs e) { if (!string.IsNullOrWhiteSpace(e.Data)) BeginInvoke((Action)(() => output.AppendText("ERROR: " + e.Data + "\r\n"))); }
    private void HandleLine(string line) { if (line.IndexOf("[WINPILOT] INSTALLED|", StringComparison.OrdinalIgnoreCase) >= 0) { output.AppendText("    installed\r\n"); return; } if (line.IndexOf("[WINPILOT] FAILED|", StringComparison.OrdinalIgnoreCase) >= 0) { output.AppendText("    failed: " + line + "\r\n"); return; } if (!line.StartsWith("[WINPILOT] INSTALLING|", StringComparison.OrdinalIgnoreCase)) output.AppendText(line + "\r\n"); }
    private void ProcessExited(object sender, EventArgs e) { BeginInvoke((Action)(() => { progressTimer.Stop(); var code = currentProcess.ExitCode; if (code == 0) { progress.Value = 100; currentLabel.Text = "Installed " + currentApp.Name + " (100%)"; output.AppendText("    Installed successfully.\r\n"); StartNextInstall(); } else HandleFailure("Exit code " + code + ". Review scripts\\logs for details."); })); }
    private void UpdateProgressEstimate() { if (!installing || currentProcess == null || currentProcess.HasExited) return; if (progressValue < 90) progressValue += 3; progress.Value = progressValue; currentLabel.Text = "Installing " + currentApp.Name + " (" + progressValue + "%)"; }
    private void HandleFailure(string reason) { progressTimer.Stop(); progress.Value = Math.Min(progressValue, 99); currentLabel.Text = "Failed to install " + currentApp.Name; output.AppendText("    Failed to install: " + reason + "\r\n"); var answer = MessageBox.Show("فشل تثبيت البرنامج:\r\n" + currentApp.Name + "\r\n\r\nالسبب:\r\n" + reason + "\r\n\r\nهل تريد إكمال تثبيت البرنامج التالي؟", "Install failed", MessageBoxButtons.YesNo, MessageBoxIcon.Error); if (answer == DialogResult.Yes) StartNextInstall(); else CancelInstall(); }
    private void CancelInstall() { if (currentProcess != null && !currentProcess.HasExited) try { currentProcess.Kill(); } catch { } output.AppendText("\r\nInstallation cancelled by user.\r\n"); status.Text = "تم الإلغاء  —  Cancelled"; installing = false; RestoreControls(); }
    private void FinishInstall() { progressTimer.Stop(); currentLabel.Text = "Install All Complete"; status.Text = "اكتمل تثبيت كل البرامج المحددة  —  Install All Complete"; output.AppendText("\r\n=== Install All Complete ===\r\nLogs: scripts\\logs\r\n"); installing = false; RestoreControls(); MessageBox.Show("Install All Complete\n\nراجع السجل لمزيد من التفاصيل.", "Weeshi", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    private void RestoreControls() { install.Enabled = true; selectAll.Enabled = true; appList.Enabled = true; }
}

internal static class Program
{
    [STAThread]
    private static void Main() { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new MainForm()); }
}


internal sealed class CategorizedCheckedListBox : CheckedListBox
{
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        base.OnDrawItem(e);
        if (e.Index < 0 || e.Index >= Items.Count - 1) return;
        var current = Items[e.Index] as AppRow;
        var next = Items[e.Index + 1] as AppRow;
        if (current == null || next == null || current.Category == next.Category) return;
        using (var pen = new Pen(Color.FromArgb(100, 130, 155)))
            e.Graphics.DrawLine(pen, e.Bounds.Left + 4, e.Bounds.Bottom - 1,
                e.Bounds.Right - 4, e.Bounds.Bottom - 1);
    }
}

internal sealed class SearchBox : TextBox
{
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, string text);
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        SendMessage(Handle, 0x1501, IntPtr.Zero, "بحث في البرامج المتاحة...");
    }
}
