using System.Linq;
using System.Reflection;

namespace Cleipnir.ObjectDB.Version2.Persistency
{
    internal static class ReflectiveSerializer
    {
        public static void Serialize(object instance, Map2 m)
        {
            var instanceType = instance.GetType();
            var fields = instanceType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var fieldValues = fields
                .Select(f => new { f.Name, Value = f.GetValue(instance) })
                .Where(a => m.IsSerializable(a.Value));

            foreach (var fieldAndValue in fieldValues)
                m[fieldAndValue.Name] = fieldAndValue.Value;
        }

        public static void Deserialize(RMap rm, object instance)
        {
            var instanceType = instance.GetType();

            foreach (var field in instanceType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var f = field;
                if (!rm.ContainsKey(field.Name))
                    continue;
                
                rm.WhenResolved<object>(field.Name, o => f.SetValue(instance, o));
            }
        }
    }
}
