// "// <copyright file="IIpAddressMonitor.cs\" company="PaulTechGuy"
// // Copyright (c) Paul Carver. All rights reserved.
// // </copyright>"

namespace DdnsUpdate.Core.Interfaces;

using System.Threading.Tasks;

public interface IIpAddressMonitorService
{
   Task ExecuteAsync(CancellationToken cancelToken);
}
