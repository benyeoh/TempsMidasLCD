using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Security.Principal;

using OpenHardwareMonitor.Hardware;

namespace TempsMidasLCD
{
    public class App: ApplicationContext
    {
        private NotifyIcon trayIcon;
        private MidasLCDDriver midasDriver;
        private SensorDriver sendorDriver;
        private bool is_exiting = false;
        private bool is_running = false;

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);

            // Initialize Tray Icon
            trayIcon = new NotifyIcon()
            {
                Text = "Temps Midas LCD",
                Icon = Properties.Resources.thermometer_xcv_icon,
                ContextMenu = new ContextMenu(new MenuItem[] {
                new MenuItem("Exit", Exit)
            }),
                Visible = true
            };
        }

        void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            trayIcon.Visible = false;
            is_exiting = true;
            while (is_running)
            {
                Thread.Sleep(100);
            }

            Application.Exit();
        }

        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine(e.Message.ToString());
            MessageBox.Show(e.Message.ToString(), "Temp Midas LCD Error");
        }

        public void Start()
        {
            new Thread(() =>
            {
                is_running = true;

                Thread.CurrentThread.IsBackground = true;

                Console.WriteLine("Initializing MidasLCD Driver...");
                midasDriver = new MidasLCDDriver();

                midasDriver.WriteText("Initializing    Sensors ...");
                Console.WriteLine("Initializing Sensor Driver...");
                sendorDriver = new SensorDriver();
                midasDriver.ClearText();
                Console.WriteLine("Done!");

                // Update sensors and drive LCD
                while (!is_exiting)
                {
                    float temps_cpu_cur = 0.0f;
                    float temps_cpu_max = 0.0f;
                    float temps_gpu_cur = 0.0f;
                    float temps_gpu_max = 0.0f;

                    sendorDriver.Update();
                    foreach (ISensor sensor in sendorDriver.GetSensors())
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            if (sensor.Name.Equals("CPU Package"))
                            {
                                temps_cpu_cur = (float)(sensor.Value ?? 0.0f);
                                temps_cpu_max = (float)(sensor.Max ?? 0.0f);
                            }
                            else if (sensor.Name.Equals("GPU Core"))
                            {
                                temps_gpu_cur = (float)(sensor.Value ?? 0.0f);
                                temps_gpu_max = (float)(sensor.Max ?? 0.0f);
                            }
                        }
                    }

                    string cpu_temps_text = String.Format("CPU {0}C {1}C", temps_cpu_cur.ToString("f1"), temps_cpu_max.ToString("f1"));
                    cpu_temps_text = cpu_temps_text.PadRight(16).Substring(0, 16);
                    string gpu_temps_text = String.Format("GPU {0}C {1}C", temps_gpu_cur.ToString("f1"), temps_gpu_max.ToString("f1"));
                    gpu_temps_text = gpu_temps_text.PadRight(16).Substring(0, 16);

                    midasDriver.WriteText(cpu_temps_text + gpu_temps_text);
                    Thread.Sleep(5000);
                }

                Console.WriteLine("Exiting");
                midasDriver.Close();

                is_running = false;
            }).Start();

        }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Check for admin privileges
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                Console.WriteLine("*** WARNING: Need to 'Run As Administrator' for this to properly work. ***\n");
            }

            // Check if already running
            var exists = System.Diagnostics.Process.GetProcessesByName(
                System.IO.Path.GetFileNameWithoutExtension(
                    System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1;

            if (!exists)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                App app = new App();
                app.Start();
                Application.Run(app);
            }
        }
    }
}
