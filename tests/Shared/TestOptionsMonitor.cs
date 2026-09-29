// -------------------------------------------------------------------------
// <copyright file="TestOptionsMonitor.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Tests.Shared;

using Microsoft.Extensions.Options;

/// <summary>
/// An <see cref="IOptionsMonitor{TOptions}"/> whose value a test sets directly.
/// </summary>
/// <typeparam name="TOptions">The options type.</typeparam>
public sealed class TestOptionsMonitor<TOptions>(TOptions value) : IOptionsMonitor<TOptions>
{
   /// <inheritdoc/>
   public TOptions CurrentValue { get; set; } = value;

   /// <inheritdoc/>
   public TOptions Get(string? name)
   {
      return this.CurrentValue;
   }

   /// <inheritdoc/>
   public IDisposable? OnChange(Action<TOptions, string?> listener)
   {
      return null;
   }
}