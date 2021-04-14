using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecycle
{
    [HarmonyPatch(typeof(InventoryGui))]
    public class InventoryGuiPatch
    {
        private static MethodInfo methodInfo_DoCrafting;
        private static MethodInfo methodInfo_SetupRequirementList;
        private static MethodInfo methodInfo_AddRecipeToList;
        private static MethodInfo methodInfo_SetRecipe;
        private static MethodInfo methodInfo_GetSelectedRecipeIndex;
        private static MethodInfo methodInfo_UpdateRecipeList;

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        internal static void PostfixUpdate(InventoryGui __instance) => ValheimRecycle.instance?.RebuildRecycleTab();

        [HarmonyPrefix]
        [HarmonyPatch("OnTabCraftPressed")]
        internal static bool PrefixOnTabCraftPressed(InventoryGui __instance)
        {
            ValheimRecycle.instance.recycleButton.interactable = true;
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch("OnTabUpgradePressed")]
        internal static bool PrefixOnTabUpgradePressed(InventoryGui __instance)
        {
            ValheimRecycle.instance.recycleButton.interactable = true;
            return true;
        }


        [HarmonyPostfix]
        [HarmonyPatch("SetupRequirement")]
        internal static void PostfixSetupRequirement(Transform elementRoot, Piece.Requirement req, int quality)
        {
            // don't flash the resource amount in requirements window if deconstructing
            if (ValheimRecycle.instance.InTabDeconstruct())
            {
                Text component3 = elementRoot.transform.Find("res_amount").GetComponent<Text>();
                int amount = Utils.GetModifiedAmount(quality, req);

                component3.text = amount.ToString();
                component3.color = Color.green;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("UpdateCraftingPanel")]
        internal static bool PrefixUpdateCraftingPanel(InventoryGui __instance, ref bool focusView,
            KeyValuePair<Recipe, ItemDrop.ItemData> ___m_selectedRecipe, List<KeyValuePair<Recipe, ItemDrop.ItemData>> ___m_availableRecipes)
        {
            if (ValheimRecycle.instance != null)
            {
                Player localPlayer = Player.m_localPlayer;
                if (!localPlayer.GetCurrentCraftingStation() && !localPlayer.NoCostCheat())
                {
                    __instance.m_tabCraft.interactable = false;
                    __instance.m_tabUpgrade.interactable = true;
                    __instance.m_tabUpgrade.gameObject.SetActive(false);
                    ValheimRecycle.instance.recycleObject.SetActive(false);
                    ValheimRecycle.instance.recycleButton.interactable = true;
                }
                else
                {
                    __instance.m_tabUpgrade.gameObject.SetActive(true);
                    if (!localPlayer.GetCurrentCraftingStation().gameObject.name.Contains("cauldron"))
                    {
                        ValheimRecycle.instance.recycleObject.SetActive(true);
                    }
                    else
                    {
                        ValheimRecycle.instance.recycleObject.SetActive(false);
                    }
                }
                List<Recipe> recipes = new List<Recipe>();
                localPlayer.GetAvailableRecipes(ref recipes);

                if (methodInfo_UpdateRecipeList == null)
                    methodInfo_UpdateRecipeList = typeof(InventoryGui).GetMethod("UpdateRecipeList", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo_UpdateRecipeList.Invoke(__instance, new object[] { recipes });

                if (___m_availableRecipes.Count <= 0)
                {
                    if (methodInfo_SetRecipe == null)
                        methodInfo_SetRecipe = typeof(InventoryGui).GetMethod("SetRecipe", BindingFlags.NonPublic | BindingFlags.Instance);
                    methodInfo_SetRecipe.Invoke(__instance, new object[] { -1, focusView });
                    return false;
                }
                if (___m_selectedRecipe.Key != null)
                {
                    if (methodInfo_GetSelectedRecipeIndex == null)
                        methodInfo_GetSelectedRecipeIndex = typeof(InventoryGui).GetMethod("GetSelectedRecipeIndex", BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    int selectedRecipeIndex = (int)methodInfo_GetSelectedRecipeIndex.Invoke(__instance, new object[] { });

                    if (methodInfo_SetRecipe == null)
                        methodInfo_SetRecipe = typeof(InventoryGui).GetMethod("SetRecipe", BindingFlags.NonPublic | BindingFlags.Instance);
                    methodInfo_SetRecipe.Invoke(__instance, new object[] { selectedRecipeIndex, focusView });
                    return false;
                }
                if (methodInfo_SetRecipe == null)
                    methodInfo_SetRecipe = typeof(InventoryGui).GetMethod("SetRecipe", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo_SetRecipe.Invoke(__instance, new object[] { 0, focusView });
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("UpdateRecipeList")]
        internal static void PostfixUpdateRecipeList(InventoryGui __instance, List<Recipe> recipes, 
            List<KeyValuePair<Recipe, ItemDrop.ItemData>> ___m_availableRecipes, List<GameObject> ___m_recipeList, float ___m_recipeListBaseSize,
            List<ItemDrop.ItemData> ___m_tempItemList)
        {
            if (ValheimRecycle.instance.InTabDeconstruct())
            {
                Player localPlayer = Player.m_localPlayer;
                ___m_availableRecipes.Clear();
                foreach (GameObject gameObject in ___m_recipeList)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
                ___m_recipeList.Clear();

                Debug.Log("Recipe list:\n");

                List<KeyValuePair<Recipe, ItemDrop.ItemData>> list = new List<KeyValuePair<Recipe, ItemDrop.ItemData>>();
                for (int l = 0; l < recipes.Count; l++)
                {

                    Recipe recipe2 = recipes[l];

                    if (recipe2.m_item.m_itemData.m_shared.m_maxQuality > 1)
                    {
                        ___m_tempItemList.Clear();
                        localPlayer.GetInventory().GetAllItems(recipe2.m_item.m_itemData.m_shared.m_name, ___m_tempItemList);

                        foreach (ItemDrop.ItemData itemData in ___m_tempItemList)
                        {
                            if (itemData.m_quality >= 1)
                            {
                                list.Add(new KeyValuePair<Recipe, ItemDrop.ItemData>(recipe2, itemData));
                            }

                        }
                    }
                }
                foreach (KeyValuePair<Recipe, ItemDrop.ItemData> keyValuePair in list)
                {
                    if (methodInfo_AddRecipeToList == null)
                        methodInfo_AddRecipeToList = typeof(InventoryGui).GetMethod("AddRecipeToList", BindingFlags.NonPublic | BindingFlags.Instance);
                    methodInfo_AddRecipeToList.Invoke(__instance, new object[] { localPlayer, keyValuePair.Key, keyValuePair.Value, true });
                }
                float num = (float)___m_recipeList.Count * __instance.m_recipeListSpace;
                num = Mathf.Max(___m_recipeListBaseSize, num);
                __instance.m_recipeListRoot.SetSizeWithCurrentAnchors((RectTransform.Axis)1, num);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("UpdateRecipe")]
        internal static bool PrefixUpdateRecipe(
            InventoryGui __instance, Player player, float dt, // basic method parameters
            // accessing private fields !!
            KeyValuePair<Recipe, ItemDrop.ItemData> ___m_selectedRecipe, Color ___m_minStationLevelBasecolor,
            float ___m_craftTimer, Recipe ___m_craftRecipe, ItemDrop.ItemData ___m_craftUpgradeItem, int ___m_selectedVariant)
        {
            if (ValheimRecycle.instance.InTabDeconstruct())
            {
                CraftingStation currentCraftingStation = player.GetCurrentCraftingStation();
                if (currentCraftingStation)
                {
                    __instance.m_craftingStationName.text = Localization.instance.Localize(currentCraftingStation.m_name);
                    __instance.m_craftingStationIcon.gameObject.SetActive(true);
                    __instance.m_craftingStationIcon.sprite = currentCraftingStation.m_icon;
                    int level = currentCraftingStation.GetLevel();
                    __instance.m_craftingStationLevel.text = level.ToString();
                    __instance.m_craftingStationLevelRoot.gameObject.SetActive(true);
                }
                else
                {
                    __instance.m_craftingStationName.text = Localization.instance.Localize("$hud_crafting");
                    __instance.m_craftingStationIcon.gameObject.SetActive(false);
                    __instance.m_craftingStationLevelRoot.gameObject.SetActive(false);
                }
                if (___m_selectedRecipe.Key)
                {
                    __instance.m_recipeIcon.enabled = true;
                    __instance.m_recipeName.enabled = true;


                    ItemDrop.ItemData value = ___m_selectedRecipe.Value;
                    // don't show item description if item will be destroyed in process
                    if (value.m_quality == 1)
                    {
                        __instance.m_recipeDecription.enabled = false;
                    }
                    else
                    {
                        __instance.m_recipeDecription.enabled = true;
                    }
                    // edit here
                    int num = (value != null) ? (value.m_quality >= 1 ? value.m_quality - 1 : 0) : 1;
                    bool flag = num <= ___m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_maxQuality;
                    int num2 = (value != null) ? value.m_variant : ___m_selectedVariant;
                    __instance.m_recipeIcon.sprite = ___m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_icons[num2];
                    // edit here
                    string text = Localization.instance.Localize(___m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_name);
                    if (___m_selectedRecipe.Key.m_amount > 1)
                    {
                        text = text + " x" + ___m_selectedRecipe.Key.m_amount;
                    }
                    __instance.m_recipeName.text = text;

                    __instance.m_recipeDecription.text = Localization.instance.Localize(ItemDrop.ItemData.GetTooltip(___m_selectedRecipe.Key.m_item.m_itemData, num, true));
                    if (value != null)
                    {
                        __instance.m_itemCraftType.gameObject.SetActive(true);
                        if (value.m_quality <= 1)
                        {
                            __instance.m_itemCraftType.text = "Item will be destroyed";
                        }
                        else
                        {
                            string text2 = Localization.instance.Localize(value.m_shared.m_name);
                            __instance.m_itemCraftType.text = "Downgrade " + text2 + " quality to " + (value.m_quality - 1).ToString();
                        }
                    }
                    else
                    {
                        __instance.m_itemCraftType.gameObject.SetActive(false);
                    }
                    __instance.m_variantButton.gameObject.SetActive(___m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_variants > 1 && ___m_selectedRecipe.Value == null);


                    if (methodInfo_SetupRequirementList == null)
                        methodInfo_SetupRequirementList = typeof(InventoryGui).GetMethod("SetupRequirementList", BindingFlags.NonPublic | BindingFlags.Instance);
                    methodInfo_SetupRequirementList.Invoke(__instance, new object[] { num + 1, player, flag });

                    int requiredStationLevel = ___m_selectedRecipe.Key.GetRequiredStationLevel(num);
                    CraftingStation requiredStation = ___m_selectedRecipe.Key.GetRequiredStation(num);
                    if (requiredStation != null && flag)
                    {
                        __instance.m_minStationLevelIcon.gameObject.SetActive(true);
                        __instance.m_minStationLevelText.text = requiredStationLevel.ToString();
                        if (currentCraftingStation == null || currentCraftingStation.GetLevel() < requiredStationLevel)
                        {
                            __instance.m_minStationLevelText.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : ___m_minStationLevelBasecolor);
                        }
                        else
                        {
                            __instance.m_minStationLevelText.color = ___m_minStationLevelBasecolor;
                        }
                    }
                    else
                    {
                        __instance.m_minStationLevelIcon.gameObject.SetActive(false);
                    }
                    // have requirements always true, as item is already present in inventory
                    bool flag2 = true;
                    // count number of slots required to deconstruct
                    bool flag3 = Utils.HaveEmptySlotsForRecipe(player.GetInventory(), ___m_selectedRecipe.Key, num + 1);
                    bool flag4 = !requiredStation || (currentCraftingStation && currentCraftingStation.CheckUsable(player, false));
                    __instance.m_craftButton.interactable = (((flag2 && flag4) || player.NoCostCheat()) && flag3 && flag);
                    Text componentInChildren = __instance.m_craftButton.GetComponentInChildren<Text>();
                    componentInChildren.text = "Recycle";
                    UITooltip component = __instance.m_craftButton.GetComponent<UITooltip>();
                    if (!flag3)
                    {
                        component.m_text = Localization.instance.Localize("$inventory_full");
                    }
                    else if (!flag4)
                    {
                        component.m_text = Localization.instance.Localize("$msg_missingstation");
                    }
                    else
                    {
                        component.m_text = "";
                    }
                }
                else
                {
                    __instance.m_recipeIcon.enabled = false;
                    __instance.m_recipeName.enabled = false;
                    __instance.m_recipeDecription.enabled = false;
                    __instance.m_qualityPanel.gameObject.SetActive(false);
                    __instance.m_minStationLevelIcon.gameObject.SetActive(false);
                    __instance.m_craftButton.GetComponent<UITooltip>().m_text = "";
                    __instance.m_variantButton.gameObject.SetActive(false);
                    __instance.m_itemCraftType.gameObject.SetActive(false);
                    for (int i = 0; i < __instance.m_recipeRequirementList.Length; i++)
                    {
                        InventoryGui.HideRequirement(__instance.m_recipeRequirementList[i].transform);
                    }
                    __instance.m_craftButton.interactable = false;
                }
                if (___m_craftTimer < 0f)
                {
                    __instance.m_craftProgressPanel.gameObject.SetActive(false);
                    __instance.m_craftButton.gameObject.SetActive(true);
                    return false;
                }
                __instance.m_craftButton.gameObject.SetActive(false);
                __instance.m_craftProgressPanel.gameObject.SetActive(true);
                __instance.m_craftProgressBar.SetMaxValue(__instance.m_craftDuration);
                __instance.m_craftProgressBar.SetValue(___m_craftTimer);
                ___m_craftTimer += dt;
                if (___m_craftTimer >= __instance.m_craftDuration)
                {
                    if (ValheimRecycle.instance.InTabDeconstruct())
                    {
                        // required fields m_craftRecipe, m_craftUpgradeItem
                        Utils.DoRecycle(player, __instance, ref ___m_craftRecipe, ref ___m_craftUpgradeItem);
                    }
                    else
                    {
                        if (methodInfo_DoCrafting == null)
                            methodInfo_DoCrafting = typeof(InventoryGui).GetMethod("DoCrafting", BindingFlags.NonPublic | BindingFlags.Instance);
                        methodInfo_DoCrafting.Invoke(__instance, new object[] { player });
                    }
                    ___m_craftTimer = -1f;
                }
                return false;
            }
            return true;
        }
    }

}