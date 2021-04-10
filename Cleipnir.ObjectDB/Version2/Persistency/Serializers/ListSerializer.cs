using System;
using System.Collections.Generic;
using System.Reflection;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Version2.Persistency.Serializers
{
    public class ListSerializer<T> : ISerializer2
    {
        public object Instance => _list;
        private readonly List<T> _list;

        public ListSerializer(List<T> list) => _list = list;

        public void SerializeInto(Map2 m)
        {
            m["¡Type"] = typeof(T).SimpleQualifiedName();
            ListSerializer.Serialize(_list, m);  
        } 
    }

    public class ListSerializerFactory : ISerializerFactory
    {
        public bool CanSerialize(object instance)
        {
            return instance.GetType().IsGenericType && instance.GetType().GetGenericTypeDefinition() == typeof(List<>);
        }

        public ISerializer2 CreateSerializer(object instance)
        {
            var genericArgument = instance.GetType().GetGenericArguments()[0];
            var listSerializerType = typeof(ListSerializer<>).MakeGenericType(genericArgument);
            var listSerializer = (ISerializer2) Activator.CreateInstance(listSerializerType, instance);
            return listSerializer;
        }

        public ISerializer2 CreateSerializer(RMap rm, Ephemerals _)
        {
            var listType = Type.GetType(rm["¡Type"].ToString());
            var deserializeMethod = typeof(ListSerializer).GetMethod("Deserialize").MakeGenericMethod(listType);
            var list = deserializeMethod.Invoke(null, new object[] {rm});
            return CreateSerializer(list);
        }
    }
    
    public static class ListSerializer
    {
        public static void Serialize<T>(List<T> l, Map2 m)
        {
            m["¡Count"] = l.Count;
            m["¡Capacity"] = l.Capacity;
            for (var i = 0; i < l.Count; i++)
                m[$"¡{i}_Value"] = l[i];
        }

        public static List<T> Deserialize<T>(RMap rm)
        {
            var count = (int) rm["¡Count"];
            var capacity = (int) rm["¡Capacity"];
            var l = new List<T>(capacity);
            var resolved = 0;
            var ts = new T[count];

            for (var i = 0; i < count; i++)
            {
                var j = i;
                rm.WhenResolved<T>($"¡{i}_Value", t =>
                {
                    ts[j] = t;
                    resolved++;
                    if (resolved == count)
                        l.AddRange(ts);
                });
            }

            return l;
        }
    }
}