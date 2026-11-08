using System;
using System.IO;
using UnityEngine;
using WeaponFactory.Logic;

namespace WeaponFactory.Save
{
    /// <summary>
    /// JSON save system. Writes atomically (temp file + replace) and keeps a backup copy.
    /// Load order: main file -> backup file -> fresh defaults. Corrupted files are renamed
    /// to *.corrupt so they can be inspected and never crash the game.
    /// </summary>
    public class SaveManager
    {
        const string FileName = "weaponfactory_save.json";
        const string BackupName = "weaponfactory_save.bak";

        public SaveData Data { get; private set; }

        public string SavePath { get { return Path.Combine(Application.persistentDataPath, FileName); } }
        string BackupPath { get { return Path.Combine(Application.persistentDataPath, BackupName); } }

        public void Load()
        {
            SaveData data;
            if (TryRead(SavePath, out data) || TryRead(BackupPath, out data))
            {
                if (data.Sanitize()) Debug.Log("[SaveManager] Save data repaired.");
                Data = data;
            }
            else
            {
                Data = new SaveData();
                Data.Sanitize();
            }
        }

        bool TryRead(string path, out SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(path)) return false;
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json) || json.Trim().Length < 2) throw new InvalidDataException("empty");
                data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) throw new InvalidDataException("null");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveManager] Could not read " + path + ": " + e.Message);
                try { if (File.Exists(path)) File.Copy(path, path + ".corrupt", true); } catch (Exception) { /* ignore */ }
                data = null;
                return false;
            }
        }

        public void Save()
        {
            if (Data == null) return;
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string json = JsonUtility.ToJson(Data, true);
                string tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(SavePath)) File.Copy(SavePath, BackupPath, true);
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(tmp, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveManager] Save failed: " + e.Message);
            }
        }

        /// <summary>Development helper: wipes progress (keeps nothing).</summary>
        public void ResetAll()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
            }
            catch (Exception e) { Debug.LogWarning("[SaveManager] Reset: " + e.Message); }
            Data = new SaveData();
            Data.Sanitize();
            Save();
        }
    }
}
