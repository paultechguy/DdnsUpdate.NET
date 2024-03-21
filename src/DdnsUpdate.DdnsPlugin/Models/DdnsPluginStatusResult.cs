// <copyright file="DdnsPluginStatusResult.cs" company="PaulTechGuy"
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>"

namespace DdnsUpdate.DdnsPlugin.Models;

/// <summary>
/// A class representing a genereralized result object with a success indicator
/// and a message.
/// </summary>
public class DdnsPluginStatusResult
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
   /// Gets a <see cref="DdnsPluginStatusResult"/> that indicates success.
   /// </summary>
   public static DdnsPluginStatusResult Success => new()
   {
      IsSuccess = true,
      Message = string.Empty,
   };

   /// <summary>
   /// Gets a <see cref="DdnsPluginStatusResult"/> that indicates fail.
   /// </summary>
   public static DdnsPluginStatusResult Fail => new()
   {
      IsSuccess = false,
      Message = "Unknown",
   };
}
