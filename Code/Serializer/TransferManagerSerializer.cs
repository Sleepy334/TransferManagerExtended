using ICities;
using SleepyCommon;
using System;
using System.Reflection;
using TransferManagerCore;
using TransferManagerCore.Settings;

namespace TransferManagerExtended // Cannot be shared with TMCE
{
    public class TransferManagerSerializer : ISerializableDataExtension
    {
        // --------------------------------------------------------------------
        // TME Settings
        public const ushort DataFileVersion = 55;

        string[] TransferManagerExtendedIds = 
        [
            "TransferManagerExtended.VersionInfo",
            "TransferManagerExtended.GlobalSettings",
            "TransferManagerExtended.BuildingSettings",
            "TransferManagerExtended.OutsideSettings"
        ];

        string[] TransferManagerCEIds = 
        [
            "TransferManagerCE.VersionInfo",
            "TransferManagerCE.GlobalSettings",
            "TransferManagerCE.BuildingSettings",
            "TransferManagerCE.OutsideSettings"
        ];

        // TME Version 54 Data settings
        private const string TransferManagerExtendedDataID = "TransferManagerExtended";

        // Transfer Manager CE Settings 
        private const string TransferManagerCEDataID = "TransferManagerCE";

        // Extended transer reason value
        public const string TransferReasonID = "TransferManagerExtended.TransferReasons";

        public static TransferManagerSerializer? instance = null;
        private ISerializableData? m_serializableData = null;

        // --------------------------------------------------------------------
        public void OnCreated(ISerializableData serializedData)
        {
            instance = this;
            m_serializableData = serializedData;
        }

