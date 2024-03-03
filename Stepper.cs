using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpticalFiberAssembly
{
    internal class Stepper(int deviceId, SerialPort serialPort)
    {
        public readonly int id = deviceId;
        public SerialPort CommSerial { get; set; } = serialPort;

        public void SetTarget(byte x)
        {
            SendInstruct(CmdCode.SET_TAR, x);
        }

        public void SetSpeed(byte speed)
        {
            SendInstruct(CmdCode.SET_SPEED, speed);
        }

        public void SetAcceleration(byte acc)
        {
            SendInstruct(CmdCode.SET_ACC, acc);
        }

        public void Run()
        {
            SendInstruct(CmdCode.CTRL_RUN, 0);
        }

        public void Stop()
        {
            SendInstruct(CmdCode.E_STOP, 0);
        }

        private void SendInstruct(CmdCode cmdCode, byte data)
        {
            byte cmd = (byte)cmdCode;
            byte head = (byte)(id << 4 | cmd);
            byte[] mes = [head, data];
            CommSerial.Write(mes, 0, mes.Length);
        }
    }
}
