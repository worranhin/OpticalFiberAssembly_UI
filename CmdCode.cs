using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpticalFiberAssembly
{
    internal enum CmdCode : byte
    {
        E_STOP = 0x00,  // emergency stop 急停
        GET_STATUS = 0x04,
        SET_ZERO = 0x08,
        SET_TAR = 0x09,  // set direction 设置方向
        SET_SPEED = 0x0a,
        SET_ACC = 0x0b,  // set acceleration 设置加速度
        CTRL_RUN = 0x0c,
        CTRL_STOP = 0x0d,
        CTRL_GO_MM = 0x0e,  // 运行 x mm
        CTRL_GO_ZERO = 0x0f   // 返回零位        
    }
}
