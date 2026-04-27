using HarmonyLib;
using static TransferManager;
using TransferManagerCore;

namespace TransferManagerCore
{
    [HarmonyPatch]
    public class TransferManagerGetTransferReason1
    {
        // We override the transfer reason for the police helicopter depot to support Crime2
        [HarmonyPatch(typeof(HelicopterDepotAI), "GetTransferReason1")]
        [HarmonyPostfix]
        public static void GetTransferReason1(HelicopterDepotAI __instance, ref TransferReason __result)
        {
            if (SaveGameSettings.GetSettings().EnableNewTransferManager)
            {
                switch (__instance.m_info.GetService())
                {
                    case ItemClass.Service.PoliceDepartment:
                        {
                            __result = (TransferReason)CustomTransferReason.Reason.Crime2;
                            break;
                        }
                }
            }
        }
    }
}