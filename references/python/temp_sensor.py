# -*- coding: utf-8 -*-

import sys
import serial
import serial.tools.list_ports
import wmi
import time


def cmd_send(ser, cmd, pause_time=0.1):
    ser.write(cmd)
    # Wait until done
    time.sleep(pause_time)


def cmd_clear(ser):
    cmd_send(ser, '\x1b\x80\x01')
    cmd_send(ser, '\x1b\x80\x02')    


def cmd_home(ser):
    cmd_send(ser, '\x1b\x80\x02')    


def cmd_nextline(ser):
    cmd_send(ser, '\x1b\x80\xc0')


def cmd_enable_2x16(ser):
    cmd_send(ser, '\x1b\x80\x38')


def find_comport():
    mmcmdb_16sil_vidpid = "VID:PID=04D8:F9C3"
    for port in serial.tools.list_ports.comports():
        if mmcmdb_16sil_vidpid in port[2]:
            return port[0]
    raise Exception("Unable to find COM port!")


def get_cpu_gpu_temps_via_msacpi():
    w = wmi.WMI(namespace="root\\wmi")
    cpu_temp = (w.MSAcpi_ThermalZoneTemperature()[0].CurrentTemperature / 10.0) - 273.15
    return ((cpu_temp, cpu_temp), None)


def get_cpu_gpu_temps_via_ohm():
    cpu = None
    gpu = None    
    w = wmi.WMI(namespace="root\OpenHardwareMonitor")
    sensors = w.Sensor()
    for sensor in sensors:
        if 'Temperature' in sensor.SensorType:
            if 'CPU Package' in sensor.Name:
                cpu = (sensor.Value, sensor.Max)
            elif 'GPU Core' in sensor.Name:
                gpu = (sensor.Value, sensor.Max)
    return cpu, gpu


def get_cpu_gpu_temps():
    is_from_ohm = True
    cpu, gpu = get_cpu_gpu_temps_via_ohm()
    if cpu is None and gpu is None:
        is_from_ohm = False
        cpu, gpu = get_cpu_gpu_temps_via_msacpi()
    return ((cpu, gpu), is_from_ohm)

   
if __name__ == '__main__':
    com_port = find_comport()
    _, is_from_ohm = get_cpu_gpu_temps()    
    
    with serial.Serial(com_port, baudrate=19200) as ser:
        cmd_send(ser, '\x1b\xf0\x00')
        cmd_send(ser, '\x1b\xf2')
        cmd_clear(ser)
        if not is_from_ohm:
            cmd_send(ser, 'OHM not detected')
            time.sleep(3)
            cmd_clear(ser)

        while True:
            cmd_home(ser)
            ((cpu, gpu), _) = get_cpu_gpu_temps()        
            if cpu is not None:
                cmd_send(ser, 'CPU %.1fC %.1fC ' % (cpu[0], cpu[1]))
            else:
                cmd_send(ser, 'CPU :(          ')
            cmd_nextline(ser)
            if gpu is not None:
                cmd_send(ser, 'GPU%.1fC %.1fC' % (cpu[0], cpu[1]))
            else:
                cmd_send(ser, 'GPU :(          ') 
            time.sleep(5)
    print("Done")
        
        