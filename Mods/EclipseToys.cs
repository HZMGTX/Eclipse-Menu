/*
 * Eclipse Menu  Mods/EclipseToys.cs
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
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Eclipse.Menu.Main;
using static Eclipse.Mods.Custom;
using Random = UnityEngine.Random;

namespace Eclipse.Mods
{
    /// <summary>The third part of the Eclipse mods: the Space Toys, little things to play with that only you can see.</summary>
    public static partial class EclipseMods
    {
        // ── Space Toys ──────────────────────────────────────────────────────────

        /// <summary>Forgets held buttons and where your hands were, run whenever one of the Space Toys with a button is turned on.</summary>
        /// <remarks>A button already held while turning a toy on would otherwise count as a press.</remarks>
        public static void ResetToys()
        {
            builderHeld = planeHeld = diceHeld = glowHeld = meteorHeld = true;
            planeLastHand = ControllerUtilities.GetTrueRightHand().position;
            glowLastHand = ControllerUtilities.GetTrueLeftHand().position;
        }

        private static Piece solarSun, solarCorona;
        private static readonly Piece[] solarPlanets = new Piece[5];
        private static readonly LineRenderer[] solarOrbits = new LineRenderer[5];
        private static LineRenderer solarRing;

        // Each planet's orbit, speed, size and colour, innermost first; the fourth one wears a ring
        private static readonly float[] SolarRadii = { 0.06f, 0.085f, 0.11f, 0.14f, 0.17f };
        private static readonly float[] SolarSpeeds = { 2.6f, 1.9f, 1.4f, 0.95f, 0.65f };
        private static readonly float[] SolarSizes = { 0.012f, 0.017f, 0.019f, 0.026f, 0.016f };
        private static readonly Color[] SolarColors =
        {
            new Color(0.72f, 0.68f, 0.64f), new Color(0.96f, 0.78f, 0.48f), new Color(0.3f, 0.62f, 1f),
            new Color(0.92f, 0.8f, 0.55f), new Color(0.42f, 0.55f, 1f)
        };
        private const int SolarRinged = 3;

        /// <summary>A tiny sun with five planets, one of them ringed, circling at different speeds just above your left palm.</summary>
        public static void MiniSolarSystem()
        {
            float scale = Scale;
            float t = Time.time;
            Vector3 centre = LeftHand + Vector3.up * (0.16f * scale);
            Quaternion tilt = Quaternion.Euler(14f, t * 8f, 6f);
            Vector3 normal = tilt * Vector3.up;

            Ensure(ref solarSun, PrimitiveType.Sphere, Color.white);
            solarSun.Object.transform.position = centre;
            solarSun.Object.transform.localScale = Vector3.one * (0.045f * scale);
            SetColor(solarSun, Color.Lerp(Accent, Color.white, 0.25f));

            Ensure(ref solarCorona, PrimitiveType.Sphere, Color.white);
            solarCorona.Object.transform.position = centre;
            solarCorona.Object.transform.localScale = Vector3.one * (0.07f * scale * (1f + Mathf.Sin(t * 3f) * 0.08f));
            SetAlpha(solarCorona, Accent, 0.25f);

            for (int i = 0; i < solarPlanets.Length; i++)
            {
                float radius = SolarRadii[i] * scale;
                float angle = t * SolarSpeeds[i] + i * 1.3f;

                Piece planet = Ensure(ref solarPlanets[i], PrimitiveType.Sphere, Color.white);
                planet.Object.transform.position = centre + tilt * new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                planet.Object.transform.localScale = Vector3.one * (SolarSizes[i] * scale);
                SetColor(planet, Color.Lerp(SolarColors[i], Second, 0.25f));

                if (solarOrbits[i] == null)
                    solarOrbits[i] = MakeRing("Eclipse_SolarOrbit");
                PlaceRing(solarOrbits[i], centre, normal, radius, 0.0015f * scale, WithAlpha(Second, 0.2f));
            }

            if (solarRing == null)
                solarRing = MakeRing("Eclipse_SolarRing");
            Vector3 ringed = solarPlanets[SolarRinged].Object.transform.position;
            PlaceRing(solarRing, ringed, tilt * Quaternion.Euler(25f, 0f, 0f) * Vector3.up, SolarSizes[SolarRinged] * 0.95f * scale, 0.004f * scale, WithAlpha(Accent, 0.85f), t * 30f);
        }

        public static void DisableMiniSolarSystem()
        {
            DiscardPiece(ref solarSun);
            DiscardPiece(ref solarCorona);
            for (int i = 0; i < solarPlanets.Length; i++)
            {
                DiscardPiece(ref solarPlanets[i]);
                DestroyLine(ref solarOrbits[i]);
            }

            DestroyLine(ref solarRing);
        }

        private sealed class BuiltPlanet
        {
            public Piece Core;
            public Piece Glow;
            public Vector3 From;
            public float Launched;
            public float Size;
            public float Radius;
            public float Height;
            public float Angle;
            public float Speed;
            public float Tint;
        }

        private const int MaxBuiltPlanets = 3;
        private static readonly List<BuiltPlanet> builtPlanets = new List<BuiltPlanet>();
        private static BuiltPlanet growingPlanet;
        private static bool builderHeld = true;

        /// <summary>Hold both grips and pull your hands apart to grow a glowing planet between them; let go and it orbits you, up to three at once.</summary>
        public static void PlanetBuilder()
        {
            float now = Time.time;
            float dt = Time.deltaTime;
            float scale = Scale;
            Vector3 left = LeftHand;
            Vector3 right = RightHand;
            bool both = leftGrab && rightGrab;

            if (Pressed(both, ref builderHeld) && growingPlanet == null)
            {
                growingPlanet = new BuiltPlanet
                {
                    Core = MakePiece(PrimitiveType.Sphere, Accent, Vector3.one),
                    Glow = MakePiece(PrimitiveType.Sphere, Accent, Vector3.one),
                    Tint = Random.value
                };
                Burst((left + right) * 0.5f, 12, 1f, false);
            }

            if (growingPlanet != null)
            {
                if (!growingPlanet.Core.Alive || !growingPlanet.Glow.Alive)
                {
                    RemoveBuiltPlanet(growingPlanet);
                    growingPlanet = null;
                }
                else if (both)
                {
                    // The planet fills the space between your hands
                    float size = Mathf.Clamp(Vector3.Distance(left, right) / scale * 0.8f, 0.04f, 1.2f);
                    growingPlanet.Size = Mathf.Lerp(growingPlanet.Size, size, 1f - Mathf.Exp(-12f * dt));
                    DrawBuiltPlanet(growingPlanet, (left + right) * 0.5f, now, scale);
                }
                else
                    LaunchBuiltPlanet(now, scale);
            }

            for (int i = builtPlanets.Count - 1; i >= 0; i--)
            {
                BuiltPlanet planet = builtPlanets[i];
                if (!planet.Core.Alive || !planet.Glow.Alive)
                {
                    RemoveBuiltPlanet(planet);
                    builtPlanets.RemoveAt(i);
                    continue;
                }

                planet.Angle += planet.Speed * dt;
                Vector3 orbit = BodyPosition + new Vector3(Mathf.Cos(planet.Angle) * planet.Radius, planet.Height, Mathf.Sin(planet.Angle) * planet.Radius) * scale;

                // Eases from where you let it go onto its orbit over the first second or so
                float settle = Mathf.SmoothStep(0f, 1f, (now - planet.Launched) / 1.2f);
                DrawBuiltPlanet(planet, Vector3.Lerp(planet.From, orbit, settle), now, scale);
            }
        }

        /// <summary>Sends the planet in your hands into an orbit round you from where it was let go.</summary>
        private static void LaunchBuiltPlanet(float now, float scale)
        {
            BuiltPlanet planet = growingPlanet;
            growingPlanet = null;

            Vector3 at = planet.Core.Object.transform.position;
            Vector3 offset = (at - BodyPosition) / scale;
            float flat = new Vector2(offset.x, offset.z).magnitude;

            planet.From = at;
            planet.Launched = now;
            planet.Radius = Mathf.Max(flat, 0.55f + planet.Size * 0.6f);
            planet.Height = Mathf.Clamp(offset.y, -0.2f, 1.2f);
            planet.Angle = flat > 0.01f ? Mathf.Atan2(offset.z, offset.x) : Random.value * Mathf.PI * 2f;

            // Wider orbits go round more slowly
            planet.Speed = (Random.value < 0.5f ? -0.8f : 0.8f) / planet.Radius;

            if (builtPlanets.Count >= MaxBuiltPlanets)
            {
                BuiltPlanet oldest = builtPlanets[0];
                if (oldest.Core.Alive)
                    Burst(oldest.Core.Object.transform.position, 30, 2f, false);
                RemoveBuiltPlanet(oldest);
                builtPlanets.RemoveAt(0);
            }

            builtPlanets.Add(planet);
            Eclipse.Menu.Main.EclipseUnlock("Planet Maker");
        }

        private static void DrawBuiltPlanet(BuiltPlanet planet, Vector3 at, float now, float scale)
        {
            Color color = Color.Lerp(Accent, Second, planet.Tint);
            float size = planet.Size * scale;

            planet.Core.Object.transform.position = at;
            planet.Core.Object.transform.localScale = Vector3.one * size;
            SetColor(planet.Core, Color.Lerp(color, Color.white, 0.15f));

            planet.Glow.Object.transform.position = at;
            planet.Glow.Object.transform.localScale = Vector3.one * (size * (1.3f + Mathf.Sin(now * 2.5f + planet.Tint * 6f) * 0.05f));
            SetAlpha(planet.Glow, color, 0.25f);
        }

        private static void RemoveBuiltPlanet(BuiltPlanet planet)
        {
            Discard(planet.Core);
            Discard(planet.Glow);
        }

        public static void DisablePlanetBuilder()
        {
            if (growingPlanet != null)
                RemoveBuiltPlanet(growingPlanet);
            growingPlanet = null;

            foreach (BuiltPlanet planet in builtPlanets)
                RemoveBuiltPlanet(planet);
            builtPlanets.Clear();
        }

        private static Piece kite;
        private static LineRenderer kiteString, kiteTail, kiteFrame;
        private static Vector3 kiteVelocity, kiteLastHand;
        private const int KiteStringPoints = 16;
        private const int KiteTailPoints = 12;
        private static readonly Vector3[] kiteStringBuffer = new Vector3[KiteStringPoints];
        private static readonly Vector3[] kiteTailBuffer = new Vector3[KiteTailPoints];
        private static readonly Vector3[] kiteFrameBuffer = new Vector3[4];

        // The kite's top, right, bottom and left corners, in its own space
        private static readonly Vector3[] KiteCorners =
        {
            new Vector3(0f, 0.42f, 0f), new Vector3(0.3f, 0.12f, 0f), new Vector3(0f, -0.58f, 0f), new Vector3(-0.3f, 0.12f, 0f)
        };

        /// <summary>A diamond kite on a string from your right hand, flying above and behind you and swaying as you move.</summary>
        public static void Kite()
        {
            float now = Time.time;
            float dt = Mathf.Max(Time.deltaTime, 0.001f);
            float scale = Scale;
            Vector3 hand = ControllerUtilities.GetTrueRightHand().position;
            Vector3 behind = -LookFlat();
            Vector3 side = Vector3.Cross(Vector3.up, behind);

            if (kite == null || !kite.Alive)
            {
                kite = MakeShaped(KiteMesh, Accent);
                kite.Object.transform.position = hand + (behind * 1.2f + Vector3.up * 1.8f) * scale;
                kiteVelocity = Vector3.zero;
                kiteLastHand = hand;
            }

            Transform body = kite.Object.transform;
            Vector3 handVelocity = Vector3.ClampMagnitude((hand - kiteLastHand) / dt, 8f * scale);
            kiteLastHand = hand;

            // The wind always blows from where you look, so the kite rides up behind you. Moving
            // your hand drags it the other way, and gusts make it wander from side to side.
            Vector3 target = hand
                + (behind * 1.3f + Vector3.up * 2.1f) * scale
                + side * (Mathf.Sin(now * 0.7f) * 0.5f * scale)
                + Vector3.up * (Mathf.Sin(now * 1.3f) * 0.2f * scale)
                - handVelocity * 0.3f;

            kiteVelocity += ((target - body.position) * 6f - kiteVelocity * 2.2f) * dt;
            Vector3 position = body.position + kiteVelocity * dt;

            // The string never stretches
            float reach = 3f * scale;
            Vector3 fromHand = position - hand;
            float length = fromHand.magnitude;
            if (length > reach)
            {
                Vector3 along = fromHand / length;
                position = hand + along * reach;
                float outward = Vector3.Dot(kiteVelocity, along);
                if (outward > 0f)
                    kiteVelocity -= along * outward;
                length = reach;
            }

            body.position = position;

            // Faces you, leaning into its sideways drift
            Vector3 facing = hand - position;
            float lean = Mathf.Clamp(Vector3.Dot(kiteVelocity, side) / scale * -15f, -40f, 40f) + Mathf.Sin(now * 2.3f) * 6f;
            if (facing.sqrMagnitude > 0.0001f)
                body.rotation = Quaternion.LookRotation(facing, Vector3.up) * Quaternion.Euler(0f, 0f, lean);
            body.localScale = Vector3.one * (0.6f * scale);
            SetAlpha(kite, Accent, 0.9f);

            if (kiteFrame == null)
            {
                kiteFrame = MakeLine("Eclipse_KiteFrame", 0.01f);
                kiteFrame.positionCount = KiteCorners.Length;
                kiteFrame.loop = true;
            }

            for (int i = 0; i < KiteCorners.Length; i++)
                kiteFrameBuffer[i] = body.TransformPoint(KiteCorners[i]);
            kiteFrame.SetPositions(kiteFrameBuffer);
            kiteFrame.startWidth = kiteFrame.endWidth = 0.01f * scale;
            kiteFrame.startColor = kiteFrame.endColor = Second;

            if (kiteString == null)
            {
                kiteString = MakeLine("Eclipse_KiteString", 0.003f);
                kiteString.positionCount = KiteStringPoints;
            }

            // A slack string sags in the middle; a taut one is nearly straight
            float sag = Mathf.Lerp(0.4f, 0.04f, length / reach) * scale;
            for (int i = 0; i < KiteStringPoints; i++)
            {
                float k = i / (float)(KiteStringPoints - 1);
                kiteStringBuffer[i] = Vector3.Lerp(hand, position, k) + Vector3.down * (Mathf.Sin(k * Mathf.PI) * sag);
            }

            kiteString.SetPositions(kiteStringBuffer);
            kiteString.startWidth = kiteString.endWidth = 0.003f * scale;
            kiteString.startColor = kiteString.endColor = new Color(1f, 1f, 1f, 0.6f);

            if (kiteTail == null)
            {
                kiteTail = MakeLine("Eclipse_KiteTail", 0.02f);
                kiteTail.positionCount = KiteTailPoints;
                kiteTail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);
            }

            Vector3 tailStart = kiteFrameBuffer[2];
            Vector3 trail = (Vector3.down * 0.8f + behind * 0.35f) * scale - kiteVelocity * 0.15f;
            for (int i = 0; i < KiteTailPoints; i++)
            {
                float k = i / (float)(KiteTailPoints - 1);
                kiteTailBuffer[i] = tailStart + trail * k + body.right * (Mathf.Sin(now * 5f - k * 6f) * 0.08f * k * scale);
            }

            kiteTail.SetPositions(kiteTailBuffer);
            kiteTail.widthMultiplier = 0.025f * scale;
            kiteTail.startColor = Accent;
            kiteTail.endColor = WithAlpha(Second, 0.3f);
        }

        public static void DisableKite()
        {
            DiscardPiece(ref kite);
            DestroyLine(ref kiteString);
            DestroyLine(ref kiteTail);
            DestroyLine(ref kiteFrame);
        }

        private static Piece yoyo, yoyoStripe;
        private static LineRenderer yoyoString;
        private static Vector3 yoyoDirection = Vector3.down, yoyoLastHand;
        private static float yoyoLength, yoyoSpeed, yoyoSpin, yoyoSpinSpeed;

        /// <summary>Hold your right grip to drop a spinning yo-yo on a string from your right hand; let go and it zips back up.</summary>
        public static void Yoyo()
        {
            float dt = Time.deltaTime;
            float scale = Scale;
            Vector3 hand = ControllerUtilities.GetTrueRightHand().position;

            if (yoyo == null || !yoyo.Alive)
            {
                yoyo = MakePiece(PrimitiveType.Cylinder, Accent, Vector3.one);
                yoyoLength = yoyoSpeed = yoyoSpinSpeed = 0f;
                yoyoDirection = Vector3.down;
                yoyoLastHand = hand;
            }

            if (yoyoStripe == null || !yoyoStripe.Alive)
                yoyoStripe = MakeDecal(YoyoStripeMesh, Second);

            Vector3 handVelocity = (hand - yoyoLastHand) / Mathf.Max(dt, 0.001f);
            yoyoLastHand = hand;

            float reach = 0.75f * scale;
            if (rightGrab)
            {
                // Falls to the end of the string and bounces there a couple of times before it sleeps
                yoyoSpeed += 7f * scale * dt;
                yoyoLength += yoyoSpeed * dt;
                if (yoyoLength >= reach)
                {
                    yoyoLength = reach;
                    yoyoSpeed = yoyoSpeed > 0.6f * scale ? -yoyoSpeed * 0.35f : 0f;
                }
            }
            else
            {
                // Climbs back up the string, faster and faster
                yoyoSpeed = Mathf.Min(yoyoSpeed, 0f) - 14f * scale * dt;
                yoyoLength += yoyoSpeed * dt;
            }

            // Back in your hand
            if (yoyoLength <= 0f)
                yoyoLength = yoyoSpeed = 0f;

            // The string swings back behind your hand as it moves, and settles to hang straight down
            Vector3 hang = Vector3.down * 3f - Vector3.ClampMagnitude(handVelocity / scale, 4f) * 0.6f;
            yoyoDirection = Vector3.Slerp(yoyoDirection, hang.normalized, 1f - Mathf.Exp(-6f * dt));

            yoyoSpinSpeed = Mathf.MoveTowards(yoyoSpinSpeed, yoyoLength > 0.01f * scale ? 1500f : 0f, 3000f * dt);
            yoyoSpin = (yoyoSpin + yoyoSpinSpeed * dt) % 360f;

            Vector3 centre = hand + yoyoDirection * (yoyoLength + 0.055f * scale);

            // Turned face-on to you so you can see it spin
            Vector3 axle = Vector3.ProjectOnPlane(Head.position - centre, yoyoDirection);
            if (axle.sqrMagnitude < 0.0001f)
                axle = Vector3.ProjectOnPlane(Head.forward, yoyoDirection);
            Quaternion turn = Quaternion.FromToRotation(Vector3.up, axle.normalized) * Quaternion.Euler(0f, yoyoSpin, 0f);
            Vector3 size = new Vector3(0.07f, 0.014f, 0.07f) * scale;

            yoyo.Object.transform.SetPositionAndRotation(centre, turn);
            yoyo.Object.transform.localScale = size;
            SetColor(yoyo, Accent);

            yoyoStripe.Object.transform.SetPositionAndRotation(centre, turn);
            yoyoStripe.Object.transform.localScale = size;
            SetColor(yoyoStripe, Second);

            if (yoyoString == null)
                yoyoString = MakeLine("Eclipse_YoyoString", 0.003f);

            yoyoString.enabled = yoyoLength > 0.005f * scale;
            yoyoString.SetPosition(0, hand);
            yoyoString.SetPosition(1, centre);
            yoyoString.startWidth = yoyoString.endWidth = 0.003f * scale;
            yoyoString.startColor = yoyoString.endColor = new Color(1f, 1f, 1f, 0.7f);
        }

        public static void DisableYoyo()
        {
            DiscardPiece(ref yoyo);
            DiscardPiece(ref yoyoStripe);
            DestroyLine(ref yoyoString);
            yoyoLength = yoyoSpeed = yoyoSpinSpeed = 0f;
        }

        private const int MaxPaperPlanes = 6;
        private static readonly List<Piece> paperPlanes = new List<Piece>();
        private static Vector3 planeLastHand;
        private static bool planeHeld = true;

        /// <summary>Right trigger throws a paper plane the way your hand is moving; it glides slowly down and fades after it lands.</summary>
        public static void PaperPlanes()
        {
            float now = Time.time;
            float dt = Time.deltaTime;
            float scale = Scale;
            var hand = ControllerUtilities.GetTrueRightHand();
            Vector3 handVelocity = (hand.position - planeLastHand) / Mathf.Max(dt, 0.001f);
            planeLastHand = hand.position;

            if (Pressed(rightTrigger > 0.5f, ref planeHeld))
            {
                // The oldest plane is folded again for the newest throw
                Piece plane = null;
                if (paperPlanes.Count >= MaxPaperPlanes)
                {
                    plane = paperPlanes[0];
                    paperPlanes.RemoveAt(0);
                }

                if (plane == null || !plane.Alive)
                    plane = MakeShaped(PaperPlaneMesh, Color.white);

                float speed = handVelocity.magnitude;
                Vector3 heading = speed > 0.8f * scale ? handVelocity / speed : hand.forward;
                Vector3 flat = new Vector3(heading.x, 0f, heading.z);

                plane.Velocity = heading * Mathf.Clamp(speed * 1.3f, 2.5f * scale, 9f * scale);
                plane.Anchor = flat.sqrMagnitude > 0.0001f ? flat.normalized : LookFlat();
                plane.Born = now;
                plane.Life = 0f;
                plane.Object.transform.position = hand.position + heading * (0.12f * scale);
                paperPlanes.Add(plane);
            }

            int layers = GTPlayer.Instance.locomotionEnabledLayers;
            Color paper = Color.Lerp(Color.white, Accent, 0.3f);

            // Anchor is the way a plane is heading and Life the moment it landed, nought while it flies
            for (int i = paperPlanes.Count - 1; i >= 0; i--)
            {
                Piece plane = paperPlanes[i];
                if (!plane.Alive)
                {
                    paperPlanes.RemoveAt(i);
                    continue;
                }

                Transform body = plane.Object.transform;
                body.localScale = Vector3.one * (0.22f * scale);

                if (plane.Life <= 0f)
                {
                    if (now - plane.Born > 20f)
                        plane.Life = now;
                    else
                        GlidePaperPlane(plane, body, now, dt, scale, layers);
                }

                float alpha = 1f;
                if (plane.Life > 0f)
                {
                    alpha = 1f - (now - plane.Life - 2.5f) / 1.5f;
                    if (alpha <= 0f)
                    {
                        Discard(plane);
                        paperPlanes.RemoveAt(i);
                        continue;
                    }
                }

                SetAlpha(plane, paper, Mathf.Min(alpha, 1f));
            }
        }

        private static void GlidePaperPlane(Piece plane, Transform body, float now, float dt, float scale, int layers)
        {
            float age = now - plane.Born;
            Vector3 velocity = plane.Velocity;
            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            float flatSpeed = flat.magnitude;
            if (flatSpeed > 0.01f)
                plane.Anchor = flat / flatSpeed;

            // However hard it is thrown, it settles into a slow glide that sinks and lifts gently and drifts off course
            plane.Anchor = Quaternion.Euler(0f, Mathf.Sin(age * 0.9f + plane.Seed) * 30f * dt, 0f) * plane.Anchor;
            flatSpeed = Mathf.MoveTowards(flatSpeed, 2f * scale, 1.5f * scale * dt);
            velocity.y = Mathf.MoveTowards(velocity.y, (-0.5f + Mathf.Sin(age * 2.4f + plane.Seed) * 0.3f) * scale, 5f * scale * dt);
            velocity = plane.Anchor * flatSpeed + Vector3.up * velocity.y;
            plane.Velocity = velocity;

            Vector3 move = velocity * dt;
            float distance = move.magnitude;
            if (distance > 0f && Physics.Raycast(body.position, move / distance, out RaycastHit hit, distance + 0.02f * scale, layers))
            {
                if (hit.normal.y < 0.5f)
                {
                    // Bounces off walls and glides on
                    body.position = hit.point + hit.normal * (0.03f * scale);
                    plane.Velocity = Vector3.Reflect(velocity, hit.normal) * 0.4f;
                    return;
                }

                body.position = hit.point + hit.normal * (0.01f * scale);
                Vector3 along = Vector3.ProjectOnPlane(plane.Anchor, hit.normal);
                if (along.sqrMagnitude > 0.0001f)
                    body.rotation = Quaternion.LookRotation(along, hit.normal);
                plane.Life = now;
                return;
            }

            body.position += move;
            if (velocity.sqrMagnitude > 0.0001f)
                body.rotation = Quaternion.LookRotation(velocity) * Quaternion.Euler(0f, 0f, Mathf.Sin(age * 2f + plane.Seed) * 15f);
        }

        public static void DisablePaperPlanes() => ClearPieces(paperPlanes);

        private static readonly string[] EightBallAnswers =
        {
            "It is\ncertain", "It is\ndecidedly so", "Without\na doubt", "Yes,\ndefinitely", "You may\nrely on it",
            "As I see it,\nyes", "Most\nlikely", "Outlook\ngood", "Yes", "Signs point\nto yes",
            "Reply hazy,\ntry again", "Ask again\nlater", "Better not\ntell you now", "Cannot\npredict now", "Concentrate\nand ask again",
            "Don't count\non it", "My reply\nis no", "My sources\nsay no", "Outlook not\nso good", "Very\ndoubtful"
        };

        private static Piece eightBall, eightWindow;
        private static TextMeshPro eightText;
        private static Vector3 eightLastHand, eightSwing;
        private static int eightShakes, eightAnswer = -1;
        private static float eightLastShake, eightAsked = -10f;

        /// <summary>Shake your left hand and a magic 8-ball gives a random answer that floats above your hand for a few seconds.</summary>
        public static void MagicEightBall()
        {
            float now = Time.time;
            float scale = Scale;
            Vector3 hand = LeftHand;

            // Measured against your body, so running about doesn't count as shaking
            Vector3 relative = hand - BodyPosition;
            if (eightBall == null || !eightBall.Alive)
            {
                eightBall = MakePiece(PrimitiveType.Sphere, Color.black, Vector3.one);
                eightLastHand = relative;
                eightSwing = Vector3.zero;
                eightShakes = 0;
            }

            Ensure(ref eightWindow, PrimitiveType.Cylinder, Color.white);

            // A shake is the hand turning back on itself at speed a few times in a row
            Vector3 velocity = (relative - eightLastHand) / Mathf.Max(Time.deltaTime, 0.001f);
            eightLastHand = relative;
            if (velocity.sqrMagnitude > 1.4f * 1.4f * scale * scale)
            {
                if (Vector3.Dot(velocity, eightSwing) < 0f)
                {
                    eightShakes++;
                    eightLastShake = now;
                }

                eightSwing = velocity;
            }

            if (now - eightLastShake > 0.5f)
                eightShakes = 0;

            if (eightShakes >= 3 && now - eightAsked > 1.2f)
                AskEightBall(now);

            Transform ball = eightBall.Object.transform;
            ball.position = hand + Vector3.up * (0.07f * scale);
            ball.localScale = Vector3.one * (0.09f * scale);
            SetColor(eightBall, new Color(0.04f, 0.03f, 0.07f, 1f));

            // The answer window always turns to face you, and lights up with a new answer
            Vector3 toHead = Head.position - ball.position;
            Vector3 facing = toHead.sqrMagnitude > 0.0001f ? toHead.normalized : Vector3.up;
            float age = now - eightAsked;
            eightWindow.Object.transform.SetPositionAndRotation(ball.position + facing * (0.044f * scale), Quaternion.FromToRotation(Vector3.up, facing));
            eightWindow.Object.transform.localScale = new Vector3(0.045f, 0.002f, 0.045f) * scale;
            SetColor(eightWindow, Color.Lerp(new Color(0.12f, 0.16f, 0.6f), Accent, Mathf.Exp(-age * 1.5f)));

            if (eightText == null)
                return;

            bool shown = age < 4f;
            ShowText(eightText, shown);
            if (!shown)
                return;

            PlaceToyText(eightText, hand + Vector3.up * (0.22f * scale), 0.06f * (1f - 0.4f * Mathf.Exp(-age * 10f)));
            eightText.color = WithAlpha(Color.Lerp(Color.white, Accent, 0.3f), Mathf.Clamp01((4f - age) / 0.6f));
        }

        private static void AskEightBall(float now)
        {
            eightShakes = 0;
            eightAsked = now;

            // Never the same answer twice in a row
            int answer = Random.Range(0, EightBallAnswers.Length);
            if (answer == eightAnswer)
                answer = (answer + 1 + Random.Range(0, EightBallAnswers.Length - 1)) % EightBallAnswers.Length;
            eightAnswer = answer;

            if (eightText == null)
                eightText = MakeWristText("Eclipse_EightBall", Color.white);
            eightText.SafeSetText(EightBallAnswers[answer]);
            Burst(eightBall.Object.transform.position, 15, 1f, false);
        }

        public static void DisableMagicEightBall()
        {
            DiscardPiece(ref eightBall);
            DiscardPiece(ref eightWindow);
            DestroyText(ref eightText);
            eightAnswer = -1;
            eightAsked = -10f;
            eightShakes = 0;
        }

        private sealed class SpaceDie
        {
            public Piece Body;
            public Piece Pips;
            public Vector3 Velocity;
            public Vector3 Spin;
            public Quaternion Rest;
            public bool Settled;
            public int Value;
        }

        // A die's faces and the number on each; opposite faces add up to seven
        private static readonly Vector3[] DieFaces = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        private static readonly int[] DieValues = { 1, 6, 2, 5, 3, 4 };

        // Which spots of a three by three grid are filled for each number, as bits
        private static readonly int[] PipLayouts = { 0, 16, 257, 273, 325, 341, 365 };

        private static readonly SpaceDie[] spaceDice = new SpaceDie[2];
        private static TextMeshPro diceText;
        private static float diceThrown, diceLanded = -1f;
        private static bool diceHeld = true, diceOut;

        /// <summary>X rolls two dice from your left hand; they tumble, land and show their total above them before fading away.</summary>
        public static void SpaceDice()
        {
            float now = Time.time;
            float dt = Time.deltaTime;
            float scale = Scale;

            if (Pressed(leftPrimary, ref diceHeld))
                RollSpaceDice(now, scale);

            if (!diceOut)
                return;

            float half = 0.035f * scale;
            float skin = 0.005f * scale;
            int layers = GTPlayer.Instance.locomotionEnabledLayers;
            int resting = 0;
            Vector3 middle = Vector3.zero;

            for (int i = 0; i < spaceDice.Length; i++)
            {
                SpaceDie die = spaceDice[i];
                if (die == null || !die.Body.Alive || !die.Pips.Alive)
                    continue;

                Transform body = die.Body.Object.transform;
                if (die.Settled)
                {
                    body.rotation = Quaternion.Slerp(body.rotation, die.Rest, 1f - Mathf.Exp(-12f * dt));
                    resting++;
                }
                // One that finds no floor in time stops where it is
                else if (now - diceThrown > 6f)
                    SettleDie(die, body, Vector3.up);
                else
                {
                    die.Velocity += Physics.gravity * (dt * scale);
                    float spin = die.Spin.magnitude;
                    if (spin > 0f)
                        body.rotation = Quaternion.AngleAxis(spin * dt, die.Spin / spin) * body.rotation;

                    Vector3 move = die.Velocity * dt;
                    float distance = move.magnitude;
                    if (distance > 0f && Physics.SphereCast(body.position, half, move / distance, out RaycastHit hit, distance + skin, layers))
                    {
                        body.position += move / distance * Mathf.Max(0f, hit.distance - skin) + hit.normal * skin;
                        if (hit.normal.y > 0.6f && die.Velocity.sqrMagnitude < 1.5f * 1.5f * scale * scale)
                            SettleDie(die, body, hit.normal);
                        else
                        {
                            // Bounces off losing most of its speed, and tumbles the way it now rolls
                            die.Velocity = Vector3.Reflect(die.Velocity, hit.normal) * 0.45f;
                            die.Spin = Vector3.Cross(hit.normal, die.Velocity).normalized * Mathf.Min(720f, die.Velocity.magnitude / scale * 400f);
                        }
                    }
                    else
                        body.position += move;
                }

                body.localScale = Vector3.one * (half * 2f);
                die.Pips.Object.transform.SetPositionAndRotation(body.position, body.rotation);
                die.Pips.Object.transform.localScale = body.localScale;
                middle += body.position / spaceDice.Length;
            }

            if (resting == spaceDice.Length && diceLanded < 0f)
            {
                diceLanded = now;
                int a = spaceDice[0].Value, b = spaceDice[1].Value;
                if (diceText == null)
                    diceText = MakeWristText("Eclipse_SpaceDice", Color.white);
                diceText.SafeSetText(a == b ? $"{a + b}\n<size=55%>{a} + {b}, doubles!</size>" : $"{a + b}\n<size=55%>{a} + {b}</size>");
                ShowText(diceText, true);
            }

            // Once both have landed the total stays up for a few seconds, then everything fades
            float fade = diceLanded < 0f ? 1f : Mathf.Clamp01(1f - (now - diceLanded - 5f) / 1.5f);
            if (fade <= 0f)
            {
                HideSpaceDice();
                return;
            }

            Color shell = Color.Lerp(new Color(0.08f, 0.06f, 0.16f), Second, 0.2f);
            foreach (SpaceDie die in spaceDice)
            {
                if (die == null || !die.Body.Alive || !die.Pips.Alive)
                    continue;

                SetAlpha(die.Body, shell, fade);
                SetAlpha(die.Pips, Accent, fade);
            }

            if (diceLanded >= 0f && diceText != null)
            {
                PlaceToyText(diceText, middle + Vector3.up * (0.2f * scale), 0.1f);
                diceText.color = WithAlpha(Color.Lerp(Color.white, Accent, 0.4f), fade);
            }
        }

        private static void RollSpaceDice(float now, float scale)
        {
            var hand = ControllerUtilities.GetTrueLeftHand();
            Vector3 ahead = LookFlat();
            Vector3 side = Vector3.Cross(Vector3.up, ahead);

            for (int i = 0; i < spaceDice.Length; i++)
            {
                SpaceDie die = spaceDice[i];
                if (die == null)
                    die = spaceDice[i] = new SpaceDie();
                if (die.Body == null || !die.Body.Alive)
                    die.Body = MakePiece(PrimitiveType.Cube, Color.white, Vector3.one);
                if (die.Pips == null || !die.Pips.Alive)
                    die.Pips = MakeDecal(PipsMesh, Color.white);

                float offset = i == 0 ? -1f : 1f;
                Transform body = die.Body.Object.transform;
                body.position = hand.position + side * (offset * 0.05f * scale);
                body.rotation = Random.rotation;

                // Tossed ahead of you, carrying your own speed so they keep up if you are moving
                die.Velocity = (ahead * Random.Range(1.6f, 2.2f) + Vector3.up * Random.Range(1.2f, 1.8f) + side * (offset * 0.25f)) * scale + Extras.Body.linearVelocity;
                die.Spin = Random.onUnitSphere * Random.Range(400f, 800f);
                die.Settled = false;

                ShowPiece(die.Body, true);
                ShowPiece(die.Pips, true);
            }

            diceThrown = now;
            diceLanded = -1f;
            diceOut = true;
            ShowText(diceText, false);
        }

        private static void SettleDie(SpaceDie die, Transform body, Vector3 up)
        {
            die.Settled = true;
            die.Velocity = die.Spin = Vector3.zero;

            // Whichever face points most nearly up is the one it lands on
            int top = 0;
            float best = -2f;
            for (int j = 0; j < DieFaces.Length; j++)
            {
                float facing = Vector3.Dot(body.rotation * DieFaces[j], up);
                if (facing > best)
                {
                    best = facing;
                    top = j;
                }
            }

            die.Value = DieValues[top];
            die.Rest = Quaternion.FromToRotation(body.rotation * DieFaces[top], up) * body.rotation;
        }

        private static void HideSpaceDice()
        {
            foreach (SpaceDie die in spaceDice)
            {
                if (die == null)
                    continue;

                ShowPiece(die.Body, false);
                ShowPiece(die.Pips, false);
            }

            ShowText(diceText, false);
            diceLanded = -1f;
            diceOut = false;
        }

        public static void DisableSpaceDice()
        {
            for (int i = 0; i < spaceDice.Length; i++)
            {
                if (spaceDice[i] != null)
                {
                    Discard(spaceDice[i].Body);
                    Discard(spaceDice[i].Pips);
                }

                spaceDice[i] = null;
            }

            DestroyText(ref diceText);
            diceLanded = -1f;
            diceOut = false;
        }

        private sealed class GlowStick
        {
            public Piece Stick;
            public Piece Halo;
            public Light Light;
            public Color Color;
            public Vector3 Velocity;
            public Vector3 Spin;
            public float Snapped;
            public float Thrown;
            public bool Landed;
        }

        private const int MaxGlowSticks = 8;
        private static readonly List<GlowStick> glowSticks = new List<GlowStick>();
        private static GlowStick heldGlowStick;
        private static Vector3 glowLastHand;
        private static bool glowHeld = true;

        /// <summary>Left grip snaps a glow stick into your left hand and letting go throws it; it keeps glowing where it lands.</summary>
        public static void GlowSticks()
        {
            float now = Time.time;
            float dt = Time.deltaTime;
            float scale = Scale;
            var hand = ControllerUtilities.GetTrueLeftHand();
            Vector3 handVelocity = (hand.position - glowLastHand) / Mathf.Max(dt, 0.001f);
            glowLastHand = hand.position;

            if (Pressed(leftGrab, ref glowHeld) && heldGlowStick == null)
                heldGlowStick = SnapGlowStick(hand.position, now);

            if (heldGlowStick != null && !leftGrab)
            {
                heldGlowStick.Velocity = Vector3.ClampMagnitude(handVelocity, 15f * scale);
                heldGlowStick.Spin = Random.onUnitSphere * (180f + heldGlowStick.Velocity.magnitude / scale * 90f);
                heldGlowStick.Thrown = now;
                heldGlowStick = null;
            }

            int layers = GTPlayer.Instance.locomotionEnabledLayers;
            float radius = 0.011f * scale;
            for (int i = glowSticks.Count - 1; i >= 0; i--)
            {
                GlowStick stick = glowSticks[i];
                if (!stick.Stick.Alive || !stick.Halo.Alive)
                {
                    if (stick == heldGlowStick)
                        heldGlowStick = null;
                    RemoveGlowStick(stick);
                    glowSticks.RemoveAt(i);
                    continue;
                }

                Transform body = stick.Stick.Object.transform;
                if (stick == heldGlowStick)
                    body.SetPositionAndRotation(hand.position, hand.rotation * Quaternion.Euler(90f, 0f, 0f));
                else if (!stick.Landed)
                {
                    // Thrown off the edge of the world
                    if (now - stick.Thrown > 10f)
                    {
                        RemoveGlowStick(stick);
                        glowSticks.RemoveAt(i);
                        continue;
                    }

                    FlyGlowStick(stick, body, dt, scale, radius, layers);
                }

                // Flares up brightly when it is snapped, then settles into a soft steady glow
                float pulse = 1f + Mathf.Sin(now * 2.5f + stick.Stick.Seed) * 0.06f;
                float flash = Mathf.Exp(-(now - stick.Snapped) * 5f);

                body.localScale = new Vector3(0.022f, 0.06f, 0.022f) * scale;
                SetColor(stick.Stick, Color.Lerp(stick.Color, Color.white, 0.35f + 0.5f * flash));

                stick.Halo.Object.transform.SetPositionAndRotation(body.position, body.rotation);
                stick.Halo.Object.transform.localScale = new Vector3(0.06f, 0.17f, 0.06f) * (scale * pulse * (1f + flash));
                SetAlpha(stick.Halo, stick.Color, 0.2f + 0.3f * flash);

                if (stick.Light != null)
                {
                    stick.Light.color = stick.Color;
                    stick.Light.range = 1.6f * scale;
                    stick.Light.intensity = 1.2f * pulse + 2f * flash;
                }
            }
        }

        private static GlowStick SnapGlowStick(Vector3 at, float now)
        {
            // The oldest stick is reused once there are eight
            GlowStick stick = null;
            if (glowSticks.Count >= MaxGlowSticks)
            {
                stick = glowSticks[0];
                glowSticks.RemoveAt(0);
            }

            if (stick == null || !stick.Stick.Alive || !stick.Halo.Alive)
            {
                if (stick != null)
                    RemoveGlowStick(stick);

                stick = new GlowStick
                {
                    Stick = MakePiece(PrimitiveType.Capsule, Color.white, Vector3.one),
                    Halo = MakePiece(PrimitiveType.Sphere, Color.white, Vector3.one)
                };
                stick.Light = stick.Stick.Object.AddComponent<Light>();
                stick.Light.type = LightType.Point;
            }

            stick.Color = Color.Lerp(Accent, Second, Random.value);
            stick.Snapped = now;
            stick.Landed = false;
            stick.Velocity = stick.Spin = Vector3.zero;
            glowSticks.Add(stick);
            Burst(at, 10, 0.8f, false);
            return stick;
        }

        private static void FlyGlowStick(GlowStick stick, Transform body, float dt, float scale, float radius, int layers)
        {
            stick.Velocity += Physics.gravity * (dt * scale);
            float spin = stick.Spin.magnitude;
            if (spin > 0f)
                body.rotation = Quaternion.AngleAxis(spin * dt, stick.Spin / spin) * body.rotation;

            Vector3 move = stick.Velocity * dt;
            float distance = move.magnitude;
            if (distance <= 0f || !Physics.SphereCast(body.position, radius, move / distance, out RaycastHit hit, distance, layers))
            {
                body.position += move;
                return;
            }

            if (hit.normal.y < 0.5f)
            {
                // Glances off walls and keeps falling
                body.position += move / distance * Mathf.Max(0f, hit.distance - radius * 0.5f) + hit.normal * (radius * 0.5f);
                stick.Velocity = Vector3.Reflect(stick.Velocity, hit.normal) * 0.4f;
                return;
            }

            // Lies flat where it lands
            Vector3 along = Vector3.ProjectOnPlane(body.up, hit.normal);
            if (along.sqrMagnitude < 0.0001f)
                along = Vector3.Cross(hit.normal, Vector3.right);

            body.SetPositionAndRotation(hit.point + hit.normal * radius, Quaternion.LookRotation(along, hit.normal) * Quaternion.Euler(90f, 0f, 0f));
            stick.Landed = true;
        }

        private static void RemoveGlowStick(GlowStick stick)
        {
            Discard(stick.Stick);
            Discard(stick.Halo);
            stick.Light = null;
        }

        public static void DisableGlowSticks()
        {
            foreach (GlowStick stick in glowSticks)
                RemoveGlowStick(stick);
            glowSticks.Clear();
            heldGlowStick = null;
        }

        private struct EchoFrame
        {
            public float At;
            public Vector3 HeadPosition, LeftPosition, RightPosition;
            public Quaternion HeadRotation, LeftRotation, RightRotation;
        }

        // Room for two seconds at well over any headset's frame rate
        private const int EchoCapacity = 720;
        private const float EchoDelay = 2f;
        private static readonly EchoFrame[] echoFrames = new EchoFrame[EchoCapacity];
        private static int echoNewest = -1, echoCount, echoRead;
        private static Piece echoHead, echoEyeLeft, echoEyeRight, echoHandLeft, echoHandRight;

        /// <summary>A faint ghost of your head and hands that replays exactly what you did two seconds ago.</summary>
        public static void EchoGhost()
        {
            float now = Time.time;
            float scale = Scale;
            Transform head = Head;
            Transform left = GorillaTagger.Instance.leftHandTransform;
            Transform right = GorillaTagger.Instance.rightHandTransform;

            echoNewest = (echoNewest + 1) % EchoCapacity;
            echoFrames[echoNewest] = new EchoFrame
            {
                At = now,
                HeadPosition = head.position,
                HeadRotation = head.rotation,
                LeftPosition = left.position,
                LeftRotation = left.rotation,
                RightPosition = right.position,
                RightRotation = right.rotation
            };
            echoCount = Mathf.Min(echoCount + 1, EchoCapacity);

            Ensure(ref echoHead, PrimitiveType.Sphere, Color.white);
            Ensure(ref echoEyeLeft, PrimitiveType.Sphere, Color.white);
            Ensure(ref echoEyeRight, PrimitiveType.Sphere, Color.white);
            Ensure(ref echoHandLeft, PrimitiveType.Cube, Color.white);
            Ensure(ref echoHandRight, PrimitiveType.Cube, Color.white);

            // Nothing to replay until two seconds have been recorded
            int oldest = (echoNewest - echoCount + 1 + EchoCapacity) % EchoCapacity;
            float target = now - EchoDelay;
            if (echoFrames[oldest].At > target)
            {
                ShowEchoGhost(false);
                return;
            }

            // The frame being replayed only ever moves forward, so it is found by stepping on from the last one
            if (echoFrames[echoRead].At > target)
                echoRead = oldest;
            while (echoRead != echoNewest && echoFrames[(echoRead + 1) % EchoCapacity].At <= target)
                echoRead = (echoRead + 1) % EchoCapacity;

            EchoFrame from = echoFrames[echoRead];
            EchoFrame to = echoRead == echoNewest ? from : echoFrames[(echoRead + 1) % EchoCapacity];
            float k = to.At > from.At ? Mathf.Clamp01((target - from.At) / (to.At - from.At)) : 0f;
            Vector3 ghostHead = Vector3.Lerp(from.HeadPosition, to.HeadPosition, k);

            // Fades away while you stand where you were, so it never sits in front of your eyes
            float fade = Mathf.Clamp01(((ghostHead - head.position).magnitude - 0.3f * scale) / (0.4f * scale));
            ShowEchoGhost(fade > 0f);
            if (fade <= 0f)
                return;

            Quaternion ghostTurn = Quaternion.Slerp(from.HeadRotation, to.HeadRotation, k);
            Vector3 eye = Vector3.one * (0.035f * scale);
            Vector3 palm = new Vector3(0.07f, 0.035f, 0.1f) * scale;

            PlaceEcho(echoHead, ghostHead, ghostTurn, Vector3.one * (0.24f * scale), Second, 0.22f * fade);
            PlaceEcho(echoEyeLeft, ghostHead + ghostTurn * new Vector3(-0.045f, 0.02f, 0.1f) * scale, ghostTurn, eye, Accent, 0.75f * fade);
            PlaceEcho(echoEyeRight, ghostHead + ghostTurn * new Vector3(0.045f, 0.02f, 0.1f) * scale, ghostTurn, eye, Accent, 0.75f * fade);
            PlaceEcho(echoHandLeft, Vector3.Lerp(from.LeftPosition, to.LeftPosition, k), Quaternion.Slerp(from.LeftRotation, to.LeftRotation, k), palm, Second, 0.35f * fade);
            PlaceEcho(echoHandRight, Vector3.Lerp(from.RightPosition, to.RightPosition, k), Quaternion.Slerp(from.RightRotation, to.RightRotation, k), palm, Second, 0.35f * fade);
        }

        private static void PlaceEcho(Piece piece, Vector3 at, Quaternion turn, Vector3 size, Color color, float alpha)
        {
            piece.Object.transform.SetPositionAndRotation(at, turn);
            piece.Object.transform.localScale = size;
            SetAlpha(piece, color, alpha);
        }

        private static void ShowEchoGhost(bool shown)
        {
            ShowPiece(echoHead, shown);
            ShowPiece(echoEyeLeft, shown);
            ShowPiece(echoEyeRight, shown);
            ShowPiece(echoHandLeft, shown);
            ShowPiece(echoHandRight, shown);
        }

        public static void DisableEchoGhost()
        {
            DiscardPiece(ref echoHead);
            DiscardPiece(ref echoEyeLeft);
            DiscardPiece(ref echoEyeRight);
            DiscardPiece(ref echoHandLeft);
            DiscardPiece(ref echoHandRight);
            echoNewest = -1;
            echoCount = echoRead = 0;
        }

        private const int MaxMeteors = 8;
        private static readonly Piece[] meteorRocks = new Piece[MaxMeteors];
        private static LineRenderer meteorLaser;
        private static TextMeshPro meteorText;
        private static Vector3 meteorLaserEnd;
        private static int meteorScore, meteorLives, meteorBest, meteorShownScore = -1, meteorShownLives = -1;
        private static float meteorNext, meteorFired = -10f, meteorOver = -1f;
        private static bool meteorHeld = true;

        /// <summary>Meteors drift in towards you from all around; press B to fire a laser from your right hand and blast them before they reach you.</summary>
        public static void MeteorDefense()
        {
            float now = Time.time;
            float dt = Time.deltaTime;
            float scale = Scale;

            if (meteorText == null)
            {
                meteorText = MakeWristText("Eclipse_MeteorDefense", Color.white);
                StartMeteorGame(now);
            }

            // After a game over your score stays up for a few seconds, then a new game starts
            if (meteorOver >= 0f && now - meteorOver > 4f)
                StartMeteorGame(now);
            bool playing = meteorOver < 0f;

            Vector3 you = Head.position + Vector3.down * (0.25f * scale);
            if (playing && now >= meteorNext)
            {
                meteorNext = now + Mathf.Max(0.7f, 2.4f - meteorScore * 0.05f);
                SpawnMeteor(you, scale);
            }

            // Each meteor's Life is its speed and its Anchor its lumpy shape
            float reach = 0.3f * scale;
            foreach (Piece rock in meteorRocks)
            {
                if (rock == null || !rock.Alive || !rock.Object.activeSelf)
                    continue;

                Transform body = rock.Object.transform;
                float size = rock.Size * scale;
                body.position = Vector3.MoveTowards(body.position, you, rock.Life * scale * dt);
                body.rotation = Quaternion.Euler(now * 70f + rock.Seed, now * 45f + rock.Seed * 2f, rock.Seed * 3f);
                body.localScale = rock.Anchor * size;
                SetColor(rock, Color.Lerp(new Color(0.45f, 0.38f, 0.34f), Accent, 0.2f + 0.1f * Mathf.Sin(now * 5f + rock.Seed)));

                Vector3 away = body.position - you;
                float distance = away.magnitude;

                // Left far behind after a teleport; it doesn't count against you
                if (distance > 30f * scale)
                {
                    rock.Object.SetActive(false);
                    continue;
                }

                if (Random.value < 0.3f)
                    Burst(body.position, 1, 0.4f, false, away);

                if (distance < reach + size * 0.5f)
                {
                    Burst(body.position, 30, 2.5f, false);
                    rock.Object.SetActive(false);
                    GorillaTagger.Instance.StartVibration(true, GorillaTagger.Instance.tagHapticStrength, 0.2f);
                    GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.tagHapticStrength, 0.2f);

                    if (playing && --meteorLives <= 0)
                    {
                        EndMeteorGame(now);
                        playing = false;
                    }
                }
            }

            var hand = ControllerUtilities.GetTrueRightHand();
            if (Pressed(rightSecondary, ref meteorHeld) && playing)
                FireMeteorLaser(hand.position, hand.forward, now, scale);

            if (meteorLaser == null)
                meteorLaser = MakeLine("Eclipse_MeteorLaser", 0.004f);

            float flash = 1f - (now - meteorFired) / 0.2f;
            meteorLaser.SetPosition(0, hand.position);
            if (flash > 0f)
            {
                meteorLaser.SetPosition(1, meteorLaserEnd);
                meteorLaser.startWidth = 0.02f * scale * flash;
                meteorLaser.endWidth = 0.01f * scale * flash;
                meteorLaser.startColor = WithAlpha(Color.Lerp(Accent, Color.white, 0.5f), flash);
                meteorLaser.endColor = WithAlpha(Accent, flash);
            }
            else
            {
                // A faint guide shows where a shot would go
                meteorLaser.SetPosition(1, hand.position + hand.forward * (4f * scale));
                meteorLaser.startWidth = meteorLaser.endWidth = 0.004f * scale;
                meteorLaser.startColor = WithAlpha(Accent, playing ? 0.3f : 0f);
                meteorLaser.endColor = WithAlpha(Accent, 0f);
            }

            PlaceOnWrist(meteorText, 0.54f, true);
            if (playing && (meteorShownScore != meteorScore || meteorShownLives != meteorLives))
            {
                meteorShownScore = meteorScore;
                meteorShownLives = meteorLives;
                meteorText.SafeSetText($"Score {meteorScore}\n<size=55%>{meteorLives} {(meteorLives == 1 ? "life" : "lives")} left</size>");
                meteorText.color = Color.Lerp(Color.white, Accent, 0.35f);
            }
        }

        private static void SpawnMeteor(Vector3 you, float scale)
        {
            for (int i = 0; i < meteorRocks.Length; i++)
            {
                Piece rock = meteorRocks[i];
                if (rock != null && rock.Alive && rock.Object.activeSelf)
                    continue;

                if (rock == null || !rock.Alive)
                    rock = meteorRocks[i] = MakePiece(PrimitiveType.Sphere, Color.white, Vector3.one);

                // From anywhere around you, though mostly from above the horizon
                Vector3 from = Random.onUnitSphere;
                from.y = Mathf.Abs(from.y) * 0.8f + 0.1f;

                rock.Object.transform.position = you + from.normalized * (9f * scale);
                rock.Size = Random.Range(0.14f, 0.24f);
                rock.Life = Random.Range(0.8f, 1.2f) + Mathf.Min(meteorScore * 0.04f, 1.2f);
                rock.Anchor = new Vector3(Random.Range(0.8f, 1.2f), Random.Range(0.75f, 1.1f), Random.Range(0.8f, 1.2f));
                rock.Object.SetActive(true);
                return;
            }
        }

        private static void FireMeteorLaser(Vector3 from, Vector3 aim, float now, float scale)
        {
            meteorFired = now;
            meteorLaserEnd = from + aim * (25f * scale);

            // The laser blasts the nearest meteor it passes through, with a little room to spare
            Piece struck = null;
            float nearest = float.MaxValue;
            foreach (Piece rock in meteorRocks)
            {
                if (rock == null || !rock.Alive || !rock.Object.activeSelf)
                    continue;

                Vector3 toRock = rock.Object.transform.position - from;
                float along = Vector3.Dot(toRock, aim);
                if (along <= 0f || along >= nearest)
                    continue;

                float reach = (rock.Size * 0.8f + 0.08f) * scale;
                if ((toRock - aim * along).sqrMagnitude <= reach * reach)
                {
                    struck = rock;
                    nearest = along;
                }
            }

            if (struck == null)
                return;

            meteorLaserEnd = struck.Object.transform.position;
            Burst(meteorLaserEnd, 45, 3.5f, false);
            struck.Object.SetActive(false);
            meteorBest = Mathf.Max(meteorBest, ++meteorScore);

            if (meteorScore == 25)
                Eclipse.Menu.Main.EclipseUnlock("Meteor Defender");
        }

        private static void StartMeteorGame(float now)
        {
            meteorScore = 0;
            meteorLives = 3;
            meteorShownScore = meteorShownLives = -1;
            meteorOver = -1f;
            meteorNext = now + 1.5f;
        }

        private static void EndMeteorGame(float now)
        {
            meteorOver = now;
            meteorBest = Mathf.Max(meteorBest, meteorScore);
            foreach (Piece rock in meteorRocks)
            {
                if (rock == null || !rock.Alive || !rock.Object.activeSelf)
                    continue;

                Burst(rock.Object.transform.position, 20, 2f, false);
                rock.Object.SetActive(false);
            }

            meteorText.SafeSetText($"Game over\n<size=55%>score {meteorScore}  ·  best {meteorBest}</size>");
            meteorText.color = Accent;
        }

        public static void DisableMeteorDefense()
        {
            for (int i = 0; i < meteorRocks.Length; i++)
                DiscardPiece(ref meteorRocks[i]);

            DestroyLine(ref meteorLaser);
            DestroyText(ref meteorText);
            meteorOver = -1f;
            meteorFired = -10f;
        }

        // ── Toy shapes ──────────────────────────────────────────────────────────

        // Spots and stripes are drawn just after the pieces they sit on, so they always show on top
        private const int DecalQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 1;

        /// <summary>A piece drawn in one of the toys' own shapes instead of a plain primitive.</summary>
        private static Piece MakeShaped(Mesh mesh, Color color)
        {
            Piece piece = MakePiece(PrimitiveType.Cube, color, Vector3.one);
            piece.Object.GetComponent<MeshFilter>().sharedMesh = mesh;
            return piece;
        }

        private static Piece MakeDecal(Mesh mesh, Color color)
        {
            Piece piece = MakeShaped(mesh, color);
            piece.Renderer.sharedMaterial.renderQueue = DecalQueue;
            return piece;
        }

        private static void ShowPiece(Piece piece, bool shown)
        {
            if (piece != null && piece.Alive && piece.Object.activeSelf != shown)
                piece.Object.SetActive(shown);
        }

        private static void ShowText(TextMeshPro text, bool shown)
        {
            if (text != null && text.gameObject.activeSelf != shown)
                text.gameObject.SetActive(shown);
        }

        /// <summary>Floats a toy's text at a spot in the world, turned to face you.</summary>
        private static void PlaceToyText(TextMeshPro text, Vector3 at, float size)
        {
            text.SafeSetFont(activeFont);
            text.transform.localScale = Vector3.one * (size * Scale);
            text.transform.position = at;
            text.transform.LookAt(Head.position);
            text.transform.Rotate(0f, 180f, 0f);
        }

        private static Mesh kiteMesh, paperPlaneMesh, pipsMesh, yoyoStripeMesh;

        /// <summary>A flat diamond, seen from both sides.</summary>
        private static Mesh KiteMesh
        {
            get
            {
                if (kiteMesh != null)
                    return kiteMesh;

                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                AddSheet(vertices, triangles, KiteCorners[0], KiteCorners[1], KiteCorners[2]);
                AddSheet(vertices, triangles, KiteCorners[0], KiteCorners[2], KiteCorners[3]);
                return kiteMesh = FinishMesh("EclipseKite", vertices, triangles);
            }
        }

        /// <summary>A folded paper dart pointing along +Z: two wings and a keel underneath.</summary>
        private static Mesh PaperPlaneMesh
        {
            get
            {
                if (paperPlaneMesh != null)
                    return paperPlaneMesh;

                Vector3 nose = new Vector3(0f, 0f, 0.5f);
                Vector3 tail = new Vector3(0f, 0f, -0.5f);
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                AddSheet(vertices, triangles, nose, tail, new Vector3(-0.36f, 0.06f, -0.5f));
                AddSheet(vertices, triangles, nose, new Vector3(0.36f, 0.06f, -0.5f), tail);
                AddSheet(vertices, triangles, nose, new Vector3(0f, -0.14f, -0.42f), tail);
                return paperPlaneMesh = FinishMesh("EclipsePaperPlane", vertices, triangles);
            }
        }

        /// <summary>The round spots of a die, just outside each face of a unit cube.</summary>
        /// <remarks>Each spot faces outwards only, so the ones round the back are never drawn through the die.</remarks>
        private static Mesh PipsMesh
        {
            get
            {
                if (pipsMesh != null)
                    return pipsMesh;

                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                for (int face = 0; face < DieFaces.Length; face++)
                {
                    Vector3 normal = DieFaces[face];
                    Vector3 u = Mathf.Abs(normal.y) > 0.5f ? Vector3.right : Vector3.up;
                    Vector3 v = Vector3.Cross(normal, u);
                    int layout = PipLayouts[DieValues[face]];

                    for (int cell = 0; cell < 9; cell++)
                    {
                        if ((layout & (1 << cell)) == 0)
                            continue;

                        Vector3 centre = normal * 0.505f + (u * (cell % 3 - 1) + v * (cell / 3 - 1)) * 0.25f;
                        for (int slice = 0; slice < 8; slice++)
                        {
                            float a = slice * Mathf.PI / 4f;
                            float b = (slice + 1) * Mathf.PI / 4f;
                            AddTriangle(vertices, triangles, centre,
                                centre + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * 0.08f,
                                centre + (u * Mathf.Cos(b) + v * Mathf.Sin(b)) * 0.08f,
                                normal);
                        }
                    }
                }

                return pipsMesh = FinishMesh("EclipseDicePips", vertices, triangles);
            }
        }

        /// <summary>A stripe across both faces of a cylinder primitive, which is two units tall and one across.</summary>
        private static Mesh YoyoStripeMesh
        {
            get
            {
                if (yoyoStripeMesh != null)
                    return yoyoStripeMesh;

                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 normal = new Vector3(0f, side, 0f);
                    Vector3 centre = normal * 1.02f;
                    Vector3 a = centre + new Vector3(-0.42f, 0f, -0.08f);
                    Vector3 b = centre + new Vector3(0.42f, 0f, -0.08f);
                    Vector3 c = centre + new Vector3(0.42f, 0f, 0.08f);
                    Vector3 d = centre + new Vector3(-0.42f, 0f, 0.08f);
                    AddTriangle(vertices, triangles, a, b, c, normal);
                    AddTriangle(vertices, triangles, a, c, d, normal);
                }

                return yoyoStripeMesh = FinishMesh("EclipseYoyoStripe", vertices, triangles);
            }
        }

        // Adds a triangle facing `outward`
        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f)
                (b, c) = (c, b);

            int index = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
        }

        // Adds a triangle facing both ways, for thin paper and kite cloth
        private static void AddSheet(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            AddTriangle(vertices, triangles, a, b, c, normal);
            AddTriangle(vertices, triangles, a, b, c, -normal);
        }

        private static Mesh FinishMesh(string name, List<Vector3> vertices, List<int> triangles)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return mesh;
        }
    }
}
