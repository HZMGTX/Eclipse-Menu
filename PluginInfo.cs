/*
 * Eclipse Menu  PluginInfo.cs
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

namespace Eclipse
{
    public class PluginInfo
    {
        public const string GUID = "org.eclipse.gorillatag.eclipsemenu";
        public const string Name = "Eclipse Menu";
        public const string Description = "A brand-new mod menu for Gorilla Tag, remade from Nova Menu.";
        public const string BuildTimestamp = "2026-10-19T06:09:14Z";
        public const string Version = "1.1.0";

        public const string BaseDirectory =
#if LEGAL || LEGAL_DEBUG
            "EclipseMenu/Legal";
#else
            "EclipseMenu";
#endif
        public const string ClientResourcePath = "EclipseMenu.Resources.Client";
        public const string ServerResourcePath = "https://raw.githubusercontent.com/HZMGTX/Eclipse-Menu/main/Resources/Server";

        // The soundboard sounds and the tutorial videos are large, so Eclipse keeps loading them from Nova's repo
        public const string SharedResourcePath = "https://raw.githubusercontent.com/HZMGTX/Nova/master/Resources/Server";

        // Nova and Eclipse patch the same game methods and share network events, so they never load together
        public const string NovaGUID = "org.nova.gorillatag.novamenu";
        public const string ServerAPI = "https://www.menu.management";
        public const string Logo = @"um";

#if DEBUG || LEGAL_DEBUG
        public static bool BetaBuild = true;
#else
        public static bool BetaBuild = false;
#endif
    }
}
