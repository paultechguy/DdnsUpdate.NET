// -------------------------------------------------------------------------
// <copyright file="Fakes.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Service.Tests;

using System.Collections.Concurrent;
using DdnsUpdate.Core.Interfaces;
using DdnsUpdate.Core.Models;
using DdnsUpdate.DdnsProvider.Interfaces;
using DdnsUpdate.DdnsProvider.Models;

/// <summary>
/// A DDNS provider that records the updates it is asked to make.
/// </summary>
internal sealed class FakeDdnsProvider : IDdnsUpdateProvider
{
   public List<string> Domains { get; } = [];

   public HashSet<string> InvalidDomains { get; } = [];

   public HashSet<string> FailingDomains { get; } = [];

   public ConcurrentQueue<(string Domain, string IpAddress)> Updates { get; } = new();

   public int GetDomainNamesCallCount { get; private set; }

   public string ProviderName => "Fake";

   public Task<List<string>> GetDomainNamesAsync()
   {
      this.GetDomainNamesCallCount++;
      return Task.FromResult(this.Domains.ToList());
   }

   public Task<DdnsProviderSuccessResult> IsDomainValidAsync(string domainName)
   {
      bool valid = !this.InvalidDomains.Contains(domainName);
      return Task.FromResult(new DdnsProviderSuccessResult { IsSuccess = valid, Message = valid ? string.Empty : "invalid" });
   }

   public Task<DdnsProviderSuccessResult> TryUpdateIpAddressAsync(string domainName, string ipAddress, CancellationToken cancelToken = default)
   {
      this.Updates.Enqueue((domainName, ipAddress));
      bool success = !this.FailingDomains.Contains(domainName);
      return Task.FromResult(new DdnsProviderSuccessResult { IsSuccess = success, Message = success ? string.Empty : "failed" });
   }
}

/// <summary>
/// An in-memory <see cref="IDdnsStateStore"/>.
/// </summary>
internal sealed class InMemoryStateStore : IDdnsStateStore
{
   public string LastIpAddress { get; set; } = string.Empty;

   public UriStatistics? SavedStatistics { get; private set; }

   public string LoadLastIpAddress()
   {
      return this.LastIpAddress;
   }

   public void SaveLastIpAddress(string ipAddress)
   {
      this.LastIpAddress = ipAddress;
   }

   public Task<UriStatistics> LoadUriStatisticsAsync()
   {
      return Task.FromResult(this.SavedStatistics ?? []);
   }

   public Task SaveUriStatisticsAsync(UriStatistics statistics)
   {
      this.SavedStatistics = statistics;
      return Task.CompletedTask;
   }
}

/// <summary>
/// An <see cref="IEmailSender"/> that records messages instead of sending them.
/// </summary>
internal sealed class RecordingEmailSender : IEmailSender
{
   public List<(string From, string To, string Subject, string? ReplyTo, string? BodyHtml)> Sent { get; } = [];

   public Task SendPlainAsync(string from, string to, string subject, string body)
   {
      return this.SendAsync(from, to, subject, bodyText: body);
   }

   public Task SendHtmlAsync(string from, string to, string subject, string body)
   {
      return this.SendAsync(from, to, subject, bodyHtml: body);
   }

   public Task SendAsync(string from, string to, string subject, string? replyTo = null, string? bodyText = null, string? bodyHtml = null)
   {
      this.Sent.Add((from, to, subject, replyTo, bodyHtml));
      return Task.CompletedTask;
   }
}