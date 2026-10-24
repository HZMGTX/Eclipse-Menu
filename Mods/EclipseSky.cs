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

        private static Piece binaryWarm, binaryCool, binaryWarmGlow, binaryCoolGlow, binaryWarmHalo, binaryCoolHalo;
        private static LineRenderer binaryStream;
        private const int BinaryStreamPoints = 14;
        private static readonly Vector3[] binaryStreamBuffer = new Vector3[BinaryStreamPoints];
        private static readonly Color BinaryWarmColor = new Color(1f, 0.6f, 0.26f), BinaryCoolColor = new Color(0.72f, 0.86f, 1f);

        /// <summary>Two suns, one warm orange and one cool blue-white, circling each other far off in your sky.</summary>
        public static void BinaryStars()
        {
            SkyKeepFarClip();
            const float warmSize = 22f, coolSize = 13f;
            float now = Time.time;
            Vector3 direction = SkyDirection(320f, 30f);
            Vector3 centre = Head.position + direction * 380f;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 skyUp = Vector3.Cross(direction, right);

            // Their orbit is seen nearly edge on and leans to one side, so they swing out across the
            // sky and pass in front of and behind each other
            Quaternion lean = Quaternion.AngleAxis(20f, direction);
            Vector3 across = lean * right;
            Vector3 deep = lean * (direction * 0.94f + skyUp * 0.34f);
            float angle = now * (Mathf.PI * 2f / 48f);
            Vector3 swing = SkyArc(across, deep, angle);
            Vector3 onward = SkyArc(deep, -across, angle);

            // The heavier blue-white sun keeps near the middle while the big orange one swings wide
            Vector3 warmAt = centre - swing * 19f;
            Vector3 coolAt = centre + swing * 11f;
            float flicker = Mathf.Sin(now * 1.7f) * 0.03f;
            BinaryPlaceSun(ref binaryWarm, ref binaryWarmGlow, ref binaryWarmHalo, warmAt, warmSize, 1f + flicker, BinaryWarmColor, Accent);
            BinaryPlaceSun(ref binaryCool, ref binaryCoolGlow, ref binaryCoolHalo, coolAt, coolSize, 1f - flicker, BinaryCoolColor, Second);

            // A thin stream of gas drawn off the swollen orange sun, curling round onto the trailing
            // side of the blue one
            Vector3 from = warmAt + (coolAt - warmAt).normalized * (warmSize * 0.45f);
            Vector3 to = coolAt - onward * (coolSize * 0.45f);
            Vector3 bend = (from + to) * 0.5f - onward * 7f;
            for (int i = 0; i < BinaryStreamPoints; i++)
            {
                float along = i / (float)(BinaryStreamPoints - 1);
                binaryStreamBuffer[i] = Vector3.Lerp(Vector3.Lerp(from, bend, along), Vector3.Lerp(bend, to, along), along);
            }

            if (binaryStream == null)
            {
                binaryStream = MakeLine("Eclipse_SkyBinaryStream", 1f);
                binaryStream.positionCount = BinaryStreamPoints;
                binaryStream.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.35f);
            }

            binaryStream.SetPositions(binaryStreamBuffer);
            binaryStream.widthMultiplier = 3.2f;
            binaryStream.startColor = WithAlpha(BinaryWarmColor, 0.5f);
            binaryStream.endColor = WithAlpha(Color.Lerp(BinaryCoolColor, Accent, 0.3f), 0.25f);
        }

        // A sun is a white-hot ball with a close glow and a wide faint halo round it
        private static void BinaryPlaceSun(ref Piece sun, ref Piece glow, ref Piece halo, Vector3 at, float size, float pulse, Color color, Color tint)
        {
            Piece body = SkyEnsure(ref sun, PrimitiveType.Sphere, SkySolidQueue, true);
            SkyPlace(body, at, Quaternion.identity, Vector3.one * size);
            SetColor(body, Color.Lerp(color, Color.white, 0.55f));

            SkyPlaceGlow(SkyEnsureGlow(ref glow), at, size * 2.4f * pulse, color, 0.6f);
            SkyPlaceGlow(SkyEnsureGlow(ref halo), at, size * 4.5f, Color.Lerp(color, tint, 0.25f), 0.2f);
        }

        public static void DisableBinaryStars()
        {
            SkyReleaseFarClip();
            DiscardPiece(ref binaryWarm);
            DiscardPiece(ref binaryCool);
            DiscardPiece(ref binaryWarmGlow);
            DiscardPiece(ref binaryCoolGlow);
            DiscardPiece(ref binaryWarmHalo);
            DiscardPiece(ref binaryCoolHalo);
            DestroyLine(ref binaryStream);
        }

        private sealed class BeltRock
        {
            public Piece Body;
            public Quaternion Start;
            public Vector3 Axis;
            public Vector3 Size;
            public Color Color;
            public float Angle;
            public float Out;
            public float Side;
            public float Spin;
        }

        private static readonly BeltRock[] beltRocks = new BeltRock[40];
        private static ParticleSystem beltDust;
        private static ParticleSystemRenderer beltDustLook;
        private const int BeltClouds = 50, BeltGrains = 300;
        private const float BeltDistance = 250f, BeltSpeed = 0.01f;

        // The rocks only fill the half of the belt above the horizon, from a little under it on one
        // side to a little under it on the other, then go round again from the start
        private const float BeltFrom = -0.3f, BeltSpan = Mathf.PI + 0.6f;

        /// <summary>A wide belt of tumbling rocks and dust arcing across your sky, slowly turning.</summary>
        public static void AsteroidBelt()
        {
            SkyKeepFarClip();
            float now = Time.time;
            Vector3 head = Head.position;

            // A great circle tipped over the sky, leaning the other way to the galaxy band
            Vector3 pole = SkyDirection(110f, 28f);
            Vector3 along = Vector3.Cross(Vector3.up, pole).normalized;
            Vector3 over = Vector3.Cross(pole, along);

            if (beltDust == null)
            {
                beltDust = SkyParticles("Eclipse_SkyBeltDust", BeltClouds + BeltGrains, SkyBackdrop);
                beltDustLook = beltDust.GetComponent<ParticleSystemRenderer>();
                BeltFillDust(pole, along, over);
            }

            // The dust goes all the way round and turns with the rocks
            beltDust.transform.SetPositionAndRotation(head, Quaternion.AngleAxis(now * BeltSpeed * Mathf.Rad2Deg, pole));
            SkyTint(beltDustLook, Color.Lerp(Color.white, Second, 0.15f));

            for (int i = 0; i < beltRocks.Length; i++)
            {
                BeltRock rock = beltRocks[i];
                if (rock == null)
                    rock = beltRocks[i] = BeltMakeRock(i);

                float angle = BeltFrom + Mathf.Repeat(rock.Angle + now * BeltSpeed, BeltSpan);

                // Shrinks away as it sinks under the horizon at either end, so none pops into view
                float fade = SkySmooth(BeltFrom, BeltFrom + 0.2f, angle) * SkySmooth(BeltFrom + BeltSpan, BeltFrom + BeltSpan - 0.2f, angle);

                bool fresh = rock.Body == null || !rock.Body.Alive;
                Piece body = SkyEnsureShape(ref rock.Body, BeltRockMesh, SkyFacetShade, SkySolidQueue, true);
                if (fresh)
                    SetColor(body, rock.Color);

                SkyPlace(body, head + SkyArc(along, over, angle) * (BeltDistance + rock.Out) + pole * rock.Side,
                    Quaternion.AngleAxis(now * rock.Spin, rock.Axis) * rock.Start, rock.Size * fade);
            }
        }

        private static BeltRock BeltMakeRock(int index)
        {
            // Mostly small rocks, with now and then a big one, in dusty greys and browns
            float size = Random.value < 0.12f ? Random.Range(9f, 13f) : Random.Range(2.5f, 6.5f);
            Color color = Color.Lerp(new Color(0.4f, 0.37f, 0.34f), new Color(0.66f, 0.59f, 0.5f), Random.value);
            if (Random.value < 0.2f)
                color = Color.Lerp(color, Random.value < 0.5f ? new Color(0.62f, 0.42f, 0.32f) : new Color(0.46f, 0.5f, 0.58f), 0.6f);

            return new BeltRock
            {
                // Spread evenly along the belt, give or take, and scattered across its width
                Angle = (index + Random.value * 0.8f) / beltRocks.Length * BeltSpan,
                Out = SkySpread() * 24f,
                Side = SkySpread() * 34f,
                Start = Random.rotationUniform,
                Axis = Random.onUnitSphere,

                // The big ones tumble more slowly
                Spin = Random.Range(10f, 28f) * (size > 8f ? 0.4f : 1f) * (Random.value < 0.5f ? 1f : -1f),
                Size = new Vector3(Random.Range(0.75f, 1.25f), Random.Range(0.7f, 1.1f), Random.Range(0.8f, 1.3f)) * size,
                Color = color
            };
        }

        // The dust is laid down once all the way round the belt: faint wide clouds and fine grains
        private static void BeltFillDust(Vector3 pole, Vector3 along, Vector3 over)
        {
            ParticleSystem.EmitParams dot = new ParticleSystem.EmitParams { velocity = Vector3.zero, startLifetime = 100000f };
            for (int i = 0; i < BeltClouds + BeltGrains; i++)
            {
                bool cloud = i < BeltClouds;
                Vector3 way = SkyArc(along, over, Random.value * Mathf.PI * 2f);
                dot.position = way * (BeltDistance + SkySpread() * 26f) + pole * (SkySpread() * (cloud ? 28f : 36f));
                dot.rotation = Random.Range(0f, 360f);
                if (cloud)
                {
                    dot.startSize = Random.Range(26f, 44f);
                    dot.startColor = new Color(0.74f, 0.66f, 0.58f, Random.Range(0.06f, 0.1f));
                }
                else
                {
                    dot.startSize = Random.Range(0.7f, 1.8f);
                    dot.startColor = WithAlpha(Color.Lerp(new Color(0.7f, 0.66f, 0.6f), new Color(0.9f, 0.86f, 0.8f), Random.value), Random.Range(0.5f, 0.9f));
                }

                beltDust.Emit(dot, 1);
            }
        }

        public static void DisableAsteroidBelt()
        {
            SkyReleaseFarClip();
            for (int i = 0; i < beltRocks.Length; i++)
            {
                if (beltRocks[i] != null)
                    DiscardPiece(ref beltRocks[i].Body);
                beltRocks[i] = null;
            }

            DestroyParticles(ref beltDust);
            beltDustLook = null;
        }

        private static ParticleSystem galaxySpiral;
        private static ParticleSystemRenderer galaxySpiralLook;
        private const int GalaxySpiralHaze = 40, GalaxySpiralClouds = 80, GalaxySpiralCore = 6, GalaxySpiralBulge = 70, GalaxySpiralStars = 220, GalaxySpiralKnots = 24;
        private const int GalaxySpiralDots = GalaxySpiralHaze + GalaxySpiralClouds + GalaxySpiralCore + GalaxySpiralBulge + GalaxySpiralStars + GalaxySpiralKnots;
        private const float GalaxySpiralRadius = 78f;

        /// <summary>A large tilted spiral galaxy far off in your sky, with a bright core and glowing arms of stars, turning very slowly.</summary>
        public static void SpiralGalaxy()
        {
            SkyKeepFarClip();
            if (galaxySpiral == null)
            {
                galaxySpiral = SkyParticles("Eclipse_SkySpiralGalaxy", GalaxySpiralDots, SkyBackdrop);
                galaxySpiralLook = galaxySpiral.GetComponent<ParticleSystemRenderer>();
                GalaxySpiralFill();
            }

            Vector3 direction = SkyDirection(150f, 46f);
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 skyUp = Vector3.Cross(direction, right);

            // Tipped well away from facing you and turned on the sky, so it shows as a long slanting oval
            float tip = 62f * Mathf.Deg2Rad;
            Vector3 pole = Quaternion.AngleAxis(32f, direction) * (skyUp * Mathf.Sin(tip) - direction * Mathf.Cos(tip));

            // Turning the way that leaves its arms trailing behind
            galaxySpiral.transform.SetPositionAndRotation(Head.position + direction * 350f,
                Quaternion.FromToRotation(Vector3.up, pole) * Quaternion.Euler(0f, Time.time * 0.8f, 0f));
            SkyTint(galaxySpiralLook, Color.Lerp(Color.white, Second, 0.2f));
        }

        // Laid down once, flat in the galaxy's own plane and in the order it is drawn: a faint haze
        // over the whole disc, clouds along two arms winding out from the middle, a bright core, then
        // the stars and the pink knots where new ones are forming
        private static void GalaxySpiralFill()
        {
            ParticleSystem.EmitParams dot = new ParticleSystem.EmitParams { velocity = Vector3.zero, startLifetime = 100000f };

            for (int i = 0; i < GalaxySpiralHaze; i++)
                GalaxySpiralDot(ref dot, GalaxySpiralFlat(Mathf.Sqrt(Random.value) * GalaxySpiralRadius * 0.95f, Random.value * Mathf.PI * 2f, 4f),
                    Random.Range(30f, 50f), new Color(0.72f, 0.8f, 1f, Random.Range(0.04f, 0.07f)));

            for (int i = 0; i < GalaxySpiralClouds; i++)
            {
                float along = Random.value;
                GalaxySpiralDot(ref dot, GalaxySpiralArm(i % 2, along, 1.4f), Random.Range(12f, 22f),
                    WithAlpha(Color.Lerp(new Color(0.55f, 0.7f, 1f), new Color(0.85f, 0.75f, 1f), Random.value), Random.Range(0.1f, 0.16f) * (1f - 0.45f * along)));
            }

            for (int i = 0; i < GalaxySpiralCore; i++)
                GalaxySpiralDot(ref dot, GalaxySpiralFlat(Random.value * 3f, Random.value * Mathf.PI * 2f, 1.5f), Random.Range(18f, 40f),
                    new Color(1f, 0.9f, 0.72f, Random.Range(0.2f, 0.3f)));

            for (int i = 0; i < GalaxySpiralBulge; i++)
            {
                float radius = Mathf.Abs(SkySpread()) * 14f;
                GalaxySpiralDot(ref dot, GalaxySpiralFlat(radius, Random.value * Mathf.PI * 2f, 7f * (1f - radius / 16f)), Random.Range(1.2f, 2.4f),
                    WithAlpha(Color.Lerp(new Color(1f, 0.86f, 0.66f), Color.white, Random.value * 0.5f), Random.Range(0.7f, 1f)));
            }

            for (int i = 0; i < GalaxySpiralStars; i++)
            {
                float along = Mathf.Pow(Random.value, 0.8f);
                Color tint = Random.value < 0.6f ? new Color(0.8f, 0.88f, 1f) : Color.white;
                GalaxySpiralDot(ref dot, GalaxySpiralArm(i % 2, along, 1f), Random.Range(1f, 2.2f), WithAlpha(tint, Random.Range(0.6f, 1f) * (1f - 0.3f * along)));
            }

            for (int i = 0; i < GalaxySpiralKnots; i++)
                GalaxySpiralDot(ref dot, GalaxySpiralArm(i % 2, Random.Range(0.2f, 0.9f), 0.6f), Random.Range(2.2f, 4f),
                    new Color(1f, 0.5f, 0.75f, Random.Range(0.6f, 0.85f)));
        }

        private static void GalaxySpiralDot(ref ParticleSystem.EmitParams dot, Vector3 position, float size, Color color)
        {
            dot.position = position;
            dot.rotation = Random.Range(0f, 360f);
            dot.startSize = size;
            dot.startColor = color;
            galaxySpiral.Emit(dot, 1);
        }

        // A point `radius` metres out from the middle of the disc, thinning out above and below it
        private static Vector3 GalaxySpiralFlat(float radius, float angle, float thickness) =>
            new Vector3(Mathf.Cos(angle) * radius, SkySpread() * thickness, Mathf.Sin(angle) * radius);

        // A point on one of the arms, `along` from the core (0) to its tip (1), scattered more the further out it is
        private static Vector3 GalaxySpiralArm(int arm, float along, float scatter)
        {
            float reach = 7f + (GalaxySpiralRadius - 7f) * along;
            float angle = arm * Mathf.PI + 1.9f * Mathf.Log(reach / 7f) + SkySpread() * scatter * 0.16f;
            return GalaxySpiralFlat(reach + SkySpread() * scatter * (2f + 6f * along), angle, 1.5f + 2.5f * (1f - along));
        }

        public static void DisableSpiralGalaxy()
        {
            SkyReleaseFarClip();
            DestroyParticles(ref galaxySpiral);
            galaxySpiralLook = null;
        }

        private static Piece pulsarCore, pulsarGlow;
        private static readonly LineRenderer[] pulsarBeams = new LineRenderer[4];

        // How far the beams lean off the spin axis in degrees, how long a turn takes, and how far the beams reach
        private const float PulsarLean = 48f, PulsarTurn = 3.2f, PulsarReach = 115f;

        /// <summary>A small bright star far off in your sky with two beams of light from its poles, sweeping round like a lighthouse.</summary>
        public static void PulsarStar()
        {
            SkyKeepFarClip();
            float now = Time.time;
            Vector3 direction = SkyDirection(250f, 28f);
            Vector3 centre = Head.position + direction * 300f;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 skyUp = Vector3.Cross(direction, right);

            // The spin axis leans towards you a little further than the beams lean off it, so once a
            // turn the beams sweep just past you and the star flashes
            float tilt = (PulsarLean + 6f) * Mathf.Deg2Rad;
            Vector3 axis = Quaternion.AngleAxis(-24f, direction) * (skyUp * Mathf.Sin(tilt) - direction * Mathf.Cos(tilt));
            Vector3 first = Vector3.Cross(axis, direction).normalized;
            Vector3 second = Vector3.Cross(axis, first);
            float lean = PulsarLean * Mathf.Deg2Rad;
            Vector3 beam = axis * Mathf.Cos(lean) + SkyArc(first, second, now * (Mathf.PI * 2f / PulsarTurn)) * Mathf.Sin(lean);
            float flash = Mathf.Pow(Mathf.Abs(Vector3.Dot(beam, direction)), 24f);

            Piece core = SkyEnsure(ref pulsarCore, PrimitiveType.Sphere, SkySolidQueue, true);
            SkyPlace(core, centre, Quaternion.identity, Vector3.one * 4.5f);
            SetColor(core, Color.Lerp(Color.white, new Color(0.75f, 0.88f, 1f), 0.35f));

            SkyPlaceGlow(SkyEnsureGlow(ref pulsarGlow), centre, 16f + 34f * flash, Color.Lerp(new Color(0.7f, 0.85f, 1f), Accent, 0.3f), 0.45f + 0.5f * flash);

            // Each beam is a bright narrow core inside a wider soft haze, spreading and fading as it goes
            for (int i = 0; i < pulsarBeams.Length; i++)
            {
                if (pulsarBeams[i] == null)
                {
                    pulsarBeams[i] = MakeLine("Eclipse_SkyPulsarBeam", 1f);
                    pulsarBeams[i].widthCurve = AnimationCurve.Linear(0f, 0.15f, 1f, 1f);
                }

                bool haze = i >= 2;
                LineRenderer line = pulsarBeams[i];
                line.SetPosition(0, centre);
                line.SetPosition(1, centre + beam * (i % 2 == 0 ? PulsarReach : -PulsarReach));
                line.widthMultiplier = haze ? 24f : 7f;
                line.startColor = haze
                    ? WithAlpha(Color.Lerp(new Color(0.6f, 0.8f, 1f), Second, 0.5f), 0.28f)
                    : WithAlpha(Color.Lerp(Color.white, Accent, 0.3f), 0.85f);
                line.endColor = WithAlpha(line.startColor, 0f);
            }
        }

        public static void DisablePulsarStar()
        {
            SkyReleaseFarClip();
            DiscardPiece(ref pulsarCore);
            DiscardPiece(ref pulsarGlow);
            for (int i = 0; i < pulsarBeams.Length; i++)
                DestroyLine(ref pulsarBeams[i]);
        }

        private const int FleetShips = 5;
        private static readonly Piece[] fleetHulls = new Piece[FleetShips];
        private static readonly Piece[] fleetEngines = new Piece[FleetShips];
        private static readonly LineRenderer[] fleetTrails = new LineRenderer[FleetShips];
        private static Vector3 fleetRise, fleetOver;
        private static float fleetStart = -100f, fleetNext = -1f, fleetDistance;
        private const float FleetCrossing = 24f;

        // Where each ship flies in the V, in metres across from the leader and ahead of it
        private static readonly Vector2[] FleetSlots =
        {
            new Vector2(0f, 0f), new Vector2(-15f, -13f), new Vector2(15f, -13f), new Vector2(-30f, -26f), new Vector2(30f, -26f)
        };

        /// <summary>Every 50 to 80 seconds a small V of sleek ships with glowing engines flies across your sky.</summary>
        public static void StarshipFleet()
        {
            SkyKeepFarClip();
            float now = Time.time;

            // The first one comes along soon after turning it on
            if (fleetNext < 0f)
                fleetNext = now + 3f;

            if (now >= fleetNext)
            {
                // Up over one horizon, across and down the other, cresting up to 50 degrees off overhead
                fleetRise = SkyDirection(Random.Range(0f, 360f), 0f);
                fleetOver = Quaternion.AngleAxis(Random.Range(-50f, 50f), fleetRise) * Vector3.up;
                fleetDistance = Random.Range(210f, 240f);
                fleetStart = now;
                fleetNext = now + Random.Range(50f, 80f);
            }

            float progress = (now - fleetStart) / FleetCrossing;
            bool flying = progress < 1f;
            for (int i = 0; i < FleetShips; i++)
            {
                SkyShow(fleetHulls[i], flying);
                SkyShow(fleetEngines[i], flying);
                if (fleetTrails[i] != null)
                    fleetTrails[i].enabled = flying;
            }

            if (!flying)
                return;

            float angle = Mathf.Lerp(-0.1f, Mathf.PI + 0.1f, progress);
            Vector3 direction = SkyArc(fleetRise, fleetOver, angle);
            Vector3 heading = SkyArc(fleetOver, -fleetRise, angle);
            Vector3 across = Vector3.Cross(direction, heading);
            Vector3 centre = Head.position + direction * fleetDistance;

            // Their tops turned towards you, and shrinking away low in the sky at either end
            Quaternion facing = Quaternion.LookRotation(heading, -direction);
            float fade = Mathf.Clamp01(Mathf.Min(progress, 1f - progress) * 10f);
            Color hull = Color.Lerp(new Color(0.82f, 0.85f, 0.92f), Second, 0.15f);
            Color engine = Color.Lerp(Color.white, Accent, 0.6f);

            for (int i = 0; i < FleetShips; i++)
            {
                // Each bobs a little in its place
                Vector3 at = centre + across * FleetSlots[i].x + heading * FleetSlots[i].y + direction * (Mathf.Sin(now * 1.3f + i * 1.9f) * 0.8f);
                Piece ship = SkyEnsureShape(ref fleetHulls[i], FleetShipMesh, SkyFacetShade, SkySolidQueue, true);
                SkyPlace(ship, at, facing, Vector3.one * (11f * fade));
                SetColor(ship, hull);

                Vector3 exhaust = at - heading * (5.4f * fade);
                SkyPlaceGlow(SkyEnsureGlow(ref fleetEngines[i]), exhaust, (6f + Mathf.Sin(now * 9f + i) * 0.6f) * fade, engine, 0.9f);

                if (fleetTrails[i] == null)
                {
                    fleetTrails[i] = MakeLine("Eclipse_SkyFleetTrail", 1f);
                    fleetTrails[i].widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                }

                LineRenderer trail = fleetTrails[i];
                trail.SetPosition(0, exhaust);
                trail.SetPosition(1, exhaust - heading * (34f * fade));
                trail.widthMultiplier = 2.6f * fade;
                trail.startColor = WithAlpha(engine, 0.75f);
                trail.endColor = WithAlpha(Accent, 0f);
            }
        }

        public static void DisableStarshipFleet()
        {
            SkyReleaseFarClip();
            for (int i = 0; i < FleetShips; i++)
            {
                DiscardPiece(ref fleetHulls[i]);
                DiscardPiece(ref fleetEngines[i]);
                DestroyLine(ref fleetTrails[i]);
            }

            fleetStart = -100f;
            fleetNext = -1f;
        }

        // One bit of the matter round the black hole: its own orbit, and where it started on it
        private struct HorizonMote
        {
            public float Radius;
            public float Phase;
            public float Speed;
            public float Lift;
            public float Heat;
            public float Alpha;
        }

        private static Piece horizonShadow, horizonAura;
        private static LineRenderer horizonPhoton, horizonLensed, horizonInner, horizonOuter;
        private static ParticleSystem horizonDisk;
        private const int HorizonClouds = 40, HorizonSparks = 180;
        private static readonly ParticleSystem.Particle[] horizonMatter = new ParticleSystem.Particle[HorizonClouds + HorizonSparks];
        private static readonly HorizonMote[] horizonMotes = new HorizonMote[HorizonClouds + HorizonSparks];

        // The size of the dark shadow, and the inner and outer edges of the disc of matter round it, in metres
        private const float HorizonRadius = 15f, HorizonInner = 22f, HorizonOuter = 57f;

        /// <summary>A distant black hole hanging in your sky: a dark disc edged with a thin ring of light, with a bright tilted disc of matter swirling round it.</summary>
        public static void BlackHoleHorizon()
        {
            SkyKeepFarClip();
            float now = Time.time;
            Vector3 direction = SkyDirection(15f, 36f);
            Vector3 centre = Head.position + direction * 340f;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 skyUp = Vector3.Cross(direction, right);

            // The disc is seen almost edge on from a little above, leaning to one side
            float open = 13f * Mathf.Deg2Rad;
            Vector3 pole = Quaternion.AngleAxis(-16f, direction) * (skyUp * Mathf.Cos(open) - direction * Mathf.Sin(open));
            Quaternion tilt = Quaternion.FromToRotation(Vector3.up, pole);

            // The shadow hides the far side of the disc, and the near side passes in front of it
            Piece shadow = SkyEnsure(ref horizonShadow, PrimitiveType.Sphere, SkySolidQueue, true);
            SkyPlace(shadow, centre, Quaternion.identity, Vector3.one * (HorizonRadius * 2f));
            SetColor(shadow, new Color(0.005f, 0f, 0.012f));

            Color hot = Color.Lerp(new Color(1f, 0.95f, 0.85f), Accent, 0.15f);
            Color warm = Color.Lerp(new Color(1f, 0.45f, 0.15f), Accent, 0.5f);
            SkyPlaceGlow(SkyEnsureGlow(ref horizonAura), centre, HorizonRadius * 7f, warm, 0.16f);

            // A thin bright ring of light bent right round the edge, and the far side of the disc
            // bent up over the top and under the bottom into a wider, softer one
            if (horizonPhoton == null)
                horizonPhoton = MakeRing("Eclipse_SkyHorizonPhoton");
            PlaceRing(horizonPhoton, centre, direction, HorizonRadius * 1.07f, HorizonRadius * 0.1f, WithAlpha(Color.Lerp(Color.white, Accent, 0.3f), 0.95f));

            if (horizonLensed == null)
                horizonLensed = MakeRing("Eclipse_SkyHorizonLensed");
            PlaceRing(horizonLensed, centre, direction, HorizonRadius * 1.35f, HorizonRadius * 0.45f, WithAlpha(Color.Lerp(warm, hot, 0.4f), 0.3f));

            // A smooth glow under the swirling matter, laid flat in the disc
            HorizonPlaceBand(ref horizonInner, "Eclipse_SkyHorizonInner", centre, pole, 31f, 13f, WithAlpha(Color.Lerp(hot, warm, 0.4f), 0.45f));
            HorizonPlaceBand(ref horizonOuter, "Eclipse_SkyHorizonOuter", centre, pole, 45f, 22f, WithAlpha(warm, 0.18f));

            if (horizonDisk == null)
            {
                horizonDisk = SkyParticles("Eclipse_SkyHorizonDisk", horizonMatter.Length, SkySoft);
                HorizonFillDisk();
            }

            horizonDisk.transform.SetPositionAndRotation(centre, tilt);

            // Matter near the middle goes round far faster than further out, and the side swinging
            // towards you looks brighter, as it does round a real black hole
            Vector3 toYou = Quaternion.Inverse(tilt) * -direction;
            for (int i = 0; i < horizonMotes.Length; i++)
            {
                HorizonMote mote = horizonMotes[i];
                float angle = mote.Phase + now * mote.Speed;
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                Color color = Color.Lerp(hot, warm, mote.Heat) * (1f + 0.5f * (cos * toYou.z - sin * toYou.x));
                color.a = mote.Alpha;

                horizonMatter[i].position = new Vector3(cos * mote.Radius, mote.Lift, sin * mote.Radius);
                horizonMatter[i].startColor = color;
            }

            horizonDisk.SetParticles(horizonMatter, horizonMatter.Length);
        }

        // Each bit keeps its own orbit for as long as the mod is on; only where it is round it changes
        private static void HorizonFillDisk()
        {
            for (int i = 0; i < horizonMotes.Length; i++)
            {
                // Soft clouds spread right across the disc, and sparks bunched up towards its hot inner edge
                bool cloud = i < HorizonClouds;
                float reach = cloud ? Random.Range(0.1f, 1f) : Mathf.Pow(Random.value, 1.7f);
                float radius = Mathf.Lerp(HorizonInner, HorizonOuter, reach);
                horizonMotes[i] = new HorizonMote
                {
                    Radius = radius,
                    Phase = Random.value * Mathf.PI * 2f,

                    // Once round in 7 seconds at the inner edge, slowing further out as Kepler's laws say
                    Speed = Mathf.PI * 2f / 7f * Mathf.Pow(HorizonInner / radius, 1.5f),
                    Lift = SkySpread() * (cloud ? 1.5f : 0.6f),
                    Heat = reach,
                    Alpha = cloud ? Random.Range(0.12f, 0.2f) : Mathf.Lerp(0.85f, 0.4f, reach)
                };

                horizonMatter[i] = new ParticleSystem.Particle
                {
                    velocity = Vector3.zero,
                    rotation = Random.Range(0f, 360f),
                    startSize = cloud ? Random.Range(9f, 16f) : Mathf.Lerp(1.6f, 3.4f, reach) * Random.Range(0.8f, 1.2f),
                    startLifetime = 100000f,
                    remainingLifetime = 100000f
                };
            }
        }

        private static void HorizonPlaceBand(ref LineRenderer band, string name, Vector3 centre, Vector3 pole, float radius, float width, Color color)
        {
            // Laid flat in the disc rather than turned to face you
            if (band == null)
            {
                band = MakeRing(name);
                band.alignment = LineAlignment.TransformZ;
            }

            band.transform.rotation = Quaternion.LookRotation(pole);
            PlaceRing(band, centre, pole, radius, width, color);
        }

        public static void DisableBlackHoleHorizon()
        {
            SkyReleaseFarClip();
            DiscardPiece(ref horizonShadow);
            DiscardPiece(ref horizonAura);
            DestroyLine(ref horizonPhoton);
            DestroyLine(ref horizonLensed);
            DestroyLine(ref horizonInner);
            DestroyLine(ref horizonOuter);
            DestroyParticles(ref horizonDisk);
        }

        private static Piece whaleBody, whaleAura;
        private static ParticleSystem whaleSparkles;
        private static float whaleShed;
        private const float WhaleLength = 64f, WhaleLap = 260f;

        // Where its sparkles come off, as a share of its length: the tips of its flukes and of its long front fins
        private static readonly Vector3[] WhaleSparkleSpots =
        {
            new Vector3(-0.19f, 0f, -0.56f), new Vector3(0.19f, 0f, -0.56f), new Vector3(-0.32f, -0.13f, -0.03f), new Vector3(0.32f, -0.13f, -0.03f)
        };

        /// <summary>A huge glowing see-through whale swimming slowly round your sky in a long loop, gently beating its tail.</summary>
        public static void SpaceWhale()
        {
            SkyKeepFarClip();
            float now = Time.time;
            float lap = now * (Mathf.PI * 2f / WhaleLap);
            Vector3 local = WhaleCourse(lap);
            Vector3 heading = (WhaleCourse(lap + 0.01f) - local).normalized;
            Vector3 at = Head.position + local;

            // Upright, leaning a little into the long turn
            Quaternion facing = Quaternion.LookRotation(heading, Vector3.up);
            float lean = Vector3.Dot(Vector3.ProjectOnPlane(-local, Vector3.up), facing * Vector3.right) > 0f ? -9f : 9f;
            facing *= Quaternion.Euler(0f, 0f, lean);

            WhaleSwim(now);
            Piece body = SkyEnsureShape(ref whaleBody, WhaleMesh, WhaleTexture, SkySeeThroughQueue, true);
            SkyPlace(body, at, facing, Vector3.one * WhaleLength);
            Color glow = Color.Lerp(Second, new Color(0.65f, 0.9f, 1f), 0.4f);
            SetAlpha(body, glow, 0.55f);

            // A soft glow round it, stretched along its length
            Piece aura = SkyEnsureGlow(ref whaleAura);
            SkyPlace(aura, at, Quaternion.LookRotation(at - Head.position, heading), new Vector3(WhaleLength * 0.5f, WhaleLength * 1.25f, 1f));
            SetAlpha(aura, glow, 0.16f);

            if (whaleSparkles == null)
                whaleSparkles = SkyParticles("Eclipse_SkyWhaleSparkles", 80, SkySoft, particles =>
                {
                    var fade = particles.colorOverLifetime;
                    fade.enabled = true;
                    fade.color = new ParticleSystem.MinMaxGradient(FadeInOut());
                });

            // Glittering dust trailing off its fins and flukes, in its own space that moves with you
            whaleSparkles.transform.position = Head.position;
            whaleShed += 10f * Time.deltaTime;
            for (; whaleShed >= 1f; whaleShed -= 1f)
                whaleSparkles.Emit(new ParticleSystem.EmitParams
                {
                    position = local + facing * (WhaleSparkleSpots[Random.Range(0, WhaleSparkleSpots.Length)] * WhaleLength) + Random.insideUnitSphere * 1.5f,
                    velocity = -heading * Random.Range(1f, 3f) + Random.insideUnitSphere * 0.8f,
                    startSize = Random.Range(1.2f, 2.8f),
                    startLifetime = Random.Range(3f, 5f),
                    startColor = WithAlpha(Color.Lerp(Accent, Second, Random.value), 0.7f)
                }, 1);
        }

        // The loop it swims round you: tipped so it climbs high on one side and dips low on the
        // other, with a slow rise and fall along the way
        private static Vector3 WhaleCourse(float angle) =>
            SkyDirection(30f, 0f) * (Mathf.Cos(angle) * 225f) + SkyDirection(120f, 18f) * (Mathf.Sin(angle) * 225f)
            + Vector3.up * (125f + Mathf.Sin(angle * 3f) * 10f);

        // Bends the body in a slow wave that runs back to the flukes, with the front fins rising and
        // falling gently
        private static void WhaleSwim(float now)
        {
            Mesh mesh = WhaleMesh;
            float stroke = now * (Mathf.PI * 2f / 5.5f);
            float fins = Mathf.Sin(now * 0.9f);
            for (int i = 0; i < whaleRest.Length; i++)
            {
                Vector3 point = whaleRest[i];
                point.y += whaleSway[i] * Mathf.Sin(stroke - whaleWave[i]) + whaleFin[i] * fins;
                whaleSwum[i] = point;
            }

            mesh.vertices = whaleSwum;
        }

        public static void DisableSpaceWhale()
        {
            SkyReleaseFarClip();
            DiscardPiece(ref whaleBody);
            DiscardPiece(ref whaleAura);
            DestroyParticles(ref whaleSparkles);
            whaleShed = 0f;
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

        /// <summary>A piece wearing one of the sky's own shapes and textures.</summary>
        private static Piece SkyEnsureShape(ref Piece piece, Mesh mesh, Texture2D texture, int queue, bool depth)
        {
            if (piece == null || !piece.Alive)
            {
                SkyEnsure(ref piece, PrimitiveType.Sphere, queue, depth);
                piece.Object.GetComponent<MeshFilter>().sharedMesh = mesh;
                piece.Renderer.sharedMaterial.mainTexture = texture;
            }

            return piece;
        }

        /// <summary>A flat round glow that fades out to its edge, for halos and engines.</summary>
        private static Piece SkyEnsureGlow(ref Piece piece)
        {
            if (piece == null || !piece.Alive)
            {
                SkyEnsure(ref piece, PrimitiveType.Quad);
                piece.Renderer.sharedMaterial.mainTexture = SkyDot(ref skySoftDot, false);
            }

            return piece;
        }

        /// <summary>Puts a glow `size` metres across at a point in the sky, turned to face you.</summary>
        private static void SkyPlaceGlow(Piece glow, Vector3 at, float size, Color color, float alpha)
        {
            SkyPlace(glow, at, Quaternion.LookRotation(at - Head.position), Vector3.one * size);
            SetAlpha(glow, color, alpha);
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

        private static Texture2D skyFacetShade;

        /// <summary>A strip of grey from shadow on the left to full light on the right, that SkyFacetMesh shades its faces from.</summary>
        private static Texture2D SkyFacetShade
        {
            get
            {
                if (skyFacetShade != null)
                    return skyFacetShade;

                const int size = 32;
                Color[] pixels = new Color[size * 2];
                for (int x = 0; x < size; x++)
                {
                    float shade = Mathf.Lerp(0.32f, 1f, x / (size - 1f));
                    pixels[x] = pixels[x + size] = new Color(shade, shade, shade, 1f);
                }

                skyFacetShade = new Texture2D(size, 2, TextureFormat.RGBA32, false) { name = "EclipseSkyFacetShade", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontUnloadUnusedAsset };
                skyFacetShade.SetPixels(pixels);
                skyFacetShade.Apply(false, true);
                return skyFacetShade;
            }
        }

        /// <summary>
        /// A mesh with every triangle a flat face of its own, turned to look away from `inside`. Each
        /// face takes a shade from SkyFacetShade by how squarely it faces `light`, give or take
        /// `speckle`, so it reads as a solid shape in any colour without needing lights in the scene.
        /// </summary>
        private static Mesh SkyFacetMesh(string name, Vector3[] corners, int[] triangles, Vector3 inside, Vector3 light, float speckle)
        {
            Vector3[] vertices = new Vector3[triangles.Length];
            Vector2[] uvs = new Vector2[triangles.Length];
            int[] order = new int[triangles.Length];
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = corners[triangles[i]], b = corners[triangles[i + 1]], c = corners[triangles[i + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(normal, a - inside) < 0f)
                {
                    Vector3 swap = b;
                    b = c;
                    c = swap;
                    normal = -normal;
                }

                // A little fixed variety from face to face, so neighbouring faces don't run together
                float shade = 0.5f + 0.5f * Vector3.Dot(normal.normalized, light) + (Mathf.Repeat(i * 0.618034f, 1f) - 0.5f) * speckle;
                Vector2 uv = new Vector2(Mathf.Clamp01(shade), 0.5f);

                vertices[i] = a;
                vertices[i + 1] = b;
                vertices[i + 2] = c;
                uvs[i] = uvs[i + 1] = uvs[i + 2] = uv;
                order[i] = i;
                order[i + 1] = i + 1;
                order[i + 2] = i + 2;
            }

            Mesh mesh = new Mesh { name = name, vertices = vertices, uv = uvs, triangles = order, hideFlags = HideFlags.DontUnloadUnusedAsset };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh beltRockMesh, fleetShipMesh;

        // Where the rock broke off something bigger: flat faces cut across it these ways
        private static readonly Vector3[] BeltRockCuts = { new Vector3(0.6f, 0.7f, 0.2f), new Vector3(-0.5f, -0.3f, 0.8f), new Vector3(0.1f, -0.9f, -0.4f) };

        /// <summary>A lumpy low-poly rock about a unit across, with a few flat broken faces.</summary>
        private static Mesh BeltRockMesh
        {
            get
            {
                if (beltRockMesh != null)
                    return beltRockMesh;

                // An icosahedron with each face split in four, then pushed in and out. Every corner is
                // worked out from its own direction, so faces that share a corner still meet.
                float g = (1f + Mathf.Sqrt(5f)) / 2f;
                Vector3[] ico =
                {
                    new Vector3(-1f, g, 0f), new Vector3(1f, g, 0f), new Vector3(-1f, -g, 0f), new Vector3(1f, -g, 0f),
                    new Vector3(0f, -1f, g), new Vector3(0f, 1f, g), new Vector3(0f, -1f, -g), new Vector3(0f, 1f, -g),
                    new Vector3(g, 0f, -1f), new Vector3(g, 0f, 1f), new Vector3(-g, 0f, -1f), new Vector3(-g, 0f, 1f)
                };
                int[] faces =
                {
                    0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                    3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
                };

                Vector3[] corners = new Vector3[faces.Length * 4];
                for (int i = 0; i < faces.Length; i += 3)
                {
                    Vector3 a = ico[faces[i]], b = ico[faces[i + 1]], c = ico[faces[i + 2]];
                    Vector3 ab = a + b, bc = b + c, ca = c + a;
                    Vector3[] split = { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca };
                    for (int k = 0; k < split.Length; k++)
                        corners[i * 4 + k] = BeltRockCorner(split[k]);
                }

                int[] triangles = new int[corners.Length];
                for (int i = 0; i < triangles.Length; i++)
                    triangles[i] = i;

                beltRockMesh = SkyFacetMesh("EclipseSkyRock", corners, triangles, Vector3.zero, new Vector3(0.45f, 0.8f, 0.4f).normalized, 0.25f);
                return beltRockMesh;
            }
        }

        private static Vector3 BeltRockCorner(Vector3 way)
        {
            way.Normalize();
            float radius = 1f + (Mathf.PerlinNoise(way.x * 1.4f + 5.3f, way.y * 1.4f + 2.1f) - 0.5f) * 0.45f
                + (Mathf.PerlinNoise(way.z * 2.6f + 8.7f, way.x * 2.6f + 1.4f) - 0.5f) * 0.2f;

            foreach (Vector3 cut in BeltRockCuts)
            {
                float facing = Vector3.Dot(way, cut.normalized);
                if (facing * radius > 0.8f)
                    radius = 0.8f / facing;
            }

            return way * (radius * 0.5f);
        }

        /// <summary>A sleek dart-shaped starship one unit long with its nose at +Z, a raised spine and swept-back wings.</summary>
        private static Mesh FleetShipMesh
        {
            get
            {
                if (fleetShipMesh != null)
                    return fleetShipMesh;

                // The nose, the top of the spine, the keel, the two wingtips and the three corners of the tail
                Vector3[] corners =
                {
                    new Vector3(0f, 0f, 0.5f), new Vector3(0f, 0.075f, -0.2f), new Vector3(0f, -0.05f, -0.2f),
                    new Vector3(-0.36f, -0.01f, -0.48f), new Vector3(0.36f, -0.01f, -0.48f),
                    new Vector3(-0.1f, 0f, -0.44f), new Vector3(0f, 0.03f, -0.46f), new Vector3(0.1f, 0f, -0.44f)
                };

                // A face up to the spine and one down to the keel from every edge of its outline
                int[] outline = { 0, 3, 5, 6, 7, 4 };
                int[] triangles = new int[outline.Length * 6];
                for (int i = 0; i < outline.Length; i++)
                {
                    int from = outline[i], to = outline[(i + 1) % outline.Length];
                    triangles[i * 6] = 1;
                    triangles[i * 6 + 1] = from;
                    triangles[i * 6 + 2] = to;
                    triangles[i * 6 + 3] = 2;
                    triangles[i * 6 + 4] = to;
                    triangles[i * 6 + 5] = from;
                }

                fleetShipMesh = SkyFacetMesh("EclipseSkyShip", corners, triangles, new Vector3(0f, 0.01f, -0.2f), new Vector3(0.35f, 0.8f, 0.45f).normalized, 0.1f);
                return fleetShipMesh;
            }
        }

        private static Mesh whaleMesh;
        private static Texture2D whaleTexture;
        private static Vector3[] whaleRest, whaleSwum;
        private static float[] whaleSway, whaleWave, whaleFin;
        private const int WhaleRings = 22, WhaleAround = 14;

        // The flat parts, drawn for the right-hand side and mirrored for the left, each a fan from
        // its first corner: a long front fin, one of the flukes and the small fin on its back
        private static readonly Vector3[] WhaleFrontFin =
        {
            new Vector3(0.1f, -0.045f, 0.2f), new Vector3(0.21f, -0.09f, 0.12f), new Vector3(0.32f, -0.13f, -0.03f), new Vector3(0.2f, -0.09f, 0.05f), new Vector3(0.1f, -0.05f, 0.12f)
        };
        private static readonly Vector3[] WhaleFluke =
        {
            new Vector3(0f, 0f, -0.44f), new Vector3(0.1f, 0f, -0.47f), new Vector3(0.19f, 0f, -0.56f), new Vector3(0.09f, 0f, -0.535f), new Vector3(0f, 0f, -0.515f)
        };
        private static readonly Vector3[] WhaleBackFin = { new Vector3(0f, 0.055f, -0.17f), new Vector3(0f, 0.088f, -0.22f), new Vector3(0f, 0.05f, -0.25f) };

        /// <summary>
        /// The space whale, one unit long with its snout at +Z: a smooth body drawn through a row of
        /// rings, two long front fins, a small fin on its back and broad flukes. It is made once, and
        /// kept with its resting shape so it can bend as it swims.
        /// </summary>
        private static Mesh WhaleMesh
        {
            get
            {
                if (whaleMesh != null)
                    return whaleMesh;

                int body = WhaleRings * (WhaleAround + 1);
                int flat = WhaleFrontFin.Length * 2 + WhaleFluke.Length * 2 + WhaleBackFin.Length;
                Vector3[] points = new Vector3[body + flat];
                Vector2[] uvs = new Vector2[points.Length];
                int[] triangles = new int[((WhaleRings - 1) * WhaleAround * 2 + ((WhaleFrontFin.Length - 2) * 2 + (WhaleFluke.Length - 2) * 2 + WhaleBackFin.Length - 2) * 2) * 3];

                // Its texture runs round the body one way and from snout to tail the other
                for (int ring = 0; ring < WhaleRings; ring++)
                {
                    float t = ring / (float)(WhaleRings - 1);

                    // A broad head and belly narrowing to a slim tail stock, taller there than it is wide
                    float radius = 0.118f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.62f))), 0.85f);
                    float wide = Mathf.Lerp(1.05f, 0.55f, SkySmooth(0.7f, 0.95f, t));
                    for (int step = 0; step <= WhaleAround; step++)
                    {
                        float around = step / (float)WhaleAround * Mathf.PI * 2f;
                        float rise = Mathf.Sin(around);
                        int index = ring * (WhaleAround + 1) + step;
                        points[index] = new Vector3(Mathf.Cos(around) * radius * wide, rise * radius * (rise > 0f ? 0.8f : 1f), 0.5f - t);
                        uvs[index] = new Vector2(step / (float)WhaleAround, t);
                    }
                }

                // Wound so every face of the body looks outwards
                int next = 0;
                for (int ring = 0; ring < WhaleRings - 1; ring++)
                    for (int step = 0; step < WhaleAround; step++)
                    {
                        int a = ring * (WhaleAround + 1) + step, b = a + 1, c = a + WhaleAround + 1, d = c + 1;
                        triangles[next++] = a;
                        triangles[next++] = c;
                        triangles[next++] = b;
                        triangles[next++] = b;
                        triangles[next++] = c;
                        triangles[next++] = d;
                    }

                int corner = body;
                WhaleAddFlat(points, uvs, triangles, ref corner, ref next, WhaleFrontFin, 1f);
                WhaleAddFlat(points, uvs, triangles, ref corner, ref next, WhaleFrontFin, -1f);
                WhaleAddFlat(points, uvs, triangles, ref corner, ref next, WhaleFluke, 1f);
                WhaleAddFlat(points, uvs, triangles, ref corner, ref next, WhaleFluke, -1f);
                WhaleAddFlat(points, uvs, triangles, ref corner, ref next, WhaleBackFin, 1f);

                // The front half hardly moves; the beat grows towards the flukes and runs back along
                // the body, and the front fins rise and fall more towards their tips
                whaleRest = points;
                whaleSwum = new Vector3[points.Length];
                whaleSway = new float[points.Length];
                whaleWave = new float[points.Length];
                whaleFin = new float[points.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    float t = 0.5f - points[i].z;
                    whaleSway[i] = 0.055f * SkySmooth(0.3f, 1.05f, t);
                    whaleWave[i] = 2.4f * t;
                    if (i >= body && points[i].z > -0.1f)
                        whaleFin[i] = 0.025f * Mathf.Clamp01((Mathf.Abs(points[i].x) - 0.1f) / 0.22f);
                }

                whaleMesh = new Mesh { name = "EclipseSkyWhale", vertices = points, uv = uvs, triangles = triangles, hideFlags = HideFlags.DontUnloadUnusedAsset };
                whaleMesh.MarkDynamic();
                whaleMesh.RecalculateBounds();

                // Room for the tail to beat without it ever being culled
                Bounds room = whaleMesh.bounds;
                room.Expand(0.2f);
                whaleMesh.bounds = room;
                return whaleMesh;
            }
        }

        // The flat parts are drawn from both sides, in the same pale shade as the belly
        private static void WhaleAddFlat(Vector3[] points, Vector2[] uvs, int[] triangles, ref int corner, ref int next, Vector3[] shape, float side)
        {
            int first = corner;
            foreach (Vector3 point in shape)
            {
                points[corner] = new Vector3(point.x * side, point.y, point.z);
                uvs[corner++] = new Vector2(0.75f, 0.5f - point.z);
            }

            for (int k = 1; k < shape.Length - 1; k++)
            {
                triangles[next++] = first;
                triangles[next++] = first + k;
                triangles[next++] = first + k + 1;
                triangles[next++] = first;
                triangles[next++] = first + k + 1;
                triangles[next++] = first + k;
            }
        }

        private static Texture2D WhaleTexture =>
            whaleTexture != null ? whaleTexture : whaleTexture = SkyPaintGlobe("EclipseSkyWhale", 128, 128, SkyPaintWhale);

        // Brightest along the belly, with grooves running back from the throat and a row of glowing
        // spots down each side. Round the body runs across the texture and snout to tail up it.
        private static Color SkyPaintWhale(float around, float latitude)
        {
            float along = latitude / Mathf.PI + 0.5f;
            float rise = Mathf.Sin(around);
            float belly = SkySmooth(-0.2f, -0.8f, rise);
            float shade = 0.55f + 0.3f * belly - 0.12f * SkySmooth(0.3f, 0.9f, rise);
            shade += 0.22f * SkySmooth(0.55f, 0.95f, Mathf.Sin(around * 30f)) * belly * SkySmooth(0.02f, 0.1f, along) * SkySmooth(0.5f, 0.4f, along);

            // The spots run from a fifth to three quarters of the way back, just above the middle of each side
            float gap = (Mathf.Repeat(along * 14f, 1f) - 0.5f) / 14f;
            float degrees = around * Mathf.Rad2Deg;
            float off = Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(degrees, 15f)), Mathf.Abs(Mathf.DeltaAngle(degrees, 165f))) * Mathf.Deg2Rad * 0.1f;
            float spot = SkySmooth(0.02f, 0.008f, Mathf.Sqrt(gap * gap + off * off)) * SkySmooth(0.15f, 0.2f, along) * SkySmooth(0.75f, 0.7f, along);

            shade = Mathf.Lerp(shade, 1f, spot);
            return new Color(shade, shade, shade, 0.8f + 0.2f * spot);
        }
    }
}
