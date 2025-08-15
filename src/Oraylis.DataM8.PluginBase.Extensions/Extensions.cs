﻿/* DataM8
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

using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Oraylis.DataM8.PluginBase.Extensions
{
   public static class Extensions
   {
      public static int? GetInt(this DataRow row ,string colName)
      {
         var objVal = row[colName];
         if (objVal is DBNull)
         {
            return null;
         }

         if (objVal is byte int8)
         {
            return Convert.ToInt32(int8);
         }

         if (objVal is Int16 int16)
         {
            return Convert.ToInt32(int16);
         }

         if (objVal is Int32 int32)
         {
            return Convert.ToInt32(int32);
         }

         if (objVal is Int64 int64)
         {
            return Convert.ToInt32(int64);
         }

         if (objVal is Decimal intDecimal)
         {
            return Convert.ToInt32(intDecimal);
         }

         throw new Exception($"Cannot convert column {colName} to data type int");
      }

      public static T? ConvertClass<T, U>(U source)
      {
         var settings = new JsonSerializerSettings
         {
            NullValueHandling = NullValueHandling.Ignore ,
            ContractResolver = new DefaultContractResolver
            {
               NamingStrategy = new CamelCaseNamingStrategy
               {
                  ProcessDictionaryKeys = false ,
                  OverrideSpecifiedNames = false
               }
            } ,
            Converters =
            {
               new StringEnumConverter()
            }
         };

         string tmpStr = JsonConvert.SerializeObject(source ,Formatting.Indented ,settings);
         var x = JsonConvert.DeserializeObject<T>(tmpStr);
         return x;
      }

      public static List<string> GetFriendlyNames(this Enum enm)
      {
         List<string> result = new List<string>();
         result.AddRange(Enum.GetNames(enm.GetType()).Select(s => s.ToFriendlyName()));
         return result;
      }
      public static T ToEnum<T>(this string value)
      {
         return (T)Enum.Parse(typeof(T) ,value ,true);
      }

      public static string? GetFriendlyName(this Enum enm)
      {
         return Enum.GetName(enm.GetType() ,enm)?.ToFriendlyName();
      }

      private static string ToFriendlyName(this string orig)
      {
         return orig.Replace("_" ," ");
      }
      public static string FromCamelCase(this string orig)
      {
         string retVal = "";
         bool lastIsUpper = false;
         for (int i = 0; i < orig.Length; i++)
         {
            string part = orig.Substring(i ,1);

            if (part.Equals(part.ToUpper()))
            {
               if (!lastIsUpper)
               {
                  lastIsUpper = true;
                  if (!retVal.Equals(retVal.ToLower()))
                  {
                     retVal += " ";
                  }
               }
            }
            else
            {
               lastIsUpper = false;
            }
            retVal += part;
         }
         return (retVal.Trim());
      }
   }
}
