/*
 * Eclipse Menu  Menu/Main.EclipseExtras.cs
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
using Eclipse.Extensions;
using Eclipse.Managers;
using Eclipse.Mods;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eclipse.Menu
{
    /// <summary>
    /// The smaller things that make Eclipse its own menu: the dashboard, What's New, the Quick
    /// Wheel, pointer styles, the intro, toast notifications and Eclipse's achievements.
    /// </summary>
    public partial class Main
    {
        // ── Achievements ────────────────────────────────────────────────────────

        private static readonly HashSet<string> eclipseTabsVisited = new HashSet<string>();
        private static readonly HashSet<int> eclipseLayoutsTried = new HashSet<int>();

        /// <summary>Unlocks one of the achievements in <see cref="AchievementManager.All"/> by name.</summary>
        internal static void EclipseUnlock(string name)
        {
            AchievementManager.Achievement achievement = AchievementManager.All.FirstOrDefault(known => known.name == name);
            if (achievement.name != null)
                AchievementManager.UnlockAchievement(achievement);
        }

        private static void NoteEclipseTab(string group)
        {
            eclipseTabsVisited.Add(group);
            if (EclipseGroups.All(known => VisibleEntries(known).Length == 0 || eclipseTabsVisited.Contains(known.Name)))
                EclipseUnlock("Explorer");
        }

        private static void NoteEclipseLayout(int layout)
        {
            eclipseLayoutsTried.Add(layout);
            if (eclipseLayoutsTried.Count >= MenuLayoutNames.Length)
                EclipseUnlock("Shape Shifter");
        }

        // ── Dashboard and What's New ────────────────────────────────────────────

        public const string DashboardCategory = "Eclipse Dashboard";

        /// <summary>Fills in the dashboard's live cards.</summary>
        internal static void RefreshEclipseDashboard()
        {
            SetDashboardCard("EclipseDashFps", $"FPS  {Mathf.RoundToInt(1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f))}");
            SetDashboardCard("EclipseDashPing", PhotonNetwork.IsConnected ? $"Ping  {PhotonNetwork.GetPing()} ms" : "Offline");
            SetDashboardCard("EclipseDashRoom", PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null
                ? $"Room  {PhotonNetwork.CurrentRoom.Name}  |  {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}"
                : "Not in a room");
            SetDashboardCard("EclipseDashTime", $"{DateTime.Now:HH:mm}  |  {DateTime.Now:ddd d MMM}");

            int on = 0;
            for (int category = 0; category < Buttons.buttons.Length && category < Buttons.categoryNames.Length; category++)
            {
                if (Buttons.categoryNames[category].Contains("Settings"))
                    continue;

                foreach (ButtonInfo button in Buttons.buttons[category])
                {
                    if (button != null && button.enabled && button.isTogglable)
                        on++;
                }
            }

            SetDashboardCard("EclipseDashMods", $"{on} {(on == 1 ? "mod" : "mods")} on  |  {favorites.Count} {(favorites.Count == 1 ? "favourite" : "favourites")}");
        }

        private static void SetDashboardCard(string name, string text)
        {
            ButtonInfo card = Buttons.GetIndex(name);
            if (card != null && card.overlapText != text)
                card.overlapText = text;
        }

        public static void OpenEclipseDashboard()
        {
            RefreshEclipseDashboard();
            Buttons.CurrentCategoryName = DashboardCategory;
        }

        // Labels on the dashboard sit on a card, like a button you can't press
        private static void AddEclipseCard(Vector3 position, Vector3 scale)
        {
            GameObject card = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(card.GetComponent<BoxCollider>());
            card.transform.parent = menu.transform;
            card.transform.rotation = Quaternion.identity;
            card.transform.localPosition = position - new Vector3(0.01f, 0f, 0f);
            card.transform.localScale = new Vector3(scale.x * 0.8f, scale.y, scale.z);
            card.AddComponent<ColorChanger>().colors = buttonColors[0];
            FollowMenuSettings(card, false);
        }

        // ── Pointer styles ──────────────────────────────────────────────────────

        public static readonly string[] PointerStyleNames = { "Star", "Ring", "Comet", "Orb" };
        public static int pointerStyle;

        public static void ApplyPointerStyle(int index) =>
            pointerStyle = Mathf.Clamp(index, 0, PointerStyleNames.Length - 1);

        // Reshapes the pointer; the collider keeps the sphere's size so clicks feel the same
        private static void StyleEclipsePointer()
        {
            if (reference == null || pointerStyle == 3 || hidePointer)
                return;

            MeshFilter filter = reference.GetComponent<MeshFilter>();
            switch (pointerStyle)
            {
                case 0:
                    filter.sharedMesh = EclipseMeshes.Star;
                    reference.transform.localScale = new Vector3(0.018f, 0.018f, 0.018f);
                    reference.AddComponent<EclipseSpin>();
                    break;
                case 1:
                    filter.sharedMesh = EclipseMeshes.Torus;
                    reference.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                    reference.AddComponent<EclipseSpin>().speed = 120f;
                    break;
                case 2:
                    {
                        reference.transform.localScale = new Vector3(0.012f, 0.012f, 0.012f);

                        GameObject tail = new GameObject("Eclipse_PointerTail");
                        tail.transform.SetParent(reference.transform, false);
                        TrailRenderer trail = tail.AddComponent<TrailRenderer>();
                        trail.material = Custom.VertexColored;
                        trail.time = 0.25f;
                        trail.minVertexDistance = 0.005f;
                        trail.widthMultiplier = 0.01f;
                        trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                        Color head = backgroundColor.GetColor(0);
                        trail.startColor = head;
                        trail.endColor = new Color(head.r, head.g, head.b, 0f);
                        break;
                    }
            }

            buttonCollider.radius = 0.5f * 0.01f / reference.transform.localScale.x;
        }

        // ── Quick Wheel ─────────────────────────────────────────────────────────

        private sealed class WheelItem
        {
            public string Name;
            public Custom.Piece Orb;
            public TextMeshPro Label;
            public Vector3 Offset;
        }

        private static readonly List<WheelItem> wheelItems = new List<WheelItem>();
        private static TextMeshPro wheelTitle;
        private static Vector3 wheelCentre, wheelRight, wheelUp;
        private static int wheelSelected = -1;
        private static bool wheelHeld = true;

        /// <summary>
        /// Hold your left stick click to pop up a wheel of your favourite mods. Move your right hand
        /// toward one and let go to switch it.
        /// </summary>
        public static void QuickWheel()
        {
            // Opening the menu while the wheel is up closes it without switching anything
            bool held = leftJoystickClick && !joystickMenu && !watchMenu;
            bool down = held && menu == null;
            if (down && !wheelHeld)
                OpenQuickWheel();

            if (down && wheelItems.Count > 0)
                UpdateQuickWheel();

            if (!down && wheelHeld)
            {
                string chosen = !held && wheelSelected >= 0 && wheelSelected < wheelItems.Count ? wheelItems[wheelSelected].Name : null;
                CloseQuickWheel();

                if (chosen != null)
                {
                    ButtonInfo button = Buttons.GetIndex(chosen);
                    if (button != null)
                    {
                        Toggle(chosen);
                        string state = button.isTogglable ? (button.enabled ? "<color=green>on</color>" : "<color=red>off</color>") : "<color=green>used</color>";
                        NotificationManager.SendNotification($"<color=grey>[</color><color=purple>QUICK WHEEL</color><color=grey>]</color> {NoRichtextTags(button.overlapText ?? button.buttonText)} {state}");
                    }
                }
            }

            wheelHeld = down;
        }

        private static void OpenQuickWheel()
        {
            CloseQuickWheel();

            List<ButtonInfo> picks = favorites.Select(Buttons.GetIndex).Where(button => button != null && !button.label).Take(8).ToList();
            if (picks.Count == 0)
            {
                NotificationManager.SendNotification("<color=grey>[</color><color=purple>QUICK WHEEL</color><color=grey>]</color> Favourite some mods first, then hold your left stick to pick one.");
                return;
            }

            EclipseUnlock("Quick Draw");

            float scale = Custom.Scale;
            Transform head = Custom.Head;
            wheelCentre = head.position + Custom.LookFlat() * (0.45f * scale) + Vector3.down * (0.12f * scale);
            Vector3 toHead = (head.position - wheelCentre).normalized;
            wheelRight = Vector3.Cross(Vector3.up, toHead).normalized;
            wheelUp = Vector3.Cross(toHead, wheelRight).normalized;

            for (int i = 0; i < picks.Count; i++)
            {
                float angle = i * Mathf.PI * 2f / picks.Count;
                Vector3 offset = (wheelRight * Mathf.Sin(angle) + wheelUp * Mathf.Cos(angle)) * (0.17f * scale);

                WheelItem item = new WheelItem
                {
                    Name = picks[i].buttonText,
                    Orb = Custom.MakePiece(PrimitiveType.Sphere, Color.white, Vector3.one * (0.04f * scale)),
                    Label = Custom.MakeWristText("Eclipse_WheelLabel", Color.white),
                    Offset = offset
                };

                item.Label.text = NoRichtextTags(picks[i].overlapText ?? picks[i].buttonText);
                wheelItems.Add(item);
            }

            wheelTitle = Custom.MakeWristText("Eclipse_WheelTitle", Color.white);
            wheelTitle.text = "Quick Wheel";
            wheelSelected = -1;
        }

        private static void UpdateQuickWheel()
        {
            float scale = Custom.Scale;
            Transform head = Custom.Head;

            // The slice the right hand is leaning toward, once it is far enough from the middle
            Vector3 lean = GorillaTagger.Instance.rightHandTransform.position - wheelCentre;
            Vector2 flat = new Vector2(Vector3.Dot(lean, wheelRight), Vector3.Dot(lean, wheelUp));
            wheelSelected = -1;
            // Only while the hand is at the wheel; anywhere else lets go without picking anything
            if (flat.magnitude > 0.05f * scale && lean.magnitude < 0.3f * scale)
            {
                float angle = Mathf.Atan2(flat.x, flat.y);
                if (angle < 0f)
                    angle += Mathf.PI * 2f;
                wheelSelected = Mathf.RoundToInt(angle / (Mathf.PI * 2f / wheelItems.Count)) % wheelItems.Count;
            }

            Color accent = backgroundColor.GetColor(0);
            for (int i = 0; i < wheelItems.Count; i++)
            {
                WheelItem item = wheelItems[i];
                bool selected = i == wheelSelected;
                Vector3 at = wheelCentre + item.Offset;

                if (item.Orb.Alive)
                {
                    item.Orb.Object.transform.position = at;
                    item.Orb.Object.transform.localScale = Vector3.one * ((selected ? 0.06f : 0.04f) * scale);
                    Custom.SetColor(item.Orb, selected ? accent : new Color(0.75f, 0.72f, 0.85f));
                }

                if (item.Label != null)
                {
                    PlaceWheelText(item.Label, at + wheelUp * (-0.045f * scale), selected ? 0.06f : 0.045f, head);
                    item.Label.color = selected ? accent : Color.white;
                }
            }

            if (wheelTitle != null)
                PlaceWheelText(wheelTitle, wheelCentre, 0.045f, head);
        }

        private static void PlaceWheelText(TextMeshPro text, Vector3 at, float size, Transform head)
        {
            text.SafeSetFont(activeFont);
            text.transform.position = at;
            text.transform.localScale = Vector3.one * (size * Custom.Scale);
            text.transform.LookAt(head.position);
            text.transform.Rotate(0f, 180f, 0f);
        }

        public static void CloseQuickWheel()
        {
            foreach (WheelItem item in wheelItems)
            {
                Custom.Discard(item.Orb);
                if (item.Label != null)
                    Object.Destroy(item.Label.gameObject);
            }

            wheelItems.Clear();
            if (wheelTitle != null)
                Object.Destroy(wheelTitle.gameObject);
            wheelTitle = null;
            wheelSelected = -1;
        }

        // The Eclipse model's lit look (Classes/Menu/EclipseLook.cs): the glowing edges and the shiny tiles
        public static bool disableEclipseGlow;
        public static bool disableEclipseShine;

        // ── Intro ───────────────────────────────────────────────────────────────

        public static bool disableIntro;

        /// <summary>A short intro when the game starts: the eclipse's corona grows and the name fades in.</summary>
        private static IEnumerator EclipseIntro()
        {
            // Wait for the saved settings, so turning the intro off sticks
            float giveUp = Time.time + 8f;
            while (!hasLoadedPreferences && Time.time < giveUp)
                yield return null;

            yield return new WaitForSeconds(0.5f);
            EclipseUnlock("Liftoff");

            if (disableIntro || GorillaTagger.Instance == null)
                yield break;

            float scale = Custom.Scale;
            Transform head = Custom.Head;
            Vector3 facing = Custom.LookFlat();
            Vector3 centre = head.position + facing * (1.6f * scale) + Vector3.up * (0.1f * scale);

            Custom.Piece disc = Custom.MakePiece(PrimitiveType.Sphere, new Color(0.03f, 0.02f, 0.08f, 1f), Vector3.zero);
            disc.Object.transform.position = centre;
            disc.Object.transform.rotation = Quaternion.LookRotation(facing);
            LineRenderer corona = EclipseMods.MakeRing("Eclipse_IntroCorona");
            LineRenderer halo = EclipseMods.MakeRing("Eclipse_IntroHalo");

            TextMeshPro title = Custom.MakeWristText("Eclipse_IntroTitle", Color.white);
            title.text = "ECLIPSE";
            TextMeshPro subtitle = Custom.MakeWristText("Eclipse_IntroSubtitle", Color.white);
            subtitle.text = $"Menu {PluginInfo.Version}";

            SoundManager.Play(SoundManager.DefaultSounds["Open"], global: true);

            const float length = 3.2f;
            float start = Time.time;
            while (Time.time - start < length)
            {
                if (disc.Object == null || corona == null || halo == null || title == null || subtitle == null)
                    break;

                float t = (Time.time - start) / length;
                float grow = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.4f), 3f);
                float fadeOut = 1f - Mathf.Clamp01((t - 0.78f) / 0.22f);
                float text = Mathf.Clamp01((t - 0.25f) / 0.25f) * fadeOut;

                disc.Object.transform.localScale = new Vector3(0.5f, 0.5f, 0.05f) * (grow * scale);
                Custom.SetAlpha(disc, new Color(0.03f, 0.02f, 0.08f), fadeOut);

                Color accent = backgroundColor.GetColor(0);
                EclipseMods.PlaceRing(corona, centre, facing, 0.27f * grow * scale, 0.025f * scale, new Color(accent.r, accent.g, accent.b, fadeOut), t * 120f);
                EclipseMods.PlaceRing(halo, centre, facing, (0.34f + Mathf.Sin(t * 12f) * 0.01f) * grow * scale, 0.06f * scale, new Color(accent.r, accent.g, accent.b, 0.3f * fadeOut));

                PlaceWheelText(title, centre + Vector3.down * (0.4f * scale), 0.22f, head);
                title.color = new Color(1f, 1f, 1f, text);
                PlaceWheelText(subtitle, centre + Vector3.down * (0.52f * scale), 0.08f, head);
                subtitle.color = new Color(0.85f, 0.82f, 1f, text);

                yield return null;
            }

            Custom.Discard(disc);
            if (corona != null)
                Object.Destroy(corona.gameObject);
            if (halo != null)
                Object.Destroy(halo.gameObject);
            if (title != null)
                Object.Destroy(title.gameObject);
            if (subtitle != null)
                Object.Destroy(subtitle.gameObject);
        }

        // ── Toast notifications ─────────────────────────────────────────────────

        public static bool classicNotifications;

        /// <summary>Puts one notification on a card in the menu's colours, with a mark for what kind it is.</summary>
        internal static string EclipseToast(string line)
        {
            string badge;
            Color tint;
            if (line.Contains(">ENABLE<"))
            {
                badge = "+";
                tint = new Color(0.45f, 1f, 0.6f);
            }
            else if (line.Contains(">DISABLE<"))
            {
                badge = "-";
                tint = new Color(1f, 0.45f, 0.45f);
            }
            else if (line.Contains(">ERROR<") || line.Contains(">WARNING<"))
            {
                badge = "!";
                tint = new Color(1f, 0.8f, 0.3f);
            }
            else
            {
                badge = "i";
                tint = backgroundColor.GetColor(0);
            }

            Color card = menuBackgroundColor.GetColor(0);
            card.a = 0.85f;
            return $"<mark=#{ColorUtility.ToHtmlStringRGBA(card)}>  <b><color=#{ColorUtility.ToHtmlStringRGB(tint)}>{badge}</color></b>  {line}  </mark>";
        }
    }

    /// <summary>Keeps the dashboard's cards up to date while it is open.</summary>
    public class EclipseDashboardTicker : MonoBehaviour
    {
        private float next;

        private void Update()
        {
            if (Time.time < next)
                return;

            next = Time.time + 0.25f;
            Main.RefreshEclipseDashboard();
        }
    }
}
