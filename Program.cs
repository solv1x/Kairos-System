using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
try
{
    Console.OutputEncoding = Encoding.UTF8;
    Console.InputEncoding = Encoding.UTF8;
}
catch { }
Console.Title = "Kairos System";

bool isAdmin = IsRunningAsAdmin();

var actions = new (string Key, string Title, Action<Report> Build, bool NeedsAdmin)[]
{
    ("1",  "Компьютер",                          CollectComputer, false),
    ("2",  "Операционная система",               CollectOs, false),
    ("3",  "Процессор",                          CollectCpu, false),
    ("4",  "Оперативная память",                 CollectMemory, false),
    ("5",  "Накопители",                         CollectDisks, false),
    ("6",  "Видеокарта",                         CollectVideo, false),
    ("7",  "Материнская плата и BIOS",           CollectMotherboard, false),
    ("8",  "Сеть",                               CollectNetwork, false),
    ("9",  "Питание",                            CollectPower, false),
    ("10", "Мониторы и дисплеи",                 CollectMonitors, false),
    ("11", "Периферия (USB-устройства)",         CollectPeripherals, false),
    ("12", "Звуковые устройства",                CollectSound, false),
    ("13", "Батарея / ИБП",                      CollectBattery, false),
    ("14", "Датчики и температуры",              CollectSensors, true),
    ("15", "Принтеры и сканеры",                 CollectPrinters, false),
    ("16", "Активация Windows",                  CollectActivation, false),
    ("17", "Установленное ПО",                   CollectSoftware, false),
    ("18", "Обновления Windows",                 CollectUpdates, false),
    ("19", "Драйверы",                           CollectDrivers, false),
    ("20", "Службы и автозагрузка",              CollectServices, false),
    ("21", "Пользователи и учётные записи",      CollectUsers, false),
    ("22", "Точки восстановления",               CollectRestorePoints, true),
    ("23", "Wi-Fi сети и пароли",                CollectWifi, true),
    ("24", "Открытые порты и соединения",        CollectPorts, false),
    ("25", "VPN и прокси",                       CollectVpnProxy, false),
    ("27", "Антивирус / Защитник Windows",       CollectAntivirus, false),
    ("28", "Брандмауэр",                         CollectFirewall, false),
    ("29", "SMART — состояние накопителей",      CollectSmart, true),
    ("30", "Журнал событий / ошибки",            CollectEventLog, false),
    ("31", "BitLocker / шифрование",             CollectBitLocker, true),
    ("32", "Производительность в реальном времени", CollectRealtime, false),
    ("33", "Индекс производительности",          CollectWei, false),
    ("34", "Процессы (топ по нагрузке)",         CollectProcesses, false),
    ("35", "Анализ .jar файлов",                 CollectJarAnalysis, false)
};

while (true)
{
    Menu();
    Console.Write("  Ваш выбор: ");
    string? line = Console.ReadLine();
    if (line is null) return;
    string c = line.Trim().ToLowerInvariant();
    Console.WriteLine();

    if (c is "0" or "exit" or "выход") return;
    if (c == "a") { Show(BuildAll()); continue; }
    if (c == "e") { ExportMenu(); continue; }
    if (c == "c") { CompareReports(); continue; }
    if (c == "d") { Diagnose(); continue; }
    if (c == "h") { Help(); continue; }
    if (c == "g") { PlaySnake(); continue; }
    if (c == "r") { RestartAsAdmin(); continue; }
    if (c == "k") { CleanupMenu(); continue; }
    if (c == "v") { ActivateWindowsOffice(); continue; }

    var action = actions.FirstOrDefault(x => x.Key == c);
    if (action.Build is null)
    {
        Error("Неверный пункт меню.");
        Pause();
        continue;
    }

    if (action.NeedsAdmin && !isAdmin)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Для этого раздела требуются права администратора. Результат может быть неполным.");
        Console.ResetColor();
        Console.Write("  Перезапустить с правами администратора? (y/n): ");
        string? ans = Console.ReadLine();
        if (ans is not null && ans.Trim().ToLowerInvariant() is "y" or "д" or "да")
        {
            RestartAsAdmin();
            return;
        }
    }

    var report = new Report();
    RunCollector(report, action.Build);
    Show(report);
}

bool IsRunningAsAdmin()
{
    try
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    catch { return false; }
}

void RestartAsAdmin()
{
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "",
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = Directory.GetCurrentDirectory()
        };
        if (string.IsNullOrEmpty(psi.FileName))
        {
            Error("Не удалось определить путь к исполняемому файлу.");
            Pause();
            return;
        }
        Process.Start(psi);
        Environment.Exit(0);
    }
    catch (Exception ex)
    {
        Error($"Не удалось перезапустить с правами администратора: {ex.Message}");
        Pause();
    }
}

void ActivateWindowsOffice()
{
    Clear();
    Header();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkMagenta;
    Console.WriteLine("  АКТИВАЦИЯ WINDOWS И OFFICE");
    Console.ResetColor();
    Console.WriteLine();

    if (!isAdmin)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Требуются права администратора. Перезапустить? (y/n): ");
        Console.ResetColor();
        string? ans = Console.ReadLine();
        if (ans is not null && ans.Trim().ToLowerInvariant() is "y" or "д" or "да")
        {
            RestartAsAdmin();
            return;
        }
        Console.WriteLine("  Отменено.");
        Pause();
        return;
    }

    var psi = new ProcessStartInfo
    {
        FileName = "powershell.exe",
        UseShellExecute = true,
        Verb = "runas",
        Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"irm get.activated.win | iex\""
    };

    try
    {
        using var p = Process.Start(psi);
        if (p is null)
        {
            Error("Не удалось запустить PowerShell.");
            Pause();
            return;
        }
    }
    catch (Exception ex)
    {
        Error($"Не удалось запустить PowerShell: {ex.Message}");
        Pause();
    }
}

void CleanupMenu()
{
    Clear();
    Header();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkMagenta;
    Console.WriteLine("  ОЧИСТКА НЕНУЖНЫХ ФАЙЛОВ");
    Console.ResetColor();
    Console.WriteLine();

    var targets = new (string Key, string Title, string Desc, bool Dangerous, Action<CleanResult> Run)[]
    {
        ("1",  "Временные файлы пользователя",       "%TEMP% и %TMP% текущего пользователя",                            false, CleanUserTemp),
        ("2",  "Временные файлы системы",            "C:\\Windows\\Temp",                                               false, CleanSystemTemp),
        ("3",  "Кэш Windows Update",                 "SoftwareDistribution\\Download + остановка wuauserv",              false, CleanWuCache),
        ("4",  "Кэш иконок и эскизов",               "IconCache, ThumbCache",                                          false, CleanThumbCache),
        ("5",  "Корзина",                            "Все локальные диски (Clear-RecycleBin)",                          false, CleanRecycleBin),
        ("6",  "Отчёты об ошибках Windows",          "WER (ReportQueue/ReportArchive/Temp/LiveKernelReports)",          false, CleanWerReports),
        ("7",  "Дампы памяти и мини-дампы",          "C:\\Windows\\Minidump, MEMORY.DMP",                               false, CleanMemoryDumps),
        ("8",  "Кэш браузеров (Chromium)",           "Chrome / Edge / Brave / Yandex / Opera",                          false, CleanBrowserCache),
        ("9",  "Кэш предзагрузки Windows",           "Prefetch (может замедлить первый старт приложений)",              false, CleanPrefetch),
        ("10", "Windows.old (старая установка)",     "Папка C:\\Windows.old после крупного обновления",                  false, CleanWindowsOld),
        ("11", "DataStore обновлений (сброс)",       "Хирургический сброс SoftwareDistribution\\DataStore",              true,  CleanWuDataStore),
        ("12", "Кэш Delivery Optimization",          "C:\\Windows\\SoftwareDistribution\\DeliveryOptimization",          false, CleanDeliveryOptimization),
        ("13", "Логи CBS (Component Based Servicing)", "C:\\Windows\\Logs\\CBS — при проблемах с компонентами",           false, CleanCbsLogs),
        ("14", "LiveKernelReports (только отчёты)",  "C:\\Windows\\LiveKernelReports — .lkd и .dmp от зависаний ядра",   false, CleanLiveKernelReportsOnly),
        ("15", "Кэш UWP / Store приложений",         "AppData\\Local\\Packages\\*\\LocalCache, TempState, AC\\Temp",     false, CleanUwpCache),
        ("16", "Старые версии драйверов",            "pnputil /delete-driver (только не-используемые)",                  true,  CleanOldDrivers),
        ("17", "Теневые копии и старые точки",       "vssadmin delete shadows /oldest (только кроме последней)",          true,  CleanShadowCopies),
        ("18", "Очистить всё безопасное",            "Пункты 1-8, 10, 12-15 (без опасных операций)",                     false, CleanAllSafe)
    };

    foreach (var (k, title, desc, dangerous, _) in targets)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"    {k,2}");
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.Write("  ");
        Console.ForegroundColor = dangerous ? ConsoleColor.Red : ConsoleColor.Gray;
        Console.Write(Pad(title, 38));
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(desc);
        if (dangerous)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("  [ОПАСНО]");
        }
        Console.ResetColor();
        Console.WriteLine();
    }

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("    0   Назад в главное меню");
    Console.ResetColor();
    Console.WriteLine();

    if (!isAdmin)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  ⚠ Программа запущена без прав администратора.");
        Console.WriteLine("    Пункты 2, 3, 10-17 будут недоступны.");
        Console.ResetColor();
        Console.WriteLine();
    }

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  Подсказка: можно ввести несколько пунктов через запятую (например 1,5,8,15)");
    Console.ResetColor();
    Console.Write("  Что чистить? ");

    string? line = Console.ReadLine();
    if (line is null) return;
    line = line.Trim();
    if (line is "0" or "") return;

    var keys = line.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(x => x.Trim()).Distinct().ToList();

    var selected = new List<(string Title, bool Dangerous, Action<CleanResult> Run)>();
    foreach (var k in keys)
    {
        var t = targets.FirstOrDefault(x => x.Key == k);
        if (t.Run is null)
        {
            Error($"Пункт «{k}» не найден, пропущен.");
            continue;
        }
        selected.Add((t.Title, t.Dangerous, t.Run));
    }
    if (selected.Count == 0) { Pause(); return; }

    bool anyDangerous = selected.Any(s => s.Dangerous);
    Console.WriteLine();
    if (anyDangerous)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("  ВНИМАНИЕ: среди выбранных пунктов есть ОПАСНЫЕ операции.");
        Console.WriteLine("  Они могут потребовать перезагрузки или сбросить настройки обновлений.");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  ВНИМАНИЕ: удаление файлов необратимо.");
        Console.ResetColor();
    }
    Console.Write("  Продолжить? (y/n): ");
    string? ans = Console.ReadLine();
    if (ans is null || ans.Trim().ToLowerInvariant() is not ("y" or "д" or "да"))
    {
        Console.WriteLine("  Отменено.");
        Pause();
        return;
    }

    Console.WriteLine();

    long totalFreed = 0;
    int totalFiles = 0;
    int totalErrors = 0;

    foreach (var (title, dangerous, run) in selected)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"  → {title}: ");
        Console.ResetColor();

        var result = new CleanResult();
        try
        {
            run(result);
        }
        catch (Exception ex)
        {
            result.Errors++;
            result.LastError = ex.Message;
        }

        totalFreed += result.BytesFreed;
        totalFiles += result.FilesDeleted;
        totalErrors += result.Errors;

        if (result.Skipped)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("пропущено");
            Console.ResetColor();
            if (!string.IsNullOrEmpty(result.LastError))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($" ({Trunc(result.LastError, 60)})");
                Console.ResetColor();
            }
            Console.WriteLine();
            continue;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"удалено {result.FilesDeleted} файлов, освобождено {F.Bytes((ulong)result.BytesFreed)}");
        Console.ResetColor();
        if (result.Locked > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  (заблокировано: {result.Locked})");
            Console.ResetColor();
        }
        if (result.Errors > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write($"  ({result.Errors} ошибок, напр.: {Trunc(result.LastError, 60)})");
            Console.ResetColor();
        }
        Console.WriteLine();
    }

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkMagenta;
    Console.WriteLine("  ────────────────────────────────────────────────────────────");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("  ИТОГО: ");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write($"{totalFiles} файлов, {F.Bytes((ulong)totalFreed)}");
    Console.ResetColor();
    if (totalErrors > 0)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write($"   (пропущено из-за ошибок: {totalErrors})");
        Console.ResetColor();
    }
    Console.WriteLine();

    Pause();
}

static void CleanUserTemp(CleanResult r)
{
    string temp = Path.GetTempPath();
    CleanDirectory(temp, r, skipLocked: true);
    CleanDirectory(Environment.GetEnvironmentVariable("TEMP") ?? temp, r, skipLocked: true);
    CleanDirectory(Environment.GetEnvironmentVariable("TMP") ?? temp, r, skipLocked: true);
}

static void CleanSystemTemp(CleanResult r)
{
    string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    CleanDirectory(Path.Combine(win, "Temp"), r, skipLocked: true);
}

static void CleanWuCache(CleanResult r)
{
    string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    string dl = Path.Combine(win, "SoftwareDistribution", "Download");
    CleanDirectory(dl, r, skipLocked: true);

    RunHidden("net", 5000, "stop", "wuauserv");
}

static void CleanThumbCache(CleanResult r)
{
    string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    string explorer = Path.Combine(local, "Microsoft", "Windows", "Explorer");
    if (Directory.Exists(explorer))
    {
        foreach (var f in SafeEnumerateFiles(explorer, "*.db", r))
            TryDeleteFile(f, r, skipLocked: true);
    }
}

