using System.Reflection;

namespace CinematicBoss
{
    public static class ModUtils
    {
        public static object GetPrivateValue(object obj, string name, BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic)
        {
            return obj.GetType().GetField(name, bindingAttr)?.GetValue(obj);
        }
        public static void SetPrivateValue(object obj, string name, object value, BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic)
        {
            obj.GetType().GetField(name, bindingAttr)?.SetValue(obj, value);
        }
    }
}
