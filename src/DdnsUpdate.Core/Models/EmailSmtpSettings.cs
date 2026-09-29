// -------------------------------------------------------------------------
// <copyright file="EmailSmtpSettings.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Models;

/// <summary>
/// The SMTP server used to send notification email (<c>applicationSettings.emailSmtpSettings</c>).
/// </summary>
public class EmailSmtpSettings
{
   /// <summary>
   /// The name of this section within <c>applicationSettings</c>.
   /// </summary>
   public const string ConfigurationName = "EmailSmtpSettings";

   /// <summary>
   /// Gets or sets the SMTP server host name (e.g. smtp.gmail.com, or localhost for Papercut-SMTP).
   /// </summary>
   public string? SmtpHost { get; set; }

   /// <summary>
   /// Gets or sets the SMTP server port (typically 587 with TLS, or 25 for a local test server).
   /// </summary>
   public int SmtpPort { get; set; }

   /// <summary>
   /// Gets or sets a value indicating whether the connection uses SSL/TLS.
   /// </summary>
   public bool SmtpEnableSsl { get; set; }

   /// <summary>
   /// Gets or sets the SMTP user name; leave empty for a server that needs no authentication.
   /// </summary>
   public string? SmtpUsername { get; set; }

   /// <summary>
   /// Gets or sets the SMTP password (for Gmail, an app password). Keep it in an
   /// <c>appsettings.{environment}.user.json</c> file, which is not committed.
   /// </summary>
   public string? SmtpPassword { get; set; }
}