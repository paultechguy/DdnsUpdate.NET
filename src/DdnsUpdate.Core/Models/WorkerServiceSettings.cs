// -------------------------------------------------------------------------
// <copyright file="WorkerServiceSettings.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Models;

public class WorkerServiceSettings
{
   public const string ConfigurationName = "WorkerServiceSettings";

   // email send

   public bool MessageIsEnabled { get; set; } = false;

   public string? MessageToEmailAddress { get; set; } = string.Empty;

   public string? MessageFromEmailAddress { get; set; } = string.Empty;

   public string? MessageReplyToEmailAddress { get; set; } = string.Empty;

   public WorkerServiceSettings()
   {
   }
}