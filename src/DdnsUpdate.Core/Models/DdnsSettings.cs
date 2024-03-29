﻿// -------------------------------------------------------------------------
// <copyright file="DdnsSettings.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Models;

public class DdnsSettings
{
   public const string ConfigurationName = "DdnsSettings";

   public decimal AfterAllDdnsUpdatePauseMinutes { get; set; }

   public bool AlwaysUpdateDdnsEvenIfUnchanged { get; set; } = true;

   public int MaximumDdnsUpdateIterations { get; set; } = 0;

   public int ParallelDdnsUpdateCount { get; set; } = 0;

   public bool RandomizeIpAddressProviderSelecion { get; set; } = true;

   public string[] IpAddressProviders { get; set; }

   public DdnsSettings()
   {
      this.IpAddressProviders = [];
   }
}