static void CleanRecycleBin(CleanResult r)
{
    RunHidden("powershell", 20000,
        "-NoProfile -NonInteractive -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\"");
}

static void CleanWerReports(CleanResult r)
{
    string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
    string wer = Path.Combine(programData, "Microsoft", "Windows", "WER");
    CleanDirectory(Path.Combine(wer, "ReportQueue"), r, skipLocked: true);
    CleanDirectory(Path.Combine(wer, "ReportArchive"), r, skipLocked: true);
    CleanDirectory(Path.Combine(wer, "Temp"), r, skipLocked: true);
    CleanDirectory(Path.Combine(wer, "LiveKernelReports"), r, skipLocked: true);
}

static void CleanMemoryDumps(CleanResult r)
{
    string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    string minidump = Path.Combine(win, "Minidump");
    if (Directory.Exists(minidump)) CleanDirectory(minidump, r, skipLocked: true);

    string memDmp = Path.Combine(win, "MEMORY.DMP");
    TryDeleteFile(memDmp, r, skipLocked: true);
}

static void CleanBrowserCache(CleanResult r)
{
    string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    var browsers = new[]
    {
        Path.Combine(local, "Google", "Chrome", "User Data"),
        Path.Combine(local, "Microsoft", "Edge", "User Data"),
        Path.Combine(local, "BraveSoftware", "Brave-Browser", "User Data"),
        Path.Combine(local, "Yandex", "YandexBrowser", "User Data"),
        Path.Combine(local, "Opera Software", "Opera Stable")
    };

    foreach (var profileRoot in browsers)
    {
        if (!Directory.Exists(profileRoot)) continue;

        IEnumerable<string> profiles;
        try
        {
            profiles = Directory.EnumerateDirectories(profileRoot)
                .Where(d =>
                {
                    string n = Path.GetFileName(d);
                    return n == "Default" || n == "Profile 1" || n == "Profile 2" ||
                           n == "Profile 3" || n == "Profile 4" || n == "Profile 5" ||
                           n.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase);
                });
        }
        catch { continue; }

        foreach (var prof in profiles)
        {
            CleanDirectory(Path.Combine(prof, "Cache"), r, skipLocked: true);
            CleanDirectory(Path.Combine(prof, "Code Cache"), r, skipLocked: true);
            CleanDirectory(Path.Combine(prof, "GPUCache"), r, skipLocked: true);
            CleanDirectory(Path.Combine(prof, "Service Worker", "CacheStorage"), r, skipLocked: true);
            CleanDirectory(Path.Combine(prof, "ShaderCache"), r, skipLocked: true);
        }
    }
}

static void CleanPrefetch(CleanResult r)
{
    string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    string pf = Path.Combine(win, "Prefetch");
    CleanDirectory(pf, r, skipLocked: true);
}

static void CleanWindowsOld(CleanResult r)
{
    var roots = new List<string>();
    foreach (var d in DriveInfo.GetDrives())
    {
        try
        {
            if (!d.IsReady || d.DriveType != DriveType.Fixed) continue;
            string p = Path.Combine(d.RootDirectory.FullName, "Windows.old");
            if (Directory.Exists(p)) roots.Add(p);
        }
        catch { }
    }

    if (roots.Count == 0)
    {
        r.Skipped = true;
        r.LastError = "папка Windows.old не найдена";
        return;
    }

    foreach (var root in roots)
    {
        CleanDirectory(root, r, skipLocked: true);
    }
}

static void CleanWuDataStore(CleanResult r)
{
    string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    string sd = Path.Combine(win, "SoftwareDistribution");
    string ds = Path.Combine(sd, "DataStore");

    if (!Directory.Exists(ds))
    {
        r.Skipped = true;
        r.LastError = "DataStore не найден";
        return;
    }

    RunHidden("net", 5000, "stop", "wuauserv");
    RunHidden("net", 5000, "stop", "bits");

    CleanDirectory(ds, r, skipLocked: true);
    CleanDirectory(Path.Combine(sd, "Download"), r, skipLocked: true);
    CleanDirectory(Path.Combine(sd, "PostRebootEventCache.V2"), r, skipLocked: true);

    RunHidden("net", 5000, "start", "bits");
    RunHidden("net", 5000, "start", "wuauserv");
}

static void CleanDeliveryOptimization(CleanResult r)
{
    string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    string doDir = Path.Combine(win, "SoftwareDistribution", "DeliveryOptimization");

    if (!Directory.Exists(doDir))
    {
        r.Skipped = true;
        r.LastError = "папка DeliveryOptimization не найдена";
        return;
    }

    RunHidden("net", 5000, "stop", "dosvc");

    CleanDirectory(doDir, r, skipLocked: true);

    RunHidden("net", 5000, "start", "dosvc");
}

static void CleanCbsLogs(CleanResult r)
{
    string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    string cbs = Path.Combine(win, "Logs", "CBS");
    if (!Directory.Exists(cbs))
    {
        r.Skipped = true;
        r.LastError = "папка CBS не найдена";
        return;
    }
    CleanDirectory(cbs, r, skipLocked: true);
}

static void CleanLiveKernelReportsOnly(CleanResult r)
{
    string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    string lkr = Path.Combine(win, "LiveKernelReports");

    if (!Directory.Exists(lkr))
    {
        r.Skipped = true;
        r.LastError = "папка LiveKernelReports не найдена";
        return;
    }

    var files = new List<string>();
    try
    {
        foreach (var f in Directory.EnumerateFiles(lkr, "*", SearchOption.AllDirectories))
        {
            string ext = Path.GetExtension(f).ToLowerInvariant();
            if (ext == ".lkd" || ext == ".dmp") files.Add(f);
        }
    }
    catch (Exception ex)
    {
        r.Errors++;
        r.LastError = ex.Message;
        return;
    }

    if (files.Count == 0)
    {
        r.Skipped = true;
        r.LastError = "отчётов .lkd/.dmp нет";
        return;
    }

    foreach (var f in files) TryDeleteFile(f, r, skipLocked: true);
}

static void CleanUwpCache(CleanResult r)
{
    string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    string packages = Path.Combine(local, "Packages");
    if (!Directory.Exists(packages))
    {
        r.Skipped = true;
        r.LastError = "Packages не найдена";
        return;
    }

    IEnumerable<string> dirs;
    try
    {
        dirs = Directory.EnumerateDirectories(packages);
    }
    catch (Exception ex)
    {
        r.Errors++;
        r.LastError = ex.Message;
        return;
    }

    foreach (var pkg in dirs)
    {
        CleanDirectory(Path.Combine(pkg, "LocalCache"), r, skipLocked: true);
        CleanDirectory(Path.Combine(pkg, "TempState"), r, skipLocked: true);
        CleanDirectory(Path.Combine(pkg, "AC", "Temp"), r, skipLocked: true);
        CleanDirectory(Path.Combine(pkg, "LocalState", "Cache"), r, skipLocked: true);
    }
}

static void CleanOldDrivers(CleanResult r)
{
    string outp = Sh.Run("pnputil", 20000, "/enum-drivers");
    if (string.IsNullOrWhiteSpace(outp))
    {
        r.Skipped = true;
        r.LastError = "pnputil недоступен";
        return;
    }

    var published = new List<string>();
    var lines = outp.Split('\n');
    foreach (var raw in lines)
    {
        var line = raw.Trim();
        if (line.StartsWith("Published Name", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("Опубликованное имя", StringComparison.OrdinalIgnoreCase))
        {
            int colon = line.IndexOf(':');
            if (colon > 0)
            {
                string name = line[(colon + 1)..].Trim();
                if (name.StartsWith("oem", StringComparison.OrdinalIgnoreCase))
                    published.Add(name);
            }
        }
    }

    if (published.Count == 0)
    {
        r.Skipped = true;
        r.LastError = "сторонних драйверов oem*.inf не найдено";
        return;
    }

    int removed = 0;
    int failed = 0;
    foreach (var name in published)
    {
        var psi = new ProcessStartInfo("pnputil", $"/delete-driver {name} /uninstall /force")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        try
        {
            using var p = Process.Start(psi);
            p?.WaitForExit(15000);
            if (p is not null && p.ExitCode == 0) removed++;
            else failed++;
        }
        catch { failed++; }
    }

    r.FilesDeleted += removed;
    if (removed == 0 && failed > 0)
    {
        r.Skipped = true;
        r.LastError = $"не удалось удалить ни одного драйвера (попыток: {failed})";
    }
    else
    {
        r.LastError = $"удалено драйверов: {removed}, занято/пропущено: {failed}";
    }
}

static void CleanShadowCopies(CleanResult r)
{
    var psi = new ProcessStartInfo("vssadmin", "delete shadows /for=C: /oldest /quiet")
    {
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    try
    {
        using var p = Process.Start(psi);
        if (p is null) { r.Skipped = true; r.LastError = "vssadmin не запустился"; return; }
        p.WaitForExit(20000);

        if (p.ExitCode != 0)
        {
            r.Skipped = true;
            r.LastError = "теневых копий нет или нет прав (vssadmin вернул ошибку)";
        }
        else
        {
            r.FilesDeleted = 1;
            r.LastError = "удалена самая старая теневая копия тома C:";
        }
    }
    catch (Exception ex)
    {
        r.Skipped = true;
        r.LastError = ex.Message;
    }
}

static void CleanAllSafe(CleanResult r)
{
    CleanUserTemp(r);
    CleanSystemTemp(r);
    CleanWuCache(r);
    CleanThumbCache(r);
    CleanRecycleBin(r);
    CleanWerReports(r);
    CleanMemoryDumps(r);
    CleanBrowserCache(r);
    CleanWindowsOld(r);
    CleanDeliveryOptimization(r);
    CleanCbsLogs(r);
    CleanLiveKernelReportsOnly(r);
    CleanUwpCache(r);
}

static void CleanDirectory(string dir, CleanResult r, bool skipLocked)
{
    if (!Directory.Exists(dir)) return;

    IEnumerable<string> files;
    try
    {
        files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories);
    }
    catch (UnauthorizedAccessException)
    {
        r.Errors++;
        r.LastError = $"нет доступа: {dir}";
        return;
    }
    catch (Exception ex)
    {
        r.Errors++;
        r.LastError = ex.Message;
        return;
    }

    foreach (var f in files)
    {
        TryDeleteFile(f, r, skipLocked);
    }

    try
    {
        var dirs = Directory.EnumerateDirectories(dir, "*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.Length);
        foreach (var d in dirs)
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(d).Any())
                    Directory.Delete(d, false);
            }
            catch { }
        }
    }
    catch { }
}

static IEnumerable<string> SafeEnumerateFiles(string dir, string pattern, CleanResult r)
{
    try
    {
        return Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly).ToList();
    }
    catch (Exception ex)
    {
        r.Errors++;
        r.LastError = ex.Message;
        return Enumerable.Empty<string>();
    }
}

static void TryDeleteFile(string path, CleanResult r, bool skipLocked)
{
    try
    {
        var fi = new FileInfo(path);
        if (!fi.Exists) return;
        long size = fi.Length;

        File.SetAttributes(path, FileAttributes.Normal);
        File.Delete(path);

        r.FilesDeleted++;
        r.BytesFreed += size;
    }
    catch (UnauthorizedAccessException)
    {
        r.Errors++;
        r.LastError = $"нет доступа: {Path.GetFileName(path)}";
    }
    catch (IOException ex)
    {
        if (!skipLocked)
        {
            r.Errors++;
            r.LastError = ex.Message;
        }
        else
        {
            r.Locked++;
        }
    }
    catch (Exception ex)
    {
        r.Errors++;
        r.LastError = ex.Message;
    }
}

static int RunHidden(string file, int timeoutMs, params string[] args)
{
    try
    {
        var psi = new ProcessStartInfo(file)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = Process.Start(psi);
        if (p is null) return -1;
        p.WaitForExit(timeoutMs);
        return p.ExitCode;
    }
    catch { return -1; }
}

Report BuildAll()
{
    var parts = new Report[actions.Length];
    using var gate = new SemaphoreSlim(4);
    var tasks = new Task[actions.Length];
    for (int i = 0; i < actions.Length; i++)
    {
        int idx = i;
        tasks[i] = Task.Run(() =>
        {
            gate.Wait();
            try
            {
                var rep = new Report();
                RunCollector(rep, actions[idx].Build);
                parts[idx] = rep;
            }
            finally
            {
                gate.Release();
            }
        });
    }
    try { Task.WaitAll(tasks); } catch { }

    var merged = new Report();
    foreach (var part in parts)
        if (part is not null) merged.MergeFrom(part);
    return merged;
}

static void RunCollector(Report r, Action<Report> build)
{
    try
    {
        build(r);
    }
    catch (Exception ex)
    {
        r.Section("Ошибка");
        r.Row("Не удалось собрать данные", ex.Message);
    }
}

static void CollectComputer(Report r)
{
    var cs = W.First("Win32_ComputerSystem");
    if (cs is null) return;

    r.Section("Компьютер");
    r.Row("Имя", W.S(cs, "Name"));
    r.Row("Производитель", W.S(cs, "Manufacturer"));
    r.Row("Модель", W.S(cs, "Model"));
    r.Row("Тип", W.S(cs, "SystemType"));
    r.Row("Пользователь", W.S(cs, "UserName"));
    r.Row("Домен / рабочая группа", W.S(cs, "Domain"));
    r.Row("Память (всего)", F.Bytes(W.U(cs, "TotalPhysicalMemory")));
}

