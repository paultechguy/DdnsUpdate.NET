// -------------------------------------------------------------------------
// <copyright file="StubHttpClientFactory.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Tests.Shared;

/// <summary>
/// An <see cref="IHttpClientFactory"/> whose clients all send through one handler.
/// </summary>
public sealed class StubHttpClientFactory(HttpMessageHandler handler, Uri? baseAddress = null) : IHttpClientFactory
{
   private readonly HttpMessageHandler handler = handler;
   private readonly Uri? baseAddress = baseAddress;

   /// <inheritdoc/>
   public HttpClient CreateClient(string name)
   {
      // the factory, not the client, owns the handler
      return new HttpClient(this.handler, disposeHandler: false) { BaseAddress = this.baseAddress };
   }
}