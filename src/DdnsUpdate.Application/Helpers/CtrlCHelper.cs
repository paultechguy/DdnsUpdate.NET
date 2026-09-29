// -------------------------------------------------------------------------
// <copyright file="CtrlCHelper.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Application.Helpers;

/// <summary>
/// Hooks Ctrl-C handling for interactive (console) runs.
/// </summary>
public static class CtrlCHelper
{
   /// <summary>
   /// Registers a handler for Ctrl-C and Ctrl-Break.
   /// </summary>
   /// <param name="handler">The handler; set <see cref="ConsoleCancelEventArgs.Cancel"/> to true
   /// to keep the process alive for a graceful shutdown.</param>
   public static void ConfigureCtrlCHandler(ConsoleCancelEventHandler handler)
   {
      Console.CancelKeyPress += handler;
   }
}