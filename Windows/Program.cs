using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Security.Principal;

using OpenHardwareMonitor.Hardware;
using System.IO;

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
            MessageBox.Show(e.Message.ToString(), "Temp Midas LCD");
        }

        private void UpdateLoop()
        {
            is_running = true;

            Thread.CurrentThread.IsBackground = true;

            sendorDriver = new SensorDriver();
            midasDriver = new MidasLCDDriver();

            while (true)
            {
                try
                {
                    Console.WriteLine("Initializing MidasLCD Driver ...");
                    midasDriver.Open();

                    Console.WriteLine("Initializing Sensors ...");
                    midasDriver.WriteText("Initializing    Sensors ...");
                    sendorDriver.Open();
                    
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

                        string cpu_temps_text = String.Format("CPU {0}C / {1}C", (int)temps_cpu_cur, (int)temps_cpu_max);
                        cpu_temps_text = cpu_temps_text.PadRight(16).Substring(0, 16);
                        string gpu_temps_text = String.Format("GPU {0}C / {1}C", (int)temps_gpu_cur, (int)temps_gpu_max);
                        gpu_temps_text = gpu_temps_text.PadRight(16).Substring(0, 16);

                        midasDriver.WriteText(cpu_temps_text + gpu_temps_text);
                        Thread.Sleep(2000);
                    }

                    Console.WriteLine("Exiting");
                    midasDriver.Close();
                    sendorDriver.Close();
                    break;
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                    Thread.Sleep(2000);
                }
            }

            is_running = false;
        }

        public void Start()
        {
            new Thread(() =>
            {
                this.UpdateLoop();
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
                string msg = "Please 'Run As Administrator' for sensors to properly function.";
                Console.WriteLine(msg);
                MessageBox.Show(msg, "Temp Midas LCD");

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
            else
            {
                Console.WriteLine("An instance is already running. Close that first.");
                MessageBox.Show("An instance is already running. Close that first.", "Temp Midas LCD");
            }
        }
    }
}
