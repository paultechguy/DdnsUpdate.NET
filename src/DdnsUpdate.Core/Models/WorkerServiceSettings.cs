// -------------------------------------------------------------------------
// <copyright file="WorkerServiceSettings.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Models;

/// <summary>
/// Notification email options for IP address changes
/// (<c>applicationSettings.workerServiceSettings</c>).
/// </summary>
/// <remarks>
/// Addresses may be plain ("user@example.com") or include a display name
/// ("Display Name &lt;user@example.com&gt;").
/// </remarks>
public class WorkerServiceSettings
{
   /// <summary>
   /// The name of this section within <c>applicationSettings</c>.
   /// </summary>
   public const string ConfigurationName = "WorkerServiceSettings";

   /// <summary>
   /// Gets or sets a value indicating whether an email is sent when the IP address changes.
   /// </summary>
   public bool MessageIsEnabled { get; set; } = false;

   /// <summary>
   /// Gets or sets the recipient address; required when email is enabled.
   /// </summary>
   public string? MessageToEmailAddress { get; set; } = string.Empty;

   /// <summary>
   /// Gets or sets the sender address; required when email is enabled.
   /// </summary>
   public string? MessageFromEmailAddress { get; set; } = string.Empty;

   /// <summary>
   /// Gets or sets an optional Reply-To address.
   /// </summary>
   public string? MessageReplyToEmailAddress { get; set; } = string.Empty;

   /// <summary>
   /// Initializes a new instance of the <see cref="WorkerServiceSettings"/> class.
   /// </summary>
   public WorkerServiceSettings()
   {
   }
}