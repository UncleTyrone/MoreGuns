using System.Reflection;

namespace MoreGuns
{
    /// <summary>
    /// String-based member access for game types that differ between Mono and Il2Cpp.
    /// Uses Type.GetProperty/GetField/GetMethod so missing names stay silent.
    /// Do not use Harmony AccessTools.Field here: it logs a warning on every Il2Cpp
    /// property-backed member (e.g. Avatar.CurrentEquippable).
    /// </summary>
    internal static class GameAccess
    {
        private static readonly Dictionary<(Type Type, string Name), MemberInfo> MemberCache = new();
        private static readonly Dictionary<(Type Type, string Name, int ArgCount), MethodInfo> MethodCache = new();
        private const BindingFlags Flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase;

        public static void Call(object instance, string method, params object[] args)
        {
            if (instance == null)
                return;
            FindMethod(instance.GetType(), method, args?.Length ?? 0)?.Invoke(instance, args);
        }

        public static T Call<T>(object instance, string method, params object[] args)
        {
            if (instance == null)
                return default;
            object result = FindMethod(instance.GetType(), method, args?.Length ?? 0)?.Invoke(instance, args);
            return result is T typed ? typed : default;
        }

        public static void Set(object instance, string member, object value)
        {
            if (instance == null)
                return;
            switch (ResolveMember(instance.GetType(), member))
            {
                case FieldInfo field:
                    field.SetValue(instance, value);
                    break;
                case PropertyInfo property when property.CanWrite:
                    property.SetValue(instance, value);
                    break;
            }
        }

        public static T Get<T>(object instance, string member)
        {
            if (instance == null)
                return default;
            switch (ResolveMember(instance.GetType(), member))
            {
                case FieldInfo field:
                    return field.GetValue(instance) is T ft ? ft : default;
                case PropertyInfo property when property.CanRead:
                    return property.GetValue(instance) is T pt ? pt : default;
                default:
                    return default;
            }
        }

        public static bool CanFire(Equippable_RangedWeapon weapon, bool checkAmmo)
        {
            return Call<bool>(weapon, "CanFire", checkAmmo);
        }

        public static void Cock(Equippable_RangedWeapon weapon)
        {
            Call(weapon, "Cock");
        }

        public static void Recalculate(FloatStack stack)
        {
            Call(stack, "Recalculate");
        }

        public static void SetCurrentEquippable(object avatar, AvatarEquippable equippable)
        {
            Set(avatar, "CurrentEquippable", equippable);
        }

        public static void CreateListingUI(ShopInterface shop, ShopListing listing)
        {
            Call(shop, "CreateListingUI", listing);
        }

        public static void RefreshShownItems(ShopInterface shop)
        {
            Call(shop, "RefreshShownItems");
        }

        private static MemberInfo ResolveMember(Type type, string name)
        {
            (Type Type, string Name) key = (type, name);
            if (MemberCache.TryGetValue(key, out MemberInfo cached))
                return cached;

            MemberInfo found = FindOnType(type, name);
            if (found == null && type.BaseType != null)
                found = FindOnType(type.BaseType, name);
            MemberCache[key] = found;
            return found;
        }

        private static MemberInfo FindOnType(Type type, string name)
        {
            PropertyInfo property = type.GetProperty(name, Flags) ?? type.GetProperty(name, Flags | BindingFlags.FlattenHierarchy);
            if (property != null)
                return property;
            return type.GetField(name, Flags) ?? type.GetField(name, Flags | BindingFlags.FlattenHierarchy);
        }

        private static MethodInfo FindMethod(Type type, string name, int argCount)
        {
            (Type Type, string Name, int ArgCount) key = (type, name, argCount);
            if (MethodCache.TryGetValue(key, out MethodInfo cached))
                return cached;

            MethodInfo found = MatchMethod(type, name, argCount);
            if (found == null && type.BaseType != null)
                found = MatchMethod(type.BaseType, name, argCount);
            MethodCache[key] = found;
            return found;
        }

        private static MethodInfo MatchMethod(Type type, string name, int argCount)
        {
            foreach (MethodInfo method in type.GetMethods(Flags | BindingFlags.FlattenHierarchy))
            {
                if (!string.Equals(method.Name, name, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (method.GetParameters().Length == argCount)
                    return method;
            }
            return null;
        }
    }
}
