
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using OpenHardwareMonitor.Hardware;

namespace TempsMidasLCD
{    
    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware)
                subHardware.Accept(this);
        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }

    public class SensorDriver
    {
        private Computer computer;
        private UpdateVisitor updateVisitor = new UpdateVisitor();
        
        public SensorDriver()
        {
            /*
            // check if the OpenHardwareMonitorLib assembly has the correct version
            if (Assembly.GetAssembly(typeof(Computer)).GetName().Version != 
                Assembly.GetExecutingAssembly().GetName().Version)
            {
                string expected = Assembly.GetAssembly(typeof(Computer)).GetName().ToString();
                string actual = Assembly.GetExecutingAssembly().GetName().ToString();
                MessageBox.Show(
                  $"The version of the file OpenHardwareMonitorLib.dll is incompatible.\n{expected}\n{actual}",
                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }*/

            this.computer = new Computer(null);

            int p = (int)Environment.OSVersion.Platform;
            if ((p == 4) || (p == 128))
            { 
                // Unix
            }
            else
            {
                // Windows
            }

            computer.HardwareAdded += new HardwareEventHandler(HardwareAdded);
            computer.HardwareRemoved += new HardwareEventHandler(HardwareRemoved);

            computer.CPUEnabled = true;
            computer.GPUEnabled = true;

            computer.Open();
            
            // Make sure the settings are saved when the user logs off
            Microsoft.Win32.SystemEvents.SessionEnded += delegate {
                computer.Close();
            };
        }

        private void SubHardwareAdded(IHardware hardware)
        {
            foreach (IHardware subHardware in hardware.SubHardware)
                SubHardwareAdded(subHardware);
        }

        private void HardwareAdded(IHardware hardware)
        {
            SubHardwareAdded(hardware);
        }

        private void HardwareRemoved(IHardware hardware)
        {
        }

        public void Update()
        {
            computer.Accept(updateVisitor);
        }

        public IList<ISensor> GetSensors()
        {
            IList<ISensor> allSensors = new List<ISensor>();
            computer.Accept(new SensorVisitor(delegate (ISensor sensor) {
                allSensors.Add(sensor);
            }));

            return allSensors;
        }
     
        public string GetReport()
        {
            return computer.GetReport();
        }

        public void ResetMinMax()
        {
            computer.Accept(new SensorVisitor(delegate (ISensor sensor) {
                sensor.ResetMin();
                sensor.ResetMax();
            }));
        }

        public void Reset()
        {
            computer.Close();
            computer.Open();
        }
    }
}