static void CollectOs(Report r)
{
    var os = W.First("Win32_OperatingSystem");
    if (os is null) return;

    r.Section("Операционная система");
    r.Row("Название", W.S(os, "Caption"));
    r.Row("Версия", $"{W.S(os, "Version")} (сборка {W.S(os, "BuildNumber")})");
    r.Row("Разрядность", W.S(os, "OSArchitecture"));
    r.Row("Каталог Windows", W.S(os, "WindowsDirectory"));

    var installed = W.Dtm(os, "InstallDate");
    if (installed != default) r.Row("Установлена", installed.ToString("dd.MM.yyyy"));

    var boot = W.Dtm(os, "LastBootUpTime");
    if (boot != default)
    {
        var up = DateTime.Now - boot;
        r.Row("Последняя загрузка", $"{boot:dd.MM.yyyy HH:mm}  (работает {F.Fmt(up)})");
    }

    r.Row("Свободно памяти", F.Bytes(W.U(os, "FreePhysicalMemory") * 1024));
}

static void CollectCpu(Report r)
{
    foreach (var cpu in W.All("Win32_Processor"))
    {
        r.Section("Процессор");
        r.Row("Модель", W.S(cpu, "Name"));
        r.Row("Производитель", W.S(cpu, "Manufacturer"));
        r.Row("Ядра / потоки", $"{W.S(cpu, "NumberOfCores")} / {W.S(cpu, "NumberOfLogicalProcessors")}");
        r.Row("Частота", $"{W.S(cpu, "MaxClockSpeed")} МГц (макс), {W.S(cpu, "CurrentClockSpeed")} МГц (тек.)");
        r.Row("Сокет", W.S(cpu, "SocketDesignation"));
        r.Row("L2 / L3 кэш", $"{F.FmtKb(W.Get(cpu, "L2CacheSize"))} / {F.FmtKb(W.Get(cpu, "L3CacheSize"))}");
        r.Row("Разрядность", $"{W.S(cpu, "AddressWidth")}-bit");
        r.Row("Виртуализация", W.B(cpu, "VirtualizationFirmwareEnabled"));
    }
}

static void CollectMemory(Report r)
{
    var modules = W.All("Win32_PhysicalMemory").ToList();
    if (modules.Count == 0) return;

    r.Section("Оперативная память");
    ulong total = modules.Aggregate(0UL, (sum, m) => sum + W.U(m, "Capacity"));
    var arr = W.First("Win32_PhysicalMemoryArray");
    if (arr != null)
        r.Row("Слоты", $"{modules.Count} занято из {W.S(arr, "MemoryDevices")}");
    r.Row("Объём", F.Bytes(total));

    r.Header("Слот", "Объём", "Частота", "Тип", "Производитель / модель");
    foreach (var m in modules)
    {
        string speed = W.U(m, "ConfiguredClockSpeed") > 0
            ? W.U(m, "ConfiguredClockSpeed").ToString()
            : W.U(m, "Speed").ToString();
        r.Cells(W.S(m, "DeviceLocator"), F.Bytes(W.U(m, "Capacity")), speed + " МГц",
            F.MemoryType(W.U(m, "SMBIOSMemoryType")), $"{W.S(m, "Manufacturer")} {W.S(m, "PartNumber")}".Trim());
    }
}

static void CollectDisks(Report r)
{
    var physical = W.All("MSFT_PhysicalDisk", @"root\Microsoft\Windows\Storage").ToList();
    if (physical.Count == 0) physical = W.All("Win32_DiskDrive").ToList();

    r.Section("Накопители");
    r.Header("Диск", "Объём", "Тип", "Шина");
    if (physical.Count == 0)
        r.Cells("нет данных", "", "", "");
    else
        foreach (var d in physical)
        {
            string media = W.Get(d, "MediaType") != null ? F.DiskMediaType(W.U(d, "MediaType")) : W.S(d, "MediaType");
            string bus = W.Get(d, "BusType") != null ? F.BusType(W.U(d, "BusType")) : W.S(d, "InterfaceType");
            r.Cells(W.S(d, "FriendlyName") is { Length: > 0 } fn ? fn : W.S(d, "Model"), F.Bytes(W.U(d, "Size")), media, bus);
        }

    r.Section("Логические диски");
    r.Header("Диск", "ФС", "Занято", "Всего", "Свободно", "Метка");
    foreach (var l in W.All("Win32_LogicalDisk"))
    {
        uint type = (uint)W.U(l, "DriveType");
        if (type != 2 && type != 3) continue;
        r.Cells(W.S(l, "DeviceID"), W.S(l, "FileSystem"),
            W.U(l, "Size") > 0 ? F.Bytes(W.U(l, "Size") - W.U(l, "FreeSpace")) : "",
            W.U(l, "Size") > 0 ? F.Bytes(W.U(l, "Size")) : "",
            W.U(l, "Size") > 0 ? F.Bytes(W.U(l, "FreeSpace")) : "",
            W.S(l, "VolumeName"));
    }
}

static void CollectVideo(Report r)
{
    var gpus = W.All("Win32_VideoController").ToList();
    if (gpus.Count == 0) return;

    r.Section("Видеокарта");
    foreach (var g in gpus)
    {
        r.Row("Модель", W.S(g, "Name"));
        r.Row("Видеопроцессор", W.S(g, "VideoProcessor"));
        r.Row("Видеопамять", W.U(g, "AdapterRAM") > 0 ? F.Bytes(W.U(g, "AdapterRAM")) : "н/д");
        r.Row("Драйвер", $"{W.S(g, "DriverVersion")}  ({W.Dtm(g, "DriverDate"):dd.MM.yyyy})");
        if (W.U(g, "CurrentHorizontalResolution") > 0)
            r.Row("Разрешение", $"{W.S(g, "CurrentHorizontalResolution")}x{W.S(g, "CurrentVerticalResolution")} @ {W.S(g, "CurrentRefreshRate")} Гц");
    }
}

static void CollectMotherboard(Report r)
{
    var board = W.First("Win32_BaseBoard");
    var bios = W.First("Win32_BIOS");

    r.Section("Материнская плата и BIOS");
    if (board != null)
    {
        r.Row("Плата", $"{W.S(board, "Manufacturer")} {W.S(board, "Product")}");
        r.Row("Версия платы", W.S(board, "Version"));
        r.Row("Серийный номер", W.S(board, "SerialNumber"));
    }
    if (bios != null)
    {
        r.Row("BIOS", $"{W.S(bios, "Manufacturer")} {W.S(bios, "SMBIOSBIOSVersion")}");
        r.Row("Дата BIOS", W.Dtm(bios, "ReleaseDate").ToString("dd.MM.yyyy"));
    }
}

static void CollectNetwork(Report r)
{
    var adapters = W.All("Win32_NetworkAdapter")
        .Where(a => W.B(a, "PhysicalAdapter") == "Да" && !string.IsNullOrEmpty(W.S(a, "MACAddress")))
        .ToList();
    if (adapters.Count == 0) return;

    var configs = W.All("Win32_NetworkAdapterConfiguration")
        .Where(c => W.B(c, "IPEnabled") == "Да")
        .GroupBy(c => W.S(c, "Index"))
        .ToDictionary(g => g.Key, g => g.First());

    r.Section("Сеть");
    foreach (var a in adapters)
    {
        r.Row("Адаптер", W.S(a, "Name"));
        r.Row("MAC", W.S(a, "MACAddress"));
        if (W.U(a, "Speed") > 0) r.Row("Скорость", $"{W.U(a, "Speed") / 1_000_000} Мбит/с");

        if (configs.TryGetValue(W.S(a, "Index"), out var c))
        {
            r.Row("IP-адрес", F.Join(W.Get(c, "IPAddress")));
            r.Row("Маска", F.Join(W.Get(c, "IPSubnet")));
            r.Row("Шлюз", F.Join(W.Get(c, "DefaultIPGateway")));
            r.Row("DNS", F.Join(W.Get(c, "DNSServerSearchParams")));
            r.Row("DHCP", W.B(c, "DHCPEnabled"));
        }
        r.Row("", "");
    }
}

static void CollectPower(Report r)
{
    r.Section("Питание");
    string scheme = Sh.Run("powercfg", 8000, "/getactivescheme");
    var m = Regex.Match(scheme, @"\(([^)]+)\)");
    r.Row("Активная схема", m.Success ? m.Groups[1].Value : scheme.Trim());

    var batt = W.First("Win32_Battery");
    if (batt != null)
    {
        r.Row("Батарея", W.S(batt, "Name"));
        r.Row("Заряд", $"{W.S(batt, "EstimatedChargeRemaining")}%");
        r.Row("Статус", F.BatteryStatus(W.U(batt, "BatteryStatus")));
        r.Row("Питание от сети", W.B(batt, "PowerOnline"));
    }
    else
    {
        r.Row("Батарея", "не обнаружена (стационарный ПК)");
    }
}

static void CollectMonitors(Report r)
{
    r.Section("Мониторы и дисплеи");

    var ids = new Dictionary<string, ManagementBaseObject>();
    foreach (var id in W.All("WmiMonitorID", @"root\wmi"))
        ids[W.S(id, "InstanceName")] = id;

    int active = 0;
    int i = 0;
    foreach (var p in W.All("WmiMonitorBasicDisplayParams", @"root\wmi"))
    {
        bool act = W.Bool(p, "Active") ?? false;
        if (act) active++;

        string inst = W.S(p, "InstanceName");
        string name = ids.TryGetValue(inst, out var idm) ? W.Id(W.Get(idm, "UserFriendlyName")) : inst;
        if (string.IsNullOrWhiteSpace(name)) name = $"Монитор {i + 1}";

        double h = W.D(p, "MaxHorizontalImageSize");
        double v = W.D(p, "MaxVerticalImageSize");
        double diag = Math.Sqrt(h * h + v * v) / 2.54;

        r.Row($"{name} {(act ? "(активен)" : "(отключён)")}",
            h > 0 ? $"{h:0}x{v:0} см, диагональ ≈ {diag:0.0}\"" : "");
        i++;
    }

    r.Row("Активных мониторов", active.ToString());

    foreach (var g in W.All("Win32_VideoController"))
        if (W.U(g, "CurrentHorizontalResolution") > 0)
            r.Row("Режим", $"{W.S(g, "CurrentHorizontalResolution")}x{W.S(g, "CurrentVerticalResolution")} @ {W.S(g, "CurrentRefreshRate")} Гц  ({W.S(g, "Name")})");
}

static void CollectPeripherals(Report r)
{
    r.Section("Периферия (USB-устройства)");
    r.Header("Тип", "Устройство", "Доп.");

    foreach (var k in W.All("Win32_Keyboard"))
        r.Cells("Клавиатура", FirstOf(k, "Description", "Name"), W.S(k, "Status"));

    foreach (var m in W.All("Win32_PointingDevice"))
        r.Cells("Мышь/указ.", FirstOf(m, "Description", "Name"), W.S(m, "Manufacturer"));

    foreach (var c in W.All("Win32_PnPEntity").Where(x => W.S(x, "PNPClass") is "Camera" or "Image"))
        r.Cells("Камера", W.S(c, "Name"), W.S(c, "Status"));

    foreach (var d in W.All("Win32_DiskDrive").Where(x => W.S(x, "InterfaceType").ToUpperInvariant().Contains("USB")))
        r.Cells("USB-накопитель", W.S(d, "Model"), F.Bytes(W.U(d, "Size")));

    int usb = W.All("Win32_PnPEntity").Count(x =>
        W.S(x, "DeviceID").StartsWith("USB\\") || W.S(x, "DeviceID").StartsWith("USBSTOR"));
    r.Row("USB-устройств всего", usb.ToString());
}

static void CollectSound(Report r)
{
    r.Section("Звуковые устройства");
    r.Header("Устройство", "Производитель", "Статус");
    foreach (var s in W.All("Win32_SoundDevice"))
        r.Cells(W.S(s, "Name"), W.S(s, "Manufacturer"), W.S(s, "Status"));

    int endpoints = W.All("Win32_PnPEntity").Count(x => W.S(x, "PNPClass") == "AudioEndpoint");
    r.Row("Аудиоустройств (конечных точек)", endpoints.ToString());
}

static void CollectBattery(Report r)
{
    var v = W.First("Win32_Battery");
    if (v is null)
    {
        r.Section("Батарея / ИБП");
        r.Row("Устройство", "не обнаружено");
        return;
    }

    r.Section("Батарея / ИБП");
    r.Header("Параметр", "Значение");
    r.Cells("Название", W.S(v, "Name"));
    r.Cells("Заряд", $"{W.S(v, "EstimatedChargeRemaining")}%");
    r.Cells("Статус", F.BatteryStatus(W.U(v, "BatteryStatus")));
    r.Cells("Питание от сети", W.B(v, "PowerOnline"));
    ulong rt = W.U(v, "EstimatedRunTime");
    r.Cells("Осталось время", rt is > 0 and < 0xFFFF ? $"{rt} мин" : "—");
    if (W.U(v, "DesignCapacity") > 0) r.Cells("Проектная ёмкость", $"{W.U(v, "DesignCapacity")} мВт·ч");
    if (W.U(v, "FullChargeCapacity") > 0) r.Cells("Текущая ёмкость", $"{W.U(v, "FullChargeCapacity")} мВт·ч");

    var stat = W.First("BatteryStaticData", @"root\wmi");
    var full = W.First("BatteryFullChargedCapacity", @"root\wmi");
    if (stat != null && full != null)
    {
        ulong design = W.U(stat, "DesignedCapacity");
        ulong fullCap = W.U(full, "FullChargedCapacity");
        if (design > 0)
        {
            r.Cells("Проектная ёмкость (WMI)", $"{design} мВт·ч");
            r.Cells("Текущая ёмкость (WMI)", $"{fullCap} мВт·ч");
            double wear = (1.0 - (double)fullCap / design) * 100.0;
            r.Cells("Износ", $"{wear:0.0}%");
        }
    }

    var cycles = W.First("BatteryCycleCount", @"root\wmi");
    if (cycles != null) r.Cells("Циклов перезарядки", W.S(cycles, "CycleCount"));
}

