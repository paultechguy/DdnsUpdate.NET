// -------------------------------------------------------------------------
// <copyright file="FakeHttpMessageHandler.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Tests.Shared;

using System.Collections.Concurrent;
using System.Net;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that answers requests from a test-supplied function and
/// records each request (including its body, captured before the content is disposed).
/// </summary>
public sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
   : HttpMessageHandler
{
   private readonly Func<HttpRequestMessage, HttpResponseMessage> responder = responder;

   /// <summary>
   /// Gets the requests received, in order.
   /// </summary>
   public ConcurrentQueue<RecordedRequest> Requests { get; } = new();

   /// <summary>
   /// Creates a response with the given status and plain-text body.
   /// </summary>
   /// <param name="status">The status code.</param>
   /// <param name="body">The response body.</param>
   /// <returns>The response.</returns>
   public static HttpResponseMessage Text(HttpStatusCode status, string body)
   {
      return new HttpResponseMessage(status) { Content = new StringContent(body) };
   }

   /// <inheritdoc/>
   protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
   {
      string? body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
      this.Requests.Enqueue(new RecordedRequest(request.Method, request.RequestUri!, request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value)), body));

      return this.responder(request);
   }

   /// <summary>
   /// A request as seen by the handler.
   /// </summary>
   /// <param name="Method">The HTTP method.</param>
   /// <param name="Uri">The absolute request URI.</param>
   /// <param name="Headers">The request headers (multiple values comma-joined).</param>
   /// <param name="Body">The request body, or null if there was none.</param>
   public sealed record RecordedRequest(HttpMethod Method, Uri Uri, Dictionary<string, string> Headers, string? Body);
}