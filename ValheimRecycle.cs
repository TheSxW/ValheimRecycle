using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecycle
{
    [BepInPlugin("org.lafchi.plugins.valheim_recycle", "Valheim Recycle", "1.3.1")]
    [BepInProcess("valheim.exe")]
    public class ValheimRecycle : BaseUnityPlugin
    {

        internal static ValheimRecycle instance;
        internal GameObject recycleObject;
        internal Button recycleButton;
        internal float width;
        Vector3 craftingPos;
        Harmony harmony;

        #region Config
        internal ConfigEntry<RecycleConfig.TabPositions> tabPosition;
        internal ConfigEntry<float> resourceMultiplier;
        internal ConfigEntry<bool> preserveOriginalItem;
        internal ConfigEntry<int> nexusID;
        #endregion

        internal bool InTabDeconstruct()
        {
            return !recycleButton.interactable;
        }
        public ValheimRecycle()
        {
            instance = this;
            harmony = Harmony.CreateAndPatchAll(typeof(InventoryGuiPatch));
            
            tabPosition = Config.Bind("General", "TabPosition", RecycleConfig.TabPositions.Left,
                             "The Recycle tab's position in the crafting menu after Upgrade. (Requires restart)");
            resourceMultiplier = Config.Bind("General", "ResourceMultiplier", 1f,
                 new ConfigDescription("The amount of resources to return from recycling (0 to 1, where 1 returns 100% of the resources and 0 returns 0%)", new AcceptableValueRange<float>(0,1))
                 );
            preserveOriginalItem = Config.Bind("General", "PreserveOriginalItem", true, 
                "[EXPERIMENTAL]\nWhether the original item's data should be preserved when downgrading. Useful for mods which add extra properties to items like EpicLoot.\nTurn off if experiencing problems.");
            nexusID = Config.Bind<int>("General", "NexusID", 425,
                "Nexus mod ID for updates");

        }
        internal void OnDestroy()
        {
            Logger.LogInfo("DESTROY");
            Destroy(recycleObject);
            harmony.UnpatchSelf();
            Logger.LogInfo("Unpatched InventoryGui");
        }

        internal GameObject GetOrCreateRecycleTab()
        {
            if (instance.recycleObject != null)
            {
                return instance.recycleObject;

            }
            recycleObject = Instantiate(InventoryGui.instance.m_tabUpgrade.gameObject, InventoryGui.instance.m_tabUpgrade.gameObject.transform.parent);
            if (recycleObject is null)
            {
                Logger.LogError($"SortButton couldn't be instantiated.");
                return null;
            }
            recycleObject.name = "Recycle";
            recycleObject.GetComponentInChildren<Text>().text = "RECYCLE";
            width = recycleObject.GetComponent<RectTransform>().rect.width;
            craftingPos = new Vector3(recycleObject.transform.localPosition.x + ((width + 10f) * ((int)tabPosition.Value + 1)), recycleObject.transform.localPosition.y, recycleObject.transform.localPosition.z);
            recycleButton = recycleObject.GetComponent<Button>();
            recycleButton.transform.localPosition = craftingPos;
            recycleButton.interactable = true;
            recycleButton.name = "RecycleButton";
            recycleButton.onClick.RemoveAllListeners();
            recycleButton.onClick.AddListener(SelectRecycleTab);
            recycleObject.SetActive(false);
            return recycleObject;
        }
        MethodInfo methodInfo = null;
        internal void SelectRecycleTab()
        {
            recycleButton.interactable = false;
            InventoryGui.instance.m_tabCraft.interactable = true;
            InventoryGui.instance.m_tabUpgrade.interactable = true;
            if (methodInfo == null)
                methodInfo = typeof(InventoryGui).GetMethod("UpdateCraftingPanel", BindingFlags.NonPublic | BindingFlags.Instance);
            methodInfo.Invoke(InventoryGui.instance, new object[] { false });
        }

        internal void RebuildRecycleTab()
        {
            GetOrCreateRecycleTab();
        }

    }
}