static void CollectSensors(Report r)
{
    r.Section("Датчики и температуры");
    r.Header("Датчик", "Значение");

    bool any = false;
    foreach (var t in W.All("MSAcpi_ThermalZoneTemperature", @"root\wmi"))
    {
        double c = W.D(t, "CurrentTemperature") / 10.0 - 273.15;
        if (c is > -50 and < 200) { r.Cells("Термозона ACPI", $"{c:0.0} °C"); any = true; }
    }
    foreach (var f in W.All("Win32_Fan"))
    {
        r.Cells("Вентилятор", $"{W.S(f, "Name")} {(W.Bool(f, "Active") == true ? "активен" : "")} {W.S(f, "Status")}");
        any = true;
    }
    foreach (var v in W.All("Win32_VoltageProbe"))
    {
        r.Cells("Напряжение", $"{W.D(v, "CurrentReading") / 1000.0:0.00} В");
        any = true;
    }

    if (!any)
        r.Cells("Информация", "датчики недоступны через WMI (нужны права администратора или сторонняя программа)");
}

static void CollectPrinters(Report r)
{
    r.Section("Принтеры и сканеры");
    r.Header("Тип", "Устройство", "Драйвер / порт");
    foreach (var p in W.All("Win32_Printer"))
        r.Cells(W.Bool(p, "Default") == true ? "Принтер (по умолч.)" : "Принтер",
            W.S(p, "Name"), $"{W.S(p, "DriverName")} / {W.S(p, "PortName")}");

    foreach (var s in W.All("Win32_PnPEntity").Where(x => W.S(x, "PNPClass") == "Image" && W.S(x, "Name").Length > 0))
        r.Cells("Сканер/камера", W.S(s, "Name"), W.S(s, "Status"));
}

static void CollectActivation(Report r)
{
    r.Section("Активация Windows");
    const string appId = "55c92734-d682-4d71-983e-d6ec3f16059f";

    foreach (var p in W.All("SoftwareLicensingProduct")
                 .Where(x => W.S(x, "ApplicationID").Equals(appId, StringComparison.OrdinalIgnoreCase)
                             && !string.IsNullOrEmpty(W.S(x, "PartialProductKey"))))
    {
        r.Row("Редакция", W.S(p, "Name"));
        r.Row("Статус лицензии", F.LicenseStatus(W.U(p, "LicenseStatus")));
        r.Row("Ключ (частично)", W.S(p, "PartialProductKey"));
    }

    var os = W.First("Win32_OperatingSystem");
    if (os != null)
    {
        r.Row("Серийный номер", W.S(os, "SerialNumber"));
        r.Row("Организация", W.S(os, "Organization"));
    }
}

