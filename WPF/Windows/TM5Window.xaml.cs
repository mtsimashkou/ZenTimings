using IniParser;
using IniParser.Model;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;

namespace ZenTimings.Windows
{
    /// <summary>
    /// Interaction logic for TM5Window.xaml
    /// </summary>
    public partial class TM5Window : Window
    {
        private const string TM5Executable = "TM5.exe";

        private static class UIAutomationConstants
        {
            public const string ButtonHomeId = "1000";
            public const string ButtonMailId = "1001";
            public const string LogListBoxId = "1004";
            public const string MessageBoxOKButtonId = "2";
        }

        public class ExternalProcessWindowHost : HwndHost
        {
            [DllImport("user32.dll")]
            private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32")]
            private static extern IntPtr SetParent(IntPtr hWnd, IntPtr hWndParent);

            [DllImport("user32")]
            private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

            private const int SWP_NOZORDER = 0x0004;
            private const int SWP_NOACTIVATE = 0x0010;
            private const int SWP_NOSIZE = 0x0001;
            private const int GWL_STYLE = -16;
            private const int WS_CHILD = 0x40000000;

            private Process process;
            public IntPtr RootHandle { get; private set; }

            public ExternalProcessWindowHost(ProcessStartInfo psi)
            {
                RootHandle = IntPtr.Zero;

                process = new Process();
                process.StartInfo = psi;
            }

            protected override HandleRef BuildWindowCore(HandleRef hwndParent)
            {
                HandleRef href = new HandleRef();

                if (process != null)
                {
                    RootHandle = hwndParent.Handle;

                    process.Start();
                    process.WaitForInputIdle();

                    SetWindowLong(process.MainWindowHandle, GWL_STYLE, WS_CHILD);
                    SetParent(process.MainWindowHandle, hwndParent.Handle);
                    SetWindowPos(process.MainWindowHandle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_NOACTIVATE);

                    href = new HandleRef(this, process.MainWindowHandle);
                }

                return href;
            }

            protected override void DestroyWindowCore(HandleRef hwnd)
            {
                process.Refresh();
                process.Close();
                process = null;
            }
        }

        public class TabView
        {
            public TabView()
            {
                ConfigView = new ConfigurationTab();
                InfoView = new InfoTab();
                LogView = new LogTab();
            }

            public ConfigurationTab ConfigView { get; set; }
            public InfoTab InfoView { get; set; }
            public LogTab LogView { get; set; }
        }

        public class ConfigurationTab
        {
            public string Header { get; set; }
            public int Language { get; set; }
            public string Cycles { get; set; }
        }

        public class InfoTab
        {
            public string Header { get; set; }
            public string Home { get; set; }
            public string Mail { get; set; }
        }

        public class LogTab
        {
            public string Header { get; set; }
        }

        public class LogItem
        {
            public DateTime Date { get; set; }
            public string Text { get; set; }
        }

        internal readonly AppSettings appSettings = (Application.Current as App)?.settings;

        private int logLinesCount = 0;
        private IntPtr tm5WindowHandle = IntPtr.Zero;

        private TabView tabView = new TabView();
        private List<AutomationElement> tm5Dialogs = new List<AutomationElement>();
        private ObservableCollection<LogItem> logView = new ObservableCollection<LogItem>();

