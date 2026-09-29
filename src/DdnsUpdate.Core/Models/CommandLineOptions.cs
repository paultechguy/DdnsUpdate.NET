// -------------------------------------------------------------------------
// <copyright file="CommandLineOptions.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Models;

using CommandLine;

/// <summary>
/// Command-line options parsed by CommandLineParser and registered for dependency injection.
/// The parser also provides --help and --version.
/// </summary>
/// <remarks>
/// The parser rejects unknown arguments and the application then exits with code 1 without
/// running, so any new argument must be declared here first.
/// </remarks>
public class CommandLineOptions
{
   /// <summary>
   /// Gets or sets a value indicating whether to preview a single pass without changing anything.
   /// </summary>
   /// <remarks>
   /// A dry run detects the external IP address and reads each enabled domain's DNS record from
   /// the provider (proving the credentials and IDs work), then reports what a real run would
   /// do. It never updates DNS, saves the last IP address or provider statistics, or sends
   /// email. The process exits with 1 if any enabled domain has a problem.
   /// </remarks>
   [Option("dry-run", HelpText = "Preview one pass: detect the IP, check every enabled domain's DNS record, and report what would change. Changes nothing.")]
   public bool DryRun { get; set; }

   /// <summary>
   /// Gets or sets a value indicating whether to run a single real pass and exit, whatever
   /// <c>maximumDdnsUpdateIterations</c> says.
   /// </summary>
   [Option("once", HelpText = "Run one real update pass, then exit.")]
   public bool Once { get; set; }
}