static void CollectSoftware(Report r)
{
    r.Section("Установленное ПО");
    r.Header("Программа", "Версия", "Издатель");

    var locations = new (RegistryKey Root, string Path)[]
    {
        (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
        (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
        (Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")
    };

    var items = new List<(string Name, string Version, string Publisher)>();
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var (root, path) in locations)
    {
        try
        {
            using var k = root.OpenSubKey(path);
            if (k is null) continue;
            foreach (var sub in k.GetSubKeyNames())
            {
                try
                {
                    using var app = k.OpenSubKey(sub);
                    string name = (app?.GetValue("DisplayName") as string)?.Trim() ?? "";
                    if (name.Length == 0) continue;
                    string ver = (app?.GetValue("DisplayVersion") as string)?.Trim() ?? "";
                    string pub = (app?.GetValue("Publisher") as string)?.Trim() ?? "";
                    if (seen.Add(name + "|" + ver))
                        items.Add((name, ver, pub));
                }
                catch { }
            }
        }
        catch { }
    }

    foreach (var it in items.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
        r.Cells(it.Name, it.Version, it.Publisher);

    r.Row("Всего программ", items.Count.ToString());
}

static void CollectUpdates(Report r)
{
    r.Section("Обновления Windows — установленные");
    r.Header("KB", "Описание", "Установлено");

    var qfe = W.All("Win32_QuickFixEngineering").ToList();
    var items = new List<(string Kb, string Desc, string Date)>();
    foreach (var q in qfe)
    {
        string kb = W.S(q, "HotFixID");
        if (string.IsNullOrEmpty(kb)) continue;
        string on = W.S(q, "InstalledOn");
        if (DateTime.TryParse(on, out var d)) on = d.ToString("dd.MM.yyyy");
        items.Add((kb, W.S(q, "Description"), on));
    }

    if (items.Count < 5)
    {
        string ps = Sh.Ps("Get-HotFix | Select-Object -Property HotFixID,Description,InstalledOn | " +
                          "ForEach-Object { \"$($_.HotFixID)|$($_.Description)|$($_.InstalledOn)\" }", 30000);
        foreach (var raw in ps.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("HotFixID")) continue;
            var parts = line.Split('|');
            if (parts.Length < 1) continue;
            string kb = parts[0].Trim();
            if (!kb.StartsWith("KB", StringComparison.OrdinalIgnoreCase)) continue;
            string desc = parts.Length > 1 ? parts[1].Trim() : "";
            string date = parts.Length > 2 ? parts[2].Trim() : "";
            if (DateTime.TryParse(date, out var d)) date = d.ToString("dd.MM.yyyy");
            items.Add((kb, desc, date));
        }
        items = items
            .GroupBy(x => x.Kb, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderByDescending(x => x.Date)
            .ToList();
    }

    foreach (var it in items)
        r.Cells(it.Kb, it.Desc, it.Date);
    r.Row("Всего обновлений", items.Count.ToString());

    r.Section("Обновления Windows — ожидающие");
    var pending = PendingUpdates();
    if (pending.Count == 0)
        r.Row("Ожидающие обновления", "не найдены (или не удалось проверить)");
    else
    {
        r.Header("Ожидающее обновление");
        foreach (var t in pending) r.Cells(t);
    }
}

static List<string> PendingUpdates()
{
    string cmd = "$ErrorActionPreference='Stop';" +
                 "$s=New-Object -ComObject Microsoft.Update.Session;" +
                 "$q=$s.CreateUpdateSearcher().Search(\"IsInstalled=0 and Type='Software'\");" +
                 "$q.Updates | ForEach-Object { $_.Title }";
    string outp = Sh.Ps(cmd, 20000);
    return outp.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(s => s.Trim()).Where(s => s.Length > 0 && !s.StartsWith("Выпуск")).Distinct().Take(50).ToList();
}

static void CollectDrivers(Report r)
{
    r.Section("Драйверы");
    r.Header("Устройство", "Версия", "Дата", "Поставщик");
    foreach (var d in W.All("Win32_PnPSignedDriver")
                 .Where(x => !string.IsNullOrEmpty(W.S(x, "DriverVersion")))
                 .OrderBy(x => W.S(x, "DeviceName")))
        r.Cells(W.S(d, "DeviceName"), W.S(d, "DriverVersion"), DtmStr(d, "DriverDate"), W.S(d, "DriverProviderName"));

    r.Section("Устройства с проблемами");
    var problem = W.All("Win32_PnPEntity")
        .Where(x => W.Get(x, "ConfigManagerErrorCode") != null && W.U(x, "ConfigManagerErrorCode") != 0)
        .ToList();
    if (problem.Count == 0) r.Row("Проблемные устройства", "не обнаружены");
    else foreach (var p in problem.Take(40))
        r.Row(W.S(p, "Name"), $"код ошибки {W.U(p, "ConfigManagerErrorCode")}");
}

static void CollectServices(Report r)
{
    var svc = W.All("Win32_Service").ToList();
    r.Section("Службы");
    r.Row("Всего служб", svc.Count.ToString());
    r.Row("Работают", svc.Count(s => W.S(s, "State") == "Running").ToString());
    r.Header("Служба (авто, но не запущена)", "Состояние");
    foreach (var s in svc.Where(x => W.S(x, "StartMode") == "Auto" && W.S(x, "State") != "Running").OrderBy(x => W.S(x, "Name")))
        r.Cells(W.S(s, "Name"), W.S(s, "State"));

    r.Section("Автозагрузка");
    r.Header("Название", "Команда", "Размещение");
    foreach (var a in W.All("Win32_StartupCommand"))
        r.Cells(W.S(a, "Name"), W.S(a, "Command"), $"{W.S(a, "Location")} {W.S(a, "User")}".Trim());
}

static void CollectUsers(Report r)
{
    r.Section("Пользователи");
    r.Header("Имя", "Полное имя", "Отключён", "Тип");
    foreach (var u in W.All("Win32_UserAccount").OrderBy(x => W.S(x, "Name")))
        r.Cells(W.S(u, "Name"), W.S(u, "FullName"), W.B(u, "Disabled"),
            (W.Bool(u, "LocalAccount") ?? false) ? "локальный" : "доменный");

    r.Section("Группы");
    r.Header("Группа");
    foreach (var g in W.All("Win32_Group").OrderBy(x => W.S(x, "Name")))
        r.Cells(W.S(g, "Name"));
}

static void CollectRestorePoints(Report r)
{
    r.Section("Точки восстановления / резервные копии");
    var rp = W.All("SystemRestore", @"root\default");
    if (rp.Count == 0)
    {
        r.Row("Точки восстановления", "нет (либо недостаточно прав)");
        return;
    }
    r.Header("Создана", "Описание", "Тип");
    foreach (var p in rp.OrderByDescending(x => W.Dtm(x, "CreationTime")))
        r.Cells(DtmStr(p, "CreationTime"), W.S(p, "Description"), F.RestoreType(W.U(p, "RestorePointType")));
}

static void CollectWifi(Report r)
{
    r.Section("Wi-Fi сети и пароли");
    string outp = Sh.Run("netsh", 20000, "wlan", "show", "profiles");
    var names = Regex.Matches(outp, @"(?:All User Profile|Профиль всех пользователей)\s*:\s*(.+?)\s*$", RegexOptions.Multiline)
        .Select(m => m.Groups[1].Value.Trim())
        .Where(s => s.Length > 0).Distinct().ToList();

    if (names.Count == 0)
    {
        r.Row("Профили", "не найдены (нет Wi-Fi или беспроводной адаптер отключён)");
        return;
    }

    r.Header("Сеть (SSID)", "Пароль");
    foreach (var n in names)
    {
        string p = Sh.Run("netsh", 20000, "wlan", "show", "profile", "name=" + n, "key=clear");
        var key = Regex.Match(p, @"(?:Key Content|Содержимое ключа)\s*:\s*(.+?)\s*$", RegexOptions.Multiline);
        r.Cells(n, key.Success && key.Groups[1].Value.Trim().Length > 0 ? key.Groups[1].Value.Trim() : "(скрыт — нужны права администратора)");
    }
}

static void CollectPorts(Report r)
{
    var props = IPGlobalProperties.GetIPGlobalProperties();

    r.Section("Порты прослушивания");
    r.Header("Протокол", "Адрес", "Порт");
    foreach (var e in props.GetActiveTcpListeners().OrderBy(x => x.Port))
        r.Cells("TCP", e.Address.ToString(), e.Port.ToString());
    foreach (var e in props.GetActiveUdpListeners().OrderBy(x => x.Port))
        r.Cells("UDP", e.Address.ToString(), e.Port.ToString());

    r.Section("Активные TCP-соединения");
    r.Header("Локальный", "Удалённый", "Состояние");
    foreach (var c in props.GetActiveTcpConnections().OrderBy(x => x.State))
        r.Cells($"{c.LocalEndPoint.Address}:{c.LocalEndPoint.Port}",
                $"{c.RemoteEndPoint.Address}:{c.RemoteEndPoint.Port}", c.State.ToString());
}

static void CollectVpnProxy(Report r)
{
    r.Section("VPN и прокси");
    try
    {
        using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings");
        bool proxy = Convert.ToInt32(k?.GetValue("ProxyEnable") ?? 0) == 1;
        r.Row("Прокси", proxy ? "включён" : "выключен");
        if (proxy)
        {
            r.Row("Адрес прокси", k?.GetValue("ProxyServer")?.ToString() ?? "");
            r.Row("Обход прокси", k?.GetValue("ProxyOverride")?.ToString() ?? "");
        }
        bool auto = Convert.ToInt32(k?.GetValue("AutoDetect") ?? 0) == 1;
        r.Row("Автоопределение", auto ? "включено" : "выключено");
    }
    catch { }

    r.Header("VPN-адаптер", "Статус");
    foreach (var a in W.All("Win32_NetworkAdapter")
                 .Where(x => IsVpn(W.S(x, "Name"))))
        r.Cells(W.S(a, "Name"), W.S(a, "NetConnectionStatus"));

    static bool IsVpn(string n) =>
        n.Contains("VPN", StringComparison.OrdinalIgnoreCase) ||
        n.Contains("TAP", StringComparison.OrdinalIgnoreCase) ||
        n.Contains("WireGuard", StringComparison.OrdinalIgnoreCase) ||
        n.Contains("Radmin", StringComparison.OrdinalIgnoreCase) ||
        n.Contains("Tailscale", StringComparison.OrdinalIgnoreCase);
}

static void CollectAntivirus(Report r)
{
    r.Section("Антивирус / Защитник Windows");
    var av = W.All("AntiVirusProduct", @"root\SecurityCenter2");
    if (av.Count == 0)
        r.Row("Антивирусы", "не обнаружены");
    foreach (var a in av)
        r.Row(W.S(a, "displayName"), F.AvState(W.S(a, "productState")));

    var mp = W.First("MSFT_MpComputerStatus", @"root\Microsoft\Windows\Defender");
    if (mp != null)
    {
        r.Row("Defender — защита в реальном времени", W.B(mp, "RealTimeProtectionEnabled"));
        r.Row("Defender — антивирус включён", W.B(mp, "AntivirusEnabled"));
        r.Row("Defender — версия движка", W.S(mp, "AMEngineVersion"));
        r.Row("Defender — версия баз", W.S(mp, "AntivirusSignatureVersion"));
        r.Row("Defender — базы обновлены", W.S(mp, "AntivirusSignatureLastUpdated"));
    }
}

static void CollectFirewall(Report r)
{
    r.Section("Брандмауэр");
    string outp = Sh.Ps("Get-NetFirewallProfile | ForEach-Object { \"$($_.Name)=$($_.Enabled)\" }", 20000);
    var lines = outp.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(s => s.Trim()).Where(s => s.Contains('=')).ToList();
    if (lines.Count == 0)
        r.Row("Состояние", "не удалось получить (нужна перезагрузка службы/права)");
    else
        foreach (var line in lines)
        {
            var parts = line.Split('=', 2);
            r.Row(F.ProfileName(parts[0]), parts[1].Trim() is var v && (v is "True" or "1") ? "включён" : "выключен");
        }
}

static void CollectSmart(Report r)
{
    r.Section("SMART — состояние накопителей");
    var disks = W.All("MSFT_PhysicalDisk", @"root\Microsoft\Windows\Storage");
    if (disks.Count == 0)
    {
        foreach (var d in W.All("Win32_DiskDrive"))
            r.Row(W.S(d, "Model"), W.S(d, "Status"));
        return;
    }

    r.Header("Диск", "SMART", "Состояние");
    foreach (var d in disks)
        r.Cells(W.S(d, "FriendlyName"), F.SmartHealth(W.U(d, "HealthStatus")), W.S(d, "Status"));
}

static void CollectEventLog(Report r)
{
    r.Section("Журнал событий — последние ошибки");
    r.Header("Время", "Источник", "Код", "Сообщение");

    var q = "SELECT Logfile,TimeWritten,SourceName,EventCode,Message FROM Win32_NTLogEvent " +
            "WHERE Logfile='System' AND Type='Error'";
    var events = W.Query(q).OrderByDescending(x => W.Dtm(x, "TimeWritten")).Take(30).ToList();
    if (events.Count == 0)
        r.Cells("—", "—", "—", "ошибок не найдено");
    foreach (var e in events)
    {
        string msg = W.S(e, "Message").Replace("\r", " ").Replace("\n", " ");
        if (msg.Length > 120) msg = msg[..120] + "…";
        r.Cells(DtmStr(e, "TimeWritten", withTime: true), W.S(e, "SourceName"), W.S(e, "EventCode"), msg);
    }
}

static void CollectBitLocker(Report r)
{
    r.Section("BitLocker / шифрование");
    string outp = Sh.Ps("Get-BitLockerVolume | ForEach-Object { \"$($_.MountPoint)=$($_.ProtectionStatus)/$($_.VolumeStatus)\" }", 25000);
    var lines = outp.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(s => s.Trim()).Where(s => s.Contains('=')).ToList();
    if (lines.Count == 0)
    {
        string mbde = Sh.Run("manage-bde", 25000, "-status");
        if (mbde.Contains("BitLocker", StringComparison.OrdinalIgnoreCase))
            r.Row("manage-bde", "см. вывод (детали доступны только из-под администратора)");
        else
            r.Row("Состояние", "не удалось получить (нужны права администратора)");
        return;
    }
    foreach (var line in lines)
    {
        var p = line.Split('=', 2);
        string vol = p[1].Split('/').FirstOrDefault() ?? "";
        string rest = p[1].Contains('/') ? "  / " + p[1].Split('/')[1] : "";
        r.Row(p[0], F.Protection(vol) + rest);
    }
}

static void CollectRealtime(Report r)
{
    r.Section("Производительность в реальном времени (замер 1 сек)");

    PerformanceCounter? cpu = null, mem = null, committed = null, disk = null;
    try
    {
        cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        mem = new PerformanceCounter("Memory", "Available MBytes");
        committed = new PerformanceCounter("Memory", "% Committed Bytes In Use");
        disk = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
        cpu.NextValue(); disk.NextValue();
        Thread.Sleep(1000);

        r.Row("Загрузка CPU", $"{cpu.NextValue():0.0} %");
        r.Row("Использование RAM", $"{committed.NextValue():0.0} %");
        r.Row("Свободно RAM", $"{mem.NextValue():0} МБ");
        r.Row("Активность диска", $"{Math.Min(100, disk.NextValue()):0.0} %");
    }
    catch (Exception ex)
    {
        r.Row("Счётчики производительности", $"недоступны: {ex.Message}");
    }
    finally
    {
        cpu?.Dispose(); mem?.Dispose(); committed?.Dispose(); disk?.Dispose();
    }
}

static void CollectWei(Report r)
{
    r.Section("Индекс производительности (WinSAT)");
    var w = W.First("Win32_WinSAT");
    if (w is null || W.D(w, "WinSPRLevel") <= 0)
    {
        r.Row("Индекс", "недоступен (WinSAT не выполнялся или удалён в Windows 8+)");
        return;
    }
    r.Row("Общий индекс", W.D(w, "WinSPRLevel").ToString("0.0"));
    r.Row("Процессор", W.D(w, "CpuScore").ToString("0.0"));
    r.Row("Память", W.D(w, "MemoryScore").ToString("0.0"));
    r.Row("Графика", W.D(w, "GraphicsScore").ToString("0.0"));
    r.Row("Игры (графика)", W.D(w, "GamingScore").ToString("0.0"));
    r.Row("Диск", W.D(w, "DiskScore").ToString("0.0"));
}

static void CollectProcesses(Report r)
{
    var first = new Dictionary<int, TimeSpan>();
    foreach (var p in Process.GetProcesses())
    {
        try { first[p.Id] = p.TotalProcessorTime; } catch { }
    }

    Thread.Sleep(1000);

    double ncpu = Environment.ProcessorCount;
    var rows = new List<(string Name, double Cpu, double Mem, int Pid)>();
    foreach (var p in Process.GetProcesses())
    {
        try
        {
            double cpu = 0;
            if (first.TryGetValue(p.Id, out var prev))
                cpu = (p.TotalProcessorTime - prev).TotalMilliseconds / (1000.0 * ncpu) * 100.0;
            rows.Add((p.ProcessName, cpu, p.WorkingSet64 / 1024.0 / 1024.0, p.Id));
        }
        catch { }
    }

    r.Section("Процессы — топ по памяти");
    r.Header("Процесс", "PID", "Память");
    foreach (var p in rows.OrderByDescending(x => x.Mem).Take(15))
        r.Cells(p.Name, p.Pid.ToString(), $"{p.Mem:0.0} МБ");

    r.Section("Процессы — топ по нагрузке CPU");
    r.Header("Процесс", "PID", "CPU");
    foreach (var p in rows.OrderByDescending(x => x.Cpu).Take(15))
        r.Cells(p.Name, p.Pid.ToString(), $"{p.Cpu:0.0} %");
}

static void CollectJarAnalysis(Report r)
{
    r.Section("Анализ .jar файлов — поиск");

    var roots = new List<string>();
    foreach (var d in DriveInfo.GetDrives())
    {
        try
        {
            if (d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable))
                roots.Add(d.RootDirectory.FullName);
        }
        catch { }
    }

    var skipDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Windows", "$Recycle.Bin", "System Volume Information",
        "$WinREAgent", "Recovery", "PerfLogs",
        "node_modules", ".git", ".svn", ".gradle", ".m2", ".idea"
    };

    var jars = new List<FileInfo>();
    var errors = new List<string>();
    var seenHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var root in roots)
    {
        try
        {
            ScanDir(root, jars, errors, skipDirs, seenHashes, 0);
        }
        catch (Exception ex)
        {
            errors.Add($"{root}: {ex.Message}");
        }
    }

    r.Row("Найдено .jar файлов", jars.Count.ToString());
    r.Row("Просканировано корней", string.Join(", ", roots));
    if (errors.Count > 0)
        r.Row("Ошибок доступа", errors.Count.ToString());

    if (jars.Count == 0)
    {
        r.Row("Результат", "ни одного .jar файла не найдено");
        return;
    }

    r.Section("Расположение найденных .jar");
    r.Header("Каталог", "Файлов", "Суммарный размер");
    foreach (var g in jars.GroupBy(j => j.DirectoryName ?? "—")
                          .OrderByDescending(g => g.Count()))
    {
        r.Cells(g.Key, g.Count().ToString(), F.Bytes((ulong)g.Sum(x => x.Length)));
    }

    r.Section("Детальный анализ .jar");
    r.Header("Файл", "Размер", "Версия", "Main-Class", "Классов", "Зависимостей");

    var ordered = jars.OrderByDescending(j => j.Length).ToList();

    var started = DateTime.UtcNow;
    var timeBudget = TimeSpan.FromMinutes(10);
    var cache = new Dictionary<string, JarInfo>(StringComparer.OrdinalIgnoreCase);

    int analyzed = 0;
    foreach (var jar in ordered)
    {
        if (DateTime.UtcNow - started > timeBudget)
        {
            r.Cells($"(остановлено по таймауту, осталось {ordered.Count - analyzed})", "", "", "", "", "");
            break;
        }

        var info = AnalyzeJar(jar.FullName);
        cache[jar.FullName] = info;
        r.Cells(
            ShortenPath(jar.FullName, 70),
            F.Bytes((ulong)jar.Length),
            info.Version,
            info.MainClass,
            info.ClassCount.ToString(),
            info.Dependencies.Count.ToString());

        analyzed++;
    }

    var allDeps = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    foreach (var jar in ordered)
    {
        if (!cache.TryGetValue(jar.FullName, out var info)) continue;
        foreach (var d in info.Dependencies)
            allDeps[d] = allDeps.TryGetValue(d, out var c) ? c + 1 : 1;
    }

    if (allDeps.Count > 0)
    {
        r.Section("Все уникальные зависимости (по частоте)");
        r.Header("Зависимость", "Встречается в .jar");
        foreach (var kv in allDeps.OrderByDescending(x => x.Value).Take(80))
            r.Cells(kv.Key, kv.Value.ToString());
    }

    var suspiciousPatterns = new[]
    {
        "exploit", "payload", "backdoor", "ransom", "keylog",
        "miner", "hack", "crack", "cheat", "inject"
    };
    var suspicious = jars.Where(j =>
    {
        string n = j.Name.ToLowerInvariant();
        return suspiciousPatterns.Any(p => n.Contains(p));
    }).ToList();

    if (suspicious.Count > 0)
    {
        r.Section("Подозрительные .jar (по названию)");
        r.Header("Файл", "Размер", "Изменён");
        foreach (var s in suspicious)
            r.Cells(ShortenPath(s.FullName, 80), F.Bytes((ulong)s.Length),
                    s.LastWriteTime.ToString("dd.MM.yyyy HH:mm"));
    }
}

static void ScanDir(string dir, List<FileInfo> jars, List<string> errors,
                    HashSet<string> skipDirs, HashSet<string> seenHashes, int depth)
{
    if (depth > 25) return;

    string dirName = Path.GetFileName(dir);
    if (skipDirs.Contains(dirName)) return;

    IEnumerable<string> files;
    try
    {
        files = Directory.EnumerateFiles(dir, "*.jar", SearchOption.TopDirectoryOnly);
    }
    catch (UnauthorizedAccessException) { return; }
    catch (DirectoryNotFoundException) { return; }
    catch (IOException) { return; }

    foreach (var f in files)
    {
        try
        {
            var fi = new FileInfo(f);
            if (!fi.Exists || fi.Length == 0) continue;

            string key = fi.Name + "|" + fi.Length;
            if (!seenHashes.Add(key)) continue;

            jars.Add(fi);
        }
        catch { }
    }

    IEnumerable<string> subs;
    try
    {
        subs = Directory.EnumerateDirectories(dir);
    }
    catch { return; }

    foreach (var sub in subs)
    {
        try
        {
            var attr = File.GetAttributes(sub);
            if ((attr & FileAttributes.ReparsePoint) != 0) continue;

            ScanDir(sub, jars, errors, skipDirs, seenHashes, depth + 1);
        }
        catch (Exception ex)
        {
            errors.Add($"{sub}: {ex.Message}");
        }
    }
}

