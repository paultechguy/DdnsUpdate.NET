// "// <copyright file=\"IEmailSender.cs\" company=\"PaulTechGuy\">
// // Copyright (c) Paul Carver. All rights reserved.
// // </copyright>"

namespace DdnsUpdate.Core.Interfaces;

using System.Threading.Tasks;

public interface IEmailSender
{
   Task SendPlainAsync(
      string from,
      string to,
      string subject,
      string body);

   Task SendHtmlAsync(
      string from,
      string to,
      string subject,
      string body);

   Task SendAsync(
      string from,
      string to,
      string subject,
      string? replyTo = null,
      string? bodyText = null,
      string? bodyHtml = null);
}
