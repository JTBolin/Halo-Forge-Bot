using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using Halo_Forge_Bot.DataModels;
using InfiniteForgeConstants.ObjectSettings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Halo_Forge_Bot.Utilities;

public static class ProcessRecovery
{
    public static Tuple<int, Dictionary<ObjectId, List<MapItem>>> GetRecoveryFiles(string mapName)
    {
        Tuple<int, Dictionary<ObjectId, List<MapItem>>> recoveryObject =
            new Tuple<int, Dictionary<ObjectId, List<MapItem>>>(0, new Dictionary<ObjectId, List<MapItem>>());

        JsonSerializerSettings s = new JsonSerializerSettings();
        s.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

        if (!File.Exists(Utils.ExePath + $"/recovery/currentObjectRecoveryIndex-{mapName}.json") ||
            !File.Exists(Utils.ExePath + $"/recovery/ObjectRecoveryData-{mapName}.json"))
        {
            throw new Exception(message: $"No recovery files found for {mapName}. Has a BOT run previously started for this map?");
        }

        using (StreamReader file = File.OpenText(Utils.ExePath + $"/recovery/currentObjectRecoveryIndex-{mapName}.json"))
        using (JsonTextReader reader = new JsonTextReader(file))
        {
            var index = JToken.ReadFrom(reader).Value<int>();
            recoveryObject =
                new Tuple<int, Dictionary<ObjectId, List<MapItem>>>(index, new Dictionary<ObjectId, List<MapItem>>());
        }

        // read JSON directly from a file
        using (StreamReader file = File.OpenText(Utils.ExePath + $"/recovery/ObjectRecoveryData-{mapName}.json"))
        using (JsonTextReader reader = new JsonTextReader(file))
        {
            var items = (JObject)JToken.ReadFrom(reader);
            recoveryObject = new Tuple<int, Dictionary<ObjectId, List<MapItem>>>(recoveryObject.Item1,
                items.ToObject<Dictionary<ObjectId, List<MapItem>>>());
        }
        

        return recoveryObject;
    }

    public static void WriteObjectRecoveryIndexToFile(int index, string mapName)
    {
        JsonSerializerSettings s = new JsonSerializerSettings();
        s.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        Directory.CreateDirectory(Utils.ExePath + "/recovery/");

        var a = JsonConvert.SerializeObject(index, s);
        File.WriteAllText(Utils.ExePath + $"/recovery/currentObjectRecoveryIndex-{mapName}.json", a);
    }

    public static void WriteObjectRecoveryFile(Dictionary<ObjectId, List<MapItem>> items, string mapName)
    {
        JsonSerializerSettings s = new JsonSerializerSettings();
        s.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        Directory.CreateDirectory(Utils.ExePath + "/recovery/");

        var a = JsonConvert.SerializeObject(items, s);
        File.WriteAllText(Utils.ExePath + $"/recovery/ObjectRecoveryData-{mapName}.json", a);
    }
}