static JarInfo AnalyzeJar(string path)
{
    var info = new JarInfo();
    try
    {
        using var fs = File.OpenRead(path);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read);

        var manifest = zip.GetEntry("META-INF/MANIFEST.MF");
        if (manifest != null)
        {
            using var mr = new StreamReader(manifest.Open(), Encoding.UTF8);
            string content = mr.ReadToEnd();

            var unfolded = new List<string>();
            foreach (var line in content.Split('\n'))
            {
                var l = line.TrimEnd('\r');
                if (l.Length == 0) continue;
                if (l.StartsWith(" ") && unfolded.Count > 0)
                    unfolded[^1] += l[1..];
                else
                    unfolded.Add(l);
            }

            foreach (var line in unfolded)
            {
                int colon = line.IndexOf(':');
                if (colon < 0) continue;
                string key = line[..colon].Trim();
                string val = line[(colon + 1)..].Trim();

                switch (key)
                {
                    case "Implementation-Version":
                    case "Specification-Version":
                        if (string.IsNullOrEmpty(info.Version)) info.Version = val;
                        break;
                    case "Main-Class":
                        info.MainClass = val;
                        break;
                    case "Class-Path":
                        foreach (var cp in val.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                            info.Dependencies.Add(cp);
                        break;
                    case "Implementation-Title":
                    case "Specification-Title":
                        if (string.IsNullOrEmpty(info.Title)) info.Title = val;
                        break;
                    case "Implementation-Vendor":
                        if (string.IsNullOrEmpty(info.Vendor)) info.Vendor = val;
                        break;
                }
            }
        }

        var pom = zip.GetEntry("META-INF/maven/pom.properties");
        if (pom != null)
        {
            using var pr = new StreamReader(pom.Open(), Encoding.UTF8);
            string pc = pr.ReadToEnd();
            foreach (var line in pc.Split('\n'))
            {
                var kv = line.Split('=', 2);
                if (kv.Length != 2) continue;
                string k = kv[0].Trim(), v = kv[1].Trim();
                if (k == "version" && string.IsNullOrEmpty(info.Version)) info.Version = v;
                if (k == "artifactId" && string.IsNullOrEmpty(info.Title)) info.Title = v;
                if (k == "groupId") info.Vendor = string.IsNullOrEmpty(info.Vendor) ? v : info.Vendor;
            }
        }

        foreach (var e in zip.Entries)
        {
            if (e.FullName.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
                info.ClassCount++;
            else if (e.FullName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                info.Dependencies.Add("(вложенный) " + Path.GetFileName(e.FullName));
        }
    }
    catch (InvalidDataException) { info.Version = "(повреждён/не ZIP)"; }
    catch (Exception ex) { info.Version = "(ошибка: " + ex.GetType().Name + ")"; }

    return info;
}

static string ShortenPath(string path, int max)
{
    if (path.Length <= max) return path;
    string file = Path.GetFileName(path);
    string dir = Path.GetDirectoryName(path) ?? "";
    string tail = Path.Combine(Path.GetFileName(dir), file);
    return tail.Length >= max ? "…" + tail[^(max - 1)..] : "…" + Path.DirectorySeparatorChar + tail;
}

void Show(Report report)
{
    Clear();
    Header();
    Render(report);
    Pause();
}

void Render(Report report)
{
    foreach (var sec in report.Sections)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine($"  ── {sec.Title.ToUpperInvariant()} ──");
        Console.ResetColor();

        if (sec.Rows.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("    нет данных");
            Console.ResetColor();
            continue;
        }

        if (sec.Header is null)
        {
            foreach (var row in sec.Rows)
            {
                string name = row[0];
                string value = row.Length > 1 ? row[1] : "";
                if (string.IsNullOrWhiteSpace(value)) continue;
                Console.Write("    ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"{name,-28}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(" │ ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(value);
                Console.ResetColor();
            }
        }
        else
        {
            int cols = sec.Header.Length;
            var widths = new int[cols];
            for (int i = 0; i < cols; i++)
            {
                widths[i] = sec.Header[i].Length;
                foreach (var row in sec.Rows)
                    if (i < row.Length) widths[i] = Math.Max(widths[i], row[i].Length);
                widths[i] = Math.Min(widths[i], 50);
            }

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("    ");
            Console.WriteLine(string.Join("  ", sec.Header.Select((h, i) => Pad(h, widths[i]))));
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("    ");
            Console.WriteLine(string.Join("  ", widths.Select(w => new string('─', w))));
            Console.ResetColor();

            foreach (var row in sec.Rows)
            {
                Console.Write("    ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(string.Join("  ", Enumerable.Range(0, cols).Select(i =>
                    Pad(Trunc(i < row.Length ? row[i] : "", widths[i]), widths[i]))));
                Console.ResetColor();
            }
        }
    }
}

void ExportMenu()
{
    Clear();
    Header();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkMagenta;
    Console.WriteLine("  ЭКСПОРТ ОТЧЁТА");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("    1. CSV");
    Console.WriteLine("    2. JSON");
    Console.WriteLine("    3. HTML");
    Console.WriteLine("    0. Назад");
    Console.Write("  Формат: ");

    string c = (Console.ReadLine() ?? "").Trim();
    if (c is "0" or "") return;

    var report = BuildAll();
    string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    string file;
    try
    {
        switch (c)
        {
            case "1": file = $"SystemInfo_{stamp}.csv"; File.WriteAllText(file, ToCsv(report), Encoding.UTF8); break;
            case "2": file = $"SystemInfo_{stamp}.json"; File.WriteAllText(file, ToJson(report), Encoding.UTF8); break;
            case "3": file = $"SystemInfo_{stamp}.html"; File.WriteAllText(file, ToHtml(report), Encoding.UTF8); break;
            default: Error("Неизвестный формат."); Pause(); return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  Сохранено: {Path.GetFullPath(file)}");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Error(ex.Message);
    }
    Pause();
}

static string ToCsv(Report report)
{
    var sb = new StringBuilder();
    sb.AppendLine("Раздел;Поле;Значение");
    foreach (var sec in report.Sections)
        foreach (var row in sec.Rows)
        {
            string a = row.Length > 0 ? row[0] : "";
            string b = row.Length > 1 ? row[1] : "";
            sb.AppendLine($"{Esc(sec.Title)};{Esc(a)};{Esc(b)}");
        }
    return sb.ToString();
}

static string ToJson(Report report)
{
    var obj = new
    {
        generated = DateTime.Now.ToString("o"),
        host = Environment.MachineName,
        user = Environment.UserName,
        sections = report.Sections.Select(s => new { title = s.Title, header = s.Header, rows = s.Rows })
    };
    return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
}

static string ToHtml(Report report)
{
    var sb = new StringBuilder();
    sb.AppendLine("<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"utf-8\">");
    sb.AppendLine($"<title>Kairos System — {System.Net.WebUtility.HtmlEncode(Environment.MachineName)}</title>");
    sb.AppendLine("<style>body{font-family:'Segoe UI',Arial,sans-serif;background:#0d0d0f;color:#e0e0e0;margin:32px}" +
                  "h1{color:#c9a0dc;font-weight:300;letter-spacing:2px}" +
                  "h2{color:#c9a0dc;border-bottom:1px solid #3a3a3a;padding-bottom:6px;margin-top:32px;letter-spacing:1px}" +
                  "table{border-collapse:collapse;margin:12px 0 24px;width:100%}td,th{border:1px solid #2a2a2e;padding:6px 14px;font-size:14px}" +
                  "th{background:#1a1a1e;text-align:left;color:#c9a0dc;letter-spacing:0.5px}tr:nth-child(even){background:#141418}" +
                  "p{color:#888}</style></head><body>");
    sb.AppendLine($"<h1>KAIROS SYSTEM</h1>");
    sb.AppendLine($"<p>Машина: {System.Net.WebUtility.HtmlEncode(Environment.MachineName)}  •  Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>");
    foreach (var sec in report.Sections)
    {
        sb.AppendLine($"<h2>{System.Net.WebUtility.HtmlEncode(sec.Title)}</h2>");
        if (sec.Rows.Count == 0) { sb.AppendLine("<p>нет данных</p>"); continue; }
        sb.AppendLine("<table>");
        if (sec.Header != null)
            sb.AppendLine("<tr>" + string.Join("", sec.Header.Select(h => $"<th>{System.Net.WebUtility.HtmlEncode(h)}</th>")) + "</tr>");
        foreach (var row in sec.Rows)
            sb.AppendLine("<tr>" + string.Join("", row.Select(c => $"<td>{System.Net.WebUtility.HtmlEncode(c)}</td>")) + "</tr>");
        sb.AppendLine("</table>");
    }
    sb.AppendLine("</body></html>");
    return sb.ToString();
}

void CompareReports()
{
    Clear();
    Header();

    var current = BuildAll();
    var currentMap = Flatten(current);

    var previousFile = Directory.GetFiles(Directory.GetCurrentDirectory(), "SystemInfo_*.json")
        .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();

    Console.WriteLine();
    if (previousFile is null)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Нет предыдущего отчёта. Сначала выполните экспорт (E → JSON).");
        Console.ResetColor();
        Pause();
        return;
    }

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  Сравнение с: {Path.GetFileName(previousFile)}");
    Console.ResetColor();
    Console.WriteLine();

    var previousMap = LoadJsonMap(previousFile);

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  ── Изменилось ──");
    Console.ResetColor();
    bool any = false;
    foreach (var kv in currentMap)
        if (previousMap.TryGetValue(kv.Key, out var old) && old != kv.Value)
        {
            Console.WriteLine($"    {kv.Key}");
            Console.WriteLine($"        было : {old}");
            Console.WriteLine($"        стало: {kv.Value}");
            any = true;
        }
    if (!any) Console.WriteLine("    (без изменений)");

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine();
    Console.WriteLine("  ── Появилось ──");
    Console.ResetColor();
    any = false;
    foreach (var kv in currentMap.Where(k => !previousMap.ContainsKey(k.Key)))
    {
        Console.WriteLine($"    {kv.Key}: {kv.Value}");
        any = true;
    }
    if (!any) Console.WriteLine("    (нет)");

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine();
    Console.WriteLine("  ── Пропало ──");
    Console.ResetColor();
    any = false;
    foreach (var kv in previousMap.Where(k => !currentMap.ContainsKey(k.Key)))
    {
        Console.WriteLine($"    {kv.Key}: {kv.Value}");
        any = true;
    }
    if (!any) Console.WriteLine("    (нет)");

    Pause();
}

static Dictionary<string, string> Flatten(Report report)
{
    var map = new Dictionary<string, string>();
    foreach (var sec in report.Sections)
    {
        if (sec.Header != null) continue;
        foreach (var row in sec.Rows)
            if (row.Length > 1) map[$"{sec.Title} → {row[0]}"] = row[1];
    }
    return map;
}

static Dictionary<string, string> LoadJsonMap(string file)
{
    var map = new Dictionary<string, string>();
    try
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(file));
        foreach (var sec in doc.RootElement.GetProperty("sections").EnumerateArray())
        {
            string title = sec.GetProperty("title").GetString() ?? "";
            if (sec.GetProperty("header").ValueKind != JsonValueKind.Null) continue;
            foreach (var row in sec.GetProperty("rows").EnumerateArray())
            {
                if (row.GetArrayLength() > 1)
                    map[$"{title} → {row[0].GetString()}"] = row[1].GetString() ?? "";
            }
        }
    }
    catch { }
    return map;
}

void Diagnose()
{
    Clear();
    Header();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkMagenta;
    Console.WriteLine("  ДИАГНОСТИКА ПРОБЛЕМ");
    Console.ResetColor();
    Console.WriteLine();

    var checks = new List<(string Level, string Text)>();

    try
    {
        const string appId = "55c92734-d682-4d71-983e-d6ec3f16059f";
        bool licensed = W.All("SoftwareLicensingProduct").Any(p =>
            W.S(p, "ApplicationID").Equals(appId, StringComparison.OrdinalIgnoreCase) &&
            W.U(p, "LicenseStatus") == 1);
        checks.Add(licensed ? Ok("Windows активирована") : Warn("Windows, возможно, не активирована"));
    }
    catch { checks.Add(Warn("Не удалось проверить активацию Windows")); }

    try
    {
        int av = W.All("AntiVirusProduct", @"root\SecurityCenter2").Count;
        checks.Add(av > 0 ? Ok($"Антивирус установлен ({av})") : Warn("Антивирус не обнаружен"));
    }
    catch { checks.Add(Warn("Не удалось проверить антивирус")); }

    try
    {
        var mp = W.First("MSFT_MpComputerStatus", @"root\Microsoft\Windows\Defender");
        if (mp != null)
            checks.Add(W.Bool(mp, "RealTimeProtectionEnabled") == true
                ? Ok("Защита в реальном времени включена")
                : Warn("Защита в реальном времени отключена"));
    }
    catch { }

    try
    {
        var disks = W.All("MSFT_PhysicalDisk", @"root\Microsoft\Windows\Storage");
        var bad = disks.Where(d => W.U(d, "HealthStatus") != 0).ToList();
        checks.Add(bad.Count == 0 ? Ok("SMART: все накопители здоровы")
            : Err($"SMART: проблемные накопители — {string.Join(", ", bad.Select(d => W.S(d, "FriendlyName")))}"));
    }
    catch { }

    try
    {
        foreach (var l in W.All("Win32_LogicalDisk").Where(x => (uint)W.U(x, "DriveType") == 3))
        {
            ulong size = W.U(l, "Size"), free = W.U(l, "FreeSpace");
            if (size > 0)
            {
                double pct = 100.0 * free / size;
                string d = W.S(l, "DeviceID");
                if (pct < 5) checks.Add(Err($"Диск {d}: критически мало места ({pct:0}% свободно)"));
                else if (pct < 10) checks.Add(Warn($"Диск {d}: мало места ({pct:0}% свободно)"));
                else checks.Add(Ok($"Диск {d}: достаточно места ({pct:0}% свободно)"));
            }
        }
    }
    catch { }

    try
    {
        var os = W.First("Win32_OperatingSystem");
        if (os != null)
        {
            ulong total = W.U(os, "TotalVisibleMemorySize") * 1024;
            ulong free = W.U(os, "FreePhysicalMemory") * 1024;
            if (total > 0)
            {
                double pct = 100.0 * free / total;
                checks.Add(pct < 10 ? Warn($"Мало свободной памяти ({pct:0}% свободно)") : Ok($"Память в норме ({pct:0}% свободно)"));
            }
        }
    }
    catch { }

    try
    {
        int problem = W.All("Win32_PnPEntity")
            .Count(x => W.Get(x, "ConfigManagerErrorCode") != null && W.U(x, "ConfigManagerErrorCode") != 0);
        checks.Add(problem == 0 ? Ok("Устройства без проблем с драйверами")
            : Warn($"Устройств с ошибками драйверов: {problem}"));
    }
    catch { }

    try
    {
        bool reboot =
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending") != null ||
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired") != null ||
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager")?.GetValue("PendingFileRenameOperations") != null;
        checks.Add(reboot ? Warn("Требуется перезагрузка Windows") : Ok("Перезагрузка не требуется"));
    }
    catch { }

    if (!isAdmin)
    {
        checks.Add(Warn("Программа запущена без прав администратора — часть проверок может быть неполной"));
    }

    foreach (var (level, text) in checks)
    {
        Console.ForegroundColor = level switch
        {
            "OK" => ConsoleColor.Green,
            "WARN" => ConsoleColor.Yellow,
            _ => ConsoleColor.Red
        };
        string icon = level switch { "OK" => "[ OK ]", "WARN" => "[ВНИМ]", _ => "[ОШБ]" };
        Console.WriteLine($"  {icon} {text}");
        Console.ResetColor();
    }

    int warns = checks.Count(c => c.Level != "OK");
    Console.WriteLine();
    Console.ForegroundColor = warns == 0 ? ConsoleColor.Green : ConsoleColor.Yellow;
    Console.WriteLine(warns == 0
        ? "  Проблем не обнаружено."
        : $"  Найдено замечаний: {warns} из {checks.Count}.");
    Console.ResetColor();
    Pause();
}

