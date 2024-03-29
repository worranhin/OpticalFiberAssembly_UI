﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpticalFiberAssembly
{
    internal class Communicate
    {
        public enum CommunicateMode { STOP, RUN, RUN_SPEED, STATUS, ZERO };

        public CommunicateMode Mode { get; set; } = CommunicateMode.STOP;
        public byte DeviceId { get; set; } = 255;
        public float? Speed { get; set; } = null;
        public float? MaxSpeed { get; set; } = null;
        public float? Acceleration { get; set; } = null;
        public float? Target { get; set; } = null;
        public float? Position { get; set; } = null;
    }
}
