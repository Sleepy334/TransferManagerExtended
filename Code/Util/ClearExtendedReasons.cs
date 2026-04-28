using ColossalFramework;
using SleepyCommon;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TransferManagerCore
{
    public class ClearExtendedReasons
    {
        // --------------------------------------------------------------------
        public static void Clear()
        {
            Vehicle[] Vehicles = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;

            HashSet<ushort> extendedVehicles = new HashSet<ushort>();
            for (int i = 0; i < Vehicles.Length; ++i)
            {
                Vehicle v = Vehicles[i];
                if (v.m_transferType > TransferManager.TRANSFER_REASON_COUNT)
                {
                    extendedVehicles.Add((ushort) i);
                }
            }

            // Perform actual de-spawning
            foreach (ushort vehicleId in extendedVehicles)
            {
                ref Vehicle vehicle = ref Vehicles[vehicleId];
                if (vehicle.m_flags != 0)
                {
                    Singleton<VehicleManager>.instance.ReleaseVehicle(vehicleId);
                }
            }
        }
    }
}