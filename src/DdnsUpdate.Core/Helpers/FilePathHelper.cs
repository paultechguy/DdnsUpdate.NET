// -------------------------------------------------------------------------
// <copyright file="FilePathHelper.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Helpers;

using System;

public static class FilePathHelper
{
   public const string CompanyName = "PaulTechGuy";

   public const string ApplicationName = "DdnsUpdate";

   public static string ApplicationDataDirectory => Path.Combine(
         Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
         CompanyName,
         ApplicationName);
}