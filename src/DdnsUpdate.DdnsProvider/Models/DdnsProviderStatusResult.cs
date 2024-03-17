// "// <copyright file=\"DdnsProviderStatusResult.cs\" company=\"PaulTechGuy\">
// // Copyright (c) Paul Carver. All rights reserved.
// // </copyright>"

namespace DdnsUpdate.DdnsProvider.Models;

/// <summary>
/// A class representing a genereralized result object with a success indicator
/// and a message.
/// </summary>
public class DdnsProviderStatusResult
{
   /// <summary>
   /// Gets or sets the a value indicating whether the result is successful.
   /// </summary>
   public bool IsSuccess { get; set; } = false;

   /// <summary>
   /// Gets or sets the message of the result.  Genrally this is empty if the
   /// status is successful.
   /// </summary>
   public string Message { get; set; } = string.Empty;

   /// <summary>
   /// Gets a <see cref="DdnsProviderStatusResult"/> that indicates success.
   /// </summary>
   public static DdnsProviderStatusResult Success => new()
   {
      IsSuccess = true,
      Message = string.Empty,
   };

   /// <summary>
   /// Gets a <see cref="DdnsProviderStatusResult"/> that indicates fail.
   /// </summary>
   public static DdnsProviderStatusResult Fail => new()
   {
      IsSuccess = false,
      Message = "Unknown",
   };
}