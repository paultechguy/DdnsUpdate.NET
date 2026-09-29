// -------------------------------------------------------------------------
// <copyright file="WorkerServiceTests.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Service.Tests;

using System.Net;
using DdnsUpdate.Core.Models;
using DdnsUpdate.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

public sealed class WorkerServiceTests
{
   private const string Provider1 = "https://ip1.example/";
   private const string Provider2 = "https://ip2.example/";
   private const string CurrentIp = "203.0.113.7";

   private readonly ApplicationSettings settings = new()
   {
      DdnsSettings = new DdnsSettings
      {
         AfterAllDdnsUpdatePauseMinutes = 60,
         MaximumDdnsUpdateIterations = 1,
         AlwaysUpdateDdnsEvenIfUnchanged = false,
         ParallelDdnsUpdateCount = 0,

         // deterministic provider order: Provider1 first
         RandomizeIpAddressProviderSelecion = false,
         IpAddressProviders = [Provider1, Provider2],
      },
   };

   private readonly FakeDdnsProvider ddnsProvider = new() { Domains = { "a.example.com", "b.example.com" } };
   private readonly InMemoryStateStore stateStore = new();
   private readonly RecordingEmailSender emailSender = new();
   private readonly FakeTimeProvider timeProvider = new();
   private readonly Dictionary<string, Func<HttpResponseMessage>> ipResponses = new()
   {
      [Provider1] = () => FakeHttpMessageHandler.Text(HttpStatusCode.OK, CurrentIp + "\n"),
      [Provider2] = () => FakeHttpMessageHandler.Text(HttpStatusCode.OK, CurrentIp),
   };

   private readonly FakeHttpMessageHandler ipHandler;

   public WorkerServiceTests()
   {
      this.ipHandler = new FakeHttpMessageHandler(request => this.ipResponses[request.RequestUri!.ToString()]());
   }

   [Fact]
   public async Task ChangedIp_UpdatesEveryDomainAndSavesIp()
   {
      this.stateStore.LastIpAddress = "198.51.100.1";

      await this.CreateWorker().ExecuteAsync(TestContext.Current.CancellationToken);

      Assert.Equal(
         ["a.example.com", "b.example.com"],
         this.ddnsProvider.Updates.Select(u => u.Domain).Order());
      Assert.All(this.ddnsProvider.Updates, u => Assert.Equal(CurrentIp, u.IpAddress));
      Assert.Equal(CurrentIp, this.stateStore.LastIpAddress);
   }

   [Fact]
   public async Task UnchangedIp_SkipsUpdates()
   {
      this.stateStore.LastIpAddress = CurrentIp;

      await this.CreateWorker().ExecuteAsync(TestContext.Current.CancellationToken);

      Assert.Empty(this.ddnsProvider.Updates);
   }

   [Fact]
   public async Task UnchangedIp_WithAlwaysUpdate_UpdatesAnyway()
   {
      this.stateStore.LastIpAddress = CurrentIp;
      this.settings.DdnsSettings.AlwaysUpdateDdnsEvenIfUnchanged = true;

      await this.CreateWorker().ExecuteAsync(TestContext.Current.CancellationToken);

      Assert.Equal(2, this.ddnsProvider.Updates.Count);
   }

   [Fact]
   public async Task NoEnabledDomains_DoesNotLookUpIp()
   {
      this.ddnsProvider.Domains.Clear();

      await this.CreateWorker().ExecuteAsync(TestContext.Current.CancellationToken);

      Assert.Empty(this.ipHandler.Requests);
      Assert.Equal(string.Empty, this.stateStore.LastIpAddress);
   }

   [Fact]
   public async Task InvalidDomain_IsSkippedButOthersAreUpdated()
   {
      this.ddnsProvider.InvalidDomains.Add("a.example.com");

      await this.CreateWorker().ExecuteAsync(TestContext.Current.CancellationToken);

      (string domain, _) = Assert.Single(this.ddnsProvider.Updates);
      Assert.Equal("b.example.com", domain);
   }

   [Fact]
   public async Task FailingProvider_FallsBackToNextAndRecordsStatistics()
   {
      this.ipResponses[Provider1] = () => FakeHttpMessageHandler.Text(HttpStatusCode.InternalServerError, "oops");
      this.ipResponses[Provider2] = () => FakeHttpMessageHandler.Text(HttpStatusCode.OK, $"<html>Your IP is {CurrentIp}</html>");

      await this.CreateWorker().ExecuteAsync(TestContext.Current.CancellationToken);

      Assert.Equal(CurrentIp, this.stateStore.LastIpAddress);
      Assert.NotNull(this.stateStore.SavedStatistics);
      UriStatisticItem first = this.stateStore.SavedStatistics.Single(s => s.Uri == new Uri(Provider1));
      UriStatisticItem second = this.stateStore.SavedStatistics.Single(s => s.Uri == new Uri(Provider2));
      Assert.Equal((0, 1), (first.SuccessCount, first.FailCount));
      Assert.Equal((1, 0), (second.SuccessCount, second.FailCount));
   }

