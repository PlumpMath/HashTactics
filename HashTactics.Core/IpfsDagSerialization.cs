using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Ipfs;

namespace HashTactics.Core
{
    public static class IpfsDagSerialization
    {
        // Ludicrous idea: Replace type with Func<type,bool>. 
        private static Dictionary<Type, Func<object, DagNode>> ConverterRepository = new Dictionary<Type, Func<object, DagNode>>();

        public static void RegisterSerializer<ObjectType>(Func<object, DagNode> serializer)
        {
            RegisterSerializer(typeof(ObjectType), serializer);
        }

        private static void RegisterSerializer(Type objectType, Func<object, DagNode> serializer)
        {
            ConverterRepository.Add(objectType, serializer);
        }

        private static DagNode ToStringUtf8Node(object input)
        {
            return new DagNode(Encoding.UTF8.GetBytes((string)input));
        }

        private static DagNode ToVarintNode(object input)
        {
            return new DagNode(Varint.Encode(Convert.ToInt64(input)));
        }

        static IpfsDagSerialization()
        {
            RegisterSerializer<string>(ToStringUtf8Node);
            RegisterSerializer<long>(ToVarintNode);
            RegisterSerializer<int>(ToVarintNode);
        }

        public static DagNode MapToDag<ObjectType>(ObjectType instance)
        {
            return MapToDag(typeof(ObjectType), instance);
        }

        private static DagNode MapToDag(Type objectType, object value)
        {
            if (ConverterRepository.ContainsKey(objectType))
            {
                return ConverterRepository[objectType](value);
            }

            List<IMerkleLink> links = new List<IMerkleLink>();
            // Okay, we need to create a converter. 
            foreach (var property in objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var property_value = property.GetValue(value);
                DagNode property_node = MapToDag(property.PropertyType, property_value);
                links.Add(property_node.ToLink(property.Name));
            }

            return new DagNode(null, links);
        }
    }

}
