using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace HollowZero
{
    public static class ReflectionHelpers
    {
        private static readonly BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly BindingFlags staticFlags = BindingFlags.NonPublic | BindingFlags.Static;

        public static T GetPrivateStaticField<T>(this Type type, string fieldName)
        {
            Console.WriteLine($"{type.Name} / {fieldName}");
            FieldInfo field = type.GetField(fieldName, staticFlags);
            return (T)field.GetValue(null);
        }

        public static T GetPrivateStaticProperty<T>(this Type type, string propName)
        {
            PropertyInfo prop = type.GetProperty(propName, staticFlags);
            return (T)prop.GetValue(null);
        }

        public static void SetPrivateStaticField(this Type type, string fieldName, object newValue)
        {
            Console.WriteLine($"{type.Name} / {fieldName} / {newValue}");
            FieldInfo field = type.GetField(fieldName, staticFlags);
            field.SetValue(null, newValue);
        }

        public static void SetPrivateStaticProperty(this Type type, string propName, object newValue)
        {
            PropertyInfo prop = type.GetProperty(propName, staticFlags);
            prop.SetValue(null, newValue);
        }
    }
}
