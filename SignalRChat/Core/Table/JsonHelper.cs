using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

using System.Web.Script.Serialization;

namespace API
{
    // Json helper for serialization any objects into Azure tables, but not only. Basically it can be used for other serialization purposes.
    // The main differences from standard Json serializer:
    // 1. Enums are serialized as strings
    // 2. Only public properties are serialized for objects, any other member fields are ignored. This is the way compatible with Azure TableEntity which works the same.
    //
    public static class JsonHelper
    {
        static JavaScriptSerializer serializer = new JavaScriptSerializer();

        // TODO: remove this enum and next function.
        internal enum ExtraTypes
        {
            SupportedType,   // such types we will not serialize/deserialize
            CustomClassType, // any class but not a String!
            EnumType,        // any enum
            ListType,        // generic List
            DictionaryType,  // generic Dictionary
            PairType,        // KeyValuePair helper (actuall can be omitted, but just an implimentation detail)
        };

        internal static ExtraTypes IsExtraType(Type objType)
        {
            if (objType.IsEnum) return ExtraTypes.EnumType;
            if (objType.IsGenericType)
            {
                Type genType = objType.GetGenericTypeDefinition();
                if (genType == typeof(List<>)) return ExtraTypes.ListType;
                if (genType == typeof(Dictionary<,>)) return ExtraTypes.DictionaryType;
                if (genType == typeof(KeyValuePair<,>)) return ExtraTypes.PairType;
            }

            if (objType != typeof(String) && objType.IsClass) return ExtraTypes.CustomClassType;

            return ExtraTypes.SupportedType;
        }

        public static string ToJson(this object obj)
        {
            if (obj == null)
            {
                return "null";
            }

            Type objType = obj.GetType();

            switch (IsExtraType(obj.GetType()))
            {
                case ExtraTypes.CustomClassType:
                    IEnumerable<PropertyInfo> fields = obj.GetType().GetProperties();
                    return "{" + String.Join(",", fields.Select(item => item.Name.ToJson() + ":" + item.GetValue(obj).ToJson())) + "}";

                case ExtraTypes.DictionaryType:
                    IEnumerable<object> dict = ((IEnumerable)obj).Cast<object>();
                    return "{" + String.Join(",", dict.Select(item => item.ToJson())) + "}";

                case ExtraTypes.EnumType:
                    return obj.ToString().ToJson();

                case ExtraTypes.ListType:
                    IEnumerable<object> list = ((IEnumerable)obj).Cast<object>();
                    return "[" + String.Join(",", list.Select(item => item.ToJson())) + "]";

                case ExtraTypes.PairType:
                    return objType.GetProperty("Key").GetValue(obj).ToJson() + ":" + objType.GetProperty("Value").GetValue(obj).ToJson();

                default:
                    return serializer.Serialize(obj);
            }
        }

        public static T FromJson<T>(this T obj, string json)
        {
            Type objType = obj.GetType();

            if (objType.IsEnum)
            {
                obj = (T)(Enum.Parse(objType, /* ugly hack. TODO: get rid */ json[0] == '"' ? "".FromJson(json) : json));
            }
            else
            {
                obj = (T)serializer.Deserialize(json, objType);
            }
            return obj;
        }
    }
}
