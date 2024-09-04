using Hacknet;

using BepInEx;

namespace HollowZero
{
    public static class ComputerExtensions
    {
        public static void AddAttachedID(this Computer comp, string id)
        {
            if (comp.attatchedDeviceIDs.Contains(id)) return;
            bool empty = comp.attatchedDeviceIDs.IsNullOrWhiteSpace();

            if(!empty)
            {
                comp.attatchedDeviceIDs += ",";
            }
            comp.attatchedDeviceIDs += id;
        }
    }
}
