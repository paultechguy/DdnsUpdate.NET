// -------------------------------------------------------------------------
// <copyright file="IEmailSender.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

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