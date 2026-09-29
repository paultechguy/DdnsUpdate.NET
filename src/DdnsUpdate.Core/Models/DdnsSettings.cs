// -------------------------------------------------------------------------
// <copyright file="DdnsSettings.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Models;

/// <summary>
/// Settings that control the DDNS update loop (<c>applicationSettings.ddnsSettings</c>).
/// </summary>
public class DdnsSettings
{
   /// <summary>
   /// The name of this section within <c>applicationSettings</c>.
   /// </summary>
   public const string ConfigurationName = "DdnsSettings";

   /// <summary>
   /// Gets or sets the minutes to wait after each pass before checking the IP address again.
   /// Values of zero or less fall back to one minute.
   /// </summary>
   public int AfterAllDdnsUpdatePauseMinutes { get; set; }

   /// <summary>
   /// Gets or sets a value indicating whether DNS records are updated even when the external
   /// IP address has not changed since the last update. Useful to verify a new configuration.
   /// </summary>
   /// <remarks>
   /// Defaults to true in code, but the shipped appsettings files set it to false.
   /// </remarks>
   public bool AlwaysUpdateDdnsEvenIfUnchanged { get; set; } = true;

   /// <summary>
   /// Gets or sets the number of passes to run before exiting; zero runs until stopped.
   /// Use 1 when running from Windows Task Scheduler.
   /// </summary>
   public int MaximumDdnsUpdateIterations { get; set; } = 0;

   /// <summary>
   /// Gets or sets how many domains are updated concurrently: a negative value uses the .NET
   /// default, zero updates all enabled domains at once, and a positive value sets the limit.
   /// </summary>
   public int ParallelDdnsUpdateCount { get; set; } = 0;

   /// <summary>
   /// Gets or sets a value indicating whether each pass starts with a randomly chosen IP address
   /// provider, rather than always the first, to spread requests across providers.
   /// </summary>
   /// <remarks>
   /// The misspelling ("Selecion") is kept because it is the configuration key in existing
   /// settings files.
   /// </remarks>
   public bool RandomizeIpAddressProviderSelecion { get; set; } = true;

   /// <summary>
   /// Gets or sets the URLs of services that return the caller's external IP address as text.
   /// </summary>
   public string[] IpAddressProviders { get; set; }

   /// <summary>
   /// Initializes a new instance of the <see cref="DdnsSettings"/> class.
   /// </summary>
   public DdnsSettings()
   {
      this.IpAddressProviders = [];
   }
}