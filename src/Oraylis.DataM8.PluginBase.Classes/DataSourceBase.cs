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

using Oraylis.DataM8.PluginBase.Interfaces;

namespace Oraylis.DataM8.PluginBase.BaseClasses
{
   public partial class DataSourceBase:IDataSourceBase
   {
      public string Name { get; set; } = "";
      public string DisplayName { get; set; } = "";
      public string Purpose { get; set; } = "";
      public string ConnectionString { get; set; } = "";
      public Dictionary<string ,string> ExtendedProperties { get; set; } = new Dictionary<string ,string>();
      public bool RealConnectionString { get; set; } = false;
      public bool Validate(bool showMessage)
      {
         return false;
      }
      public bool Connect(string connectionString)
      {
         throw new NotImplementedException();
      }

   }
}
