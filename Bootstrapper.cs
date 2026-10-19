/*
 * Eclipse Menu  Bootstrapper.cs
 * A brand-new mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Poison Software
 * Copyright (C) 2026  HZMGTX
 * https://github.com/HZMGTX/Eclipse-Menu
 *
 * Remade from Nova Menu (https://github.com/HZMGTX/Nova),
 * which was modified from Poison Menu (formerly Seralyth Menu)
 * https://github.com/heycanihavethis/Poison
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
using Eclipse.Menu;
using Eclipse.Patches;
using Eclipse.Patches.Menu;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Eclipse
{
    internal static class Bootstrapper
    {
        private static bool initialized;
        public static bool FirstLaunch;
        public static GameObject Loader;

        private const float IntegrityCheckIntervalSeconds = 120f;

        internal static void Initialize()
        {
            if (initialized) return;
            initialized = true;

            // A second guard for loaders without BepInIncompatibility (MelonLoader, injectors)
            if (NovaLoaded())
            {
                LogManager.LogError("Eclipse Menu was not started because Nova Menu is also installed. They patch the same game code, so remove one of them.");
                return;
            }

            FirstLaunch = !Directory.Exists(PluginInfo.BaseDirectory);
            string[] existingDirectories =
            {
                "",
                "/Sounds",
                "/Plugins",
                "/Backups",
                "/Macros",
                "/TTS",
                "/PlayerInfo",
                "/CustomScripts",
                "/Friends",
                "/Friends/Messages",
                "/Achievements"
            };
            foreach (string dir in existingDirectories)
            {
                string target = $"{PluginInfo.BaseDirectory}{dir}";
                if (!Directory.Exists(target))
                    Directory.CreateDirectory(target);
            }
            PatchHandler.PatchAll(true);
            if (File.Exists($"{PluginInfo.BaseDirectory}/Eclipse_Preferences.txt"))
            {
                if (File.ReadAllLines($"{PluginInfo.BaseDirectory}/Eclipse_Preferences.txt")[0]
                    .Split(";;")
                    .Contains("Accept TOS"))
                {
                    TOSPatches.enabled = true;
                }
            }
            if (File.Exists($"{PluginInfo.BaseDirectory}/Eclipse_DisableTelemetry.txt"))
                ServerData.DisableTelemetry = true;
            GorillaTagger.OnPlayerSpawned(LoadMenu);
        }
        private static bool NovaLoaded()
        {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (name != null && name.StartsWith("Nova Menu", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static void LoadMenu()
        {
            PatchHandler.PatchAll();
            Loader = new GameObject("Eclipse_Loader");
            CoroutineManager coroutineManager = Loader.AddComponent<CoroutineManager>();
            Loader.AddComponent<NotificationManager>();
            Loader.AddComponent<CustomBoardManager>();
            Loader.AddComponent<UI>();
            Loader.AddComponent<Hud>();
            UnityEngine.Object.DontDestroyOnLoad(Loader);
            coroutineManager.StartCoroutine(PatchIntegrityLoop());
        }

        private static IEnumerator PatchIntegrityLoop()
        {
            var wait = new WaitForSeconds(IntegrityCheckIntervalSeconds);

            while (true)
            {
                if (PatchHandler.instance != null)
                {
                    try
                    {
                        PatchHandler.PatchIntegrityCheck();
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError($"PatchIntegrityCheck threw: {ex}");
                    }
                }

                yield return wait;
            }
        }
    }
}
