// -------------------------------------------------------------------------
// <copyright file="IEmailSender.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Interfaces;

using System.Threading.Tasks;

/// <summary>
/// Sends email notifications (currently used for IP address change alerts).
/// </summary>
public interface IEmailSender
{
   /// <summary>
   /// Sends a plain-text email.
   /// </summary>
   /// <param name="from">The sender address; either "address" or "Display Name &lt;address&gt;".</param>
   /// <param name="to">The recipient address, in the same formats as <paramref name="from"/>.</param>
   /// <param name="subject">The subject line.</param>
   /// <param name="body">The plain-text body.</param>
   /// <returns>A <see cref="Task"/> that completes when the message has been handed to the server.</returns>
   Task SendPlainAsync(
      string from,
      string to,
      string subject,
      string body);

   /// <summary>
   /// Sends an HTML email.
   /// </summary>
   /// <param name="from">The sender address; either "address" or "Display Name &lt;address&gt;".</param>
   /// <param name="to">The recipient address, in the same formats as <paramref name="from"/>.</param>
   /// <param name="subject">The subject line.</param>
   /// <param name="body">The HTML body.</param>
   /// <returns>A <see cref="Task"/> that completes when the message has been handed to the server.</returns>
   Task SendHtmlAsync(
      string from,
      string to,
      string subject,
      string body);

   /// <summary>
   /// Sends an email with any combination of plain-text and HTML bodies.
   /// </summary>
   /// <param name="from">The sender address; either "address" or "Display Name &lt;address&gt;".</param>
   /// <param name="to">The recipient address, in the same formats as <paramref name="from"/>.</param>
   /// <param name="subject">The subject line.</param>
   /// <param name="replyTo">An optional Reply-To address.</param>
   /// <param name="bodyText">An optional plain-text body.</param>
   /// <param name="bodyHtml">An optional HTML body, sent as an alternate view.</param>
   /// <returns>A <see cref="Task"/> that completes when the message has been handed to the server.</returns>
   Task SendAsync(
      string from,
      string to,
      string subject,
      string? replyTo = null,
      string? bodyText = null,
      string? bodyHtml = null);
}