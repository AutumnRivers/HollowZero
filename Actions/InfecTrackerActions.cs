using Hacknet;

using HollowZero.Executables;

using Pathfinder.Action;
using Pathfinder.Executable;

namespace HollowZero.Actions
{
    internal class LaunchInfecTrackerAction : PathfinderAction
    {
        public override void Trigger(object os_obj)
        {
            OS os = (OS)os_obj;

            InfecTracker infecTracker = new InfecTracker();
            infecTracker.bounds.X = os.ram.bounds.X;
            infecTracker.bounds.Width = os.ram.bounds.Width;
            OS.currentInstance.AddGameExecutable(infecTracker);
        }
    }
}
