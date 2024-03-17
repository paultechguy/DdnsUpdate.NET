// -------------------------------------------------------------------------
// <copyright file="IIpAddressMonitorService.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Interfaces;

using System.Threading.Tasks;

public interface IIpAddressMonitorService
{
   Task ExecuteAsync(CancellationToken cancelToken);
}