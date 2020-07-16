using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;

namespace ServarrAPI.Datastore
{
    public static class MappingExtensions
    {
        public static bool IsReadable(this PropertyInfo propertyInfo)
        {
            return propertyInfo.CanRead && propertyInfo.GetGetMethod(false) != null;
        }

        public static bool IsWritable(this PropertyInfo propertyInfo)
        {
            return propertyInfo.CanWrite && propertyInfo.GetSetMethod(false) != null;
        }

        public static bool IsSimpleType(this Type type)
        {
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>) ||
                                       type.GetGenericTypeDefinition() == typeof(List<>) ||
                                       type.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                type = type.GetGenericArguments()[0];
            }

            return type.IsPrimitive
                   || type.IsEnum
                   || type == typeof(string)
                   || type == typeof(DateTime)
                   || type == typeof(Version)
                   || type == typeof(decimal);
        }

        public static PropertyInfo GetMemberName<T, TChild>(this Expression<Func<T, TChild>> member)
        {
            if (!(member.Body is MemberExpression memberExpression))
            {
                memberExpression = (member.Body as UnaryExpression).Operand as MemberExpression;
            }

            return (PropertyInfo)memberExpression.Member;
        }

        public static bool IsMappableProperty(this MemberInfo memberInfo)
        {
            var propertyInfo = memberInfo as PropertyInfo;

            if (propertyInfo == null)
            {
                return false;
            }

            if (!propertyInfo.IsReadable() || !propertyInfo.IsWritable())
            {
                return false;
            }

            // This is a bit of a hack but is the only way to see if a type has a handler set in Dapper
#pragma warning disable 618
            SqlMapper.LookupDbType(propertyInfo.PropertyType, "", false, out var handler);
#pragma warning restore 618
            if (propertyInfo.PropertyType.IsSimpleType() ||
                propertyInfo.PropertyType == typeof(byte[]) ||
                handler != null)
            {
                return true;
            }

            return false;
        }
    }
}
