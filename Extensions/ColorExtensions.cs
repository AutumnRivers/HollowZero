using Microsoft.Xna.Framework;

namespace HollowZero
{
    public static class ColorUtils
    {
        public static Color FromString(string colorString)
        {
            string[] stringArr = colorString.Split(',');
            return new Color(int.Parse(stringArr[0]), int.Parse(stringArr[1]), int.Parse(stringArr[2]));
        }
    }
}
