using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LinePutScript;
using LinePutScript.Dictionary;
using VPet_Simulator.Core;
using VPet_Simulator.Windows;
using VPet_Simulator.Windows.Interface;

internal static class Program
{
    private static string reportPath = "";

    // Run only against a disposable COPY of the published folder: this creates saves and a test mod.
    [STAThread]
    private static void Main(string[] args)
    {
        // WPF fixes ResourceAssembly to the executable entry assembly. This harness hosts
        // the shipped app in a separate executable, so redirect its resource root before use.
        FieldInfo resourceAssembly = typeof(Application).GetFields(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(f => f.FieldType == typeof(Assembly) && f.Name.Contains("resourceAssembly", StringComparison.OrdinalIgnoreCase));
        resourceAssembly.SetValue(null, typeof(App).Assembly);
        RunTests(args);
    }

    private static void RunTests(string[] args)
    {
        string phase = args[0];
        string profile = args.Single(x => x.StartsWith("prefix#"))[7..^2];
        string root = Path.GetDirectoryName(typeof(App).Assembly.Location)!;
        ExtensionValue.BaseDirectory = root;
        reportPath = Path.Combine(root, $"smoke-{phase}-{(profile.Length == 0 ? "default" : profile)}.txt");
        if (!File.Exists(Path.Combine(root, ".standalone-smoke-sandbox")))
            throw new InvalidOperationException("Use a marked disposable sandbox, never the release or an existing installation.");
        File.WriteAllText(reportPath, "");

        string prefix = profile.Length == 0 ? "" : "-" + profile;
        if (phase == "init")
        {
            var settings = new LPS();
            settings["SingleTips"].SetBool("helloworld", true);
            settings["SingleTips"].SetDateTime("tutorial", DateTime.Now);
            settings["gameconfig"].SetInt("resolution", 250);
            settings["gameconfig"].SetBool("allowmove", true);
            settings["gameconfig"].SetBool("startboot", false);
            settings["onmod"].Add(new Sub("smokelocal"));
            File.WriteAllText(Path.Combine(root, $"Setting{prefix}.lps"), settings.ToString());
            string mod = Path.Combine(root, "mod", "smoke_local");
            Directory.CreateDirectory(mod);
            File.WriteAllText(Path.Combine(mod, "info.lps"),
                "vupmod#SmokeLocal:|author#Smoke:|gamever#11000:|ver#11000:|\nintro#Local mod smoke test:|");
            // An explicit default-profile argument must override this marker.
            File.WriteAllText(Path.Combine(root, "startup_unwanted-profile"), "");
        }

        new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMinutes(3));
            Finish(1, "FAIL: startup/test timeout");
        }) { IsBackground = true }.Start();

        var app = new App();
        app.InitializeComponent();
        // Host a hidden MainWindow below instead of opening the app's default StartupUri.
        typeof(Application).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(f => f.FieldType == typeof(Uri) && f.Name.Contains("startupUri", StringComparison.OrdinalIgnoreCase))
            .SetValue(app, null);
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        app.DispatcherUnhandledException += (_, e) => Finish(1, "FAIL: " + e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Finish(1, "FAIL: " + e.ExceptionObject);

        app.Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                var window = new MainWindow();
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                timer.Tick += async (_, _) =>
                {
                    if (window.Main?.IsWorking != true || window.winSetting == null)
                        return;
                    timer.Stop();
                    try
                    {
                        Check(window.PrefixSave == prefix, "selected profile, including explicit default");
                        Check(window.Pets.Count > 0 && window.Core.Graph!.GraphsList.Count > 0 && window.Foods.Count > 0,
                            "core pet animations and food resources loaded");
                        Check(window.OnModInfo.Any(x => x.Name == "SmokeLocal"), "enabled mod loaded from local folder");
                        Check(window.SteamID == 0 && !window.IsSteamUser && await window.GenerateAuthKey() == 0,
                            "legacy plugin identity is inert");
                        string argument = (string)typeof(MainWindow).GetProperty("StartupProfileArgument",
                            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
                        Check(argument == $"prefix#{profile}:|", "restart profile argument preserves spaces and default");

                        var save = window.GameSavesData;
                        if (phase == "init")
                        {
                            int touches = save.Statistics![(gint)"stat_touch_head"];
                            window.Main.DisplayToTouchHead();
                            Check(save.Statistics[(gint)"stat_touch_head"] == touches + 1, "head interaction updates local statistics");
                            await Task.Delay(400);
                            touches = save.Statistics[(gint)"stat_touch_body"];
                            window.Main.DisplayToTouchBody();
                            Check(save.Statistics[(gint)"stat_touch_body"] == touches + 1, "body interaction updates local statistics");
                            save.GameSave.StrengthFood = 20;
                            save.GameSave.EatFood(new Food { Name = "SmokeFood", StrengthFood = 10, Exp = 5 });
                            Check(save.GameSave.StrengthFood > 20, "feeding changes pet state locally");
                            save.GameSave.Name = "Standalone Smoke " + profile;
                            save.GameSave.Money = 12345;
                            save.Statistics[(gint)"stat_touch_head"] = 42;
                            window.Save();
                            Check(Directory.GetFiles(Path.Combine(root, "Saves"), $"Save{prefix}_*.lps").Length > 0,
                                "local save written");
                            Check(Directory.GetFiles(Path.Combine(root, "Saves_BKP"), $"Save{prefix}_*.lps").Length > 0,
                                "local backup written");
                        }
                        else
                        {
                            Check(save.GameSave.Name == "Standalone Smoke " + profile && Math.Abs(save.GameSave.Money - 12345) < 0.01,
                                "pet state restored in a fresh process");
                            Check(save.Statistics![(gint)"stat_touch_head"] == 42, "local statistics restored in a fresh process");
                            string backup = Directory.GetFiles(Path.Combine(root, "Saves_BKP"), $"Save{prefix}_*.lps").First();
                            save.GameSave.Money = 1;
                            Check(window.SavesLoad(new LPS(File.ReadAllText(backup))) && Math.Abs(window.GameSavesData.GameSave.Money - 12345) < 0.01,
                                "backup restores pet data");
                        }

                        var manager = new winSaveManager(window);
                        var grid = (DataGrid)manager.FindName("DataGridSaves");
                        Check(grid.Items.Count > 0, "save manager lists local saves and backups");
                        foreach (object row in grid.Items)
                        {
                            string file = (string)row.GetType().GetProperty("FullPath")!.GetValue(row)!;
                            Check(Path.GetFileName(file).StartsWith($"Save{prefix}_"), "save manager isolates selected profile");
                        }
                        _ = new winCharacterPanel(window);
                        var report = new winReport(window);
                        Check(((CheckBox)report.FindName("IncludeSave")).IsChecked == false, "report attachment requires explicit selection");

                        bool IsSteam(string name) => name.Contains("steam", StringComparison.OrdinalIgnoreCase)
                            || name.Contains("Facepunch", StringComparison.OrdinalIgnoreCase);
                        Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => IsSteam(a.GetName().Name!)), "no Steam managed assemblies loaded");
                        Check(!Process.GetCurrentProcess().Modules.Cast<ProcessModule>().Any(m => IsSteam(m.ModuleName)),
                            "no Steam native modules loaded");
                        Finish(0, "PASS: " + phase + ", profile=" + profile);
                    }
                    catch (Exception ex) { Finish(1, "FAIL: " + ex); }
                };
                timer.Start();
            }
            catch (Exception ex) { Finish(1, "FAIL: " + ex); }
        }));
        app.Run();
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        File.AppendAllText(reportPath, "OK: " + name + Environment.NewLine);
    }

    private static void Finish(int code, string message)
    {
        File.AppendAllText(reportPath, message + Environment.NewLine);
        Environment.Exit(code);
    }
}