        // --------------------------------------------------------------------
        // From version 55 we save each settings object to its own tuple.
        public void OnSaveData()
        {
            try
            {
                if (m_serializableData is not null)
                {
                    // --------------------------------------------------------
                    // Version information
                    {
                        FastList<byte> Data = new FastList<byte>();

                        // Write out global data version first
                        StorageData.WriteUInt16(DataFileVersion, Data);

                        Version modVersion = Assembly.GetExecutingAssembly().GetName().Version;
                        StorageData.WriteInt32(modVersion.Major, Data);
                        StorageData.WriteInt32(modVersion.Minor, Data);
                        StorageData.WriteInt32(modVersion.Build, Data);
                        StorageData.WriteInt32(modVersion.Revision, Data);

                        m_serializableData.SaveData(TransferManagerExtendedIds[0], Data.ToArray());
                    }

                    // --------------------------------------------------------
                    // Global Settings
                    {
                        FastList<byte> Data = new FastList<byte>();
                        SaveGameSettings.SaveData(Data);
                        m_serializableData.SaveData(TransferManagerExtendedIds[1], Data.ToArray());
                    }

                    // --------------------------------------------------------
                    // Building Settings
                    {
                        FastList<byte> Data = new FastList<byte>();
                        BuildingSettingsSerializer.SaveData(Data);
                        m_serializableData.SaveData(TransferManagerExtendedIds[2], Data.ToArray());
                    }

                    // --------------------------------------------------------
                    // OutsideConnectionSettings Settings
                    {
                        FastList<byte> Data = new FastList<byte>();
                        OutsideConnectionSettings.SaveData(Data);
                        m_serializableData.SaveData(TransferManagerExtendedIds[3], Data.ToArray());
                    }

                    // --------------------------------------------------------
                    // Transfer Reason array entries (We save from TRANSFER_REASON_COUNT to 256.
                    {
                        FastList<byte> Data = new FastList<byte>();
                        TransferManagerAwakePatch.SaveData(Data);
                        m_serializableData.SaveData(TransferReasonID, Data.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                CDebug.Log("Could not save data. " + ex.Message);
            }
        }

        // --------------------------------------------------------------------
        public void OnLoadData()
        {
            try
            {
                // Clear any previous settings
                TransferManagerExtendedMod.Instance.ClearSettings();

                if (m_serializableData is null)
                {
                    CDebug.LogError("m_serializableData is null");
                    return;
                }

                // --------------------------------------------------------
                // Transfer Reason array data
                {
                    byte[] Data = m_serializableData.LoadData(TransferReasonID);
                    if (Data is not null && Data.Length > 0)
                    {
                        if (!TransferManagerAwakePatch.LoadData(Data))
                        {
                            CDebug.LogError("Extended transfer reason array data not loaded.");
                        }
                    }
                }

                // --------------------------------------------------------
                // Transfer Manager Extended Settings
                ushort SaveGameFileVersion = LoadTransferManagerVersionInfo(TransferManagerExtendedIds[0], TransferManagerExtendedDataID, out int iMajor, out int iMinor, out int iBuild, out int iRevision);
                if (SaveGameFileVersion > 0)
                {
                    if (SaveGameFileVersion > DataFileVersion)
                    {
                        string sMessage = $"This saved game was saved with a newer version of {TransferManagerExtendedMod.Instance.BaseModName}.\r\n";
                        sMessage += "\r\n";
                        sMessage += "Unable to load Transfer Manager settings.\r\n";
                        sMessage += "\r\n";
                        sMessage += "Saved game data version: " + SaveGameFileVersion + "\r\n";
                        sMessage += "MOD data version: " + DataFileVersion + "\r\n";
                        Prompt.Info(TransferManagerExtendedMod.Instance.Name, sMessage);
                        return;
                    }
                    else
                    {
                        CDebug.Log($"Settings written by Transfer Manager Extended v: {iMajor}.{iMinor}.{iBuild}.{iRevision}");

                        if (SaveGameFileVersion >= 55)
                        {
                            // From data file version 55 onwards each settings object uses its own data tuple.
                            LoadTransferManagerMultipleDataTuple(SaveGameFileVersion, TransferManagerExtendedIds);
                        }
                        else
                        {
                            LoadTransferManagerSingleDataTuple(TransferManagerExtendedDataID, out iMajor, out iMinor, out iBuild, out iRevision);
                        }

                        return;
                    }
                }
                else
                {
                    CDebug.Log($"No settings found for {TransferManagerExtendedMod.Instance.ModName}");

                    // --------------------------------------------------------
                    // Try Transfer Manager CE settings import
                    SaveGameFileVersion = LoadTransferManagerVersionInfo(TransferManagerCEIds[0], TransferManagerCEDataID, out iMajor, out iMinor, out iBuild, out iRevision);
                    if (SaveGameFileVersion > 0)
                    {
                        CDebug.Log($"Settings written by Transfer Manager CE v{iMajor}.{iMinor}.{iBuild}.{iRevision} found attempting to import");

                        if (SaveGameFileVersion > DataFileVersion)
                        {
                            CDebug.LogError($"Settings found but too new to import.");
                            return;
                        }
                        else if (SaveGameFileVersion >= 55)
                        {
                            LoadTransferManagerMultipleDataTuple(SaveGameFileVersion, TransferManagerExtendedIds);
                        }
                        else
                        {
                            LoadTransferManagerSingleDataTuple(TransferManagerCEDataID, out iMajor, out iMinor, out iBuild, out iRevision);
                        }

                        string sMessage = $"Settings imported from Transfer Manager CE v{iMajor}.{iMinor}.{iBuild}.{iRevision}.\r\n";
                        Prompt.Info(TransferManagerExtendedMod.Instance.Name, sMessage);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                string sErrorMessage = "Loading of Transfer Manager save game settings failed with the following error:\r\n";
                sErrorMessage += "\r\n";
                sErrorMessage += ex.Message;
                Prompt.ErrorFormat(TransferManagerExtendedMod.Instance.Name, sErrorMessage);
            }
        }

        // --------------------------------------------------------------------
        public ushort LoadTransferManagerVersionInfo(string sVersionInfoId, string sSingleTupleId, out int iMajor, out int iMinor, out int iBuild, out int iRevision)
        {
            iMajor = 0;
            iMinor = 0;
            iBuild = 0;
            iRevision = 0;

            // --------------------------------------------------------
            // Version Settings 55+
            byte[] Data = m_serializableData.LoadData(sVersionInfoId);
            if (Data is not null && Data.Length > 0)
            {
                int Index = 0;

                ushort SaveGameFileVersion = StorageData.ReadUInt16(Data, ref Index);
                iMajor = StorageData.ReadInt32(Data, ref Index);
                iMinor = StorageData.ReadInt32(Data, ref Index);
                iBuild = StorageData.ReadInt32(Data, ref Index);
                iRevision = StorageData.ReadInt32(Data, ref Index);

                return SaveGameFileVersion;
            }

            // --------------------------------------------------------
            // Data file version 54 saved the version information to one data tuple.
            Data = m_serializableData.LoadData(sSingleTupleId);
            if (Data is not null && Data.Length > 0)
            {
                int Index = 0;

                ushort SaveGameFileVersion = StorageData.ReadUInt16(Data, ref Index);
                if (SaveGameFileVersion >= 30)
                {
                    iMajor = StorageData.ReadInt32(Data, ref Index);
                    iMinor = StorageData.ReadInt32(Data, ref Index);
                    iBuild = StorageData.ReadInt32(Data, ref Index);
                    iRevision = StorageData.ReadInt32(Data, ref Index);
                }

                return SaveGameFileVersion;
            }

            return 0;
        }

        // --------------------------------------------------------------------
        public void LoadTransferManagerMultipleDataTuple(ushort SaveGameFileVersion, string[] strDataIds)
        {
            // From data file version 55 onwards each settings object uses its own data tuple.

            // --------------------------------------------------------
            // Global Settings
            {
                int iIndex = 0;
                byte[] Data = m_serializableData.LoadData(strDataIds[1]);
                if (Data is not null && Data.Length > 0)
                {
                    SaveGameSettings.LoadData(SaveGameFileVersion, Data, ref iIndex);
                }
            }

            // --------------------------------------------------------
            // Building Settings
            {
                int iIndex = 0;
                byte[] Data = m_serializableData.LoadData(strDataIds[2]);
                if (Data is not null && Data.Length > 0)
                {
                    BuildingSettingsSerializer.LoadData(SaveGameFileVersion, Data, ref iIndex);
                }
            }

            // --------------------------------------------------------
            // Outside Connection Settings
            {
                int iIndex = 0;
                byte[] Data = m_serializableData.LoadData(strDataIds[3]);
                if (Data is not null && Data.Length > 0)
                {
                    OutsideConnectionSettings.LoadData(SaveGameFileVersion, Data, ref iIndex);
                }
            }
        }

        // --------------------------------------------------------------------
        public void LoadTransferManagerSingleDataTuple(string strDataId, out int iMajor, out int iMinor, out int iBuild, out int iRevision)
        {
            iMajor = 0;
            iMinor = 0;
            iBuild = 0;
            iRevision = 0;

            // Try and import TransferManagerCE settings
            byte[] Data = m_serializableData.LoadData(strDataId);
            if (Data is not null && Data.Length > 0)
            {
                int Index = 0;
                ushort SaveGameFileVersion = StorageData.ReadUInt16(Data, ref Index);
#if DEBUG
                CDebug.Log($"Settings Found {strDataId} - Data length: {Data.Length} Data Version: {SaveGameFileVersion}");
#endif
                if (SaveGameFileVersion <= 54)
                {
                    // Since settings version 30 the mod version is also saved in
                    // case the settings version isn't updated correctly.
                    if (SaveGameFileVersion >= 30)
                    {
                        iMajor = StorageData.ReadInt32(Data, ref Index);
                        iMinor = StorageData.ReadInt32(Data, ref Index);
                        iBuild = StorageData.ReadInt32(Data, ref Index);
                        iRevision = StorageData.ReadInt32(Data, ref Index);
#if DEBUG
                        CDebug.Log($"Settings written by {strDataId} v: {iMajor}.{iMinor}.{iBuild}.{iRevision}");
#endif
                    }
                    
                    StorageData.CheckStartTuple("SaveGameSettings", SaveGameFileVersion, Data, ref Index);
                    SaveGameSettings.LoadData(SaveGameFileVersion, Data, ref Index);
                    StorageData.CheckEndTuple("SaveGameSettings", SaveGameFileVersion, Data, ref Index);

                    StorageData.CheckStartTuple("BuildingSettingsSerializer", SaveGameFileVersion, Data, ref Index);
                    BuildingSettingsSerializer.LoadData(SaveGameFileVersion, Data, ref Index);
                    StorageData.CheckEndTuple("BuildingSettingsSerializer", SaveGameFileVersion, Data, ref Index);

                    StorageData.CheckStartTuple("OutsideConnectionSettings", SaveGameFileVersion, Data, ref Index);
                    OutsideConnectionSettings.LoadData(SaveGameFileVersion, Data, ref Index);
                    StorageData.CheckEndTuple("OutsideConnectionSettings", SaveGameFileVersion, Data, ref Index);
                }
            }
        }

        // --------------------------------------------------------------------
        public void OnReleased()
        {
            TransferManagerSerializer.instance = (TransferManagerSerializer) null;
        }
    }
}