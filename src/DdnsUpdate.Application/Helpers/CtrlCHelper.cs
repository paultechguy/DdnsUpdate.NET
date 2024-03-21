// "// <copyright file="CtrlCHelper.cs\" company="PaulTechGuy"
// // Copyright (c) Paul Carver. All rights reserved.
// // </copyright>"

namespace DdnsUpdate.Application.Helpers;

public static class CtrlCHelper
{
   public static void ConfigureCtrlCHandler(ConsoleCancelEventHandler handler)
   {
      Console.CancelKeyPress += handler;
   }
}
