using Hacknet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowZero.Daemons.Event
{
    public class UnavoidableEventDaemon : EventDaemon
    {
        public UnavoidableEventDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        public override void navigatedTo()
        {
            base.navigatedTo();

            LockUpModules();
        }

        internal static void LockUpModules()
        {
            OS os = OS.currentInstance;

            os.ram.inputLocked = true;
            os.ram.guiInputLockStatus = true;

            os.netMap.inputLocked = true;
            os.netMap.guiInputLockStatus = true;

            os.terminal.inputLocked = true;
            os.terminal.guiInputLockStatus = true;
        }

        internal static void UnlockModules()
        {
            OS os = OS.currentInstance;

            os.ram.inputLocked = false;
            os.ram.guiInputLockStatus = false;

            os.netMap.inputLocked = false;
            os.netMap.guiInputLockStatus = false;

            os.terminal.inputLocked = false;
            os.terminal.guiInputLockStatus = false;
        }
    }
}
