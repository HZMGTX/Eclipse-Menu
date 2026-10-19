/*
 * Eclipse Menu  Plugin.BepInEx.cs
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

using BepInEx;
using Eclipse.Managers;
using Eclipse.Menu;
using System.ComponentModel;

namespace Eclipse
{
    [Description(PluginInfo.Description)]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    // Nova and Eclipse patch the same game methods and share network events, so BepInEx
    // skips Eclipse (and logs why) when Nova Menu is installed too
    [BepInIncompatibility(PluginInfo.NovaGUID)]
    public class PluginBepInEx : BaseUnityPlugin
    {
        public static bool FirstLaunch;

        private void Awake()
        {
            LogManager.SetLogger((level, msg) =>
            {
                switch (level)
                {
                    case Level.Error:
                        Logger.LogError(msg);
                        break;
                    case Level.Warning:
                        Logger.LogWarning(msg);
                        break;
                    case Level.Debug:
                        Logger.LogDebug(msg);
                        break;
                    default:
                        Logger.LogInfo(msg);
                        break;
                }
            });
            Bootstrapper.Initialize();
        }


        private void OnDestroy() =>
            Main.UnloadMenu();
    }
}