   [Fact]
   public async Task NoProviderReturnsValidIp_UpdatesNothing()
   {
      // the regex matches this dotted quad, but it is not a valid IPv4 address
      this.ipResponses[Provider1] = () => FakeHttpMessageHandler.Text(HttpStatusCode.OK, "999.1.1.1");
      this.ipResponses[Provider2] = () => FakeHttpMessageHandler.Text(HttpStatusCode.OK, "no address here");

      await this.CreateWorker().ExecuteAsync(TestContext.Current.CancellationToken);

      Assert.Empty(this.ddnsProvider.Updates);
      Assert.Equal(string.Empty, this.stateStore.LastIpAddress);
   }

   [Fact]
   public async Task ChangedIp_WithEmailEnabled_SendsNotification()
   {
      this.stateStore.LastIpAddress = "198.51.100.1";
      this.settings.WorkerServiceSettings = new WorkerServiceSettings
      {
         MessageIsEnabled = true,
         MessageFromEmailAddress = "DDNS <ddns@example.com>",
         MessageToEmailAddress = "me@example.com",
         MessageReplyToEmailAddress = "replies@example.com",
      };

      await this.CreateWorker().ExecuteAsync(TestContext.Current.CancellationToken);

      var sent = Assert.Single(this.emailSender.Sent);
      Assert.Equal("DDNS <ddns@example.com>", sent.From);
      Assert.Equal("me@example.com", sent.To);
      Assert.Equal("replies@example.com", sent.ReplyTo);
      Assert.Contains("198.51.100.1", sent.BodyHtml);
      Assert.Contains(CurrentIp, sent.BodyHtml);
   }

   [Fact]
   public async Task ChangedIp_WithEmailDisabled_SendsNothing()
   {
      await this.CreateWorker().ExecuteAsync(TestContext.Current.CancellationToken);

      Assert.Empty(this.emailSender.Sent);
   }

   [Fact]
   public async Task MaximumIterations_RunsThatManyPassesWaitingInBetween()
   {
      this.settings.DdnsSettings.MaximumDdnsUpdateIterations = 3;

      Task run = this.CreateWorker().ExecuteAsync(TestContext.Current.CancellationToken);
      await this.AdvanceUntilCompletedAsync(run);

      Assert.Equal(3, this.ddnsProvider.GetDomainNamesCallCount);
   }

   [Fact]
   public async Task Cancellation_DuringPause_EndsTheLoop()
   {
      this.settings.DdnsSettings.MaximumDdnsUpdateIterations = 0; // run until canceled
      using var cancelSource = new CancellationTokenSource();

      Task run = this.CreateWorker().ExecuteAsync(cancelSource.Token);
      await WaitUntilAsync(() => this.ddnsProvider.GetDomainNamesCallCount >= 1);
      await cancelSource.CancelAsync();

      await run.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
      Assert.Equal(1, this.ddnsProvider.GetDomainNamesCallCount);
   }

   private static async Task WaitUntilAsync(Func<bool> condition)
   {
      DateTime deadline = DateTime.UtcNow.AddSeconds(10);
      while (!condition())
      {
         Assert.True(DateTime.UtcNow < deadline, "Timed out waiting for the condition");
         await Task.Delay(10, TestContext.Current.CancellationToken);
      }
   }

   private WorkerService CreateWorker()
   {
      return new WorkerService(
         new TestOptionsMonitor<ApplicationSettings>(this.settings),
         NullLogger<WorkerService>.Instance,
         this.emailSender,
         new StubHttpClientFactory(this.ipHandler),
         this.ddnsProvider,
         this.stateStore,
         this.timeProvider);
   }

   // the fake clock only moves when told to; keep nudging it past each pause until the run ends
   private async Task AdvanceUntilCompletedAsync(Task run)
   {
      DateTime deadline = DateTime.UtcNow.AddSeconds(10);
      while (!run.IsCompleted)
      {
         Assert.True(DateTime.UtcNow < deadline, "Timed out waiting for the worker to finish");
         this.timeProvider.Advance(TimeSpan.FromMinutes(this.settings.DdnsSettings.AfterAllDdnsUpdatePauseMinutes));
         await Task.Delay(10, TestContext.Current.CancellationToken);
      }

      await run;
   }
}