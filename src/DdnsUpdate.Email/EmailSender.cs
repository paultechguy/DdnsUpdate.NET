// -------------------------------------------------------------------------
// <copyright file="EmailSender.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Email;

using DdnsUpdate.Core.Interfaces;
using DdnsUpdate.Core.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

/// <summary>
/// An <see cref="IEmailSender"/> that sends through the SMTP server configured in
/// <c>applicationSettings.emailSmtpSettings</c>, using MailKit.
/// </summary>
/// <remarks>
/// MailKit replaces System.Net.Mail.SmtpClient, which Microsoft no longer recommends because it
/// does not support modern protocols such as implicit TLS on port 465.
/// </remarks>
public sealed class EmailSender(
   IOptionsMonitor<ApplicationSettings> appSettingsMonitor)
   : IEmailSender
{
   // the implicit-TLS SMTP port; every other port negotiates TLS with STARTTLS
   private const int ImplicitTlsPort = 465;

   // a monitor rather than IOptions so SMTP settings edits apply without a restart
   private readonly IOptionsMonitor<ApplicationSettings> appSettingsMonitor = appSettingsMonitor;

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
      ArgumentNullException.ThrowIfNull(from);
      ArgumentNullException.ThrowIfNull(to);
      ArgumentNullException.ThrowIfNull(subject);

      using var message = new MimeMessage();

      // Parse accepts both "user@example.com" and "Display Name <user@example.com>"
      message.From.Add(MailboxAddress.Parse(from));
      message.To.Add(MailboxAddress.Parse(to));
      message.Subject = subject;

      // do we have a reply to email address
      if (replyTo is not null)
      {
         message.ReplyTo.Add(MailboxAddress.Parse(replyTo));
      }

      // with both bodies, clients that can't render HTML fall back to the text
      var bodyBuilder = new BodyBuilder
      {
         TextBody = bodyText ?? (bodyHtml is null ? string.Empty : null),
         HtmlBody = bodyHtml,
      };
      message.Body = bodyBuilder.ToMessageBody();

      EmailSmtpSettings smtp = this.appSettingsMonitor.CurrentValue.EmailSmtpSettings;

      // smtpEnableSsl keeps its SmtpClient meaning (STARTTLS) and adds implicit TLS on port 465;
      // with it off the connection is unencrypted, as before
      SecureSocketOptions socketOptions = !smtp.SmtpEnableSsl
         ? SecureSocketOptions.None
         : smtp.SmtpPort == ImplicitTlsPort
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

      using var client = new SmtpClient();
      string host = string.IsNullOrWhiteSpace(smtp.SmtpHost)
         ? throw new InvalidOperationException("applicationSettings.emailSmtpSettings.smtpHost is empty")
         : smtp.SmtpHost;
      await client.ConnectAsync(host, smtp.SmtpPort, socketOptions);

      // no username means an unauthenticated server (e.g. Papercut-SMTP on localhost)
      if (!string.IsNullOrWhiteSpace(smtp.SmtpUsername))
      {
         await client.AuthenticateAsync(smtp.SmtpUsername, smtp.SmtpPassword ?? string.Empty);
      }

      _ = await client.SendAsync(message);
      await client.DisconnectAsync(quit: true);
   }
}