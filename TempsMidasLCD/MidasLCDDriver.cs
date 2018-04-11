using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;

namespace TempsMidasLCD
{
    public class MidasLCDDriver
    {
        private SerialPort serialPort = new SerialPort();

        byte[] _CLEAR = new byte[] { 0x1B, 0x80, 0x01 };
        byte[] _HOME = new byte[] { 0x1B, 0x80, 0x02 };
        byte[] _NEXTLINE = new byte[] { 0x1B, 0x80, 0xC0 };
        byte[] _ENABLE_2X16 = new byte[] { 0x1B, 0x80, 0x38 };

        public MidasLCDDriver()
        {
            string MIDAS_VID = "04D8";
            string MIDAS_PID = "F9C3";
            List<string> comports = ComPortNames(MIDAS_VID, MIDAS_PID);

            if (comports.Count == 0)
            {
                throw new System.IO.IOException("Cannot find COM port for Midas LCD. Is it plugged in?");
            }

            serialPort.BaudRate = 9600;
            serialPort.PortName = comports[0];
            serialPort.Open();

            this.CmdReset();
            this.CmdEnable2x16();
            this.WriteText("Hello!");
        }

        private void Write(byte[] buffer, int pauseTime=100)
        {
            serialPort.Write(buffer, 0, buffer.Length);
            Thread.Sleep(pauseTime);
        }

        private void CmdReset()
        {
            this.Write(_CLEAR);
            this.Write(_HOME);
        }

        private void CmdHome()
        {
            this.Write(_HOME);
        }

        private void CmdNextLine()
        {
            this.Write(_NEXTLINE);
        }

        private void CmdEnable2x16()
        {
            this.Write(_ENABLE_2X16);
        }

        private List<string> ComPortNames(String VID, String PID)
        {
            String pattern = String.Format("^VID_{0}.PID_{1}", VID, PID);
            Regex _rx = new Regex(pattern, RegexOptions.IgnoreCase);
            List<string> comports = new List<string>();

            RegistryKey rk1 = Registry.LocalMachine;
            RegistryKey rk2 = rk1.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum");

            foreach (String s3 in rk2.GetSubKeyNames())
            {
                RegistryKey rk3 = rk2.OpenSubKey(s3);
                foreach (String s in rk3.GetSubKeyNames())
                {
                    if (_rx.Match(s).Success)
                    {
                        RegistryKey rk4 = rk3.OpenSubKey(s);
                        foreach (String s2 in rk4.GetSubKeyNames())
                        {
                            RegistryKey rk5 = rk4.OpenSubKey(s2);
                            string location = (string)rk5.GetValue("LocationInformation");
                            RegistryKey rk6 = rk5.OpenSubKey("Device Parameters");
                            string portName = (string)rk6.GetValue("PortName");
                            if (!String.IsNullOrEmpty(portName) && SerialPort.GetPortNames().Contains(portName))
                                comports.Add((string)rk6.GetValue("PortName"));
                        }
                    }
                }
            }

            return comports;
        }

        public void Close()
        {
            this.CmdReset();
            this.WriteText("Goodbye!");
            serialPort.Close();
        }

        public void ClearText()
        {
            this.CmdReset();
        }

        public void WriteText(string text)
        {
            this.CmdHome();

            string line1 = text.Length > 16 ? text.Substring(0, 16) : text;
            string line2 = text.Length > 16 ? text.Substring(16) : null;

            this.Write(Encoding.ASCII.GetBytes(line1));
            if (line2 != null)
            {
                this.CmdNextLine();
                this.Write(Encoding.ASCII.GetBytes(line2));
            }
        }
    }
}
