using JSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot
{
    static class ResourcesModel
    {
        public static readonly string SettingsDirectory;
        public static readonly string SettingsFilePath;
        public static readonly string GuildsFilePath;

        static ResourcesModel()
        {
            SettingsDirectory = Environment.CurrentDirectory + "/YNBBot/Settings/";
            SettingsFilePath = SettingsDirectory + "Settings.json";
            GuildsFilePath = SettingsDirectory + "Guilds.json";
        }

        public static bool CheckSettingsFilesExistence()
        {
            return File.Exists(SettingsFilePath);
        }

        public static async Task InitiateBasicFiles()
        {
            Directory.CreateDirectory(SettingsDirectory);
            await SettingsModel.SaveSettings();
        }

        #region Save/Load

        public static async Task<LoadFileOperation> LoadToJSONObject(string path)
        {
            LoadFileOperation operation = new LoadFileOperation()
            {
                Success = false,
                Result = null
            };
            if (File.Exists(path))
            {
                string fileContent = "";
                try
                {
                    fileContent = await File.ReadAllTextAsync(path, Encoding.UTF8);
                    operation.Success = JSONContainer.TryParse(fileContent, out operation.Result, out string error);
                    return operation;
                }
                catch (Exception e)
                {
                    await BotCore.Logger(new Discord.LogMessage(Discord.LogSeverity.Critical, "Save/Load", "Failed to load " + path, e));
                }
            }
            return operation;
        }

        public static async Task WriteJSONObjectToFile(string path, JSONContainer json)
        {
            try
            {
                await File.WriteAllTextAsync(path, json.Build(), Encoding.UTF8);
            }
            catch (Exception e)
            {
                await BotCore.Logger(new Discord.LogMessage(Discord.LogSeverity.Critical, "Save/Load", "Failed to save " + path, e));
            }
        }
        #endregion
        #region MacroMethods
        
        public static string GetMentionsFromUserIdList(List<ulong> userIds)
        {
            StringBuilder result = new StringBuilder();
            if (userIds.Count == 1)
            {
                result.Append(GetMentionFromUserId(userIds[0]));
            } else if (userIds.Count >= 2)
            {
                foreach (ulong userId in userIds)
                {
                    result.Append(GetMentionFromUserId(userId));
                    result.Append(" ");
                }
            }
            return result.ToString().TrimEnd();
        }

        public static string GetMentionFromUserId(ulong userId)
        {
            return Var.client.GetUser(userId).Mention;
        }

        #endregion
    }

    public struct LoadFileOperation
    {
        public bool Success;
        public JSONContainer Result;
    }
}