using ICities;
using SleepyCommon;
using System;
using System.Reflection;
using TransferManagerCore.Settings;

namespace TransferManagerCore
{
    public class Serializer : ISerializableDataExtension
    {
        public const string DataID = "TransferManagerExtended";
        public const ushort DataVersion = 54;

        public static Serializer? instance = null;
        private ISerializableData? m_serializableData = null;

        public void OnCreated(ISerializableData serializedData)
        {
            instance = this;
            m_serializableData = serializedData;
        }

        public void OnLoadData()
        {
            try
            {
                // Clear any previous settings
                TransferManagerExtendedMod.Instance.ClearSettings();

                if (m_serializableData is not null)
                {
                    byte[] Data = m_serializableData.LoadData(DataID);
                    if (Data is not null && Data.Length > 0)
                    {
                        ushort SaveGameFileVersion;
                        int Index = 0;

                        SaveGameFileVersion = StorageData.ReadUInt16(Data, ref Index);
#if DEBUG
                        CDebug.Log("Data length: " + Data.Length.ToString() + "; Data Version: " + SaveGameFileVersion);
#endif
                        if (SaveGameFileVersion <= DataVersion)
                        {
                            // Since settings version 30 the mod version is also saved in
                            // case the settings version isn't updated correctly.
                            int iMajor = StorageData.ReadInt32(Data, ref Index);
                            int iMinor = StorageData.ReadInt32(Data, ref Index);
                            int iBuild = StorageData.ReadInt32(Data, ref Index);
                            int iRevision = StorageData.ReadInt32(Data, ref Index);
                            CDebug.Log($"Settings written by mod version: {iMajor}.{iMinor}.{iBuild}.{iRevision}");

                            StorageData.CheckStartTuple("SaveGameSettings", SaveGameFileVersion, Data, ref Index);
                            SaveGameSettings.LoadData(SaveGameFileVersion, Data, ref Index);
                            StorageData.CheckEndTuple("SaveGameSettings", SaveGameFileVersion, Data, ref Index);

                            StorageData.CheckStartTuple("BuildingSettingsSerializer", SaveGameFileVersion, Data, ref Index);
                            BuildingSettingsSerializer.LoadData(SaveGameFileVersion, Data, ref Index);
                            StorageData.CheckEndTuple("BuildingSettingsSerializer", SaveGameFileVersion, Data, ref Index);

                            StorageData.CheckStartTuple("OutsideConnectionSettings", SaveGameFileVersion, Data, ref Index);
                            OutsideConnectionSettings.LoadData(SaveGameFileVersion, Data, ref Index);
                            StorageData.CheckEndTuple("OutsideConnectionSettings", SaveGameFileVersion, Data, ref Index);

                            StorageData.CheckStartTuple("TransferManager", SaveGameFileVersion, Data, ref Index);
                            TransferManagerAwakePatch.LoadData(SaveGameFileVersion, Data, ref Index);
                            StorageData.CheckEndTuple("TransferManager", SaveGameFileVersion, Data, ref Index);
                        }
                        else
                        {
                            string sMessage = $"This saved game was saved with a newer version of {TransferManagerExtendedMod.Instance.BaseModName}.\r\n";
                            sMessage += "\r\n";
                            sMessage += "Unable to load Transfer Manager settings.\r\n";
                            sMessage += "\r\n";
                            sMessage += "Saved game data version: " + SaveGameFileVersion + "\r\n";
                            sMessage += "MOD data version: " + DataVersion + "\r\n";
                            Prompt.Info(TransferManagerExtendedMod.Instance.Name, sMessage);
                        }
                    }
                    else
                    {
                        CDebug.Log("Save data not found");
                    }
                }
                else
                {
                    CDebug.Log("m_serializableData is null");
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

        public void OnSaveData()
        {
            try
            {
                if (m_serializableData is not null)
                {
                    FastList<byte> Data = new FastList<byte>();
                    // Always write out data version first
                    StorageData.WriteUInt16(DataVersion, Data);

                    // Now also writes out mod version in case I forget to incrmement settings version
                    Version modVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    StorageData.WriteInt32(modVersion.Major, Data);
                    StorageData.WriteInt32(modVersion.Minor, Data);
                    StorageData.WriteInt32(modVersion.Build, Data);
                    StorageData.WriteInt32(modVersion.Revision, Data);

                    // Global settings
                    StorageData.WriteTupleStart(Data);
                    SaveGameSettings.SaveData(Data);
                    StorageData.WriteTupleEnd(Data);

                    // Building settings
                    StorageData.WriteTupleStart(Data);
                    BuildingSettingsSerializer.SaveData(Data);
                    StorageData.WriteTupleEnd(Data);

                    // Outside connection settings
                    StorageData.WriteTupleStart(Data);
                    OutsideConnectionSettings.SaveData(Data);
                    StorageData.WriteTupleEnd(Data);

                    StorageData.WriteTupleStart(Data);
                    TransferManagerAwakePatch.SaveData(Data);
                    StorageData.WriteTupleEnd(Data);

                    m_serializableData.SaveData(DataID, Data.ToArray());
                }
            }
            catch (Exception ex)
            {
                CDebug.Log("Could not save data. " + ex.Message);
            }
        }

        public void OnReleased()
        {
            Serializer.instance = (Serializer) null;
        }
    }
}