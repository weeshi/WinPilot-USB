using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Windows.Forms;

internal static class UpdaterProgram
{
    private static Dictionary<string, string> Parse(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++) {
            if (!args[i].StartsWith("--")) continue;
            var key = args[i].Substring(2);
            result[key] = i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[++i] : "true";
        }
        return result;
    }

    private static void Log(string target, string text)
    {
        try {
            var folder = Path.Combine(target, "scripts", "logs"); Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "update.log"), DateTime.Now.ToString("s") + " " + text + Environment.NewLine, Encoding.UTF8);
        } catch { }
    }

    private static void ExtractSafe(string package, string stage)
    {
        var stageRoot = Path.GetFullPath(stage).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        using (var archive = ZipFile.OpenRead(package)) foreach (var entry in archive.Entries) {
            var destination = Path.GetFullPath(Path.Combine(stage, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
            if (!destination.StartsWith(stageRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unsafe path in update package: " + entry.FullName);
            if (string.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(destination); continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            entry.ExtractToFile(destination, true);
        }
    }

    private static void CopyTree(string stage, string target, string backup, List<string> changed, HashSet<string> created)
    {
        foreach (var source in Directory.GetFiles(stage, "*", SearchOption.AllDirectories)) {
            var relative = source.Substring(stage.TrimEnd(Path.DirectorySeparatorChar).Length).TrimStart(Path.DirectorySeparatorChar);
            var destination = Path.GetFullPath(Path.Combine(target, relative));
            var targetRoot = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!destination.StartsWith(targetRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unsafe target path: " + relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            if (File.Exists(destination)) {
                var backupFile = Path.Combine(backup, relative); Directory.CreateDirectory(Path.GetDirectoryName(backupFile)); File.Copy(destination, backupFile, true);
            } else created.Add(destination);
            File.Copy(source, destination, true); changed.Add(destination);
        }
    }

    private static void Rollback(string target, string backup, List<string> changed, HashSet<string> created)
    {
        for (int i = changed.Count - 1; i >= 0; i--) try {
            var destination = changed[i];
            if (created.Contains(destination)) { if (File.Exists(destination)) File.Delete(destination); continue; }
            var relative = destination.Substring(Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar).Length).TrimStart(Path.DirectorySeparatorChar);
            var backupFile = Path.Combine(backup, relative); if (File.Exists(backupFile)) File.Copy(backupFile, destination, true);
        } catch { }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        var options = Parse(args);
        string package, target, restart, pidText;
        if (!options.TryGetValue("package", out package) || !options.TryGetValue("target", out target) || !options.TryGetValue("restart", out restart)) return 2;
        options.TryGetValue("pid", out pidText); int pid; int.TryParse(pidText, out pid);
        bool silent = options.ContainsKey("silent"), noRestart = options.ContainsKey("no-restart");
        var work = Path.Combine(Path.GetTempPath(), "WinPilot-Apply-" + Guid.NewGuid().ToString("N"));
        var stage = Path.Combine(work, "stage"); var backup = Path.Combine(work, "backup");
        var changed = new List<string>(); var created = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try {
            target = Path.GetFullPath(target); package = Path.GetFullPath(package);
            if (!File.Exists(package) || !Directory.Exists(target)) throw new FileNotFoundException("Update package or target folder was not found.");
            if (pid > 0) try { var process = Process.GetProcessById(pid); if (!process.WaitForExit(60000)) throw new TimeoutException("WinPilot did not close in time."); } catch (ArgumentException) { }
            Directory.CreateDirectory(stage); Directory.CreateDirectory(backup); ExtractSafe(package, stage);
            if (!File.Exists(Path.Combine(stage, "WinPilot-USB.exe"))) throw new InvalidDataException("The update package has no WinPilot-USB.exe.");
            CopyTree(stage, target, backup, changed, created);
            Log(target, "Update installed successfully from " + Path.GetFileName(package));
            if (!noRestart) Process.Start(new ProcessStartInfo { FileName = Path.Combine(target, restart), WorkingDirectory = target, UseShellExecute = true });
            if (!silent) MessageBox.Show("تم تثبيت تحديث WinPilot بنجاح.\nUpdate installed successfully.", "WinPilot Updater", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        } catch (Exception ex) {
            Rollback(target, backup, changed, created); Log(target, "Update failed and rollback ran: " + ex);
            if (!silent) MessageBox.Show("فشل التحديث وتمت استعادة الملفات السابقة.\n\n" + ex.Message, "WinPilot Updater", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        } finally { try { Directory.Delete(work, true); } catch { } try { if (File.Exists(package)) File.Delete(package); } catch { } }
    }
}
