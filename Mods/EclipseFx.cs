/*
 * Eclipse Menu  Mods/EclipseFx.cs
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

using Eclipse.Extensions;
using Eclipse.Utilities;
using GorillaLocomotion;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Eclipse.Menu.Main;
using static Eclipse.Mods.Custom;
using Random = UnityEngine.Random;

namespace Eclipse.Mods
{
    /// <summary>The second half of the Eclipse mods: tools, visuals around you and toys.</summary>
    public static partial class EclipseMods
    {
        // ── Tools ───────────────────────────────────────────────────────────────

        private static Piece moonbeam;
        private static Light moonbeamLight;

        /// <summary>A soft beam of moonlight shining from your right hand.</summary>
        public static void Moonbeam()
        {
            var hand = ControllerUtilities.GetTrueRightHand();
            float scale = Scale;

            Ensure(ref moonbeam, PrimitiveType.Cylinder, Color.white);
            moonbeam.Object.transform.rotation = Quaternion.FromToRotation(Vector3.up, hand.forward);
            moonbeam.Object.transform.localScale = new Vector3(0.22f, 3f, 0.22f) * scale;
            moonbeam.Object.transform.position = hand.position + hand.forward * (3f * scale);
            SetAlpha(moonbeam, Color.Lerp(Color.white, Second, 0.3f), 0.08f);

            if (moonbeamLight == null)
            {
                moonbeamLight = new GameObject("Eclipse_MoonbeamLight").AddComponent<Light>();
                moonbeamLight.type = LightType.Spot;
                moonbeamLight.range = 25f;
                moonbeamLight.spotAngle = 35f;
                moonbeamLight.intensity = 2f;
            }

            moonbeamLight.transform.SetPositionAndRotation(hand.position, Quaternion.LookRotation(hand.forward));
            moonbeamLight.color = Color.Lerp(Color.white, Second, 0.3f);
        }

        public static void DisableMoonbeam()
        {
            DiscardPiece(ref moonbeam);
            if (moonbeamLight != null)
                UnityEngine.Object.Destroy(moonbeamLight.gameObject);
            moonbeamLight = null;
        }

        private sealed class Beacon
        {
            public Piece Beam;
            public Piece Star;
            public LineRenderer Base;
            public Vector3 Point;
        }

        private static readonly List<Beacon> beacons = new List<Beacon>();
        private static bool beaconHeld = true;

        /// <summary>B drops a beam of starlight wherever your right hand points, to mark a spot. Up to three at once.</summary>
        public static void StarlightBeacon()
        {
            if (Pressed(rightSecondary, ref beaconHeld))
            {
                var hand = ControllerUtilities.GetTrueRightHand();
                if (Physics.Raycast(hand.position, hand.forward, out RaycastHit hit, 80f * Scale, NoInvisLayerMask()))
                {
                    if (beacons.Count >= 3)
                    {
                        RemoveBeacon(beacons[0]);
                        beacons.RemoveAt(0);
                    }

                    beacons.Add(new Beacon
                    {
                        Beam = MakePiece(PrimitiveType.Cylinder, Accent, Vector3.one),
                        Star = MakeStar(Color.white, 0.3f),
                        Base = MakeRing("Eclipse_BeaconBase"),
                        Point = hit.point
                    });
                }
            }

            float scale = Scale;
            for (int i = 0; i < beacons.Count; i++)
            {
                Beacon beacon = beacons[i];
                if (!beacon.Beam.Alive || !beacon.Star.Alive || beacon.Base == null)
                    continue;

                float pulse = 1f + Mathf.Sin(Time.time * 3f + i) * 0.15f;
                beacon.Beam.Object.transform.position = beacon.Point + Vector3.up * (6f * scale);
                beacon.Beam.Object.transform.localScale = new Vector3(0.08f * pulse, 6f, 0.08f * pulse) * scale;
                SetAlpha(beacon.Beam, Accent, 0.35f);

                beacon.Star.Object.transform.position = beacon.Point + Vector3.up * (12.2f * scale);
                beacon.Star.Object.transform.localScale = Vector3.one * (0.35f * scale * pulse);
                beacon.Star.Object.transform.rotation = Quaternion.Euler(0f, Time.time * 90f, 0f);
                SetColor(beacon.Star, Second);

                PlaceRing(beacon.Base, beacon.Point + Vector3.up * (0.03f * scale), Vector3.up, 0.4f * scale * pulse, 0.04f * scale, Accent);
            }
        }

        private static void RemoveBeacon(Beacon beacon)
        {
            Discard(beacon.Beam);
            Discard(beacon.Star);
            DestroyLine(ref beacon.Base);
        }

        public static void DisableStarlightBeacon()
        {
            foreach (Beacon beacon in beacons)
                RemoveBeacon(beacon);
            beacons.Clear();
        }

        private static TextMeshPro speedometer;
        private static float topSpeed, speedometerNext;

        /// <summary>How fast you are going, and your top speed, on your left wrist.</summary>
        public static void Speedometer()
        {
            if (speedometer == null)
            {
                speedometer = MakeWristText("Eclipse_Speedometer", Color.white);
                topSpeed = 0f;
            }

            PlaceOnWrist(speedometer, 0.42f);

            float speed = Extras.Body.linearVelocity.magnitude;
            topSpeed = Mathf.Max(topSpeed, speed);
            if (Time.time < speedometerNext)
                return;
            speedometerNext = Time.time + 0.1f;

            speedometer.text = $"{speed:0.0} m/s\n<size=55%>top {topSpeed:0.0}</size>";
            speedometer.color = Color.Lerp(Color.white, Accent, Mathf.Clamp01(speed / 12f));
        }

        public static void DisableSpeedometer() => DestroyText(ref speedometer);

        private static TextMeshPro heightMeter;
        private static float heightMeterNext;

        /// <summary>How high above the ground you are, on your left wrist.</summary>
        public static void HeightMeter()
        {
            if (heightMeter == null)
                heightMeter = MakeWristText("Eclipse_HeightMeter", Color.white);

            PlaceOnWrist(heightMeter, 0.54f);
            if (Time.time < heightMeterNext)
                return;
            heightMeterNext = Time.time + 0.15f;

            Vector3 from = BodyPosition;
            string above = Physics.Raycast(from, Vector3.down, out RaycastHit hit, 300f, GTPlayer.Instance.locomotionEnabledLayers)
                ? $"{Mathf.Max(0f, hit.distance - 0.45f * Scale):0.0} m up"
                : "high up";

            heightMeter.text = $"{above}\n<size=55%>altitude {from.y:0}</size>";
            heightMeter.color = Color.Lerp(Color.white, Second, 0.35f);
        }

        public static void DisableHeightMeter() => DestroyText(ref heightMeter);

        private static TextMeshPro sessionTimer;
        private static float sessionStart, sessionNext;

        /// <summary>How long you have been playing since you turned it on, on your left wrist.</summary>
        public static void SessionTimer()
        {
            if (sessionTimer == null)
                sessionTimer = MakeWristText("Eclipse_SessionTimer", Color.white);

            PlaceOnWrist(sessionTimer, 0.66f);
            if (Time.time < sessionNext)
                return;
            sessionNext = Time.time + 0.5f;

            int seconds = Mathf.FloorToInt(Time.time - sessionStart);
            sessionTimer.text = seconds >= 3600
                ? $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}"
                : $"{seconds / 60:00}:{seconds % 60:00}";
            sessionTimer.color = Color.Lerp(Color.white, Accent, 0.25f);
        }

        public static void DisableSessionTimer() => DestroyText(ref sessionTimer);

        private static LineRenderer compassRing;
        private static readonly TextMeshPro[] compassLetters = new TextMeshPro[4];
        private static readonly string[] CompassNames = { "N", "E", "S", "W" };

        /// <summary>A compass ring around your feet that always points north.</summary>
        public static void CompassRing()
        {
            float scale = Scale;
            Vector3 centre = Feet + Vector3.up * (0.05f * scale);

            if (compassRing == null)
                compassRing = MakeRing("Eclipse_CompassRing");
            PlaceRing(compassRing, centre, Vector3.up, 0.7f * scale, 0.015f * scale, WithAlpha(Accent, 0.8f));

            for (int i = 0; i < compassLetters.Length; i++)
            {
                if (compassLetters[i] == null)
                {
                    compassLetters[i] = MakeWristText("Eclipse_Compass" + CompassNames[i], Color.white);
                    compassLetters[i].text = CompassNames[i];
                }

                TextMeshPro letter = compassLetters[i];
                float angle = i * Mathf.PI / 2f;
                letter.SafeSetFont(activeFont);
                letter.transform.position = centre + new Vector3(Mathf.Sin(angle), 0.12f, Mathf.Cos(angle)) * (0.82f * scale);
                letter.transform.localScale = Vector3.one * (0.1f * scale);
                letter.transform.LookAt(Head.position);
                letter.transform.Rotate(0f, 180f, 0f);
                letter.color = i == 0 ? Accent : Color.white;
            }
        }

        public static void DisableCompassRing()
        {
            DestroyLine(ref compassRing);
            for (int i = 0; i < compassLetters.Length; i++)
                DestroyText(ref compassLetters[i]);
        }

        private const int PingSamples = 40;
        private static readonly float[] pings = new float[PingSamples];
        private static readonly Vector3[] pingBuffer = new Vector3[PingSamples];
        private static int pingIndex;
        private static float pingNext;
        private static LineRenderer pingGraph;
        private static TextMeshPro pingText;

        /// <summary>Your ping over the last ten seconds, drawn as a little graph above your right wrist.</summary>
        public static void PingGraph()
        {
            if (pingGraph == null)
            {
                pingGraph = MakeLine("Eclipse_PingGraph", 0.005f);
                pingGraph.positionCount = PingSamples;
            }

            if (pingText == null)
                pingText = MakeWristText("Eclipse_PingText", Color.white);

            if (Time.time > pingNext)
            {
                pingNext = Time.time + 0.25f;
                pings[pingIndex] = PhotonNetwork.IsConnected ? PhotonNetwork.GetPing() : 0f;
                pingIndex = (pingIndex + 1) % PingSamples;

                int ping = Mathf.RoundToInt(pings[(pingIndex + PingSamples - 1) % PingSamples]);
                pingText.text = PhotonNetwork.IsConnected ? $"{ping} ms" : "offline";
                pingText.color = ping < 80 ? new Color(0.5f, 1f, 0.6f) : ping < 160 ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.45f, 0.4f);
            }

            PlaceOnWrist(pingText, 0.42f, true);

            float scale = Scale;
            Vector3 origin = RightHand + Vector3.up * (0.27f * scale);
            Vector3 toHead = Head.position - origin;
            toHead.y = 0f;
            Vector3 across = Vector3.Cross(Vector3.up, toHead.sqrMagnitude < 0.0001f ? Vector3.forward : toHead.normalized);

            float top = 100f;
            for (int i = 0; i < PingSamples; i++)
                top = Mathf.Max(top, pings[i]);

            for (int i = 0; i < PingSamples; i++)
            {
                float value = pings[(pingIndex + i) % PingSamples] / top;
                pingBuffer[i] = origin + across * ((i / (float)(PingSamples - 1) - 0.5f) * 0.22f * scale) + Vector3.up * (value * 0.08f * scale);
            }

            pingGraph.SetPositions(pingBuffer);
            pingGraph.startWidth = pingGraph.endWidth = 0.005f * scale;
            pingGraph.startColor = WithAlpha(Second, 0.5f);
            pingGraph.endColor = Accent;
        }

        public static void DisablePingGraph()
        {
            DestroyLine(ref pingGraph);
            DestroyText(ref pingText);
        }

        // ── Visuals around you ──────────────────────────────────────────────────

        private static TrailRenderer auroraLeft, auroraRight;

        /// <summary>Ribbons of aurora light flowing from your hands.</summary>
        public static void AuroraRibbons()
        {
            if (auroraLeft == null)
                auroraLeft = MakeAurora("Eclipse_AuroraLeft", LeftHand);
            if (auroraRight == null)
                auroraRight = MakeAurora("Eclipse_AuroraRight", RightHand);

            auroraLeft.transform.position = LeftHand;
            auroraRight.transform.position = RightHand;
            auroraLeft.widthMultiplier = auroraRight.widthMultiplier = 0.12f * Scale;
        }

        private static TrailRenderer MakeAurora(string name, Vector3 at)
        {
            TrailRenderer trail = new GameObject(name).AddComponent<TrailRenderer>();
            trail.transform.position = at;
            trail.material = VertexColored;
            trail.time = 1.5f;
            trail.minVertexDistance = 0.04f;
            trail.widthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.3f, 1f, 0.65f), 0f), new GradientColorKey(new Color(0.2f, 0.7f, 1f), 0.5f), new GradientColorKey(new Color(0.65f, 0.35f, 1f), 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.Clear();
            return trail;
        }

        public static void DisableAuroraRibbons()
        {
            if (auroraLeft != null)
                UnityEngine.Object.Destroy(auroraLeft.gameObject);
            if (auroraRight != null)
                UnityEngine.Object.Destroy(auroraRight.gameObject);
            auroraLeft = auroraRight = null;
        }

        private static Piece holeLeft, holeRight;
        private static LineRenderer swirlLeft, swirlRight, swirlLeftB, swirlRightB;

        /// <summary>A tiny black hole in each hand, with glowing matter swirling round it.</summary>
        public static void BlackHoleHands()
        {
            float scale = Scale;
            PlaceBlackHole(ref holeLeft, ref swirlLeft, ref swirlLeftB, LeftHand, scale, 0f);
            PlaceBlackHole(ref holeRight, ref swirlRight, ref swirlRightB, RightHand, scale, 1.7f);
        }

        private static void PlaceBlackHole(ref Piece hole, ref LineRenderer swirl, ref LineRenderer swirlB, Vector3 at, float scale, float offset)
        {
            Ensure(ref hole, PrimitiveType.Sphere, Color.black);
            hole.Object.transform.position = at;
            hole.Object.transform.localScale = Vector3.one * (0.08f * scale);
            SetColor(hole, new Color(0.01f, 0f, 0.03f, 1f));

            if (swirl == null) swirl = MakeRing("Eclipse_Swirl");
            if (swirlB == null) swirlB = MakeRing("Eclipse_SwirlB");

            float t = Time.time + offset;
            Vector3 tilt = Quaternion.Euler(Mathf.Sin(t * 1.3f) * 25f, t * 80f, Mathf.Cos(t) * 20f) * Vector3.up;
            PlaceRing(swirl, at, tilt, 0.08f * scale, 0.012f * scale, Accent, t * 200f);
            PlaceRing(swirlB, at, Quaternion.Euler(60f, 0f, 0f) * tilt, 0.105f * scale, 0.008f * scale, WithAlpha(Second, 0.5f), -t * 150f);
        }

        public static void DisableBlackHoleHands()
        {
            DiscardPiece(ref holeLeft);
            DiscardPiece(ref holeRight);
            DestroyLine(ref swirlLeft);
            DestroyLine(ref swirlRight);
            DestroyLine(ref swirlLeftB);
            DestroyLine(ref swirlRightB);
        }

        private static ParticleSystem nebula;

        /// <summary>Soft clouds of coloured nebula gas drifting around you.</summary>
        public static void NebulaCloud()
        {
            if (nebula == null)
            {
                nebula = MakeParticles("Eclipse_Nebula", null, particles =>
                {
                    var main = particles.main;
                    main.loop = true;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
                    main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.4f, 0.85f, 0.12f), new Color(0.35f, 0.4f, 1f, 0.12f));
                    main.maxParticles = 80;

                    var emission = particles.emission;
                    emission.rateOverTime = 10f;

                    var shape = particles.shape;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 4f;

                    var fade = particles.colorOverLifetime;
                    fade.enabled = true;
                    fade.color = new ParticleSystem.MinMaxGradient(FadeInOut());
                });
            }

            Follow(nebula, BodyPosition);
        }

        public static void DisableNebulaCloud() => DestroyParticles(ref nebula);

        private static readonly List<Piece> footprints = new List<Piece>();
        private static Vector3 lastFootprint;

        /// <summary>Little glowing stars left where you step, fading after a few seconds.</summary>
        public static void StarFootprints()
        {
            float scale = Scale;
            if (Extras.Grounded() && (Feet - lastFootprint).sqrMagnitude > 0.45f * 0.45f * scale * scale && footprints.Count < 40)
            {
                if (Physics.Raycast(BodyPosition, Vector3.down, out RaycastHit hit, 1.2f * scale, GTPlayer.Instance.locomotionEnabledLayers))
                {
                    Piece print = MakeStar(Accent, 0.12f);
                    print.Object.transform.position = hit.point + hit.normal * (0.02f * scale);
                    print.Object.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    print.Object.transform.localScale = new Vector3(0.12f, 0.02f, 0.12f) * scale;
                    print.Born = Time.time;
                    footprints.Add(print);
                }

                lastFootprint = Feet;
            }

            for (int i = footprints.Count - 1; i >= 0; i--)
            {
                Piece print = footprints[i];
                float age = (Time.time - print.Born) / 6f;
                if (!print.Alive || age >= 1f)
                {
                    Discard(print);
                    footprints.RemoveAt(i);
                    continue;
                }

                SetAlpha(print, Accent, 1f - age);
            }
        }

        public static void DisableStarFootprints() => ClearPieces(footprints);

        private static ParticleSystem aura;

        /// <summary>Stardust drifting off your body.</summary>
        public static void StarAura()
        {
            if (aura == null)
            {
                aura = MakeParticles("Eclipse_StarAura", null, particles =>
                {
                    var main = particles.main;
                    main.loop = true;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
                    main.maxParticles = 200;

                    var emission = particles.emission;
                    emission.rateOverTime = 30f;

                    var shape = particles.shape;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.35f;

                    var fade = particles.colorOverLifetime;
                    fade.enabled = true;
                    fade.color = new ParticleSystem.MinMaxGradient(FadeInOut());
                });
            }

            var colors = aura.main;
            colors.startColor = new ParticleSystem.MinMaxGradient(Accent, Second);
            Follow(aura, BodyPosition);
        }

        public static void DisableStarAura() => DestroyParticles(ref aura);

        private static ParticleSystem warp;

        /// <summary>Stars streak past you when you are moving fast, like jumping to warp.</summary>
        public static void WarpSpeed()
        {
            if (warp == null)
            {
                warp = MakeParticles("Eclipse_Warp", null, particles =>
                {
                    var main = particles.main;
                    main.loop = true;
                    main.maxParticles = 400;
                    main.scalingMode = ParticleSystemScalingMode.Local;

                    var emission = particles.emission;
                    emission.enabled = false;

                    ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
                    renderer.renderMode = ParticleSystemRenderMode.Stretch;
                    renderer.velocityScale = 0.06f;
                    renderer.lengthScale = 1f;
                });
                warp.transform.localScale = Vector3.one;
            }

            Vector3 velocity = Extras.Body.linearVelocity;
            float speed = velocity.magnitude;
            if (speed < 6f)
                return;

            float scale = Scale;
            Vector3 direction = velocity / speed;
            Vector3 side = Vector3.Cross(direction, Mathf.Abs(direction.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 up = Vector3.Cross(side, direction);

            ParticleSystem.EmitParams streak = new ParticleSystem.EmitParams();
            int count = Mathf.Clamp(Mathf.RoundToInt((speed - 5f) * 0.8f), 1, 12);
            for (int i = 0; i < count; i++)
            {
                Vector2 disc = Random.insideUnitCircle.normalized * Random.Range(1.2f, 4f);
                streak.position = Head.position + direction * (10f * scale) + (side * disc.x + up * disc.y) * scale;
                streak.velocity = -direction * (speed * 2f + 10f);
                streak.startSize = 0.04f * scale;
                streak.startLifetime = 0.6f;
                streak.startColor = Color.Lerp(Color.white, Second, Random.value * 0.5f);
                warp.Emit(streak, 1);
            }
        }

        public static void DisableWarpSpeed() => DestroyParticles(ref warp);

        // ── Toys ────────────────────────────────────────────────────────────────

        private static Piece pocketPlanet;
        private static LineRenderer pocketRing;
        private static Vector3 planetVelocity, lastLeftHand;
        private static bool planetHeld;
        private static float planetThrown = -10f;

        /// <summary>A little planet that floats beside you. Grab it with your left grip and throw it; it comes back.</summary>
        public static void PocketPlanet()
        {
            float scale = Scale;
            Vector3 hand = LeftHand;
            Vector3 handVelocity = (hand - lastLeftHand) / Mathf.Max(Time.deltaTime, 0.001f);
            lastLeftHand = hand;

            Vector3 home = BodyPosition + Head.right * (-0.45f * scale) + Vector3.up * ((0.2f + Mathf.Sin(Time.time * 1.6f) * 0.04f) * scale);

            if (pocketPlanet == null || !pocketPlanet.Alive)
            {
                pocketPlanet = MakePiece(PrimitiveType.Sphere, Second, Vector3.one);
                pocketPlanet.Object.transform.position = home;
                planetHeld = false;
                planetThrown = -10f;
            }

            Transform planet = pocketPlanet.Object.transform;
            planet.localScale = Vector3.one * (0.12f * scale);

            if (planetHeld)
            {
                planet.position = hand;
                if (!leftGrab)
                {
                    planetHeld = false;
                    planetThrown = Time.time;
                    planetVelocity = Vector3.ClampMagnitude(handVelocity, 25f);
                }
            }
            else if (leftGrab && (planet.position - hand).sqrMagnitude < 0.25f * 0.25f * scale * scale)
                planetHeld = true;
            else if (Time.time - planetThrown < 3f)
            {
                planetVelocity *= 1f - Mathf.Clamp01(0.5f * Time.deltaTime);
                planet.position += planetVelocity * Time.deltaTime;
            }
            else
                planet.position = Vector3.Lerp(planet.position, home, 1f - Mathf.Exp(-4f * Time.deltaTime));

            SetColor(pocketPlanet, Second);

            if (pocketRing == null)
                pocketRing = MakeRing("Eclipse_PocketRing");
            PlaceRing(pocketRing, planet.position, Quaternion.Euler(25f, 0f, 0f) * Vector3.up, 0.11f * scale, 0.015f * scale, Accent, Time.time * 60f);
        }

        public static void DisablePocketPlanet()
        {
            DiscardPiece(ref pocketPlanet);
            DestroyLine(ref pocketRing);
            planetHeld = false;
        }

        private static readonly Piece[] asteroids = new Piece[3];

        /// <summary>Three asteroids juggled back and forth between your hands.</summary>
        public static void AsteroidJuggle()
        {
            float scale = Scale;
            const float period = 1.3f;
            for (int i = 0; i < asteroids.Length; i++)
            {
                Piece rock = Ensure(ref asteroids[i], PrimitiveType.Cube, Color.grey);
                float cycle = Time.time / period + i / (float)asteroids.Length;
                float t = cycle % 1f;
                bool leftToRight = Mathf.FloorToInt(cycle) % 2 == 0;

                Vector3 from = leftToRight ? LeftHand : RightHand;
                Vector3 to = leftToRight ? RightHand : LeftHand;
                rock.Object.transform.position = Vector3.Lerp(from, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.45f * scale);
                rock.Object.transform.rotation = Quaternion.Euler(Time.time * 140f + i * 50f, Time.time * 90f, i * 30f);
                rock.Object.transform.localScale = new Vector3(0.06f, 0.05f, 0.055f) * scale;
                SetColor(rock, Color.Lerp(new Color(0.45f, 0.42f, 0.5f), Accent, 0.25f));
            }
        }

        public static void DisableAsteroidJuggle()
        {
            for (int i = 0; i < asteroids.Length; i++)
                DiscardPiece(ref asteroids[i]);
        }

        private static readonly List<Piece> rockets = new List<Piece>();
        private static bool rocketHeld = true;

        /// <summary>X launches a little firework rocket from your left hand.</summary>
        public static void RocketLaunch()
        {
            float scale = Scale;
            if (Pressed(leftPrimary, ref rocketHeld) && rockets.Count < 5)
            {
                var hand = ControllerUtilities.GetTrueLeftHand();
                Piece rocket = MakePiece(PrimitiveType.Capsule, Accent, new Vector3(0.04f, 0.07f, 0.04f) * scale);
                rocket.Object.transform.position = hand.position;
                rocket.Velocity = (hand.forward + Vector3.up).normalized * 3f;
                rocket.Born = Time.time;
                rockets.Add(rocket);
            }

            for (int i = rockets.Count - 1; i >= 0; i--)
            {
                Piece rocket = rockets[i];
                if (!rocket.Alive)
                {
                    rockets.RemoveAt(i);
                    continue;
                }

                Transform body = rocket.Object.transform;
                if (Time.time - rocket.Born > 1.6f)
                {
                    Burst(body.position, 60, 5f, true);
                    Discard(rocket);
                    rockets.RemoveAt(i);
                    continue;
                }

                rocket.Velocity += (rocket.Velocity.normalized * 14f + Vector3.up * 4f) * Time.deltaTime;
                body.position += rocket.Velocity * (Time.deltaTime * scale);
                body.rotation = Quaternion.FromToRotation(Vector3.up, rocket.Velocity);
                if (Random.value < 0.6f)
                    Burst(body.position, 1, 0.5f, true, -rocket.Velocity);
                SetColor(rocket, Accent);
            }
        }

        public static void DisableRocketLaunch() => ClearPieces(rockets);

        private sealed class Wormhole
        {
            public Piece Core;
            public LineRenderer Rim;
            public LineRenderer Glow;
            public Vector3 Centre;
            public Vector3 Facing;
        }

        private static Wormhole wormholeA, wormholeB;
        private static bool wormholeHeld = true;
        private static float wormholeReady;

        /// <summary>
        /// Click your right stick to open a wormhole in front of you, and again for its partner.
        /// Walk into either one to come out of the other. A third click starts over.
        /// </summary>
        public static void WormholePair()
        {
            float scale = Scale;
            if (Pressed(rightJoystickClick, ref wormholeHeld))
            {
                Vector3 facing = LookFlat();
                Vector3 centre = BodyPosition + facing * (1.5f * scale);

                if (wormholeA != null && wormholeB != null)
                {
                    RemoveWormhole(ref wormholeA);
                    RemoveWormhole(ref wormholeB);
                }

                Wormhole made = new Wormhole
                {
                    Core = MakePiece(PrimitiveType.Sphere, Color.black, Vector3.one),
                    Rim = MakeRing("Eclipse_WormholeRim"),
                    Glow = MakeRing("Eclipse_WormholeGlow"),
                    Centre = centre,
                    Facing = facing
                };

                if (wormholeA == null)
                    wormholeA = made;
                else
                    wormholeB = made;

                wormholeReady = Time.time + 0.75f;
            }

            DrawWormhole(wormholeA, scale, 0f);
            DrawWormhole(wormholeB, scale, 1f);

            if (wormholeA == null || wormholeB == null || Time.time < wormholeReady)
                return;

            float reach = 0.55f * scale;
            Vector3 body = BodyPosition;
            Wormhole exit = (body - wormholeA.Centre).sqrMagnitude < reach * reach ? wormholeB
                : (body - wormholeB.Centre).sqrMagnitude < reach * reach ? wormholeA
                : null;

            if (exit == null)
                return;

            // Come out a step in front of the other one, so you don't land straight back in
            TeleportPlayer(exit.Centre + exit.Facing * (1f * scale), true);
            Eclipse.Menu.Main.EclipseUnlock("Wormhole Traveller");
            Burst(exit.Centre, 40, 3f, false);
            wormholeReady = Time.time + 1.5f;
        }

        private static void DrawWormhole(Wormhole wormhole, float scale, float offset)
        {
            if (wormhole == null || !wormhole.Core.Alive || wormhole.Rim == null || wormhole.Glow == null)
                return;

            float t = Time.time + offset;
            Transform core = wormhole.Core.Object.transform;
            core.position = wormhole.Centre;
            core.rotation = Quaternion.LookRotation(wormhole.Facing);
            core.localScale = new Vector3(1.05f, 1.05f, 0.05f) * scale;
            SetColor(wormhole.Core, new Color(0.02f, 0f, 0.06f, 1f));

            PlaceRing(wormhole.Rim, wormhole.Centre, wormhole.Facing, 0.55f * scale, 0.05f * scale, Accent, t * 90f);
            PlaceRing(wormhole.Glow, wormhole.Centre, wormhole.Facing, (0.62f + Mathf.Sin(t * 3f) * 0.03f) * scale, 0.09f * scale, WithAlpha(Second, 0.35f));
        }

        private static void RemoveWormhole(ref Wormhole wormhole)
        {
            if (wormhole == null)
                return;

            Discard(wormhole.Core);
            DestroyLine(ref wormhole.Rim);
            DestroyLine(ref wormhole.Glow);
            wormhole = null;
        }

        public static void DisableWormholePair()
        {
            RemoveWormhole(ref wormholeA);
            RemoveWormhole(ref wormholeB);
        }

        private static readonly List<Piece> fallingStars = new List<Piece>();
        private static float nextFallingStar;
        private static int catcherScore, catcherShown = -1;
        private static TextMeshPro catcherText;

        /// <summary>Stars drift down around you. Touch them with your hands to catch them; your score is on your right wrist.</summary>
        public static void StarCatcher()
        {
            float scale = Scale;
            if (Time.time > nextFallingStar && fallingStars.Count < 6)
            {
                nextFallingStar = Time.time + 1.2f;
                Vector2 spot = Random.insideUnitCircle * 1.3f;
                Piece star = MakeStar(Color.white, 0.09f);
                star.Object.transform.position = BodyPosition + new Vector3(spot.x, 1.6f, spot.y) * scale;
                star.Born = Time.time;
                fallingStars.Add(star);
            }

            float reach = 0.16f * scale;
            for (int i = fallingStars.Count - 1; i >= 0; i--)
            {
                Piece star = fallingStars[i];
                if (!star.Alive || Time.time - star.Born > 8f)
                {
                    Discard(star);
                    fallingStars.RemoveAt(i);
                    continue;
                }

                Transform body = star.Object.transform;
                body.position += Vector3.down * (0.35f * scale * Time.deltaTime);
                body.rotation = Quaternion.Euler(0f, Time.time * 120f + star.Seed, 20f);
                body.localScale = Vector3.one * (0.09f * scale);
                SetColor(star, Color.Lerp(Accent, Color.white, Mathf.PingPong(Time.time * 2f + star.Seed, 1f)));

                if ((body.position - LeftHand).sqrMagnitude < reach * reach || (body.position - RightHand).sqrMagnitude < reach * reach)
                {
                    Burst(body.position, 25, 2f, false);
                    VRRig.LocalRig.PlayHandTapLocal(50, false, 0.25f);
                    if (++catcherScore == 25)
                        Eclipse.Menu.Main.EclipseUnlock("Star Catcher");
                    Discard(star);
                    fallingStars.RemoveAt(i);
                }
            }

            if (catcherText == null)
            {
                catcherText = MakeWristText("Eclipse_StarCatcher", Color.white);
                catcherShown = -1;
            }

            PlaceOnWrist(catcherText, 0.3f, true);
            if (catcherShown != catcherScore)
            {
                catcherShown = catcherScore;
                catcherText.text = catcherScore == 1 ? "1 star" : $"{catcherScore} stars";
                catcherText.color = Accent;
            }
        }

        public static void DisableStarCatcher()
        {
            ClearPieces(fallingStars);
            DestroyText(ref catcherText);
        }
    }
}
