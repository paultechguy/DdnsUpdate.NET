// -------------------------------------------------------------------------
// <copyright file="CtrlCHelper.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Application.Helpers;

public static class CtrlCHelper
{
   public static void ConfigureCtrlCHandler(ConsoleCancelEventHandler handler)
   {
      Console.CancelKeyPress += handler;
   }
}