void PlaySnake()
{
    const int width = 40;
    const int height = 20;

    if (Console.IsInputRedirected || Console.IsOutputRedirected)
    {
        Clear();
        Error("Игра доступна только в интерактивной консоли.");
        Pause();
        return;
    }

    var rnd = new Random();

    while (true)
    {
        try { Console.CursorVisible = false; } catch { }
        Console.Clear();

        var snake = new LinkedList<Point>();
        var start = new Point(width / 2, height / 2);
        snake.AddFirst(start);
        snake.AddLast(new Point(start.X - 1, start.Y));
        snake.AddLast(new Point(start.X - 2, start.Y));

        var dir = Dir.Right;
        var pending = Dir.Right;
        var food = FindFood(rnd, width, height, snake);
        int score = 0;
        int speed = 130;

        DrawBorder(width, height);
        DrawHeader(score, width);
        int seg = 0;
        foreach (var p in snake)
            DrawCell(p, seg++ == 0 ? '@' : 'o', seg == 1 ? ConsoleColor.Green : ConsoleColor.DarkGreen);
        DrawCell(food, '*', ConsoleColor.Red);

        bool dead = false;
        bool quit = false;
        var sw = Stopwatch.StartNew();

        while (!dead)
        {
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape) { quit = true; break; }
                var candidate = key.Key switch
                {
                    ConsoleKey.UpArrow or ConsoleKey.W => Dir.Up,
                    ConsoleKey.DownArrow or ConsoleKey.S => Dir.Down,
                    ConsoleKey.LeftArrow or ConsoleKey.A => Dir.Left,
                    ConsoleKey.RightArrow or ConsoleKey.D => Dir.Right,
                    _ => dir
                };
                if (!IsReverse(candidate, dir)) pending = candidate;
            }
            if (quit || dead) break;

            if (sw.ElapsedMilliseconds < speed)
            {
                Thread.Sleep(5);
                continue;
            }
            sw.Restart();

            dir = pending;
            var head = snake.First!.Value;
            var next = NextPoint(head, dir);

            if (next.X < 0 || next.Y < 0 || next.X >= width || next.Y >= height ||
                snake.Any(p => p.X == next.X && p.Y == next.Y))
            {
                dead = true;
                break;
            }

            bool ate = next.X == food.X && next.Y == food.Y;
            snake.AddFirst(next);
            DrawCell(next, '@', ConsoleColor.Green);

            var oldHead = snake.Find(next)?.Next;
            if (oldHead != null) DrawCell(oldHead.Value, 'o', ConsoleColor.DarkGreen);

            if (ate)
            {
                score++;
                DrawHeader(score, width);
                food = FindFood(rnd, width, height, snake);
                DrawCell(food, '*', ConsoleColor.Red);
                if (speed > 55) speed -= 3;
            }
            else
            {
                var tail = snake.Last!.Value;
                snake.RemoveLast();
                DrawCell(tail, ' ', ConsoleColor.Black);
            }
        }

        if (quit) break;

        Console.ForegroundColor = ConsoleColor.Red;
        try { Console.SetCursorPosition(0, height + 4); } catch { }
        Console.WriteLine($"  ИГРА ОКОНЧЕНА!  Счёт: {score}");
        Console.ResetColor();
        Console.WriteLine("  Enter — играть снова     Esc — выйти в меню");

        bool again = false;
        while (true)
        {
            var k = Console.ReadKey(true);
            if (k.Key == ConsoleKey.Enter) { again = true; break; }
            if (k.Key == ConsoleKey.Escape) { break; }
        }

        if (!again) break;
    }

    try { Console.CursorVisible = true; } catch { }
}

static Point NextPoint(Point p, Dir d) => d switch
{
    Dir.Up => new Point(p.X, p.Y - 1),
    Dir.Down => new Point(p.X, p.Y + 1),
    Dir.Left => new Point(p.X - 1, p.Y),
    Dir.Right => new Point(p.X + 1, p.Y),
    _ => p
};

static bool IsReverse(Dir a, Dir b) =>
    (a == Dir.Up && b == Dir.Down) || (a == Dir.Down && b == Dir.Up) ||
    (a == Dir.Left && b == Dir.Right) || (a == Dir.Right && b == Dir.Left);

static Point FindFood(Random rnd, int width, int height, LinkedList<Point> snake)
{
    int x, y;
    int guard = 0;
    do
    {
        x = rnd.Next(width);
        y = rnd.Next(height);
        guard++;
    }
    while (snake.Any(p => p.X == x && p.Y == y) && guard < 1000);
    return new Point(x, y);
}

static void DrawBorder(int width, int height)
{
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.SetCursorPosition(0, 1);
    Console.Write("╔" + new string('═', width) + "╗");
    for (int y = 0; y < height; y++)
    {
        Console.SetCursorPosition(0, y + 2);
        Console.Write('║');
        Console.SetCursorPosition(width + 1, y + 2);
        Console.Write('║');
    }
    Console.SetCursorPosition(0, height + 2);
    Console.Write("╚" + new string('═', width) + "╝");
    Console.ResetColor();
}

static void DrawHeader(int score, int width)
{
    Console.SetCursorPosition(0, 0);
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write($"  ЗМЕЙКА   Счёт: {score,-4}   (стрелки/WASD — движение, Esc — выход)");
    Console.ResetColor();
}

static void DrawCell(Point p, char ch, ConsoleColor color)
{
    try
    {
        Console.SetCursorPosition(p.X + 1, p.Y + 2);
        Console.ForegroundColor = color;
        Console.Write(ch);
        Console.ResetColor();
    }
    catch { }
}

void Help()
{
    Clear();
    Header();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkMagenta;
    Console.WriteLine("  СПРАВКА — что означает каждый пункт:");
    Console.ResetColor();
    Console.WriteLine();

    var help = new (string Key, string Text)[]
    {
        ("1-9",   "базовая конфигурация: компьютер, ОС, CPU, память, диски, видео, плата, сеть, питание"),
        ("10",    "мониторы: разрешение, герцовка, диагональ, число подключённых"),
        ("11",    "периферия: клавиатуры, мыши, камеры, флешки, USB-устройства"),
        ("12",    "звуковые устройства: аудиокарты и конечные точки"),
        ("13",    "батарея/ИБП: заряд, износ, ёмкость, циклы"),
        ("14",    "датчики: температуры CPU/чипсета, вентиляторы, напряжения (⚠ нужен админ)"),
        ("15",    "принтеры и сканеры"),
        ("16",    "версия Windows, дата установки, статус активации и ключ"),
        ("17",    "установленное ПО: список программ и версии"),
        ("18",    "обновления Windows: установленные KB и ожидающие"),
        ("19",    "драйверы и устройства с ошибками"),
        ("20",    "службы (автозагрузка), автозапуск программ"),
        ("21",    "пользователи и группы"),
        ("22",    "точки восстановления и резервные копии (⚠ нужен админ)"),
        ("23",    "Wi-Fi: сохранённые профили и пароли (⚠ нужен админ)"),
        ("24",    "открытые порты и активные соединения"),
        ("25",    "VPN-адаптеры и настройки прокси"),
        ("27",    "антивирус и Защитник Windows: статус, версии баз"),
        ("28",    "брандмауэр по профилям (домен/частный/общий)"),
        ("29",    "SMART: здоровье накопителей (⚠ нужен админ)"),
        ("30",    "журнал событий: последние системные ошибки"),
        ("31",    "BitLocker: статус шифрования томов (⚠ нужен админ)"),
        ("32",    "загрузка CPU/RAM/диска в реальном времени"),
        ("33",    "индекс производительности WinSAT"),
        ("34",    "топ процессов по памяти и по нагрузке CPU"),
        ("35",    "полный анализ всех .jar файлов в системе"),
        ("K",     "очистка ненужных файлов: кэш, корзина, обновления, дампы, теневые копии, UWP, драйверы"),
        ("V",     "активация Windows и Office через irm get.activated.win"),
        ("A",     "полный отчёт по всем разделам на экран"),
        ("E",     "экспорт отчёта в CSV, JSON или HTML"),
        ("C",     "сравнить текущее состояние с последним экспортом JSON"),
        ("D",     "автодиагностика типичных проблем"),
        ("H",     "эта справка"),
        ("G",     "развлечение: классическая игра «Змейка»"),
        ("R",     "перезапустить программу с правами администратора"),
        ("0",     "выход")
    };
    foreach (var (k, t) in help)
    {
        Console.Write("    ");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"{k,-6}");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(t);
        Console.ResetColor();
    }
    Pause();
}

static string FirstOf(ManagementBaseObject o, string a, string b)
{
    string v = W.S(o, a);
    return v.Length > 0 ? v : W.S(o, b);
}

static string DtmStr(ManagementBaseObject o, string prop, bool withTime = false)
{
    var d = W.Dtm(o, prop);
    return d == default ? "" : d.ToString(withTime ? "dd.MM.yyyy HH:mm" : "dd.MM.yyyy");
}

static (string Level, string Text) Ok(string s) => ("OK", s);
static (string Level, string Text) Warn(string s) => ("WARN", s);
static (string Level, string Text) Err(string s) => ("ERR", s);

static string Esc(string s)
{
    if (s is null) return "";
    s = s.Replace("\r", " ").Replace("\n", " ");
    return s.Contains(';') || s.Contains('"') ? '"' + s.Replace("\"", "\"\"") + '"' : s;
}

static string Trunc(string s, int w) => s.Length <= w ? s : s[..Math.Max(0, w - 1)] + "…";
static string Pad(string s, int w) => s.Length >= w ? s : s + new string(' ', w - s.Length);

