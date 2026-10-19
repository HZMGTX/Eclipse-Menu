/*
 * Eclipse Menu  Patches/Menu/PlayerSerializePatch.cs
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

using HarmonyLib;
using Eclipse.Managers;
using System;

namespace Eclipse.Patches.Menu
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.SerializeReadShared))]
    public class PlayerSerializePatch
    {
        public static bool stopSerialization;
        public static float? delay;

        public static event Action<VRRig> OnPlayerSerialize;
        public static bool Prefix(VRRig __instance, InputStruct data)
        {
            if (stopSerialization)
                return false;

            if (delay != null)
            {
                CoroutineManager.instance.StartCoroutine(
                    Eclipse.Menu.Main.SerializationDelay(() =>
                    {
                        // delay may have been cleared since this was queued
                        float? oldDelay = delay;
                        delay = null;
                        try
                        {
                            if (__instance != null)
                                __instance.SerializeReadShared(data);
                        }
                        catch { }
                        finally
                        {
                            delay = oldDelay;
                        }
                    }, delay.Value)
                );

                return false;
            }

            return true;
        }

        public static void Postfix(VRRig __instance, InputStruct data) =>
            OnPlayerSerialize?.Invoke(__instance);
    }
}
