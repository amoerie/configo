using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Configo.Client.JsonSchemaGenerator;

internal sealed class ConfigoJsonContractResolver : DefaultContractResolver
{
    private static readonly HashSet<Type> GenericTypesToIgnore = new HashSet<Type>(new List<Type>
    {
        typeof(Action<>),
        typeof(Func<>),
        typeof(Func<,>),
        typeof(Func<,,>),
        typeof(Func<,,,>),
        typeof(Func<,,,,>),
        typeof(Func<,,,,,>),
        typeof(Func<,,,,,,>),
        typeof(Func<,,,,,,,>),
        typeof(Func<,,,,,,,,>),
        typeof(Func<,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,>),
    });

    private static readonly HashSet<Type> TypesToIgnore = new HashSet<Type>(new List<Type>
    {
        typeof(Action)
    });


    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);
        Type? propertyType = property.PropertyType;

        if (propertyType is null)
        {
            property.ShouldSerialize = _ => false;
            property.ShouldDeserialize = _ => false;
            return property;
        }

        if (propertyType.Namespace?.StartsWith("System.Collections") != true && (propertyType.IsInterface || propertyType.IsAbstract))
        {
            property.ShouldSerialize = _ => false;
            property.ShouldDeserialize = _ => false;
            return property;
        }

        if (propertyType.IsGenericType)
        {
            if (GenericTypesToIgnore.Contains(propertyType.GetGenericTypeDefinition()))
            {
                property.ShouldSerialize = _ => false;
                property.ShouldDeserialize = _ => false;
                return property;
            }
        }
        else
        {
            if (TypesToIgnore.Contains(propertyType))
            {
                property.ShouldSerialize = _ => false;
                property.ShouldDeserialize = _ => false;
            }
        }

        return property;
    }
}
