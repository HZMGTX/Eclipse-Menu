/*
 * Eclipse Menu  Mods/EclipseSky.cs
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
using Eclipse.Menu;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using static Eclipse.Mods.Custom;
using Random = UnityEngine.Random;

namespace Eclipse.Mods
{
    /// <summary>The Space Sky: big things hanging in your sky that only you can see, in the Space Sky category.</summary>
    /// <remarks>
    /// Everything here sits a long way off but moves with your head, so it hangs in the sky like
    /// the real thing and you can never walk up to it. It all stays within about 430 m, and while
    /// any of it is on your camera is made to see at least that far. It keeps the same size
    /// however big or small you are.
    /// </remarks>
    public static partial class EclipseMods
    {
        public const string SkyCategory = "Space Sky";

        // ── Space Sky ───────────────────────────────────────────────────────────

        // How far your camera has to see for the furthest sky piece, with a little to spare
        private const float SkyFarClip = 480f;
        private static float skyFarClipBefore = -1f;
        private static Camera skyFarClipCamera;

        /// <summary>Makes sure your own camera sees as far as the sky pieces sit, while any Space Sky mod is on.</summary>
        private static void SkyKeepFarClip()
        {
            Camera view = Camera.main;
            if (view == null || view.farClipPlane >= SkyFarClip)
                return;

            // Remember what it was, so it goes back once the last sky mod is off
            if (skyFarClipCamera != view)
            {
                skyFarClipCamera = view;
                skyFarClipBefore = view.farClipPlane;
            }

            view.farClipPlane = SkyFarClip;
        }

        /// <summary>Puts your camera's view distance back once no Space Sky mod is on.</summary>
        private static void SkyReleaseFarClip()
        {
            int category = Buttons.GetCategory(SkyCategory);
            if (category >= 0)
                foreach (ButtonInfo button in Buttons.buttons[category])
                    if (button.enabled && button.isTogglable)
                        return;

            if (skyFarClipCamera != null && skyFarClipBefore > 0f)
                skyFarClipCamera.farClipPlane = skyFarClipBefore;

            skyFarClipCamera = null;
            skyFarClipBefore = -1f;
        }

        private static Piece skyGiant, skyGiantShade;
        private static LineRenderer skyGiantGlow;
        private static readonly LineRenderer[] skyGiantRings = new LineRenderer[3];

        // Each ring's middle and width as a share of the planet's radius, innermost first, with a
        // dark gap between the bright middle ring and the outer one
        private static readonly float[] SkyRingMiddles = { 1.38f, 1.74f, 2.15f };
        private static readonly float[] SkyRingWidths = { 0.28f, 0.42f, 0.22f };
        private static readonly Color[] SkyRingColors =
        {
            new Color(0.62f, 0.56f, 0.5f, 0.3f), new Color(0.93f, 0.86f, 0.72f, 0.85f), new Color(0.8f, 0.74f, 0.64f, 0.6f)
        };

        /// <summary>A huge ringed gas giant low on your horizon, slowly turning, with bands of colour across it.</summary>
        public static void RingedPlanet()
        {
            SkyKeepFarClip();
            const float distance = 330f, radius = 48f;
            Vector3 direction = SkyDirection(215f, 13f);
            Vector3 centre = Head.position + direction * distance;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 skyUp = Vector3.Cross(direction, right);

            // The rings and the equator share one tilt: tipped towards you so the rings open out,
            // and leaning a little to one side
            float open = 22f * Mathf.Deg2Rad;
            Vector3 pole = Quaternion.AngleAxis(-14f, direction) * (skyUp * Mathf.Cos(open) - direction * Mathf.Sin(open));
            Vector3 light = (right * 0.8f - direction * 0.45f + Vector3.up * 0.3f).normalized;

            Piece planet = SkyEnsureGlobe(ref skyGiant, SkyGiantTexture, SkySolidQueue, true);
            planet.Object.transform.SetPositionAndRotation(centre, Quaternion.FromToRotation(Vector3.up, pole) * Quaternion.Euler(0f, Time.time * -2.5f, 0f));
            planet.Object.transform.localScale = Vector3.one * (radius * 2f);

            // Night falls across one side; the shadow stays put while the planet turns under it
            Piece shade = SkyEnsureGlobe(ref skyGiantShade, SkyShadeTexture, SkySolidQueue + 1, false);
            shade.Object.transform.SetPositionAndRotation(centre, SkyFaceLight(light));
            shade.Object.transform.localScale = Vector3.one * (radius * 2.02f);

            // A thin haze of atmosphere round its edge; the planet hides the half that crosses it
            if (skyGiantGlow == null)
                skyGiantGlow = MakeRing("Eclipse_SkyGiantGlow");
            PlaceRing(skyGiantGlow, centre, direction, radius * 1.04f, radius * 0.1f, WithAlpha(Second, 0.3f));

            for (int i = 0; i < skyGiantRings.Length; i++)
            {
                // Laid flat in the ring plane rather than turned to face you, so they open out like real rings
                if (skyGiantRings[i] == null)
                {
                    skyGiantRings[i] = MakeRing("Eclipse_SkyGiantRing");
                    skyGiantRings[i].alignment = LineAlignment.TransformZ;
                }

                LineRenderer ring = skyGiantRings[i];
                ring.transform.rotation = Quaternion.LookRotation(pole);
                PlaceRing(ring, centre, pole, SkyRingMiddles[i] * radius, SkyRingWidths[i] * radius,
                    WithAlpha(Color.Lerp(SkyRingColors[i], Accent, 0.2f), SkyRingColors[i].a));
            }
        }

        public static void DisableRingedPlanet()
        {
            SkyReleaseFarClip();
            DiscardPiece(ref skyGiant);
            DiscardPiece(ref skyGiantShade);
            DestroyLine(ref skyGiantGlow);
            for (int i = 0; i < skyGiantRings.Length; i++)
                DestroyLine(ref skyGiantRings[i]);
        }

        private static Piece skyMoonBig, skyMoonSmall;

        /// <summary>Two moons of different sizes and colours hanging in your sky, drifting slowly.</summary>
        public static void TwinMoons()
        {
            SkyKeepFarClip();
            float t = Time.time;

            // The small one drifts a little faster, so the pair slowly change places
            SkyPlaceMoon(ref skyMoonBig, SkyDirection(55f + t * 0.2f, 34f + Mathf.Sin(t * 0.013f) * 6f), 280f, 30f, 0.4f, new Color(0.93f, 0.94f, 1f));
            SkyPlaceMoon(ref skyMoonSmall, SkyDirection(78f + t * 0.32f, 24f + Mathf.Sin(t * 0.021f + 1f) * 9f), 300f, 12f, 1.3f,
                Color.Lerp(new Color(1f, 0.66f, 0.46f), Second, 0.3f));
        }

        // `phase` is how far round from behind you the light comes: 0 is a full moon, 1 a half
        // moon and more than that a crescent
        private static void SkyPlaceMoon(ref Piece moon, Vector3 direction, float distance, float size, float phase, Color color)
        {
            SkyEnsureGlobe(ref moon, SkyMoonTexture, SkySeeThroughQueue, false);

            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 light = Vector3.SlerpUnclamped(-direction, right, phase);

            moon.Object.transform.SetPositionAndRotation(Head.position + direction * distance, SkyFaceLight(light));
            moon.Object.transform.localScale = Vector3.one * size;
            SetColor(moon, color);
        }

        public static void DisableTwinMoons()
        {
            SkyReleaseFarClip();
            DiscardPiece(ref skyMoonBig);
            DiscardPiece(ref skyMoonSmall);
        }

        private static readonly Piece[] skyStationParts = new Piece[8];
        private static readonly Piece[] skyStationLights = new Piece[3];

        // The hub, the modules in front of and behind it, the truss across it and the four solar
        // panels on the truss: where each sits and how big it is, in metres. The station flies
        // along +Z with the faces of its panels turned towards you.
        private static readonly Vector3[] SkyStationSpots =
        {
            Vector3.zero, new Vector3(0f, 0f, 4.4f), new Vector3(0f, 0f, -4f), Vector3.zero,
            new Vector3(-6.4f, 0f, 1.5f), new Vector3(-6.4f, 0f, -1.5f), new Vector3(6.4f, 0f, 1.5f), new Vector3(6.4f, 0f, -1.5f)
        };
        private static readonly Vector3[] SkyStationSizes =
        {
            new Vector3(3f, 3f, 3f), new Vector3(2.2f, 2.2f, 5.8f), new Vector3(2f, 2f, 5f), new Vector3(19f, 0.45f, 0.45f),
            new Vector3(5.4f, 0.12f, 2.5f), new Vector3(5.4f, 0.12f, 2.5f), new Vector3(5.4f, 0.12f, 2.5f), new Vector3(5.4f, 0.12f, 2.5f)
        };
        private static readonly Color[] SkyStationColors =
        {
            new Color(0.86f, 0.86f, 0.9f), new Color(0.95f, 0.95f, 0.97f), new Color(0.78f, 0.8f, 0.84f), new Color(0.55f, 0.56f, 0.6f)
        };

        // The red light on the left tip, the green one on the right and the strobe on the nose
        private static readonly Vector3[] SkyStationLightSpots = { new Vector3(-9.8f, 0f, 0f), new Vector3(9.8f, 0f, 0f), new Vector3(0f, 0f, 7.6f) };

        /// <summary>A small space station with solar-panel wings and blinking lights, slowly orbiting high across your sky.</summary>
        public static void SpaceStation()
        {
            SkyKeepFarClip();
            float t = Time.time;

            // A circle round a point high in the sky, so it climbs from low on one side, passes
            // nearly overhead and sinks again without ever going under the horizon
            Vector3 direction = SkyCircle(SkyDirection(120f, 62f), 52f, t * (Mathf.PI * 2f / 150f), out Vector3 heading);
            Vector3 centre = Head.position + direction * 170f;
            Quaternion facing = Quaternion.LookRotation(heading, -direction) * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.15f) * 25f);

            // Now and then the panels catch the sun as the station rolls
            float glint = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 0.3f)), 8f);
            Color panel = Color.Lerp(Color.Lerp(new Color(0.12f, 0.2f, 0.48f), Second, 0.3f), Color.white, glint * 0.4f);

            for (int i = 0; i < skyStationParts.Length; i++)
            {
                Piece part = SkyEnsure(ref skyStationParts[i], PrimitiveType.Cube, SkySolidQueue, true);
                SkyPlace(part, centre + facing * SkyStationSpots[i], facing, SkyStationSizes[i]);
                SetColor(part, i < SkyStationColors.Length ? SkyStationColors[i] : panel);
            }

            float strobe = t % 2f;
            SkyPlaceStationLight(0, centre, facing, new Color(1f, 0.25f, 0.2f), t % 1.2f < 0.5f);
            SkyPlaceStationLight(1, centre, facing, new Color(0.3f, 1f, 0.45f), (t + 0.6f) % 1.2f < 0.5f);
            SkyPlaceStationLight(2, centre, facing, Accent, strobe < 0.07f || (strobe > 0.22f && strobe < 0.29f));
        }

        private static void SkyPlaceStationLight(int index, Vector3 centre, Quaternion facing, Color color, bool lit)
        {
            Piece light = SkyEnsure(ref skyStationLights[index], PrimitiveType.Sphere);
            SkyShow(light, lit);
            if (!lit)
                return;

            SkyPlace(light, centre + facing * SkyStationLightSpots[index], facing, Vector3.one * 1.5f);
            SetColor(light, color);
        }

        public static void DisableSpaceStation()
        {
            SkyReleaseFarClip();
            for (int i = 0; i < skyStationParts.Length; i++)
                DiscardPiece(ref skyStationParts[i]);
            for (int i = 0; i < skyStationLights.Length; i++)
                DiscardPiece(ref skyStationLights[i]);
        }

        private sealed class SkySatellite
        {
            public Piece Body;
            public Piece Wing;
            public Piece Blink;
            public Vector3 Rise;
            public Vector3 Over;
            public float Start;
            public float Duration;
            public float Distance;
            public float Flash;
        }

        private static readonly SkySatellite[] skySatellites = new SkySatellite[4];

        /// <summary>A few tiny blinking satellites crossing your sky on different paths at different speeds.</summary>
        public static void Satellites()
        {
            SkyKeepFarClip();
            float now = Time.time;
            for (int i = 0; i < skySatellites.Length; i++)
            {
                SkySatellite satellite = skySatellites[i];
                if (satellite == null)
                {
                    // Each starts partway across, so they don't all come up at once
                    satellite = skySatellites[i] = new SkySatellite();
                    SkySendSatellite(satellite, now);
                    satellite.Start -= Random.value * satellite.Duration;
                }

                float progress = (now - satellite.Start) / satellite.Duration;
                if (progress >= 1f)
                {
                    SkySendSatellite(satellite, now);
                    progress = 0f;
                }

                // Up over one horizon, across and down the other, then off on a new path
                float angle = Mathf.Lerp(-0.08f, Mathf.PI + 0.08f, progress);
                Vector3 direction = SkyArc(satellite.Rise, satellite.Over, angle);
                Vector3 heading = SkyArc(satellite.Over, -satellite.Rise, angle);
                Vector3 at = Head.position + direction * satellite.Distance;
                Quaternion facing = Quaternion.LookRotation(heading, -direction) * Quaternion.Euler(0f, 0f, now * 20f + i * 70f);

                // Shrinks away low in the sky, as if passing in and out of the Earth's shadow
                float fade = Mathf.Clamp01(Mathf.Min(progress, 1f - progress) * 12f);

                Piece body = SkyEnsure(ref satellite.Body, PrimitiveType.Cube, SkySolidQueue, true);
                SkyPlace(body, at, facing, new Vector3(1.2f, 0.9f, 1.6f) * fade);
                SetColor(body, new Color(0.78f, 0.78f, 0.82f));

                Piece wing = SkyEnsure(ref satellite.Wing, PrimitiveType.Cube, SkySolidQueue, true);
                SkyPlace(wing, at, facing, new Vector3(5f, 0.06f, 1f) * fade);
                SetColor(wing, Color.Lerp(new Color(0.15f, 0.25f, 0.6f), Second, 0.3f));

                bool lit = (now + i * 0.37f) % satellite.Flash < 0.12f;
                Piece blink = SkyEnsure(ref satellite.Blink, PrimitiveType.Sphere);
                SkyShow(blink, lit);
                if (lit)
                {
                    SkyPlace(blink, at + facing * new Vector3(0f, 0f, 1f), facing, Vector3.one * (1.8f * fade));
                    SetColor(blink, Color.Lerp(Color.white, Accent, 0.5f));
                }
            }
        }

        private static void SkySendSatellite(SkySatellite satellite, float now)
        {
            // The top of each path leans up to 60 degrees off straight overhead
            satellite.Rise = SkyDirection(Random.Range(0f, 360f), 0f);
            satellite.Over = Quaternion.AngleAxis(Random.Range(-60f, 60f), satellite.Rise) * Vector3.up;
            satellite.Start = now;
            satellite.Duration = Random.Range(28f, 70f);
            satellite.Distance = Random.Range(200f, 260f);
            satellite.Flash = Random.Range(0.9f, 1.8f);
        }

        public static void DisableSatellites()
        {
            SkyReleaseFarClip();
            for (int i = 0; i < skySatellites.Length; i++)
            {
                SkySatellite satellite = skySatellites[i];
                if (satellite == null)
                    continue;

                DiscardPiece(ref satellite.Body);
                DiscardPiece(ref satellite.Wing);
                DiscardPiece(ref satellite.Blink);
                skySatellites[i] = null;
            }
        }

        private static Piece skyCometCore, skyCometComa;
        private static LineRenderer skyCometTail, skyCometHaze;
        private static ParticleSystem skyCometDust;
        private const int SkyCometPoints = 24;
        private static readonly Vector3[] skyCometBuffer = new Vector3[SkyCometPoints];
        private static Vector3 skyCometFrom, skyCometAcross;
        private static float skyCometStart = -100f, skyCometNext = -1f, skyCometLength, skyCometSweep, skyCometDistance, skyCometShed;

        /// <summary>Every 40 to 60 seconds a bright comet with a long glowing tail streaks across your sky.</summary>
        public static void CometFlyby()
        {
            SkyKeepFarClip();
            float now = Time.time;
            if (skyCometDust == null)
                skyCometDust = SkyParticles("Eclipse_SkyCometDust", 120, SkySoft, particles =>
                {
                    var fade = particles.colorOverLifetime;
                    fade.enabled = true;
                    fade.color = new ParticleSystem.MinMaxGradient(FadeInOut());
                });

            // The dust lives in its own space, which moves with you just as the comet does
            skyCometDust.transform.position = Head.position;

            // The first one comes along soon after turning it on
            if (skyCometNext < 0f)
                skyCometNext = now + 2f;

            if (now >= skyCometNext)
            {
                // Comes in high on one side and sweeps across and a little down
                skyCometFrom = SkyDirection(Random.Range(0f, 360f), Random.Range(32f, 58f));
                Vector3 side = Vector3.Cross(Vector3.up, skyCometFrom).normalized * (Random.value < 0.5f ? 1f : -1f);
                skyCometAcross = Vector3.ProjectOnPlane(side + Vector3.down * Random.Range(0.05f, 0.25f), skyCometFrom).normalized;
                skyCometSweep = Random.Range(85f, 110f) * Mathf.Deg2Rad;
                skyCometLength = Random.Range(6.5f, 8.5f);
                skyCometDistance = Random.Range(210f, 240f);
                skyCometStart = now;
                skyCometNext = now + Random.Range(40f, 60f);
            }

            float age = now - skyCometStart;
            bool flying = age < skyCometLength;
            SkyShow(skyCometCore, flying);
            SkyShow(skyCometComa, flying);
            if (skyCometTail != null)
                skyCometTail.enabled = flying;
            if (skyCometHaze != null)
                skyCometHaze.enabled = flying;
            if (!flying)
                return;

            float fade = Mathf.Clamp01(Mathf.Min(age, skyCometLength - age) / 1.2f);
            float along = age / skyCometLength * skyCometSweep;
            Vector3 head = Head.position;
            Vector3 local = SkyArc(skyCometFrom, skyCometAcross, along) * skyCometDistance;
            Vector3 heading = SkyArc(skyCometAcross, -skyCometFrom, along);
            Vector3 at = head + local;

            Piece core = SkyEnsure(ref skyCometCore, PrimitiveType.Sphere);
            SkyPlace(core, at, Quaternion.identity, Vector3.one * 3.2f);
            SetAlpha(core, Color.Lerp(Color.white, Accent, 0.15f), fade);

            // The glowing coma always draws over the core, so the two never swap places
            Piece coma = SkyEnsure(ref skyCometComa, PrimitiveType.Sphere, SkySeeThroughQueue + 1);
            SkyPlace(coma, at, Quaternion.identity, Vector3.one * (9f + Mathf.Sin(now * 5f) * 0.6f));
            SetAlpha(coma, Accent, 0.35f * fade);

            // The tail follows the path it has just come along
            for (int i = 0; i < SkyCometPoints; i++)
                skyCometBuffer[i] = head + SkyArc(skyCometFrom, skyCometAcross, along - i * (0.4f / (SkyCometPoints - 1))) * skyCometDistance;

            if (skyCometHaze == null)
                skyCometHaze = SkyMakeTail("Eclipse_SkyCometHaze");
            skyCometHaze.SetPositions(skyCometBuffer);
            skyCometHaze.widthMultiplier = 13f;
            skyCometHaze.startColor = WithAlpha(Accent, 0.3f * fade);
            skyCometHaze.endColor = WithAlpha(Second, 0f);

            if (skyCometTail == null)
                skyCometTail = SkyMakeTail("Eclipse_SkyCometTail");
            skyCometTail.SetPositions(skyCometBuffer);
            skyCometTail.widthMultiplier = 4.5f;
            skyCometTail.startColor = WithAlpha(Color.Lerp(Color.white, Accent, 0.3f), 0.95f * fade);
            skyCometTail.endColor = WithAlpha(Accent, 0f);

            // Sparkling dust shed behind it
            skyCometShed += 30f * Time.deltaTime;
            for (; skyCometShed >= 1f; skyCometShed -= 1f)
                skyCometDust.Emit(new ParticleSystem.EmitParams
                {
                    position = local + Random.insideUnitSphere * 2f,
                    velocity = -heading * Random.Range(3f, 8f) + Random.insideUnitSphere * 1.5f,
                    startSize = Random.Range(1.5f, 3.5f),
                    startLifetime = Random.Range(1.2f, 2.2f),
                    startColor = WithAlpha(Color.Lerp(Accent, Second, Random.value), 0.6f * fade)
                }, 1);
        }

        private static LineRenderer SkyMakeTail(string name)
        {
            LineRenderer tail = MakeLine(name, 1f);
            tail.positionCount = SkyCometPoints;
            tail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            return tail;
        }

        public static void DisableCometFlyby()
        {
            SkyReleaseFarClip();
            DiscardPiece(ref skyCometCore);
            DiscardPiece(ref skyCometComa);
            DestroyLine(ref skyCometTail);
            DestroyLine(ref skyCometHaze);
            DestroyParticles(ref skyCometDust);
            skyCometStart = -100f;
            skyCometNext = -1f;
        }

        private static ParticleSystem skyGalaxy;
        private static ParticleSystemRenderer skyGalaxyLook;
        private const int SkyGalaxyClouds = 70, SkyGalaxyStars = 360;

        /// <summary>A faint band of the Milky Way arcing across your sky.</summary>
        public static void GalaxyBand()
        {
            SkyKeepFarClip();
            if (skyGalaxy == null)
            {
                skyGalaxy = SkyParticles("Eclipse_SkyGalaxy", SkyGalaxyClouds + SkyGalaxyStars, SkyBackdrop);
                skyGalaxyLook = skyGalaxy.GetComponent<ParticleSystemRenderer>();
                SkyFillGalaxy();
            }

            skyGalaxy.transform.position = Head.position;
            SkyTint(skyGalaxyLook, Color.Lerp(Color.white, Second, 0.25f));
        }

        // The band is laid down once and kept: a great circle tipped steeply over the sky,
        // brightest and widest round its core
        private static void SkyFillGalaxy()
        {
            const float distance = 300f, core = 0.9f;
            Vector3 pole = SkyDirection(20f, 24f);
            Vector3 along = Vector3.Cross(Vector3.up, pole).normalized;
            Vector3 over = Vector3.Cross(pole, along);

            ParticleSystem.EmitParams dot = new ParticleSystem.EmitParams { velocity = Vector3.zero, startLifetime = 100000f };
            for (int i = 0; i < SkyGalaxyClouds + SkyGalaxyStars; i++)
            {
                // A third of the clouds bunch up round the core
                bool cloud = i < SkyGalaxyClouds;
                float angle = cloud && i % 3 == 0 ? core + SkySpread() * 0.35f : Random.Range(-0.2f, Mathf.PI + 0.2f);
                float bulge = 1f + 0.8f * Mathf.Clamp01(1f - Mathf.Abs(angle - core) / 0.5f);
                Vector3 direction = SkyArc(along, over, angle);

                dot.position = direction * (distance + SkySpread() * 15f) + pole * (SkySpread() * (cloud ? 38f : 28f) * bulge);
                dot.rotation = Random.Range(0f, 360f);
                if (cloud)
                {
                    dot.startSize = Random.Range(28f, 55f) * Mathf.Sqrt(bulge);
                    dot.startColor = Color.Lerp(new Color(0.85f, 0.88f, 1f, 0.11f), new Color(1f, 0.9f, 0.76f, 0.15f), Random.value * 0.5f + (bulge - 1f) * 0.6f);
                }
                else
                {
                    Color tint = Random.value < 0.5f ? new Color(0.7f, 0.8f, 1f) : new Color(1f, 0.9f, 0.7f);
                    dot.startSize = Random.Range(0.6f, 1.5f);
                    dot.startColor = WithAlpha(Color.Lerp(Color.white, tint, Random.value * 0.6f), Random.Range(0.5f, 1f));
                }

                skyGalaxy.Emit(dot, 1);
            }
        }

        public static void DisableGalaxyBand()
        {
            SkyReleaseFarClip();
            DestroyParticles(ref skyGalaxy);
            skyGalaxyLook = null;
        }

        private static Piece skyRocketBody, skyRocketNose, skyRocketFlame, skyRocketGlow;
        private static ParticleSystem skyRocketSmoke;
        private static Vector3 skyRocketPad, skyRocketDownrange;
        private static float skyRocketStart = -100f, skyRocketNext = -1f, skyRocketPuffs;
        private const float SkyRocketBurn = 11f, SkyRocketFlight = 14f;

        /// <summary>Now and then a distant rocket lifts off from your horizon and climbs away on a curve with a flame and a trail of smoke, then fades.</summary>
        public static void RocketLaunches()
        {
            SkyKeepFarClip();
            float now = Time.time;
            if (skyRocketSmoke == null)
                skyRocketSmoke = SkyParticles("Eclipse_SkyRocketSmoke", 200, SkySmoke, particles =>
                {
                    // Each puff swells as it thins out
                    var size = particles.sizeOverLifetime;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.6f));

                    var fade = particles.colorOverLifetime;
                    fade.enabled = true;
                    fade.color = new ParticleSystem.MinMaxGradient(SkySmokeFade());
                });

            // Positions here are all from your head, and the smoke lives in its own space that moves with you
            skyRocketSmoke.transform.position = Head.position;

            if (skyRocketNext < 0f)
                skyRocketNext = now + 3f;

            if (now >= skyRocketNext)
            {
                // A pad just under the horizon somewhere, flying off to one side so you see it arc over
                Vector3 outward = SkyDirection(Random.Range(0f, 360f), 0f);
                skyRocketPad = outward * Random.Range(260f, 280f) + Vector3.down * 6f;
                skyRocketDownrange = (Vector3.Cross(Vector3.up, outward) * (Random.value < 0.5f ? 1f : -1f) + outward * 0.35f).normalized;
                skyRocketStart = now;
                skyRocketPuffs = 0f;
                skyRocketNext = now + Random.Range(25f, 45f);
            }

            float age = now - skyRocketStart;
            bool flying = age < SkyRocketFlight;
            SkyShow(skyRocketBody, flying);
            SkyShow(skyRocketNose, flying);
            SkyShow(skyRocketFlame, flying);
            SkyShow(skyRocketGlow, flying);
            if (!flying)
                return;

            // Straight up at first, leaning further over the higher it climbs
            Vector3 engine = skyRocketPad + Vector3.up * (1.1f * age * age) + skyRocketDownrange * (0.055f * age * age * age);
            Vector3 heading = Vector3.up * (2.2f * age) + skyRocketDownrange * (0.165f * age * age);
            heading = heading.sqrMagnitude > 0.0001f ? heading.normalized : Vector3.up;
            Quaternion facing = Quaternion.FromToRotation(Vector3.up, heading);
            Vector3 at = Head.position + engine;

            // Once the engine cuts out the flame dies at once and the rocket dwindles away
            float size = Mathf.Clamp01((SkyRocketFlight - age) / (SkyRocketFlight - SkyRocketBurn));
            float thrust = Mathf.Clamp01((SkyRocketBurn - age) / 0.4f) * size;
            float flame = thrust * (0.85f + Mathf.Sin(now * 40f) * 0.1f + Mathf.Sin(now * 23f) * 0.08f);

            Piece body = SkyEnsure(ref skyRocketBody, PrimitiveType.Cylinder, SkySolidQueue, true);
            SkyPlace(body, at + heading * (6f * size), facing, new Vector3(2.4f, 6f, 2.4f) * size);
            SetColor(body, new Color(0.93f, 0.93f, 0.95f));

            Piece nose = SkyEnsure(ref skyRocketNose, PrimitiveType.Sphere, SkySolidQueue, true);
            SkyPlace(nose, at + heading * (12f * size), facing, new Vector3(2.4f, 3.6f, 2.4f) * size);
            SetColor(nose, Accent);

            Piece fire = SkyEnsure(ref skyRocketFlame, PrimitiveType.Sphere);
            SkyPlace(fire, at - heading * (4.5f * flame), facing, new Vector3(1.8f * thrust, 9f * flame, 1.8f * thrust));
            SetColor(fire, new Color(1f, 0.9f, 0.65f));

            Piece glow = SkyEnsure(ref skyRocketGlow, PrimitiveType.Sphere);
            SkyPlace(glow, at, Quaternion.identity, Vector3.one * (7f * flame));
            SetAlpha(glow, Color.Lerp(new Color(1f, 0.55f, 0.2f), Accent, 0.35f), 0.4f);

            if (age >= SkyRocketBurn)
                return;

            // Thick billows round the pad as it lifts off, then a steady trail behind it
            bool liftoff = age < 2.5f;
            Vector3 exhaust = engine - heading * 3f;
            skyRocketPuffs += (liftoff ? 28f : 16f) * Time.deltaTime;
            for (; skyRocketPuffs >= 1f; skyRocketPuffs -= 1f)
            {
                Vector3 billow = Random.insideUnitSphere;
                skyRocketSmoke.Emit(new ParticleSystem.EmitParams
                {
                    position = exhaust + billow * 2f,
                    velocity = liftoff ? new Vector3(billow.x * 9f, Mathf.Abs(billow.y) * 2f, billow.z * 9f) : -heading * 4f + billow * 1.5f,
                    rotation = Random.Range(0f, 360f),
                    startSize = Random.Range(5f, 9f),
                    startLifetime = Random.Range(4f, 6.5f),
                    startColor = new Color(0.9f, 0.9f, 0.92f, 0.6f)
                }, 1);
            }
        }

        // Lit warm by the flame as it leaves, then grey, thinning away to nothing
        private static Gradient SkySmokeFade()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.75f, 0.45f), 0f), new GradientColorKey(Color.white, 0.12f), new GradientColorKey(new Color(0.8f, 0.8f, 0.82f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.05f), new GradientAlphaKey(0.6f, 0.45f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        public static void DisableRocketLaunches()
        {
            SkyReleaseFarClip();
            DiscardPiece(ref skyRocketBody);
            DiscardPiece(ref skyRocketNose);
            DiscardPiece(ref skyRocketFlame);
            DiscardPiece(ref skyRocketGlow);
            DestroyParticles(ref skyRocketSmoke);
            skyRocketStart = -100f;
            skyRocketNext = -1f;
        }

        private static ParticleSystem skyNebula;
        private static float skyNebulaNext;

        // The middle of each nebula as a bearing and a height in degrees, and its own colour
        private static readonly Vector2[] SkyNebulaSpots = { new Vector2(35f, 58f), new Vector2(160f, 70f), new Vector2(265f, 50f) };
        private static readonly Color[] SkyNebulaColors = { new Color(1f, 0.35f, 0.7f), new Color(0.25f, 0.75f, 1f), new Color(0.6f, 0.35f, 1f) };

        /// <summary>Huge soft clouds of coloured nebula glowing high in your sky.</summary>
        public static void NebulaSky()
        {
            SkyKeepFarClip();
            float now = Time.time;
            if (skyNebula == null)
            {
                skyNebula = SkyParticles("Eclipse_SkyNebula", 140, SkyBackdrop, particles =>
                {
                    var fade = particles.colorOverLifetime;
                    fade.enabled = true;
                    fade.color = new ParticleSystem.MinMaxGradient(FadeInOut());

                    // Each cloud billows out a little over its life
                    var size = particles.sizeOverLifetime;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.85f, 1f, 1.15f));
                });

                // Starts with the sky already full, each cloud partway through its life
                for (int i = 0; i < 60; i++)
                    SkyAddNebulaPuff(Random.Range(6f, 30f));
                skyNebulaNext = now;
            }

            // The whole sky of clouds turns slowly round you
            skyNebula.transform.SetPositionAndRotation(Head.position, Quaternion.Euler(0f, now * 0.5f, 0f));

            if (now >= skyNebulaNext)
            {
                skyNebulaNext = now + 0.35f;
                SkyAddNebulaPuff(Random.Range(22f, 32f));
            }
        }

        private static void SkyAddNebulaPuff(float life)
        {
            int cloud = Random.Range(0, SkyNebulaSpots.Length);
            Vector3 middle = SkyDirection(SkyNebulaSpots[cloud].x, SkyNebulaSpots[cloud].y);

            // Spread across the sky far more than towards or away from you, so each nebula reads as
            // a broad patch, with a few bright young stars scattered through it
            Vector3 spread = Vector3.ProjectOnPlane(Random.insideUnitSphere, middle) * 70f + middle * (SkySpread() * 20f);
            bool star = Random.value < 0.15f;
            Color tint = Color.Lerp(SkyNebulaColors[cloud], cloud == 1 ? Second : Accent, 0.45f);

            skyNebula.Emit(new ParticleSystem.EmitParams
            {
                position = middle * 330f + spread,
                velocity = Vector3.zero,
                rotation = Random.Range(0f, 360f),
                angularVelocity = Random.Range(-1.5f, 1.5f),
                startSize = star ? Random.Range(1.5f, 3f) : Random.Range(45f, 100f),
                startLifetime = life,
                startColor = star ? new Color(0.9f, 0.95f, 1f, 0.95f) : WithAlpha(tint, Random.Range(0.1f, 0.17f))
            }, 1);
        }

        public static void DisableNebulaSky()
        {
            SkyReleaseFarClip();
            DestroyParticles(ref skyNebula);
        }

        // ── Sky helpers ─────────────────────────────────────────────────────────

        // Solid things write depth and draw just before everything see-through, so a planet hides
        // the far side of its own rings and a station's parts never swap places. The galaxy and
        // nebulae draw before all of it, so whatever else is in the sky is always in front of them.
        private const int SkySeeThroughQueue = (int)RenderQueue.Transparent;
        private const int SkySolidQueue = SkySeeThroughQueue - 2;
        private const int SkyBackdropQueue = SkySeeThroughQueue - 10;

        /// <summary>The way to a point in the sky, from its compass bearing and its height above the horizon, in degrees.</summary>
        private static Vector3 SkyDirection(float bearing, float height) =>
            Quaternion.Euler(-height, bearing, 0f) * Vector3.forward;

        /// <summary>A point `angle` radians along the great circle that starts at `from` and heads towards `towards`.</summary>
        private static Vector3 SkyArc(Vector3 from, Vector3 towards, float angle) =>
            from * Mathf.Cos(angle) + towards * Mathf.Sin(angle);

        /// <summary>A point on a circle drawn round `centre` on the sky, `radius` degrees out from it, and the way along the circle there.</summary>
        private static Vector3 SkyCircle(Vector3 centre, float radius, float angle, out Vector3 heading)
        {
            Vector3 across = Vector3.Cross(centre, Vector3.up).normalized;
            Vector3 up = Vector3.Cross(across, centre);
            heading = SkyArc(up, -across, angle);
            return SkyArc(centre, SkyArc(across, up, angle), radius * Mathf.Deg2Rad);
        }

        /// <summary>Turns a globe so the side its texture lights, +X, faces `light`.</summary>
        private static Quaternion SkyFaceLight(Vector3 light)
        {
            Vector3 pole = Vector3.ProjectOnPlane(Vector3.up, light);
            if (pole.sqrMagnitude < 0.0001f)
                pole = Vector3.ProjectOnPlane(Vector3.forward, light);
            pole.Normalize();
            return Quaternion.LookRotation(Vector3.Cross(light, pole), pole);
        }

        // Like a shader's smoothstep: 0 below `from`, 1 above `to` and an S-curve between
        private static float SkySmooth(float from, float to, float value)
        {
            float t = Mathf.Clamp01((value - from) / (to - from));
            return t * t * (3f - 2f * t);
        }

        // Roughly bell-shaped between -1 and 1, so clouds and bands thin out towards their edges
        private static float SkySpread() => (Random.value + Random.value + Random.value) * (2f / 3f) - 1f;

        private static Piece SkyEnsure(ref Piece piece, PrimitiveType type, int queue = SkySeeThroughQueue, bool depth = false)
        {
            if (piece == null || !piece.Alive)
            {
                piece = MakePiece(type, Color.white, Vector3.one);
                Material material = piece.Renderer.sharedMaterial;
                material.renderQueue = queue;
                if (depth)
                    material.SetFloat("_ZWrite", 1f);
            }

            return piece;
        }

        /// <summary>A sphere wearing one of the sky's painted textures.</summary>
        private static Piece SkyEnsureGlobe(ref Piece piece, Texture2D texture, int queue, bool depth)
        {
            if (piece == null || !piece.Alive)
            {
                SkyEnsure(ref piece, PrimitiveType.Sphere, queue, depth);
                piece.Object.GetComponent<MeshFilter>().sharedMesh = SkyGlobeMesh;
                piece.Renderer.sharedMaterial.mainTexture = texture;
            }

            return piece;
        }

        private static void SkyShow(Piece piece, bool shown)
        {
            if (piece != null && piece.Alive && piece.Object.activeSelf != shown)
                piece.Object.SetActive(shown);
        }

        private static void SkyPlace(Piece piece, Vector3 position, Quaternion rotation, Vector3 size)
        {
            Transform body = piece.Object.transform;
            body.SetPositionAndRotation(position, rotation);
            body.localScale = size;
        }

        /// <summary>A particle system that gives off only what it is told to, in its own space, so its particles move with it.</summary>
        private static ParticleSystem SkyParticles(string name, int most, Material material, Action<ParticleSystem> configure = null)
        {
            ParticleSystem made = MakeParticles(name, null, particles =>
            {
                var main = particles.main;
                main.loop = true;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.scalingMode = ParticleSystemScalingMode.Local;
                main.startSpeed = 0f;
                main.maxParticles = most;

                var emission = particles.emission;
                emission.enabled = false;

                configure?.Invoke(particles);
            });

            // Faced at your eyes rather than lined up with your view, so the big ones don't swing about as you turn your head
            made.transform.localScale = Vector3.one;
            ParticleSystemRenderer look = made.GetComponent<ParticleSystemRenderer>();
            look.sharedMaterial = material;
            look.alignment = ParticleSystemRenderSpace.Facing;
            return made;
        }

        private static MaterialPropertyBlock skyTintBlock;
        private static int skyColorId, skyBaseColorId;

        /// <summary>Tints everything one renderer draws without touching the material it shares.</summary>
        private static void SkyTint(Renderer renderer, Color tint)
        {
            if (renderer == null)
                return;

            if (skyTintBlock == null)
            {
                skyTintBlock = new MaterialPropertyBlock();
                skyColorId = Shader.PropertyToID("_Color");
                skyBaseColorId = Shader.PropertyToID("_BaseColor");
            }

            skyTintBlock.SetColor(skyColorId, tint);
            skyTintBlock.SetColor(skyBaseColorId, tint);
            renderer.SetPropertyBlock(skyTintBlock);
        }

        // ── Sky materials and shapes ────────────────────────────────────────────

        private static Material skySoft, skySmoke, skyBackdrop;
        private static Texture2D skySoftDot, skyWispDot;

        /// <summary>Soft round sparks, for the comet's dust.</summary>
        private static Material SkySoft =>
            skySoft != null ? skySoft : skySoft = SkyMakeMaterial("EclipseSkySoft", SkyDot(ref skySoftDot, false), SkySeeThroughQueue);

        /// <summary>Soft wispy puffs, for rocket smoke.</summary>
        private static Material SkySmoke =>
            skySmoke != null ? skySmoke : skySmoke = SkyMakeMaterial("EclipseSkySmoke", SkyDot(ref skyWispDot, true), SkySeeThroughQueue);

        /// <summary>Soft wispy puffs drawn behind everything else in the sky, for the galaxy and nebulae.</summary>
        private static Material SkyBackdrop =>
            skyBackdrop != null ? skyBackdrop : skyBackdrop = SkyMakeMaterial("EclipseSkyBackdrop", SkyDot(ref skyWispDot, true), SkyBackdropQueue);

        private static Material SkyMakeMaterial(string name, Texture2D texture, int queue) =>
            new Material(VertexColored) { name = name, mainTexture = texture, renderQueue = queue, hideFlags = HideFlags.DontUnloadUnusedAsset };

        /// <summary>A round glow that fades out to its edge, either smooth or broken up like wisps of gas.</summary>
        private static Texture2D SkyDot(ref Texture2D cached, bool wispy)
        {
            if (cached != null)
                return cached;

            const int size = 64;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f;
                    float v = (y + 0.5f) / size * 2f - 1f;
                    float glow = SkySmooth(1f, 0f, Mathf.Sqrt(u * u + v * v));
                    if (wispy)
                        glow *= 0.35f + 0.65f * (Mathf.PerlinNoise(x * 0.09f + 17.3f, y * 0.09f + 4.1f) * 0.65f + Mathf.PerlinNoise(x * 0.21f + 2.7f, y * 0.21f + 9.8f) * 0.35f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, glow);
                }

            // Mipmapped, so the tiny far-off stars don't sparkle as you move
            cached = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = wispy ? "EclipseSkyWisp" : "EclipseSkyDot", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontUnloadUnusedAsset };
            cached.SetPixels(pixels);
            cached.Apply(true, true);
            return cached;
        }

        private static Mesh skyGlobeMesh;

        /// <summary>
        /// A sphere one unit across like the built-in one, but with its texture laid on a known way:
        /// longitude runs from +X towards +Z, and latitude from the bottom to the top. Shading painted
        /// into a texture can then be turned to face the light.
        /// </summary>
        private static Mesh SkyGlobeMesh
        {
            get
            {
                if (skyGlobeMesh != null)
                    return skyGlobeMesh;

                const int around = 32, rows = 16;
                Vector3[] vertices = new Vector3[(around + 1) * (rows + 1)];
                Vector3[] normals = new Vector3[vertices.Length];
                Vector2[] uvs = new Vector2[vertices.Length];
                for (int row = 0; row <= rows; row++)
                {
                    float latitude = (row / (float)rows - 0.5f) * Mathf.PI;
                    for (int column = 0; column <= around; column++)
                    {
                        float longitude = column / (float)around * Mathf.PI * 2f;
                        int index = row * (around + 1) + column;
                        normals[index] = new Vector3(Mathf.Cos(latitude) * Mathf.Cos(longitude), Mathf.Sin(latitude), Mathf.Cos(latitude) * Mathf.Sin(longitude));
                        vertices[index] = normals[index] * 0.5f;
                        uvs[index] = new Vector2(column / (float)around, row / (float)rows);
                    }
                }

                // Wound so every face looks outwards
                int[] triangles = new int[around * rows * 6];
                int next = 0;
                for (int row = 0; row < rows; row++)
                    for (int column = 0; column < around; column++)
                    {
                        int a = row * (around + 1) + column, b = a + 1, c = a + around + 1, d = c + 1;
                        triangles[next++] = a;
                        triangles[next++] = c;
                        triangles[next++] = b;
                        triangles[next++] = b;
                        triangles[next++] = c;
                        triangles[next++] = d;
                    }

                skyGlobeMesh = new Mesh { name = "EclipseSkyGlobe", vertices = vertices, normals = normals, uv = uvs, triangles = triangles, hideFlags = HideFlags.DontUnloadUnusedAsset };
                skyGlobeMesh.RecalculateBounds();
                return skyGlobeMesh;
            }
        }

        private static Texture2D skyGiantTexture, skyShadeTexture, skyMoonTexture;

        private static Texture2D SkyGiantTexture =>
            skyGiantTexture != null ? skyGiantTexture : skyGiantTexture = SkyPaintGlobe("EclipseSkyGiant", 128, 64, SkyPaintGiant);

        private static Texture2D SkyShadeTexture =>
            skyShadeTexture != null ? skyShadeTexture : skyShadeTexture = SkyPaintGlobe("EclipseSkyShade", 64, 32, SkyPaintShade);

        private static Texture2D SkyMoonTexture =>
            skyMoonTexture != null ? skyMoonTexture : skyMoonTexture = SkyPaintGlobe("EclipseSkyMoon", 64, 32, SkyPaintMoon);

        /// <summary>Paints a texture for the sky globe, given the longitude and latitude of each texel in radians.</summary>
        private static Texture2D SkyPaintGlobe(string name, int width, int height, Func<float, float, Color> paint)
        {
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float latitude = ((y + 0.5f) / height - 0.5f) * Mathf.PI;
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = paint((x + 0.5f) / width * Mathf.PI * 2f, latitude);
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        // Pale and tawny bands that wave a little, cold grey poles and one big oval storm
        private static Color SkyPaintGiant(float longitude, float latitude)
        {
            float wave = latitude + Mathf.Sin(longitude * 3f + latitude * 7f) * 0.03f + Mathf.Sin(longitude * 7f - latitude * 4f) * 0.012f;
            float band = Mathf.Sin(wave * 11f + Mathf.Sin(wave * 4f) * 1.4f) * 0.5f + 0.5f;
            float fine = Mathf.Sin(wave * 23f) * 0.5f + 0.5f;

            Color color = Color.Lerp(new Color(0.93f, 0.86f, 0.7f), new Color(0.78f, 0.58f, 0.4f), band);
            color = Color.Lerp(color, new Color(0.66f, 0.46f, 0.34f), fine * band * 0.35f);
            color = Color.Lerp(color, new Color(0.62f, 0.62f, 0.66f), SkySmooth(0.9f, 1.4f, Mathf.Abs(latitude)));

            float east = Mathf.DeltaAngle(longitude * Mathf.Rad2Deg, 200f) / 22f;
            float north = (latitude + 0.36f) / 0.11f;
            float storm = east * east + north * north;
            color = Color.Lerp(color, new Color(0.92f, 0.86f, 0.76f), SkySmooth(1.8f, 1f, storm) * 0.6f);
            return Color.Lerp(color, new Color(0.78f, 0.38f, 0.26f), SkySmooth(1f, 0.3f, storm));
        }

        // Black, clear on the day side and darkening across a soft line into night, lit from +X
        private static Color SkyPaintShade(float longitude, float latitude) =>
            new Color(0f, 0f, 0f, 0.82f * SkySmooth(0.25f, -0.3f, Mathf.Cos(latitude) * Mathf.Cos(longitude)));

        // Grey highlands and darker seas, lit from +X. The unlit side is clear, so it shows the sky
        // behind it as a real moon does by day.
        private static Color SkyPaintMoon(float longitude, float latitude)
        {
            Vector3 point = new Vector3(Mathf.Cos(latitude) * Mathf.Cos(longitude), Mathf.Sin(latitude), Mathf.Cos(latitude) * Mathf.Sin(longitude));

            // Noise taken from the point on the sphere, so there is no seam where the texture wraps round
            float seas = Mathf.PerlinNoise(point.x * 1.7f + 3.1f, point.y * 1.7f + 7.4f) * 0.6f + Mathf.PerlinNoise(point.z * 1.7f + 11.2f, point.y * 1.7f + 1.3f) * 0.4f;
            float speckle = Mathf.PerlinNoise(point.x * 6f + point.z * 2f + 5f, point.y * 6f + 2f);
            float albedo = 0.9f - SkySmooth(0.48f, 0.62f, seas) * 0.28f - speckle * 0.08f;
            float shade = albedo * (0.6f + 0.4f * Mathf.Sqrt(Mathf.Max(point.x, 0f)));
            return new Color(shade, shade, shade, SkySmooth(-0.03f, 0.12f, point.x));
        }
    }
}
