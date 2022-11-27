using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Threading;
using BondReader;
using BondReader.Schemas;
using BondReader.Schemas.Items;
using Halo_Forge_Bot.config;
using Halo_Forge_Bot.DataModels;
using Halo_Forge_Bot.GameUI;
using Halo_Forge_Bot.Utilities;
using InfiniteForgeConstants.Forge_UI;
using InfiniteForgeConstants.Forge_UI.Object_Browser;
using InfiniteForgeConstants.Forge_UI.Object_Properties;
using InfiniteForgeConstants.ObjectSettings;
using Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using TextCopy;
using WindowsInput.Native;
using Clipboard = TextCopy.Clipboard;


namespace Halo_Forge_Bot;

public static class Bot
{
    public static async Task StartBot(BondSchema map, string mapName, int itemStart = 0, int itemEnd = 0, bool resumeFromLast = false)
    {
        // TODO: create a class for both blender and .mvar files, maybe use the blender file json
        // TODO: add checks to the ui to stop the starting of the bot without halo being open / crash detection
        MemoryHelper.Memory.OpenProcess(ForgeUI.SetHaloProcess().Id); 

        int startIndex = itemStart;
        Dictionary<ObjectId, List<MapItem>> items = new();

        BuildUILayout();

        items = InitMapItems(map, mapName, itemStart, itemEnd, resumeFromLast, items, ref startIndex);

        ForgeUI.SetHaloProcess();
        await BeginForging(mapName, resumeFromLast, items, startIndex);
    }

    private static async Task BeginForging(string mapName, bool resumeFromLast, Dictionary<ObjectId, List<MapItem>> items, int startIndex)
    {
        int itemCountID = 0;
        int saveCount = 0;

        foreach (var item in items)
        {
            //TODO: extract all the data processing and the bot logic from each other
            await MoveCursorToTopOfMenu();

            await SetItemBrowserToActiveMenu();

            if (GetMapItem(item, out var mapitem)) continue;

            await NavigateToItem(mapitem);

            await NavigateToSubCategory(mapitem);

            await HoverItem(mapitem);
            
            foreach (var mapItem in item.Value) // the start of the item spawning loop
            {
                if (mapItem.UniqueId < startIndex && resumeFromLast)
                {
                    continue;
                }

                ProcessRecovery.WriteObjectRecoveryIndexToFile(mapItem.UniqueId, mapName);

                saveCount++;
                await SpawnItem();
                await Save(saveCount);
                await PropertyHelper.SetMainProperties(mapItem.Schema);
                itemCountID++;
            }

            await Task.Delay(75);
            Input.Simulate.Keyboard.KeyPress(VirtualKeyCode.BACK);
            await Task.Delay(100);

            while (MemoryHelper.GetGlobalHover() != mapitem.ParentFolder.ParentCategory.CategoryOrder - 1)
            {
                Input.Simulate.Keyboard.KeyPress(VirtualKeyCode.VK_W);
                await Task.Delay(33);
            }

            await Task.Delay(100);
            Input.Simulate.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            await Task.Delay(500);
        }
    }

    private static async Task SpawnItem()
    {
        await Task.Delay(200);
        while (MemoryHelper.GetMenusVisible() == 1)
        {
            Input.Simulate.Keyboard.KeyPress(VirtualKeyCode.RETURN); // spawn the item?
            await Task.Delay(200);
        }
    }

    private static async Task HoverItem(ForgeUIObject? mapitem)
    {
        while (MemoryHelper.GetGlobalHover() != mapitem.ObjectOrder - 1) // hover item
        {
            Input.PressKey(VirtualKeyCode.VK_S);
            await Task.Delay(33);
        }
    }

    private static async Task NavigateToSubCategory(ForgeUIObject? mapitem)
    {
        while (MemoryHelper.GetGlobalHover() !=
               mapitem.ParentFolder.ParentCategory.CategoryOrder +
               mapitem.ParentFolder.FolderOffset - 1) // move cursor to sub cat
        {
            Log.Debug("Move To subCat currentHover:{Hover} , Required Hover: {Reqired}",
                MemoryHelper.GetGlobalHover(),
                mapitem.ParentFolder.ParentCategory.CategoryOrder +
                mapitem.ParentFolder.FolderOffset - 1);
            Input.Simulate.Keyboard.KeyPress(VirtualKeyCode.VK_S);

            await Task.Delay(33);
        }

        await Task.Delay(200);
        Input.Simulate.Keyboard.KeyPress(VirtualKeyCode.RETURN); // enter the sub cat
        await Task.Delay(200);
    }

    private static bool GetMapItem(KeyValuePair<ObjectId, List<MapItem>> item, out ForgeUIObject? mapitem)
    {
        var currentObjectId = item.Key;

        ForgeObjectBrowser.FindItem(currentObjectId, out mapitem); // gets the items ui location data

        if (mapitem == null)
        {
            Log.Warning("Skipping null item, MapId: {id}, Name: {name} ", Enum.GetName(currentObjectId));
            return true;
        }

        return false;
    }

