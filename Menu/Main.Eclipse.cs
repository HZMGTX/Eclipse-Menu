/*
 * Eclipse Menu  Menu/Main.Eclipse.cs
 * A brand-new mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  HZMGTX
 * https://github.com/HZMGTX/Eclipse-Menu
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using Eclipse.Classes.Menu;
using Eclipse.Managers;
using Eclipse.Mods;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Eclipse.Menu
{
    /// <summary>
    /// The Eclipse Panel: the menu's own layout. Tabs down the left side group the categories,
    /// the categories of the open tab sit down the right side, the mods stay in the middle and the
    /// page arrows move to a row along the bottom.
    /// </summary>
    public partial class Main
    {
        public const int LayoutPanel = 0, LayoutOrbit = 1, LayoutGrid = 2, LayoutClassic = 3;
        public static readonly string[] MenuLayoutNames = { "Eclipse Panel", "Orbit Wheel", "Holo Grid", "Classic" };
        public static int menuLayout = LayoutPanel;

        public const string EclipseTabPrefix = "EclipseTab:";
        public const string EclipseChipPrefix = "EclipseCat:";
        public const string EclipseMoreChips = "EclipseMore";

        /// <summary>Rounded tiles, the glowing rim, the star pointer and the push-in on press.</summary>
        public static bool EclipseModel => menuLayout != LayoutClassic && !shouldRound;

        /// <summary>
        /// Whether this menu gets the Eclipse frame: the tab rails and the page row. The Eclipse
        /// Panel, the Orbit Wheel and the Holo Grid all share it. Anything that depends on the
        /// classic list's layout (typing, prompts, the joystick and watch menus, long or scrolling
        /// pages) is drawn the classic way until it is done.
        /// </summary>
        public static bool EclipsePanelActive =>
            menuLayout != LayoutClassic && CurrentPrompt == null && !inTextInput && !joystickMenu && !watchMenu &&
            !longmenu && !disorganized && !pageScrolling && !annoyingMode && buttonOffset == 0;

        private sealed class EclipseGroup
        {
            public string Name;
            public string[] Entries;
            public bool AdminOnly;
            public Func<string[]> DynamicEntries;
        }

        // Each entry is a category, or the name of the button that opens one (Players and
        // Achievements build their page when opened, so they go through their button)
        private static readonly EclipseGroup[] EclipseGroups =
        {
            new EclipseGroup { Name = "Home", Entries = new[] { DashboardCategory, "Main", "Favorite Mods", "Enabled Mods", "Players", "Friends", "Achievements", "What's New", "Credits" } },
            new EclipseGroup { Name = "Eclipse", Entries = new[] { "Eclipse Mods" } },
            new EclipseGroup { Name = "Movement", Entries = new[] { "Movement Mods", "Macros" } },
            new EclipseGroup { Name = "Visuals", Entries = new[] { "Visual Mods" } },
            new EclipseGroup { Name = "Fun", Entries = new[] { "Fun Mods", "Custom Mods", "Sound Mods", "Soundboard", "Voice Changers", "Custom Map Mods" } },
            new EclipseGroup { Name = "Room", Entries = new[] { "Room Mods", "Important Mods", "Safety Mods", "Room Mod Checker" } },
            new EclipseGroup { Name = "Combat", Entries = new[] { "Advantage Mods", "Projectile Mods" } },
            new EclipseGroup { Name = "Power", Entries = new[] { "Master Mods", "Overpowered Mods", "Experimental Mods", "Detected Mods" } },
            new EclipseGroup { Name = "Admin", AdminOnly = true, Entries = new[] { "Admin Mods", "Mod Givers", "Admin World Controls", "Console Assets" } },
            new EclipseGroup { Name = "Settings", DynamicEntries = SettingsEntries }
        };

        private static readonly Dictionary<string, string> EclipseChipNames = new Dictionary<string, string>
        {
            { DashboardCategory, "Dashboard" },
            { "Main", "Hub" },
            { "Favorite Mods", "Favorites" },
            { "Enabled Mods", "Enabled" },
            { "Room Mod Checker", "Mod Checker" },
            { "Custom Map Mods", "Maps" },
            { "Voice Changers", "Voices" },
            { "Admin World Controls", "World" },
            { "Console Assets", "Assets" },
            { "Settings", "All Settings" },
            { "Category Settings", "Categories" }
        };

        private static int eclipseGroup;
        private static int eclipseChipPage;
        private static string eclipseLastCategory;
        private static string eclipseLastEntry;

        private const int EclipseRailSlots = 10;
        private const float EclipseRailWidth = 0.46f;
        private const float EclipseRailGap = 0.035f;

        // Every page the settings hub links to, in the hub's own order
        private static string[] SettingsEntries()
        {
            List<string> entries = new List<string> { "Settings" };
            int settings = Buttons.GetCategory("Settings");
            if (settings >= 0)
                entries.AddRange(Buttons.buttons[settings]
                    .Where(button => button != null && !button.isTogglable && !button.label && !button.buttonText.StartsWith("Exit "))
                    .Select(button => button.buttonText));
            return entries.ToArray();
        }

        private static string[] EntriesOf(EclipseGroup group) =>
            group.DynamicEntries != null ? group.DynamicEntries() : group.Entries;

        private static bool EntryVisible(string entry)
        {
            if (skipButtons.Contains(entry))
                return false;

            ButtonInfo opener = Buttons.GetIndex(entry);
#if LEGAL || LEGAL_DEBUG
            return entry == "Main" || (opener != null && opener.legal);
#else
            return opener != null || Buttons.GetCategory(entry) >= 0;
#endif
        }

        private static string[] VisibleEntries(EclipseGroup group)
        {
            if (group.AdminOnly && !isAdmin)
                return Array.Empty<string>();

            return EntriesOf(group).Where(EntryVisible).ToArray();
        }

        // The tab a category belongs to, or -1 for pages that aren't on any tab (a player's page, presets)
        private static int GroupOfCategory(string category)
        {
            for (int i = 0; i < EclipseGroups.Length; i++)
            {
                if (EntriesOf(EclipseGroups[i]).Contains(category))
                    return i;
            }

            if (category.Contains("Admin") || category.StartsWith("Console") || category == "Spawned Assets")
                return Array.FindIndex(EclipseGroups, group => group.Name == "Admin");
            if (category.EndsWith("Settings") || category == "Menu Presets")
                return Array.FindIndex(EclipseGroups, group => group.Name == "Settings");
            if (category == "Sound Library" || category == "Sound Properties")
                return Array.FindIndex(EclipseGroups, group => group.Name == "Fun");
            if (category.StartsWith("Room Mod Checker"))
                return Array.FindIndex(EclipseGroups, group => group.Name == "Room");
            if (category == "Detected Settings" || category == "Patreon Mods")
                return Array.FindIndex(EclipseGroups, group => group.Name == "Power");

            return -1;
        }

        private static string ChipName(string entry)
        {
            if (EclipseChipNames.TryGetValue(entry, out string name))
                return name;

            foreach (string suffix in new[] { " Mods", " Settings" })
            {
                if (entry.EndsWith(suffix) && entry.Length > suffix.Length)
                    return entry.Substring(0, entry.Length - suffix.Length);
            }

            return entry;
        }

        /// <summary>Draws the tab rail, the category rail and the page row.</summary>
        private static void DrawEclipsePanel()
        {
            string category = Buttons.CurrentCategoryName;
            int owner = GroupOfCategory(category);
            if (owner >= 0)
                eclipseGroup = owner;

            List<int> tabs = new List<int>();
            for (int i = 0; i < EclipseGroups.Length; i++)
            {
                if (VisibleEntries(EclipseGroups[i]).Length > 0)
                    tabs.Add(i);
            }

            if (!tabs.Contains(eclipseGroup) && tabs.Count > 0)
                eclipseGroup = tabs[0];

            float half = thinMenu ? 0.5f : 0.75f;
            float leftCentre = half + EclipseRailGap + EclipseRailWidth / 2f;

            // Tabs
            GameObject leftRail = AddEclipseSlab(new Vector3(0.5f, leftCentre, 0f), new Vector3(0.1f, EclipseRailWidth, 1f));
            AddEclipseRim(leftRail);

            float tabStep = Mathf.Min(0.092f, 0.92f / Mathf.Max(1, Mathf.Min(tabs.Count, EclipseRailSlots)));
            for (int i = 0; i < tabs.Count && i < EclipseRailSlots; i++)
            {
                EclipseGroup group = EclipseGroups[tabs[i]];
                AddEclipseTile(EclipseTabPrefix + tabs[i], group.Name, leftCentre, 0.46f - tabStep * (i + 0.5f), tabStep, tabs[i] == eclipseGroup, -1f);
            }

            // Categories in the open tab
            string[] entries = tabs.Count > 0 ? VisibleEntries(EclipseGroups[eclipseGroup]) : Array.Empty<string>();
            GameObject rightRail = AddEclipseSlab(new Vector3(0.5f, -leftCentre, 0f), new Vector3(0.1f, EclipseRailWidth, 1f));
            AddEclipseRim(rightRail);

            // Pages built by a button (Players, Category Settings) live in a temporary category
            int current = Array.IndexOf(entries, category);
            if (current < 0 && category == "Temporary Category" && eclipseLastEntry != null && Buttons.GetCategory(eclipseLastEntry) < 0)
                current = Array.IndexOf(entries, eclipseLastEntry);

            bool paged = entries.Length > EclipseRailSlots;
            int perPage = paged ? EclipseRailSlots - 1 : EclipseRailSlots;
            int chipPages = Mathf.Max(1, (entries.Length + perPage - 1) / perPage);

            // Follow the page to wherever the menu went, unless the player is flicking through it
            if (category != eclipseLastCategory && current >= 0)
                eclipseChipPage = current / perPage;
            eclipseLastCategory = category;
            eclipseChipPage = Mathf.Clamp(eclipseChipPage, 0, chipPages - 1);

            string[] shown = entries.Skip(eclipseChipPage * perPage).Take(perPage).ToArray();
            int slots = shown.Length + (paged ? 1 : 0);
            float chipStep = Mathf.Min(0.092f, 0.92f / Mathf.Max(1, slots));
            for (int i = 0; i < shown.Length; i++)
                AddEclipseTile(EclipseChipPrefix + shown[i], ChipName(shown[i]), -leftCentre, 0.46f - chipStep * (i + 0.5f), chipStep, Array.IndexOf(entries, shown[i]) == current, 1f);

            if (paged)
                AddEclipseTile(EclipseMoreChips, $"More {eclipseChipPage + 1}/{chipPages}", -leftCentre, 0.46f - chipStep * (shown.Length + 0.5f), chipStep, false, 1f);

            // Page row along the bottom, between the search and info buttons
            if (!disablePageButtons)
            {
                ExtGradient arrows = buttonColors[swapButtonColors ? 1 : 0];
                AdvancedAddButton("PreviousPage", new Vector3(0.09f, 0.12f, 0.08f), new Vector3(0.56f, 0.17f, -0.58f), new Vector3(0.064f, 0.17f * 0.3f, -0.58f * 0.3825f), arrows, new Vector2(0.03f, 0.03f), 0);
                AddEclipseText($"{pageNumber + 1} / {Mathf.Max(0, LastPage) + 1}", new Vector3(0.064f, 0f, -0.58f * 0.3825f), new Vector2(0.06f, 0.018f), textColors[0]);
                AdvancedAddButton("NextPage", new Vector3(0.09f, 0.12f, 0.08f), new Vector3(0.56f, -0.17f, -0.58f), new Vector3(0.064f, -0.17f * 0.3f, -0.58f * 0.3825f), arrows, new Vector2(0.03f, 0.03f), 1);
            }
        }

        // A background slab under the rails, coloured like the panel
        private static GameObject AddEclipseSlab(Vector3 position, Vector3 scale)
        {
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(slab.GetComponent<BoxCollider>());
            slab.transform.parent = menu.transform;
            slab.transform.rotation = Quaternion.identity;
            slab.transform.localPosition = position;
            slab.transform.localScale = scale;

            slab.AddComponent<ColorChanger>().colors = menuBackgroundColor;
            FollowMenuSettings(slab, false);
            return slab;
        }

        /// <summary>A thin accent-coloured rim just behind a slab, so the panel's edge glows.</summary>
        private static void AddEclipseRim(GameObject slab)
        {
            if (!EclipseModel || shouldOutline || slab == null)
                return;

            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(rim.GetComponent<BoxCollider>());
            rim.transform.parent = menu.transform;
            rim.transform.rotation = Quaternion.identity;

            Vector3 position = slab.transform.localPosition;
            Vector3 scale = slab.transform.localScale;
            rim.transform.localPosition = position - new Vector3(0.004f, 0f, 0f);

            // The same thickness on every side, after the menu's own uneven scale
            rim.transform.localScale = new Vector3(scale.x * 0.8f, scale.y + 0.03f, scale.z + 0.03f * 0.3f / 0.3825f);

            rim.AddComponent<ColorChanger>().colors = backgroundColor;
        }

        // A tab or category tile with its label. `inner` is the side the panel is on (-1 or 1).
        private static void AddEclipseTile(string relatedText, string label, float centreY, float z, float step, bool selected, float inner)
        {
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (!keyboardMenuCondition && !isKeyboardPc)
                tile.layer = 2;

            tile.GetComponent<BoxCollider>().isTrigger = true;
            tile.transform.parent = menu.transform;
            tile.transform.rotation = Quaternion.identity;
            tile.transform.localScale = new Vector3(0.09f, EclipseRailWidth - 0.06f, step * 0.82f);
            tile.transform.localPosition = new Vector3(0.56f, centreY, z);

            tile.AddComponent<ButtonCollider>().relatedText = relatedText;

            bool on = selected ^ swapButtonColors;
            tile.AddComponent<ColorChanger>().colors = buttonColors[on ? 1 : 0];
            FollowMenuSettings(tile, !on);

            if (selected)
            {
                // A small notch on the side facing the mods
                GameObject notch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(notch.GetComponent<BoxCollider>());
                notch.transform.parent = menu.transform;
                notch.transform.rotation = Quaternion.identity;
                notch.transform.localScale = new Vector3(0.03f, 0.022f, step * 0.5f);
                notch.transform.localPosition = new Vector3(0.6f, centreY + inner * (EclipseRailWidth / 2f - 0.065f), z);
                notch.AddComponent<ColorChanger>().colors = textColors[on ? 2 : 1];
            }

            AddEclipseText(label, new Vector3(0.064f, (centreY - inner * 0.012f) * 0.3f, z * 0.3825f), new Vector2((EclipseRailWidth - 0.13f) * 0.3f, step * 0.82f * 0.3825f * 0.8f), textColors[on ? 2 : 1]);
        }

        private static TextMeshPro AddEclipseText(string text, Vector3 position, Vector2 size, ExtGradient color)
        {
            TextMeshPro label = new GameObject { transform = { parent = canvasObj.transform } }.AddComponent<TextMeshPro>();
            label.font = activeFont;
            label.text = FollowMenuSettings(text);
            label.fontSize = 1;
            label.richText = true;
            label.fontStyle = activeFontStyle;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 0;
            label.AddComponent<UIColorChanger>().colors = color;

            RectTransform rect = label.GetComponent<RectTransform>();
            rect.sizeDelta = NoAutoSizeText ? new Vector2(9f, 0.015f) : size;
            rect.localPosition = position;
            rect.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            if (hideTextOnCamera)
                rect.gameObject.layer = 19;

            FollowMenuSettings(label);
            return label;
        }

        /// <summary>Handles the panel's own buttons. Returns false for any other button.</summary>
        private static bool EclipsePanelClick(string buttonText)
        {
            if (buttonText == EclipseMoreChips)
            {
                eclipseChipPage++;
                int perPage = EclipseRailSlots - 1;
                int count = VisibleEntries(EclipseGroups[eclipseGroup]).Length;
                if (eclipseChipPage * perPage >= count)
                    eclipseChipPage = 0;
                return true;
            }

            if (buttonText.StartsWith(EclipseTabPrefix))
            {
                if (!int.TryParse(buttonText.Substring(EclipseTabPrefix.Length), out int group) || group < 0 || group >= EclipseGroups.Length)
                    return true;

                string[] entries = VisibleEntries(EclipseGroups[group]);
                if (entries.Length == 0)
                    return true;

                eclipseGroup = group;
                eclipseChipPage = 0;
                NoteEclipseTab(EclipseGroups[group].Name);

                // Clicking the open tab again goes back to its first page
                OpenEclipseEntry(entries[0]);
                return true;
            }

            if (buttonText.StartsWith(EclipseChipPrefix))
            {
                OpenEclipseEntry(buttonText.Substring(EclipseChipPrefix.Length));
                return true;
            }

            return false;
        }

        // Opens a category the same way its button on the hub would
        private static void OpenEclipseEntry(string entry)
        {
            if (isSearching)
                Settings.Search();

            eclipseLastEntry = entry;

            ButtonInfo opener = Buttons.GetIndex(entry);
            if (opener != null && !opener.isTogglable && !opener.label && opener.method != null)
            {
                if (opener.detected && !allowDetected)
                {
                    NotificationManager.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> This mod is detected and requires permission to run.");
                    return;
                }

                try { opener.method.Invoke(); }
                catch (Exception exc)
                {
                    LogManager.LogError($"Error opening {entry} at {exc.StackTrace}: {exc.Message}");
                }
                return;
            }

            if (Buttons.GetCategory(entry) >= 0)
                Buttons.CurrentCategoryName = entry;
        }

        // ── Orbit Wheel and Holo Grid ───────────────────────────────────────────

        // Set to LayoutOrbit or LayoutGrid while a page's buttons are being placed as tiles
        private static int eclipseTileLayout;

        /// <summary>Which tile layout this page's buttons get, or 0 for the list.</summary>
        private static int EclipseTileLayoutFor(ButtonInfo[] page)
        {
            if (!EclipsePanelActive || (menuLayout != LayoutOrbit && menuLayout != LayoutGrid) || checkMode)
                return 0;

            if (PageSize > (menuLayout == LayoutOrbit ? 10 : 12))
                return 0;

            // Cyclers need their arrows either side, so a page of settings stays a list
            if (incrementalButtons && page.Any(button => button != null && button.incremental))
                return 0;

            return menuLayout;
        }

        /// <summary>Where a page button goes in the Orbit Wheel or the Holo Grid.</summary>
        private static bool EclipseTilePlacement(int index, out Vector3 position, out Vector3 scale, out Vector3 textPosition, out Vector2 textSize)
        {
            float y, z, width, height;
            switch (eclipseTileLayout)
            {
                case LayoutGrid:
                    {
                        // Two columns of square-ish tiles over the same space as the list
                        int rows = Mathf.Max(1, Mathf.CeilToInt(PageSize / 2f));
                        float pitch = 0.78f / rows;
                        width = thinMenu ? 0.43f : 0.63f;
                        height = pitch * 0.82f;
                        y = (index % 2 == 0 ? 1f : -1f) * (width / 2f + 0.02f);
                        z = 0.32f - pitch * (index / 2 + 0.5f);
                        break;
                    }
                case LayoutOrbit:
                    {
                        // Round the wheel clockwise from the top
                        float angle = index * Mathf.PI * 2f / Mathf.Max(1, PageSize);
                        width = thinMenu ? 0.3f : 0.4f;
                        height = 0.09f;
                        y = -Mathf.Sin(angle) * (thinMenu ? 0.33f : 0.5f);
                        z = Mathf.Cos(angle) * 0.33f + OrbitCentre;
                        break;
                    }
                default:
                    position = scale = textPosition = Vector3.zero;
                    textSize = Vector2.zero;
                    return false;
            }

            position = new Vector3(0.56f, y, z);
            scale = new Vector3(0.09f, width, height);
            textPosition = new Vector3(0.064f, y * 0.3f, z * 0.3825f);
            textSize = new Vector2((width - 0.05f) * 0.3f, height * 0.3825f * 0.8f);
            return true;
        }

        private const float OrbitCentre = -0.05f;

        /// <summary>The pieces that go with a tile layout: the wheel's hub and orbit, or the grid's scanlines.</summary>
        private static void DrawEclipseTileExtras(int layout)
        {
            if (layout == LayoutOrbit)
            {
                float ry = thinMenu ? 0.33f : 0.5f;

                // The orbit itself, a dotted ellipse through the tiles
                ExtGradient orbitColor = new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.Lerp(menuBackgroundColor.GetColor(0), backgroundColor.GetColor(0), 0.6f)) };
                for (int i = 0; i < 32; i++)
                {
                    float angle = (i + 0.5f) * Mathf.PI * 2f / 32f;
                    AddEclipseDecor(new Vector3(0.552f, -Mathf.Sin(angle) * ry, Mathf.Cos(angle) * 0.33f + OrbitCentre), new Vector3(0.012f, 0.018f, 0.018f * 0.3f / 0.3825f), orbitColor);
                }

                // The hub: a dark disc with the page's name on it
                GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(hub.GetComponent<Collider>());
                hub.transform.parent = menu.transform;
                hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                hub.transform.localPosition = new Vector3(0.56f, 0f, OrbitCentre);
                hub.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f * 0.3f / 0.3825f);
                hub.AddComponent<ColorChanger>().colors = menuBackgroundColor;

                string name = Buttons.CurrentCategoryName == "Temporary Category" && eclipseLastEntry != null ? eclipseLastEntry : Buttons.CurrentCategoryName;
                AddEclipseText(ChipName(name), new Vector3(0.064f, 0f, OrbitCentre * 0.3825f), new Vector2(0.08f, 0.028f), textColors[0]);
            }
            else if (layout == LayoutGrid)
            {
                // Faint scanlines across the panel, for the hologram look
                ExtGradient lineColor = new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.Lerp(menuBackgroundColor.GetColor(0), backgroundColor.GetColor(0), 0.25f)) };
                float width = (thinMenu ? 1f : 1.5f) * 0.96f;
                for (float z = 0.45f; z > -0.47f; z -= 0.075f)
                    AddEclipseDecor(new Vector3(0.552f, 0f, z), new Vector3(0.008f, width, 0.004f), lineColor);
            }
        }

        private static void AddEclipseDecor(Vector3 position, Vector3 scale, ExtGradient color)
        {
            GameObject decor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(decor.GetComponent<BoxCollider>());
            decor.transform.parent = menu.transform;
            decor.transform.rotation = Quaternion.identity;
            decor.transform.localPosition = position;
            decor.transform.localScale = scale;
            decor.AddComponent<ColorChanger>().colors = color;
        }

        public static void ApplyMenuLayout(int index)
        {
            menuLayout = Mathf.Clamp(index, 0, MenuLayoutNames.Length - 1);
            NoteEclipseLayout(menuLayout);
        }

        // Starts small, grows a little past full size and settles
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float k = t - 1f;
            return 1f + c3 * k * k * k + c1 * k * k;
        }
    }
}