        public TM5Window()
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(appSettings.TM5ExecutablePath) &&
                File.Exists(appSettings.TM5ExecutablePath) &&
                Path.GetFileName(appSettings.TM5ExecutablePath).Equals(TM5Executable))
            {
                InitializeTM5Controls();
            }
            else
            {
                canvasOpen.Visibility = Visibility.Visible;
            }
        }

        private void InitializeTM5Controls()
        {
            dgLogView.ItemsSource = logView;

            var linkFileName = Path.GetDirectoryName(appSettings.TM5ExecutablePath) + "\\bin\\Cfg.link";
            var configFileName = File.ReadAllText(linkFileName);

            var parser = new FileIniDataParser();
            parser.Parser.Configuration.SkipInvalidLines = true;
            parser.Parser.Configuration.AssigmentSpacer = "";

            IniData data = parser.ReadFile(configFileName);

            tabView.ConfigView.Header = $"Configuration: {data["Main Section"]["Config Name"]} @ {data["Main Section"]["Config Author"]}";
            tabView.ConfigView.Language = Convert.ToInt32(data["Main Section"]["Language"]);
            tabView.ConfigView.Cycles = data["Main Section"]["Cycles"];

            //tabControl.DataContext = tabView;

            //borderHost.Height = borderHost.Height + 100;
            //tabControl.Visibility = Visibility.Collapsed;

            borderHost.Child = new ExternalProcessWindowHost(new ProcessStartInfo
            {
                FileName = appSettings.TM5ExecutablePath,
                UseShellExecute = false
            });
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (canvasOpen.Visibility == Visibility.Hidden &&
                tm5WindowHandle == IntPtr.Zero)
            {
                StartTM5Process();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (tm5WindowHandle != IntPtr.Zero)
            {
                StopTM5Process();
            }
        }

        private void StartTM5Process()
        {
            /*var processes = Process.GetProcessesByName("TM5");
            if (processes.Length == 1)
            {
                processes[0].Kill();
            }*/

            tm5WindowHandle = (borderHost.Child as ExternalProcessWindowHost).RootHandle;

            AutomationElement element = AutomationElement.FromHandle(tm5WindowHandle);
            //AutomationElement parent = TreeWalker.ControlViewWalker.GetParent(element);

            Title = element.Current.Name;

            var elements = element.FindAll(TreeScope.Children, new OrCondition(
                //new PropertyCondition(AutomationElement.AutomationIdProperty, UIAutomationConstants.LogListBoxId),
                new PropertyCondition(AutomationElement.AutomationIdProperty, UIAutomationConstants.ButtonHomeId),
                new PropertyCondition(AutomationElement.AutomationIdProperty, UIAutomationConstants.ButtonMailId)));

            foreach (AutomationElement item in elements)
            {
                if (item.Current.AutomationId == UIAutomationConstants.ButtonHomeId)
                {
                    tabView.InfoView.Home = item.Current.Name;
                }
                else if (item.Current.AutomationId == UIAutomationConstants.ButtonMailId)
                {
                    tabView.InfoView.Mail = item.Current.Name;
                }
                /*else if (item.Current.AutomationId == UIAutomationConstants.LogListBoxId)
                {
                    tabView.InfoView.Header = item.Current.Name.Trim();
                }*/
            }

            Automation.AddAutomationEventHandler(
                WindowPattern.WindowOpenedEvent,
                AutomationElement.RootElement,
                TreeScope.Children,
                new AutomationEventHandler(OnAutomationEvent));

            /*Automation.AddAutomationEventHandler(
                WindowPattern.WindowClosedEvent,
                AutomationElement.RootElement,
                TreeScope.Subtree,
                new AutomationEventHandler(OnWindowClosedEvent));*/

            Automation.AddStructureChangedEventHandler(
                element,
                TreeScope.Children,
                new StructureChangedEventHandler(OnStructureChangedEvent));

            tabControl.DataContext = tabView;

            PrintAllAutomationElements(element);
        }

        private void StopTM5Process()
        {
            //PrintAllAutomationElements();

            tm5WindowHandle = IntPtr.Zero;

            Automation.RemoveAllEventHandlers();

            foreach (var dialog in tm5Dialogs)
            {
                var button = dialog.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, UIAutomationConstants.MessageBoxOKButtonId));
                if (button != null)
                {
                    var invokePattern = button.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                    invokePattern.Invoke();
                }
            }

            (borderHost.Child as ExternalProcessWindowHost).Dispose();
        }

        /*private void OnWindowClosedEvent(object sender, AutomationEventArgs e)
        {
            if (sender is null)
                return;

            Console.WriteLine((sender as AutomationElement).Current.Name);
        }*/

        private void OnAutomationEvent(object sender, AutomationEventArgs e)
        {
            var dialog = sender as AutomationElement;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var transform = (sender as AutomationElement).GetCurrentPattern(TransformPattern.Pattern) as TransformPattern;
                //transform.Move(100, 100);
            });

            tm5Dialogs.Add(dialog);

            //Console.WriteLine((sender as AutomationElement).Current.Name);
        }

        private void OnStructureChangedEvent(object sender, StructureChangedEventArgs e)
        {
            var element = (sender as AutomationElement);

            if (element.Current.LocalizedControlType == "list")
            {
                var elements = element.FindAll(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition);

                for (int i = logLinesCount; i < elements.Count; i++)
                {
                    //Console.WriteLine($"{elements[i].Current.Name}");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        logView.Add(new LogItem { Date = DateTime.Now, Text = elements[i].Current.Name });
                    });
                }
                logLinesCount = elements.Count;
            }
        }

        private void PrintAllAutomationElements()
        {
            PrintAllAutomationElements(AutomationElement.FromHandle(tm5WindowHandle));
        }

        private void PrintAllAutomationElements(AutomationElement root)
        {
            AutomationElementCollection elements = root.FindAll(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition);
            foreach (AutomationElement element in elements)
            {
                Console.WriteLine($"{element.Current.LocalizedControlType} <-> {element.Current.Name} <-> {element.Current.AutomationId}");
            }
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = $"TM5 Executable|{TM5Executable}";
            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();

            if (openFileDialog.ShowDialog() == true)
            {
                appSettings.TM5ExecutablePath = openFileDialog.FileName;
                appSettings.Save();

                InitializeTM5Controls();

                canvasOpen.Visibility = Visibility.Hidden;

                StartTM5Process();
            }
        }

        private void btnInfoHome_Click(object sender, RoutedEventArgs e)
        {
            //Process.Start("https://testmem.tz.ru");
            AutomationElement element = AutomationElement.FromHandle(tm5WindowHandle);

            var button = element.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, UIAutomationConstants.ButtonHomeId));
            var invokePattern = button.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            invokePattern.Invoke();
        }

        private void btnInfoMail_Click(object sender, RoutedEventArgs e)
        {
            AutomationElement element = AutomationElement.FromHandle(tm5WindowHandle);

            var button = element.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, UIAutomationConstants.ButtonMailId));
            var invokePattern = button.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            invokePattern.Invoke();
        }
    }
}
