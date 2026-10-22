/*
 * Eclipse Menu  Mods/Eclipse.cs
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
using Eclipse.Menu;
using GorillaTag.Rendering;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Eclipse.Menu.Main;
using static Eclipse.Mods.Custom;
using Random = UnityEngine.Random;

namespace Eclipse.Mods
{
    /// <summary>Eclipse's own mods, in the Eclipse Mods category.</summary>
    /// <remarks>
    /// Space-themed, and every one of them acts on you alone: how you move, and things only you
    /// can see. Nothing here touches another player or sends anything over the network. The class
    /// is split over two files; this one holds the shared helpers, the sky, the effects on you and
    /// the movement mods.
    /// </remarks>
    public static partial class EclipseMods
    {
        public const string Category = "Eclipse Mods";

        // ── Colour ──────────────────────────────────────────────────────────────

        public static readonly string[] ColorNames = { "Corona", "Moonlight", "Nebula", "Aurora", "Mine", "Rainbow" };
        private static int colorMode;

        public static void SetColorMode(int mode) =>
            colorMode = Mathf.Clamp(mode, 0, ColorNames.Length - 1);

        /// <summary>The main colour every Eclipse mod draws in, chosen with Eclipse Mods Colour.</summary>
        internal static Color Accent => Pick(0f);

        /// <summary>A second colour from the same set, for the parts that sit next to the main one.</summary>
        internal static Color Second => Pick(0.5f);

        private static Color Pick(float shift)
        {
            float t = Time.time;
            float wave = Mathf.PingPong(t * 0.4f + shift, 1f);
            switch (colorMode)
            {
                case 1: return Color.Lerp(new Color(0.72f, 0.8f, 1f), Color.white, wave);
                case 2: return Color.Lerp(new Color(1f, 0.42f, 0.84f), new Color(0.6f, 0.42f, 1f), wave);
                case 3: return Color.Lerp(new Color(0.18f, 0.9f, 0.62f), new Color(0.22f, 0.71f, 1f), wave);
                case 4: return VRRig.LocalRig.playerColor;
                case 5: return Color.HSVToRGB((t * 0.15f + shift) % 1f, 0.85f, 1f);
                default: return Color.Lerp(new Color(1f, 0.62f, 0.26f), new Color(1f, 0.84f, 0.4f), wave);
            }
        }

        private static Color WithAlpha(Color color, float alpha) =>
            new Color(color.r, color.g, color.b, alpha);

        // ── Shared helpers ──────────────────────────────────────────────────────

        internal static Vector3 BodyPosition => Extras.BodyPosition;
        internal static Vector3 Feet => Extras.BodyPosition + Vector3.down * (0.45f * Scale);
        internal static Vector3 LeftHand => GorillaTagger.Instance.leftHandTransform.position;
        internal static Vector3 RightHand => GorillaTagger.Instance.rightHandTransform.position;

        private const int RingPoints = 48;
        private static readonly Vector3[] ringBuffer = new Vector3[RingPoints];

        internal static LineRenderer MakeRing(string name)
        {
            LineRenderer ring = MakeLine(name, 0.02f);
            ring.positionCount = RingPoints;
            ring.loop = true;
            return ring;
        }

        /// <summary>Lays a ring out around a centre. The ring lies flat across `normal`.</summary>
        internal static void PlaceRing(LineRenderer ring, Vector3 centre, Vector3 normal, float radius, float width, Color color, float spin = 0f)
        {
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, spin, 0f);
            for (int i = 0; i < RingPoints; i++)
            {
                float angle = i * Mathf.PI * 2f / RingPoints;
                ringBuffer[i] = centre + rotation * new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }

            ring.SetPositions(ringBuffer);
            ring.startWidth = ring.endWidth = width;
            ring.startColor = ring.endColor = color;
        }

        /// <summary>A small glowing star, using the menu's own star shape.</summary>
        internal static Piece MakeStar(Color color, float size)
        {
            Piece piece = MakePiece(PrimitiveType.Cube, color, Vector3.one * size);
            piece.Object.GetComponent<MeshFilter>().sharedMesh = EclipseMeshes.Star;
            piece.Size = size;
            return piece;
        }

        internal static Piece EnsureStar(ref Piece piece, Color color, float size)
        {
            if (piece == null || !piece.Alive)
                piece = MakeStar(color, size);
            return piece;
        }

        private static void DiscardPiece(ref Piece piece)
        {
            Discard(piece);
            piece = null;
        }

        private static Gradient FadeInOut()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        /// <summary>Forgets held buttons and timers, run whenever one of the button mods is turned on.</summary>
        /// <remarks>A button already held while turning a mod on would otherwise count as a press.</remarks>
        public static void ResetState()
        {
            supernovaHeld = starJumpHeld = beaconHeld = rocketHeld = wormholeHeld = true;
            dashCharge = 0f;
            dashHeld = rightGrab;
            hopGrounded = true;
            sessionStart = Time.time;
            catcherScore = 0;
        }

        /// <summary>Turns off every Eclipse mod that is on.</summary>
        public static void TurnOffAll()
        {
            int category = Buttons.GetCategory(Category);
            if (category < 0)
                return;

            foreach (ButtonInfo button in Buttons.buttons[category])
                if (button.enabled && button.isTogglable)
                    Main.Toggle(button);

            NotificationManager.SendNotification("Every Eclipse mod is off.", 3000);
        }

        // ── Sky and scene ───────────────────────────────────────────────────────

        private static Piece skyDisc;
        private static LineRenderer skyCorona, skyHalo;

        /// <summary>A total eclipse hanging in your sky: a dark moon ringed by a burning corona.</summary>
        public static void EclipseSky()
        {
            if (skyDisc == null)
                Main.EclipseUnlock("Stargazer");

            Vector3 direction = Quaternion.Euler(-32f, 35f, 0f) * Vector3.forward;
            Vector3 centre = Head.position + direction * 90f;

            Ensure(ref skyDisc, PrimitiveType.Sphere, Color.black);
            skyDisc.Object.transform.position = centre;
            skyDisc.Object.transform.localScale = Vector3.one * 14f;
            SetColor(skyDisc, new Color(0.02f, 0.01f, 0.05f, 1f));

            if (skyCorona == null)
                skyCorona = MakeRing("Eclipse_SkyCorona");
            if (skyHalo == null)
                skyHalo = MakeRing("Eclipse_SkyHalo");

            float pulse = 1f + Mathf.Sin(Time.time * 1.4f) * 0.04f;
            PlaceRing(skyCorona, centre, direction, 7.5f * pulse, 1.4f, Accent);
            PlaceRing(skyHalo, centre, direction, 9.4f * pulse, 3.2f, WithAlpha(Second, 0.3f));
        }

        public static void DisableEclipseSky()
        {
            DiscardPiece(ref skyDisc);
            DestroyLine(ref skyCorona);
            DestroyLine(ref skyHalo);
        }

        private static ParticleSystem starField;

        /// <summary>Twinkling stars all around you, whatever map you are on.</summary>
        public static void StarField()
        {
            if (starField == null)
            {
                starField = MakeParticles("Eclipse_StarField", null, particles =>
                {
                    var main = particles.main;
                    main.loop = true;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
                    main.startSpeed = 0f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
                    main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(0.75f, 0.82f, 1f));
                    main.maxParticles = 600;

                    var emission = particles.emission;
                    emission.rateOverTime = 110f;

                    var shape = particles.shape;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 14f;
                    shape.radiusThickness = 0.35f;

                    var fade = particles.colorOverLifetime;
                    fade.enabled = true;
                    fade.color = new ParticleSystem.MinMaxGradient(FadeInOut());
                });
            }

            Follow(starField, Head.position);
        }

        public static void DisableStarField() => DestroyParticles(ref starField);

        private static ParticleSystem starfall;

        /// <summary>Bright stars raining down across the whole sky above you.</summary>
        public static void Starfall()
        {
            if (starfall == null)
            {
                starfall = MakeParticles("Eclipse_Starfall", null, particles =>
                {
                    var main = particles.main;
                    main.loop = true;
                    main.startLifetime = 2.5f;
                    main.startSpeed = 0f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.55f), Color.white);
                    main.maxParticles = 60;

                    var emission = particles.emission;
                    emission.rateOverTime = 5f;

                    var shape = particles.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(70f, 1f, 70f);

                    var velocity = particles.velocityOverLifetime;
                    velocity.enabled = true;
                    velocity.space = ParticleSystemSimulationSpace.World;
                    velocity.x = new ParticleSystem.MinMaxCurve(-9f);
                    velocity.y = new ParticleSystem.MinMaxCurve(-16f);
                    velocity.z = new ParticleSystem.MinMaxCurve(-4f);

                    var fade = particles.colorOverLifetime;
                    fade.enabled = true;
                    fade.color = new ParticleSystem.MinMaxGradient(FadeInOut());

                    ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
                    renderer.renderMode = ParticleSystemRenderMode.Stretch;
                    renderer.velocityScale = 0.12f;
                    renderer.lengthScale = 2f;
                });
            }

            Follow(starfall, Head.position + new Vector3(10f, 28f, 4f) * Scale);
        }

        public static void DisableStarfall() => DestroyParticles(ref starfall);

        /// <summary>Tints the air a deep night blue.</summary>
        public static void NightFilter()
        {
            if (ZoneShaderSettings.activeInstance != null)
                ZoneShaderSettings.activeInstance.SetGroundFogValue(new Color(0.05f, 0.03f, 0.16f, 0.35f), 0f, float.MaxValue, 0f);
        }

        public static void DisableNightFilter()
        {
            if (ZoneShaderSettings.activeInstance != null && ZoneShaderSettings.defaultsInstance != null)
                ZoneShaderSettings.activeInstance.CopySettings(ZoneShaderSettings.defaultsInstance);
        }

        private static LineRenderer constellation;
        private static readonly Piece[] constellationStars = new Piece[4];
        private static readonly Vector3[] constellationPoints = new Vector3[4];

        /// <summary>Your head, hands and body joined up like a constellation.</summary>
        public static void ConstellationLines()
        {
            constellationPoints[0] = Head.position + Vector3.up * (0.12f * Scale);
            constellationPoints[1] = LeftHand;
            constellationPoints[2] = BodyPosition;
            constellationPoints[3] = RightHand;

            if (constellation == null)
            {
                constellation = MakeLine("Eclipse_Constellation", 0.01f);
                constellation.positionCount = constellationPoints.Length;
                constellation.loop = true;
            }

            constellation.SetPositions(constellationPoints);
            constellation.startWidth = constellation.endWidth = 0.008f * Scale;
            constellation.startColor = constellation.endColor = WithAlpha(Accent, 0.8f);

            for (int i = 0; i < constellationStars.Length; i++)
            {
                Piece star = EnsureStar(ref constellationStars[i], Color.white, 0.05f);
                star.Object.transform.position = constellationPoints[i];
                star.Object.transform.localScale = Vector3.one * (0.05f * Scale * (1f + Mathf.Sin(Time.time * 3f + i) * 0.2f));
                star.Object.transform.rotation = Quaternion.Euler(0f, Time.time * 60f + i * 30f, 0f);
                SetColor(star, Color.Lerp(Color.white, Second, 0.4f));
            }
        }

        public static void DisableConstellationLines()
        {
            DestroyLine(ref constellation);
            for (int i = 0; i < constellationStars.Length; i++)
                DiscardPiece(ref constellationStars[i]);
        }

        private static LineRenderer galaxyArmA, galaxyArmB;
        private const int GalaxyPoints = 40;
        private static readonly Vector3[] galaxyBuffer = new Vector3[GalaxyPoints];

        /// <summary>A slowly turning spiral galaxy under your feet.</summary>
        public static void GalaxyFloor()
        {
            if (galaxyArmA == null)
                galaxyArmA = MakeGalaxyArm("Eclipse_GalaxyA");
            if (galaxyArmB == null)
                galaxyArmB = MakeGalaxyArm("Eclipse_GalaxyB");

            Vector3 centre = Feet + Vector3.up * (0.02f * Scale);
            float turn = Time.time * 0.6f;
            PlaceGalaxyArm(galaxyArmA, centre, turn);
            PlaceGalaxyArm(galaxyArmB, centre, turn + Mathf.PI);
        }

        private static LineRenderer MakeGalaxyArm(string name)
        {
            LineRenderer arm = MakeLine(name, 0.04f);
            arm.positionCount = GalaxyPoints;
            arm.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.15f);
            return arm;
        }

        private static void PlaceGalaxyArm(LineRenderer arm, Vector3 centre, float turn)
        {
            float scale = Scale;
            for (int i = 0; i < GalaxyPoints; i++)
            {
                float k = i / (float)(GalaxyPoints - 1);
                float angle = turn + k * 4.2f;
                float radius = (0.08f + k * 1.1f) * scale;
                galaxyBuffer[i] = centre + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }

            arm.SetPositions(galaxyBuffer);
            arm.widthMultiplier = 0.06f * scale;
            arm.startColor = WithAlpha(Accent, 0.9f);
            arm.endColor = WithAlpha(Second, 0.1f);
        }

        public static void DisableGalaxyFloor()
        {
            DestroyLine(ref galaxyArmA);
            DestroyLine(ref galaxyArmB);
        }

        // ── On you ──────────────────────────────────────────────────────────────

        private static LineRenderer coronaLeft, coronaRight, coronaLeftOuter, coronaRightOuter;

        /// <summary>Burning corona rings around both of your hands.</summary>
        public static void CoronaHands()
        {
            if (coronaLeft == null) coronaLeft = MakeRing("Eclipse_CoronaLeft");
            if (coronaRight == null) coronaRight = MakeRing("Eclipse_CoronaRight");
            if (coronaLeftOuter == null) coronaLeftOuter = MakeRing("Eclipse_CoronaLeftOuter");
            if (coronaRightOuter == null) coronaRightOuter = MakeRing("Eclipse_CoronaRightOuter");

            float scale = Scale;
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.12f;
            Transform left = GorillaTagger.Instance.leftHandTransform;
            Transform right = GorillaTagger.Instance.rightHandTransform;

            PlaceRing(coronaLeft, left.position, left.forward, 0.07f * scale, 0.012f * scale, Accent);
            PlaceRing(coronaRight, right.position, right.forward, 0.07f * scale, 0.012f * scale, Accent);
            PlaceRing(coronaLeftOuter, left.position, left.forward, 0.1f * scale * pulse, 0.02f * scale, WithAlpha(Second, 0.35f));
            PlaceRing(coronaRightOuter, right.position, right.forward, 0.1f * scale * pulse, 0.02f * scale, WithAlpha(Second, 0.35f));
        }

        public static void DisableCoronaHands()
        {
            DestroyLine(ref coronaLeft);
            DestroyLine(ref coronaRight);
            DestroyLine(ref coronaLeftOuter);
            DestroyLine(ref coronaRightOuter);
        }

        private static readonly Piece[] moons = new Piece[3];

        /// <summary>Three little moons circling your head.</summary>
        public static void OrbitingMoons()
        {
            float scale = Scale;
            for (int i = 0; i < moons.Length; i++)
            {
                Piece moon = Ensure(ref moons[i], PrimitiveType.Sphere, Color.white);
                float radius = (0.32f + i * 0.14f) * scale;
                float speed = 1.5f - i * 0.4f;
                float angle = Time.time * speed + i * 2.1f;
                Quaternion tilt = Quaternion.Euler(12f * (i - 1), 0f, 18f * (i - 1));

                moon.Object.transform.position = Head.position + tilt * new Vector3(Mathf.Cos(angle) * radius, 0.05f * scale, Mathf.Sin(angle) * radius);
                moon.Object.transform.localScale = Vector3.one * ((0.05f + 0.02f * i) * scale);
                SetColor(moon, i == 0 ? Accent : i == 1 ? Second : new Color(0.85f, 0.85f, 0.92f));
            }
        }

        public static void DisableOrbitingMoons()
        {
            for (int i = 0; i < moons.Length; i++)
                DiscardPiece(ref moons[i]);
        }

        private static LineRenderer saturnRing, saturnInner;

        /// <summary>Planetary rings around your body.</summary>
        public static void SaturnRing()
        {
            if (saturnRing == null) saturnRing = MakeRing("Eclipse_SaturnRing");
            if (saturnInner == null) saturnInner = MakeRing("Eclipse_SaturnInner");

            float scale = Scale;
            Vector3 tilt = Quaternion.Euler(18f, Time.time * 10f, 0f) * Vector3.up;
            PlaceRing(saturnRing, BodyPosition, tilt, 0.62f * scale, 0.06f * scale, WithAlpha(Accent, 0.85f));
            PlaceRing(saturnInner, BodyPosition, tilt, 0.5f * scale, 0.04f * scale, WithAlpha(Second, 0.45f));
        }

        public static void DisableSaturnRing()
        {
            DestroyLine(ref saturnRing);
            DestroyLine(ref saturnInner);
        }

        private static TrailRenderer cometTrail;

        /// <summary>A glowing comet tail streams out behind you.</summary>
        public static void CometTrail()
        {
            if (cometTrail == null)
            {
                cometTrail = new GameObject("Eclipse_CometTrail").AddComponent<TrailRenderer>();
                cometTrail.material = VertexColored;
                cometTrail.time = 1.2f;
                cometTrail.minVertexDistance = 0.08f;
                cometTrail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                cometTrail.transform.position = BodyPosition;
                cometTrail.Clear();
            }

            cometTrail.transform.position = BodyPosition;
            cometTrail.widthMultiplier = 0.3f * Scale;
            cometTrail.startColor = WithAlpha(Accent, 0.8f);
            cometTrail.endColor = WithAlpha(Second, 0f);
        }

        public static void DisableCometTrail()
        {
            if (cometTrail != null)
                UnityEngine.Object.Destroy(cometTrail.gameObject);
            cometTrail = null;
        }

        private static Piece planetHat;
        private static LineRenderer planetHatRing;

        /// <summary>A tiny ringed planet floating just above your head.</summary>
        public static void PlanetHat()
        {
            float scale = Scale;
            Vector3 centre = Head.position + Vector3.up * (0.3f * scale) + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.015f * scale);

            Ensure(ref planetHat, PrimitiveType.Sphere, Color.white);
            planetHat.Object.transform.position = centre;
            planetHat.Object.transform.localScale = Vector3.one * (0.16f * scale);
            SetColor(planetHat, Second);

            if (planetHatRing == null)
                planetHatRing = MakeRing("Eclipse_PlanetHatRing");
            PlaceRing(planetHatRing, centre, Quaternion.Euler(22f, 0f, 10f) * Vector3.up, 0.15f * scale, 0.025f * scale, Accent, Time.time * 40f);
        }

        public static void DisablePlanetHat()
        {
            DiscardPiece(ref planetHat);
            DestroyLine(ref planetHatRing);
        }

        private static readonly Piece[] crownStars = new Piece[8];

        /// <summary>A crown of little stars turning above your head.</summary>
        public static void HaloCrown()
        {
            float scale = Scale;
            Vector3 centre = Head.position + Vector3.up * (0.22f * scale);
            for (int i = 0; i < crownStars.Length; i++)
            {
                Piece star = EnsureStar(ref crownStars[i], Color.white, 0.05f);
                float angle = Time.time * 0.9f + i * Mathf.PI * 2f / crownStars.Length;
                star.Object.transform.position = centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (0.16f * scale);
                star.Object.transform.localScale = Vector3.one * (0.05f * scale);
                star.Object.transform.rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg * 2f, 0f);
                SetColor(star, i % 2 == 0 ? Accent : Second);
            }
        }

        public static void DisableHaloCrown()
        {
            for (int i = 0; i < crownStars.Length; i++)
                DiscardPiece(ref crownStars[i]);
        }

        private static bool supernovaHeld = true;
        private static int supernovaCount;
        private static readonly List<Piece> shockwaves = new List<Piece>();

        /// <summary>Right trigger sets off a supernova from your hand: a shower of sparks and a glowing shockwave.</summary>
        public static void SupernovaBurst()
        {
            if (Pressed(rightTrigger > 0.5f, ref supernovaHeld) && shockwaves.Count < 6)
            {
                Vector3 at = RightHand;
                Burst(at, 70, 4f, false);

                if (++supernovaCount == 10)
                    Main.EclipseUnlock("Supernova");

                Piece wave = MakePiece(PrimitiveType.Sphere, Accent, Vector3.one * 0.1f);
                wave.Object.transform.position = at;
                wave.Born = Time.time;
                shockwaves.Add(wave);
            }

            for (int i = shockwaves.Count - 1; i >= 0; i--)
            {
                Piece wave = shockwaves[i];
                float age = (Time.time - wave.Born) / 0.7f;
                if (!wave.Alive || age >= 1f)
                {
                    Discard(wave);
                    shockwaves.RemoveAt(i);
                    continue;
                }

                wave.Object.transform.localScale = Vector3.one * (Mathf.Lerp(0.1f, 2.2f, 1f - (1f - age) * (1f - age)) * Scale);
                SetAlpha(wave, Accent, 0.45f * (1f - age));
            }
        }

        public static void DisableSupernovaBurst() => ClearPieces(shockwaves);

        private static TextMeshPro moonWatch;
        private static float moonWatchNext;

        /// <summary>The time and tonight's real moon phase, on your left wrist.</summary>
        public static void MoonPhaseWatch()
        {
            if (moonWatch == null)
                moonWatch = MakeWristText("Eclipse_MoonWatch", Color.white);

            PlaceOnWrist(moonWatch, 0.3f);
            if (Time.time < moonWatchNext)
                return;
            moonWatchNext = Time.time + 1f;

            // Days since a known new moon, wrapped by the length of a lunar month
            const double synodicMonth = 29.530588853;
            double age = (DateTime.UtcNow - new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc)).TotalDays % synodicMonth;
            if (age < 0)
                age += synodicMonth;

            double phase = age / synodicMonth;
            int lit = (int)Math.Round((1 - Math.Cos(phase * Math.PI * 2)) * 50);
            string[] names = { "New Moon", "Waxing Crescent", "First Quarter", "Waxing Gibbous", "Full Moon", "Waning Gibbous", "Last Quarter", "Waning Crescent" };
            string name = names[(int)Math.Floor(phase * 8 + 0.5) % 8];

            moonWatch.text = $"{DateTime.Now:HH:mm}\n<size=55%>{name}  {lit}%</size>";
            moonWatch.color = Color.Lerp(Color.white, Accent, 0.35f);
        }

        public static void DisableMoonPhaseWatch() => DestroyText(ref moonWatch);

        // ── Movement (yours only) ───────────────────────────────────────────────

        /// <summary>The Moon's gravity: a sixth of Earth's, so every jump floats.</summary>
        public static void MoonGravity()
        {
            if (Extras.Grounded())
                return;

            Extras.Body.linearVelocity -= Physics.gravity * (5f / 6f * Time.deltaTime);
        }

        /// <summary>No gravity at all, with a little drag so you drift to a stop. Hold your left grip to fall.</summary>
        public static void ZeroGFloat()
        {
            if (leftGrab)
                return;

            Vector3 velocity = Extras.Body.linearVelocity;
            velocity -= Physics.gravity * Time.deltaTime;
            velocity *= 1f - Mathf.Clamp01(0.6f * Time.deltaTime);
            Extras.Body.linearVelocity = velocity;
        }

        private static float dashCharge, dashReady;
        private static bool dashHeld;

        /// <summary>Hold your right grip to charge, let go to dash the way you look. A longer charge goes further.</summary>
        public static void SolarDash()
        {
            if (rightGrab)
            {
                if (!dashHeld)
                    dashCharge = 0f;

                dashHeld = true;
                dashCharge = Mathf.Min(1f, dashCharge + Time.deltaTime);
                if (Random.value < 0.3f)
                    Burst(RightHand, 1, 0.6f, false);
                return;
            }

            if (dashHeld && dashCharge > 0.2f && Time.time > dashReady)
            {
                Extras.Body.linearVelocity = Head.forward * (8f + 10f * dashCharge);
                Burst(BodyPosition, 30, 3f, false, -Head.forward);
                dashReady = Time.time + 0.4f;
            }

            dashHeld = false;
            dashCharge = 0f;
        }

        private static bool starJumpHeld = true;

        /// <summary>A on the ground launches you high into the air in a burst of stars.</summary>
        public static void StarJump()
        {
            if (!Pressed(rightPrimary, ref starJumpHeld) || !Extras.Grounded())
                return;

            Vector3 velocity = Extras.Body.linearVelocity;
            velocity.y = 10f;
            Extras.Body.linearVelocity = velocity;
            Burst(Feet, 40, 3f, true, Vector3.up);
        }

        /// <summary>Hold Y to fall upwards.</summary>
        public static void GravityFlip()
        {
            if (!leftSecondary)
                return;

            Vector3 velocity = Extras.Body.linearVelocity;
            velocity -= Physics.gravity * (2f * Time.deltaTime);
            velocity.y = Mathf.Min(velocity.y, 9f);
            Extras.Body.linearVelocity = velocity;
        }

        private static bool hopGrounded = true;

        /// <summary>Every jump slings you forward like a spacecraft swinging round a planet.</summary>
        public static void OrbitHop()
        {
            bool grounded = Extras.Grounded();
            if (hopGrounded && !grounded && Extras.Body.linearVelocity.y > 1.5f)
            {
                Extras.Body.linearVelocity += LookFlat() * 4f;
                Burst(Feet, 12, 1.5f, true);
            }

            hopGrounded = grounded;
        }

        private static float slideSparks;

        /// <summary>Hold your left trigger on the ground to slide the way you look, trailing sparks.</summary>
        public static void CometSlide()
        {
            if (leftTrigger < 0.5f || !Extras.Grounded())
                return;

            Vector3 velocity = Extras.Body.linearVelocity;
            Vector3 flat = Vector3.MoveTowards(new Vector3(velocity.x, 0f, velocity.z), LookFlat() * 7f, 20f * Time.deltaTime);
            Extras.Body.linearVelocity = new Vector3(flat.x, velocity.y, flat.z);

            if (Time.time > slideSparks)
            {
                slideSparks = Time.time + 0.05f;
                Burst(Feet, 3, 1f, true, -LookFlat());
            }
        }

        /// <summary>Falling fast near the ground fires landing thrusters that slow you down.</summary>
        public static void LunarLander()
        {
            Vector3 velocity = Extras.Body.linearVelocity;
            if (velocity.y > -7f)
                return;

            if (!Physics.Raycast(BodyPosition, Vector3.down, 5f * Scale, GorillaLocomotion.GTPlayer.Instance.locomotionEnabledLayers))
                return;

            velocity.y = Mathf.MoveTowards(velocity.y, -2.5f, 30f * Time.deltaTime);
            Extras.Body.linearVelocity = velocity;
            Burst(Feet, 4, 2f, true, Vector3.down);
        }
    }
}
