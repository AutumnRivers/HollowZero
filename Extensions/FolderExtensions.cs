using Hacknet;

namespace HollowZero
{
    public static class FolderExtensions
    {
        public static bool TryFindFile(this Folder folder, string filename, out FileEntry file)
        {
            if(folder.searchForFile(filename) != null)
            {
                file = folder.searchForFile(filename);
                return true;
            } else
            {
                file = null;
                return false;
            }
        }
    }
}