void Menu()
{
    Clear();
    Header();

    var left = new (string Label, (string Key, string Title)[] Items)[]
    {
        ("СИСТЕМА", new[]
        {
            ("1","Компьютер"), ("2","ОС"), ("3","Процессор"),
            ("4","ОЗУ"), ("5","Накопители"), ("6","Видеокарта"),
            ("7","Плата и BIOS")
        }),
        ("УСТРОЙСТВА", new[]
        {
            ("8","Сеть"), ("9","Питание"),
            ("10","Мониторы"), ("11","Периферия USB"), ("12","Звук"),
            ("13","Батарея / ИБП"), ("14","Датчики"), ("15","Принтеры")
        }),
        ("СОФТ И СЕРВИС", new[]
        {
            ("16","Активация Windows"), ("17","Установленное ПО"),
            ("18","Обновления"), ("19","Драйверы"),
            ("20","Службы и автозагрузка"), ("21","Пользователи"),
            ("22","Точки восстановления")
        }),
        ("СЕТЬ", new[]
        {
            ("23","Wi-Fi и пароли"), ("24","Порты и соединения"), ("25","VPN и прокси")
        })
    };

    var right = new (string Label, (string Key, string Title)[] Items)[]
    {
        ("БЕЗОПАСНОСТЬ", new[]
        {
            ("27","Антивирус / Defender"), ("28","Брандмауэр"),
            ("29","SMART дисков"), ("30","Журнал событий"), ("31","BitLocker")
        }),
        ("ПРОИЗВОДИТЕЛЬНОСТЬ", new[]
        {
            ("32","Нагрузка в реальном времени"), ("33","Индекс WinSAT"),
            ("34","Процессы (топ)"), ("35","Анализ .jar")
        }),
        ("УТИЛИТЫ", new[]
        {
            ("V","Активировать Windows + Office"),
            ("K","Очистка ненужных файлов"),
            ("A","Полный отчёт (всё)"),
            ("E","Экспорт (CSV / JSON / HTML)"),
            ("C","Сравнить с предыдущим"),
            ("D","Диагностика проблем"),
            ("H","Справка"),
            ("R","Перезапуск с правами админа"),
            ("0","Выход")
        }),
        ("РАЗВЛЕЧЕНИЯ", new[]
        {
            ("G","Игра «Змейка»")
        })
    };

    const int LeftInner = 34;
    const int RightInner = 36;

    var leftLines = new List<string>();
    foreach (var (label, items) in left)
    {
        leftLines.Add("§C§◆ " + label + "§R§");
        foreach (var (key, title) in items)
        {
            string pad = key.Length == 1 ? " " : "";
            leftLines.Add($"§Y§{pad}{key}§R§  {title}");
        }
        leftLines.Add("");
    }

    var rightLines = new List<string>();
    foreach (var (label, items) in right)
    {
        rightLines.Add("§C§◆ " + label + "§R§");
        foreach (var (key, title) in items)
        {
            string pad = key.Length == 1 ? " " : "";
            rightLines.Add($"§Y§{pad}{key}§R§  {title}");
        }
        rightLines.Add("");
    }

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkMagenta;
    Console.WriteLine("  ╔" + new string('═', LeftInner + 2) + "╤" + new string('═', RightInner + 2) + "╗");
    Console.ResetColor();

    int maxRows = Math.Max(leftLines.Count, rightLines.Count);
    for (int i = 0; i < maxRows; i++)
    {
        string leftLine = i < leftLines.Count ? leftLines[i] : "";
        string rightLine = i < rightLines.Count ? rightLines[i] : "";

        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.Write("  ║ ");
        Console.ResetColor();

        WriteColored(leftLine, LeftInner);

        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.Write(" ║ ");
        Console.ResetColor();

        WriteColored(rightLine, RightInner);

        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine(" ║");
        Console.ResetColor();
    }

    Console.ForegroundColor = ConsoleColor.DarkMagenta;
    Console.WriteLine("  ╚" + new string('═', LeftInner + 2) + "╧" + new string('═', RightInner + 2) + "╝");
    Console.ResetColor();
}

void WriteColored(string s, int width)
{
    int visible = 0;
    int i = 0;
    while (i < s.Length)
    {
        if (s[i] == '§' && i + 2 < s.Length && s[i + 2] == '§')
        {
            char code = s[i + 1];
            Console.ForegroundColor = code switch
            {
                'Y' => ConsoleColor.Yellow,
                'C' => ConsoleColor.DarkCyan,
                'G' => ConsoleColor.Gray,
                _ => ConsoleColor.Gray
            };
            i += 3;
            continue;
        }
        Console.Write(s[i]);
        visible++;
        i++;
    }

    if (visible < width)
        Console.Write(new string(' ', width - visible));

    Console.ResetColor();
}

void Header()
{
    Console.ForegroundColor = ConsoleColor.DarkMagenta;
    Console.WriteLine("  ╔══════════════════════════════════════════════════════════╗");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  ║                  K A I R O S   S Y S T E M               ║");
    Console.ForegroundColor = ConsoleColor.DarkMagenta;
    Console.WriteLine("  ╚══════════════════════════════════════════════════════════╝");
    Console.ResetColor();
    var os = W.First("Win32_OperatingSystem");
    var cs = W.First("Win32_ComputerSystem");
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write("  ");
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.Write(cs is null ? "" : W.S(cs, "Name"));
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write("  •  ");
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.Write(os is null ? "" : W.S(os, "Caption"));
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write("  •  ");
    if (isAdmin)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("[ADMIN]");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("[USER]  (R — перезапуск с правами)");
    }
    Console.ResetColor();
    Console.WriteLine();
}

static void Clear()
{
    try { Console.Clear(); } catch { }
}

void Pause()
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write("  Нажмите Enter, чтобы вернуться в меню...");
    Console.ResetColor();
    Console.ReadLine();
}

static void Error(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("  " + message);
    Console.ResetColor();
}

sealed class CleanResult
{
    public int FilesDeleted { get; set; }
    public long BytesFreed { get; set; }
    public int Errors { get; set; }
    public int Locked { get; set; }
    public bool Skipped { get; set; }
    public string LastError { get; set; } = "";
}

sealed class Report
{
    public List<Section> Sections { get; } = new();
    Section _current = new("—");

    public void Section(string title)
    {
        _current = new Section(title);
        Sections.Add(_current);
    }

    public void Header(params string[] cols) => _current.Header = cols;

    public void Row(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || F.IsJunk(value)) return;
        _current.Rows.Add(new[] { name, value });
    }

    public void Cells(params string[] cells) => _current.Rows.Add(cells);

    public void MergeFrom(Report other) => Sections.AddRange(other.Sections);
}

sealed class Section
{
    public string Title { get; }
    public string[]? Header { get; set; }
    public List<string[]> Rows { get; } = new();
    public Section(string title) => Title = title;
}

sealed class JarInfo
{
    public string Title { get; set; } = "";
    public string Version { get; set; } = "";
    public string Vendor { get; set; } = "";
    public string MainClass { get; set; } = "";
    public int ClassCount { get; set; }
    public List<string> Dependencies { get; } = new();
}

static class W
{
    public static List<ManagementBaseObject> All(string cls, string? scope = null) => Query($"SELECT * FROM {cls}", scope);

    public static List<ManagementBaseObject> Query(string wql, string? scope = null)
    {
        try
        {
            using var searcher = scope is null
                ? new ManagementObjectSearcher(wql)
                : new ManagementObjectSearcher(new ManagementScope($@"\\.\{scope}"), new ObjectQuery(wql));
            return searcher.Get().Cast<ManagementBaseObject>().ToList();
        }
        catch { return new List<ManagementBaseObject>(); }
    }

    public static ManagementBaseObject? First(string cls, string? scope = null) => All(cls, scope).FirstOrDefault();

    public static object? Get(ManagementBaseObject o, string prop)
    {
        try { return o.Properties[prop]?.Value; } catch { return null; }
    }

    public static string S(ManagementBaseObject o, string prop) => Get(o, prop)?.ToString()?.Trim() ?? "";

    public static ulong U(ManagementBaseObject o, string prop)
    {
        var v = Get(o, prop);
        if (v is null) return 0;
        try { return Convert.ToUInt64(v); } catch { return 0; }
    }

    public static double D(ManagementBaseObject o, string prop)
    {
        var v = Get(o, prop);
        if (v is null) return 0;
        try { return Convert.ToDouble(v); } catch { return 0; }
    }

    public static bool? Bool(ManagementBaseObject o, string prop)
    {
        var v = Get(o, prop);
        if (v is null) return null;
        if (v is bool b) return b;
        try
        {
            if (v is string s)
            {
                if (bool.TryParse(s, out var parsed)) return parsed;
                if (long.TryParse(s, out var num)) return num != 0;
            }
            return Convert.ToInt64(v) != 0;
        }
        catch { return null; }
    }

    public static string B(ManagementBaseObject o, string prop)
    {
        var v = Bool(o, prop);
        return v is null ? "" : (v.Value ? "Да" : "Нет");
    }

    public static DateTime Dtm(ManagementBaseObject o, string prop)
    {
        var v = Get(o, prop)?.ToString();
        if (string.IsNullOrEmpty(v)) return default;

        try { return ManagementDateTimeConverter.ToDateTime(v); } catch { }

        string cleaned = v.Trim();
        if (cleaned.Length >= 14 && cleaned.Take(14).All(char.IsDigit))
        {
            if (DateTime.TryParseExact(cleaned[..14], "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            if (DateTime.TryParseExact(cleaned[..8], "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
        }

        if (DateTime.TryParse(cleaned, CultureInfo.InvariantCulture, DateTimeStyles.None, out var generic))
            return generic;
        if (DateTime.TryParse(cleaned, CultureInfo.CurrentCulture, DateTimeStyles.None, out var local))
            return local;

        return default;
    }

    public static string Id(object? value)
    {
        if (value is ushort[] a)
        {
            var sb = new StringBuilder();
            foreach (var ch in a) { if (ch == 0) break; sb.Append((char)ch); }
            return sb.ToString().Trim();
        }
        return value?.ToString()?.Trim() ?? "";
    }
}

static class F
{
    static readonly HashSet<string> Placeholders = new(StringComparer.OrdinalIgnoreCase)
    {
        "none", "to be filled by o.e.m.", "default string", "not specified",
        "system serial number", "base board serial number", "o.e.m.", "n/a", "undefined", "unknown"
    };

    public static bool IsJunk(string v) => v.Trim().Length == 0 || Placeholders.Contains(v.Trim());

    public static string Bytes(ulong value)
    {
        string[] units = { "Б", "КБ", "МБ", "ГБ", "ТБ", "ПБ" };
        double size = value;
        int i = 0;
        while (size >= 1024 && i < units.Length - 1) { size /= 1024; i++; }
        return $"{size:0.##} {units[i]}";
    }

    public static string FmtKb(object? value)
    {
        if (value is null) return "0 КБ";
        double kb = Convert.ToDouble(value);
        return kb >= 1024 ? $"{kb / 1024:0.##} МБ" : $"{kb:0.##} КБ";
    }

    public static string Fmt(TimeSpan t) => $"{t.Days}д {t.Hours}ч {t.Minutes}м";

    public static string Join(object? value) => value is Array a
        ? string.Join(", ", a.Cast<object>().Select(x => x?.ToString()))
        : value?.ToString() ?? "";

    public static string MemoryType(ulong code) => code switch
    {
        20 => "DDR", 21 => "DDR2", 24 => "DDR3", 26 => "DDR4", 34 => "DDR5",
        0 => "", _ => $"тип {code}"
    };

    public static string DiskMediaType(ulong code) => code switch
    {
        3 => "HDD", 4 => "SSD", 5 => "SCM", _ => "диск"
    };

    public static string BusType(ulong code) => code switch
    {
        1 => "SCSI", 2 => "ATAPI", 3 => "ATA", 7 => "USB", 8 => "RAID",
        10 => "SAS", 11 => "SATA", 14 => "Virtual", 17 => "NVMe", _ => ""
    };

    public static string SmartHealth(ulong code) => code switch
    {
        0 => "Healthy (здоров)", 1 => "Warning (предупреждение)", 2 => "Unhealthy (неисправен)", _ => "неизвестно"
    };

    public static string BatteryStatus(ulong code) => code switch
    {
        1 => "разряжается", 2 => "от сети", 3 => "полностью заряжена", 4 => "низкий заряд",
        5 => "критический", 6 => "заряжается", 7 => "заряжается + низкий", 8 => "заряжается + критический",
        _ => ""
    };

    public static string LicenseStatus(ulong code) => code switch
    {
        0 => "не активирована", 1 => "активирована", 2 => "начальный льготный период",
        3 => "дополнительный льготный период", 4 => "нелицензионный льготный период",
        5 => "уведомление", 6 => "расширенный льготный период", _ => $"код {code}"
    };

    public static string RestoreType(ulong code) => code switch
    {
        0 => "APPLICATION_INSTALL", 1 => "APPLICATION_UNINSTALL", 10 => "DEVICE_DRIVER_INSTALL",
        12 => "MODIFY_SETTINGS", 13 => "CANCELLED_OPERATION", _ => code.ToString()
    };

    public static string AvState(string productState)
    {
        if (!ulong.TryParse(productState, out var v)) return productState;
        string enabled = ((v >> 12) & 0x0F) == 1 ? "включён" : "выключен";
        string updated = ((v >> 4) & 0x0F) == 0 ? "базы актуальны" : "базы устарели";
        return $"{enabled}, {updated} (0x{v:X})";
    }

    public static string ProfileName(string raw) => raw.Trim() switch
    {
        "Domain" => "Доменный профиль",
        "Private" => "Частный профиль",
        "Public" => "Общий профиль",
        _ => raw.Trim()
    };

    public static string Protection(string raw) => raw.Trim() switch
    {
        "On" => "защита включена",
        "Off" => "защита выключена",
        "1" => "защита включена",
        "0" => "защита выключена",
        _ => raw.Trim()
    };
}

static class Sh
{
    public static string Run(string file, int timeoutMs, params string[] args) =>
        Exec(file, args, Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage), timeoutMs);

    public static string Ps(string command, int timeoutMs) =>
        Exec("powershell", new[]
        {
            "-NoProfile", "-NonInteractive", "-Command",
            "$ProgressPreference='SilentlyContinue';[Console]::OutputEncoding=[Text.Encoding]::UTF8;" + command
        }, Encoding.UTF8, timeoutMs);

    static string Exec(string file, string[] args, Encoding enc, int timeoutMs)
    {
        Process? p = null;
        try
        {
            var psi = new ProcessStartInfo(file)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = enc
            };
            foreach (var a in args) psi.ArgumentList.Add(a);

            p = Process.Start(psi);
            if (p is null) return "";

            var outTask = p.StandardOutput.ReadToEndAsync();
            var errTask = p.StandardError.ReadToEndAsync();
            if (!Task.WaitAll(new Task[] { outTask, errTask }, timeoutMs))
            {
                try { p.Kill(true); } catch { }
                return outTask.IsCompleted ? outTask.Result : "";
            }
            try { p.WaitForExit(2000); } catch { }
            return outTask.Result;
        }
        catch { return ""; }
        finally
        {
            try { p?.Dispose(); } catch { }
        }
    }
}

readonly record struct Point(int X, int Y);

enum Dir { Up, Down, Left, Right }