    private static async Task SetItemBrowserToActiveMenu()
    {
        while (MemoryHelper.GetTopBrowserHover() != 0) // set item browser to active menu
        {
            Input.Simulate.Keyboard.KeyPress(VirtualKeyCode.VK_E);
            await Task.Delay(10);
        }
    }

    private static async Task NavigateToItem(ForgeUIObject mapitem)
    {
        //navigate to item with memory checks
        while (MemoryHelper.GetGlobalHover() !=
               mapitem.ParentFolder.ParentCategory.CategoryOrder - 1) //Set cursor to correct cat
        {
            Log.Debug("Move To Cat currentHover:{Hover} , Required Hover: {Reqired}",
                MemoryHelper.GetGlobalHover(), mapitem.ParentFolder.ParentCategory.CategoryOrder - 1);
            Input.Simulate.Keyboard.KeyPress(VirtualKeyCode.VK_S);
            
            await Task.Delay(233);
            Input.Simulate.Keyboard.KeyPress(VirtualKeyCode.RETURN); // open the cat
            await Task.Delay(200);

        }
    }

    private static async Task MoveCursorToTopOfMenu()
    {
        // reset the cursor to the top of the current menu (in most cases the object browser)
        while (MemoryHelper.GetGlobalHover() != 0)
        {
            Input.Simulate.Keyboard.KeyPress(VirtualKeyCode.VK_W);
        }

        await Task.Delay(200);
    }

    private static async Task Save(int saveCount)
    {
        await Task.Delay(200);

        if (saveCount % 10 == 0)
        {
            //todo add a save count setting to the ui
            await Task.Delay(100);
            Input.Simulate.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_S);
            saveCount = 0;
            await Task.Delay(100);
        }
    }

    private static Dictionary<ObjectId, List<MapItem>> InitMapItems(BondSchema map, string mapName, int itemStart, int itemEnd, bool resumeFromLast,
        Dictionary<ObjectId, List<MapItem>> items, ref int startIndex)
    {
        if (resumeFromLast)
        {
            try
            {
                var recoveryValues = ProcessRecovery.GetRecoveryFiles(mapName);
                items = recoveryValues.Item2;
                startIndex = recoveryValues.Item1;
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"Recovery files not found: {ex.Message}");
                throw;
            }
        }
        else
        {
            var splitItemList = new List<ItemSchema>(); // item list of the items to process
            var tempArray =
                map.Items.ToArray().OrderBy(item => item.ItemId.Int)
                    .ToList(); // temp to an array to i know now for sure its in the correct order. might be unnecessary 

            if (itemEnd == 0)
            {
                itemEnd = tempArray.Count();
            }

            int index = 0;

            for (int i = itemStart; i < itemEnd; i++) // extracting the requested items from the map. 
            {
                splitItemList.Add(tempArray[i]);
            }

            foreach (var itemSchema in splitItemList)
            {
                var id = (ObjectId) itemSchema.ItemId.Int;

                if (itemSchema.StaticDynamicFlagUnknown != 21)
                {
                    //todo have a better way to detect if an item is default static / dynamic. the bot will currently break if we try and spawn a dynamic by default item

                    Log.Warning(
                        "Item with id: {ItemID} is dynamic, we currently only support static items, skipping this item",
                        id);
                    continue;
                }

                var mapItem = new MapItem(index++, itemSchema);

                if (items.ContainsKey(id)) // collect similar items into lists to reduce the bots ui traveling 
                {
                    items[id].Add(mapItem);
                    continue;
                }

                items.Add(id, new List<MapItem>());
                items[id].Add(mapItem);
            }

            ProcessRecovery.WriteObjectRecoveryFile(items, mapName);
        }

        return items;
    }

    public static void BuildUILayout()
    {
        //Your cursed stuff is in here if you still need it, manually fixed the part of the file.
        //ConformForgeObjects.BuildUiLayout();

        string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
        var data = File.ReadAllLines(strWorkPath + "/config/ForgeObjects.txt");

        foreach (var line in data)
        {
            var objectData = line.Split(":");
            if (objectData[0] == "Z_NULL" || objectData[0] == "Z_UNUSED") continue;

            ForgeObjectBrowser.AddItem(ConformForgeObjects.FixCapital(objectData[0].ToLower()),
                ConformForgeObjects.FixCapital(objectData[1].ToLower()),
                Enum.GetName((ObjectId)int.Parse(objectData[3])) ?? objectData[2], Enum.Parse<ObjectId>(objectData[3]));
        }
    }


    public static ForgeUIObject? GetItemByID(ObjectId id)
    {
        foreach (var category in ForgeObjectBrowser.Categories)
        {
            foreach (var subFolder in category.Value.CategoryFolders)
            {
                foreach (var obj in subFolder.Value.FolderObjects)
                {
                    if (obj.Value.ObjectId == id)
                    {
                        return obj.Value;
                    }
                }
            }
        }

        return null;
    }
}