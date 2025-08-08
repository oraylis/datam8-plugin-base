/* DataM8
 * Copyright (C) 2024-2025 ORAYLIS GmbH
 *
 * This file is part of DataM8.
 *
 * DataM8 is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * DataM8 is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Oraylis.DataM8.PluginBase.Helper
{
   public static class ObjectMapper
   {
      // Public API
      public static TDest? Map<TSrc, TDest>(TSrc? src)
          => (TDest?)MapInternal(src ,typeof(TDest) ,new Dictionary<object ,object>(ReferenceEqualityComparer.Instance));

      // -------- intern --------
      private static object? MapInternal(object? src ,Type destType ,Dictionary<object ,object> visited)
      {
         if (src is null)
         {
            return null;
         }

         var sType = src.GetType();

         // Primitive / einfache Typen direkt behandeln
         if (IsSimple(sType) && IsSimple(destType))
         {
            return ChangeType(src ,destType);
         }

         // Zyklenschutz
         if (!sType.IsValueType)
         {
            if (visited.TryGetValue(src ,out var cached))
            {
               return cached;
            }
         }

         // Collections
         if (TryMapEnumerable(src ,sType ,destType ,visited ,out var enumerableResult))
         {
            return enumerableResult;
         }

         // Objekt -> Objekt (rekursiv über Properties)
         var dest = Activator.CreateInstance(destType);
         if (dest is null)
         {
            return null;
         }

         if (!sType.IsValueType)
         {
            visited[src] = dest;
         }

         var sProps = GetReadableProps(sType);
         var dProps = GetWritableProps(destType).ToDictionary(p => p.Name ,p => p);

         foreach (var sp in sProps)
         {
            if (!dProps.TryGetValue(sp.Name ,out var dp))
            {
               continue;
            }

            var sval = sp.GetValue(src);
            if (sval is null)
            {
               dp.SetValue(dest ,null);
               continue;
            }

            // direkt zuweisbar?
            if (dp.PropertyType.IsAssignableFrom(sp.PropertyType))
            {
               dp.SetValue(dest ,sval);
               continue;
            }

            // rekursiv mappen (inkl. Konvertierung)
            var mapped = MapInternal(sval ,dp.PropertyType ,visited);
            dp.SetValue(dest ,mapped);
         }

         return dest;
      }

      private static bool TryMapEnumerable(object src ,Type sType ,Type destType ,Dictionary<object ,object> visited ,out object? result)
      {
         result = null;

         // Strings sind IEnumerable<char>, sollen aber als simple Typen behandelt werden
         if (sType == typeof(string) || destType == typeof(string))
         {
            return false;
         }

         // Ziel ist Array
         if (destType.IsArray && typeof(IEnumerable).IsAssignableFrom(sType))
         {
            var sElType = GetElementType(sType) ?? typeof(object);
            var dElType = destType.GetElementType() ?? typeof(object);

            var srcItems = ((IEnumerable)src).Cast<object?>();
            var list = new List<object?>();
            foreach (var item in srcItems)
            {
               list.Add(MapInternal(item ,dElType ,visited));
            }

            var arr = Array.CreateInstance(dElType ,list.Count);
            for (int i = 0; i < list.Count; i++)
            {
               arr.SetValue(list[i] ,i);
            }

            result = arr;
            return true;
         }

         // Ziel ist generische ICollection/IEnumerable (z. B. List<T>, ICollection<T>)
         if (IsGenericEnumerable(destType) && typeof(IEnumerable).IsAssignableFrom(sType))
         {
            var dElType = GetElementType(destType) ?? typeof(object);
            var listType = typeof(List<>).MakeGenericType(dElType);
            var list = (IList)Activator.CreateInstance(listType)!;

            foreach (var item in ((IEnumerable)src).Cast<object?>())
            {
               list.Add(MapInternal(item ,dElType ,visited));
            }

            // Wenn Ziel konkret instanziierbar & hat Add(T), versuche Zieltyp
            if (!destType.IsInterface && !destType.IsAbstract)
            {
               var target = Activator.CreateInstance(destType);
               var add = destType.GetMethod("Add" ,new[] { dElType });
               if (target != null && add != null)
               {
                  foreach (var it in list)
                  {
                     add.Invoke(target ,new[] { it });
                  }

                  result = target;
                  return true;
               }
            }

            result = list;
            return true;
         }

         return false;
      }

      // Helpers
      private static bool IsSimple(Type t)
          => t.IsPrimitive
             || t.IsEnum
             || t == typeof(string)
             || t == typeof(decimal)
             || t == typeof(DateTime)
             || t == typeof(DateTimeOffset)
             || t == typeof(Guid)
             || t == typeof(TimeSpan);

      private static object? ChangeType(object value ,Type destType)
      {
         if (value is null)
         {
            return null;
         }

         var vType = value.GetType();
         if (destType.IsAssignableFrom(vType))
         {
            return value;
         }

         if (destType.IsEnum)
         {
            return value is string s
                ? Enum.Parse(destType ,s ,true)
                : Enum.ToObject(destType ,System.Convert.ChangeType(value ,Enum.GetUnderlyingType(destType)));
         }

         if (destType == typeof(Guid))
         {
            return value is string gs ? Guid.Parse(gs) : new Guid(System.Convert.ToString(value)!);
         }

         if (destType == typeof(string))
         {
            return value.ToString();
         }

         return System.Convert.ChangeType(value ,Nullable.GetUnderlyingType(destType) ?? destType);
      }

      private static IEnumerable<PropertyInfo> GetReadableProps(Type t)
          => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
              .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

      private static IEnumerable<PropertyInfo> GetWritableProps(Type t)
          => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
              .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0);

      private static bool IsGenericEnumerable(Type t)
          => t != typeof(string) &&
             (t.IsArray ||
              (t.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(t.GetGenericTypeDefinition())) ||
              t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));

      private static Type? GetElementType(Type t)
      {
         if (t.IsArray)
         {
            return t.GetElementType();
         }

         var ienum = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)
             ? t
             : t.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
         return ienum?.GetGenericArguments().FirstOrDefault();
      }

      // Referenzgleichheit für visited-Map
      private sealed class ReferenceEqualityComparer:IEqualityComparer<object>
      {
         public static readonly ReferenceEqualityComparer Instance = new();
         public new bool Equals(object? x ,object? y) => ReferenceEquals(x ,y);
         public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
      }
   }
}
