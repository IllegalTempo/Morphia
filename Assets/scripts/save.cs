using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

[Serializable]
public class save
{
    public SaveInfo_Player[] playerSaveInfos = new SaveInfo_Player[2];
    public List<ItemDataEntry> ItemData = new List<ItemDataEntry>();
    public Dictionary<ItemIdentifier,SaveInfo_Item> FindSavedItem = new Dictionary<ItemIdentifier, SaveInfo_Item>();
    public string CurrentStage = "intro";
    public string CurrentSaveName = "";
    public static save instance = new save();

    public string[] GetFilesInSaveFolder()
    {
        try
        {
            string saveFolder = Application.persistentDataPath + "/saves/";
            if (Directory.Exists(saveFolder))
            {
                //most recent last edit at [0]
                string[] files = Directory.GetFiles(saveFolder, "*.json");
                Array.Sort(files, (a, b) =>
                    File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a))
                );
                return files;
            }
            else
            {
                Debug.LogWarning("Save folder does not exist: " + saveFolder +", Creating");
                //Create the folder
                Directory.CreateDirectory(saveFolder);
                return new string[0];
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to get files in save folder: " + e.Message);
            return new string[0];
        }
    }
    public string GetSavePath(string SaveName)
    {
        return Path.Combine(Application.persistentDataPath + "/saves/", SaveName + ".json");
    }
    public string GetSaveName(string path)
    {
        return Path.GetFileNameWithoutExtension(path);
    }
    /// <summary>
    /// Saves the current game state to a JSON file
    /// </summary>
    public void SaveToFile(string path)
    {
        try
        {
            // Serialize to JSON
            string json = JsonUtility.ToJson(this, true);
            
            // Write to file
            File.WriteAllText(path, json);
            
            Debug.Log("Game saved successfully to: " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save game: " + e.Message);
        }
    }
    public void NewSave(string saveName)
    {
        CurrentSaveName = saveName;
        playerSaveInfos = new SaveInfo_Player[2]
        {
            new SaveInfo_Player(new Vector3(0,10,0), Quaternion.identity),
            new SaveInfo_Player(new Vector3(0,10,0), Quaternion.identity)
        };
        ItemData = new List<ItemDataEntry>();
        CurrentStage = "intro";
        SaveToFile(GetSavePath(saveName));
    }
    /// <summary>
    /// Loads the game state from a JSON file
    /// </summary>
    public bool LoadFromFile(string path)
    {
        Debug.Log("Loading from path " + path);
        try
        {
            CurrentSaveName = GetSaveName(path);
            // Check if save file exists
            if (!File.Exists(path))
            {
                Debug.LogWarning("Save file not found at: " + path);
                return false;
            }

            // Read from file
            string json = File.ReadAllText(path);
            
            // Deserialize from JSON (overwrites current instance data)
            JsonUtility.FromJsonOverwrite(json, this);
            
            //Convert 
            foreach(ItemDataEntry item in ItemData)
            {
                FindSavedItem[item.identifier] = item.itemInfo;
            }
            Debug.Log("Game loaded successfully from: " + path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load game: " + e.Message);
            return false;
        }
    }

    /// <summary>
    /// Deletes the save file if it exists
    /// </summary>
    public void DeleteSaveFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log("Save file deleted successfully");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to delete save file: " + e.Message);
        }
    }

    /// <summary>
    /// Checks if a save file exists
    /// </summary>
    public bool SaveFileExists(string path)
    {
        return File.Exists(path);
    }
}

/// <summary>
/// Entry for serializing dictionary key-value pairs
/// </summary>
[Serializable]
public class ItemDataEntry
{
    public ItemIdentifier identifier;
    public SaveInfo_Item itemInfo;
}
[Serializable]

public class ItemIdentifier
{
    public string stage;
    public int ItemID;
    public ItemIdentifier(string stage, int itemID)
    {
        this.stage = stage;
        ItemID = itemID;
    }
    public override bool Equals(object obj)
    {
        if (obj is ItemIdentifier other)
        {
            return stage == other.stage && ItemID == other.ItemID;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return (stage, ItemID).GetHashCode();
    }
}
[Serializable]
public class SaveInfo_Player
{
    public Vector3 position;
    public Quaternion rotation;
    public SaveInfo_Player(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
    }
}
[Serializable]
public class SaveInfo_Item
{
    public Vector3 position;
    public Quaternion rotation;
    public int Parented_To_Player = -1;
    public SaveInfo_Item(Vector3 pos, Quaternion rot, int parented_to_player)
    {
        position = pos;
        rotation = rot;
        Parented_To_Player = parented_to_player;
    }
}


