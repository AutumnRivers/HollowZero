using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq.Expressions;

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

        public static Delegate CreateDelegate(this MethodInfo methodInfo, object target)
        {
            Func<Type[], Type> getType;
            var isAction = methodInfo.ReturnType.Equals((typeof(void)));
            var types = methodInfo.GetParameters().Select(p => p.ParameterType);

            if (isAction)
            {
                getType = Expression.GetActionType;
            }
            else
            {
                getType = Expression.GetFuncType;
                types = types.Concat(new[] { methodInfo.ReturnType });
            }

            if (methodInfo.IsStatic)
            {
                return Delegate.CreateDelegate(getType(types.ToArray()), methodInfo);
            }

            return Delegate.CreateDelegate(getType(types.ToArray()), target, methodInfo.Name);
        }

        public static Type[] GetTypes(this MethodInfo methodInfo)
        {
            Func<Type[], Type> getType;
            var types = methodInfo.GetParameters().Select(p => p.ParameterType);
            var isAction = methodInfo.ReturnType.Equals((typeof(void)));

            if (isAction)
            {
                getType = Expression.GetActionType;
            }
            else
            {
                getType = Expression.GetFuncType;
                types = types.Concat(new[] { methodInfo.ReturnType });
            }

            return types.ToArray();
        }

        public static bool HasTypes(this MethodInfo methodInfo, params Type[] targetTypes)
        {
            var types = methodInfo.GetTypes();
            bool hasAllTypes = true;
            
            foreach(var type in types)
            {
                hasAllTypes = targetTypes.Contains(type);
            }

            return hasAllTypes;
        }
    }
}
