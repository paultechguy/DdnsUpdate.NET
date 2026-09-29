// -------------------------------------------------------------------------
// <copyright file="EmailSender.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Email;

using System.Net;
using System.Net.Mail;
using System.Text;
using DdnsUpdate.Core.Interfaces;
using DdnsUpdate.Core.Models;
using Microsoft.Extensions.Options;

/// <summary>
/// An <see cref="IEmailSender"/> that sends through the SMTP server configured in
/// <c>applicationSettings.emailSmtpSettings</c>.
/// </summary>
public class EmailSender(
   IOptions<ApplicationSettings> appSettings)
   : IEmailSender
{
   // note: IOptions is read once, so SMTP settings changes need an application restart
   private readonly ApplicationSettings appSettings = appSettings.Value;

   /// <inheritdoc/>
   public async Task SendPlainAsync(
      string from,
      string to,
      string subject,
      string body)
   {
      await this.SendAsync(from, to, subject, replyTo: null, bodyText: body, bodyHtml: null);
   }

   /// <inheritdoc/>
   public async Task SendHtmlAsync(
      string from,
      string to,
      string subject,
      string body)
   {
      await this.SendAsync(from, to, subject, replyTo: null, bodyText: null, bodyHtml: body);
   }

   /// <inheritdoc/>
   public async Task SendAsync(
      string from,
      string to,
      string subject,
      string? replyTo = null,
      string? bodyText = null,
      string? bodyHtml = null)
   {
      using var message = new MailMessage();
      message.SubjectEncoding = System.Text.Encoding.UTF8;
      message.From = new MailAddress(from ?? throw new ArgumentNullException(nameof(from)));
      message.To.Add(to ?? throw new ArgumentNullException(nameof(to)));
      message.Body = bodyText ?? string.Empty; // blank body text if none provided
      message.Subject = subject ?? throw new ArgumentNullException(nameof(subject));

      // do we have a reply to email address
      if (replyTo is not null)
      {
         message.ReplyToList.Add(replyTo);
      }

      // the HTML goes in an alternate view so clients that can't render HTML fall back to Body
      if (bodyHtml is not null)
      {
         var htmlView = AlternateView.CreateAlternateViewFromString(bodyHtml, Encoding.UTF8, "text/html");
         message.AlternateViews.Add(htmlView);
      }

      using var client = new SmtpClient(this.appSettings.EmailSmtpSettings.SmtpHost, this.appSettings.EmailSmtpSettings.SmtpPort);
      client.EnableSsl = this.appSettings.EmailSmtpSettings.SmtpEnableSsl;

      // no username means an unauthenticated server (e.g. Papercut-SMTP on localhost)
      client.UseDefaultCredentials = string.IsNullOrWhiteSpace(this.appSettings.EmailSmtpSettings.SmtpUsername);
      if (!client.UseDefaultCredentials)
      {
         client.Credentials = new NetworkCredential(this.appSettings.EmailSmtpSettings.SmtpUsername, this.appSettings.EmailSmtpSettings.SmtpPassword);
      }

      await client.SendMailAsync(message);
   }
}