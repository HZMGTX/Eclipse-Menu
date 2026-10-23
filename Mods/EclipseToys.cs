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
            builderHeld = planeHeld = diceHeld = glowHeld = meteorHeld = boomerangHeld = true;
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

        private static Piece scopeEyepiece, scopeTube, scopeBarrel, scopeGlass, scopeLens;
        private static LineRenderer scopeLensRim, scopeBand;
        private static Camera scopeCamera;
        private static RenderTexture scopeView;
        private static float scopeRaise;

        /// <summary>Hold your left trigger to raise a little brass telescope whose lens shows a zoomed-in live view of whatever it points at.</summary>
        public static void StarScope()
        {
            bool held = leftTrigger > 0.5f;
            scopeRaise = Mathf.MoveTowards(scopeRaise, held ? 1f : 0f, Time.deltaTime * 6f);

            if (scopeRaise <= 0f)
            {
                if (scopeCamera != null)
                    scopeCamera.enabled = false;
                ShowStarScope(false);
                return;
            }

            // The camera and its picture are made the first time the scope comes up, then kept for next time
            if (scopeCamera == null)
                MakeScopeCamera();
            scopeCamera.enabled = held;

            if (scopeLens == null || !scopeLens.Alive)
                scopeLens = MakeScopeLens();

            Ensure(ref scopeEyepiece, PrimitiveType.Cylinder, Color.white);
            Ensure(ref scopeTube, PrimitiveType.Cylinder, Color.white);
            Ensure(ref scopeBarrel, PrimitiveType.Cylinder, Color.white);
            Ensure(ref scopeGlass, PrimitiveType.Cylinder, Color.white);
            if (scopeLensRim == null)
                scopeLensRim = MakeRing("Eclipse_StarScopeRim");
            if (scopeBand == null)
                scopeBand = MakeRing("Eclipse_StarScopeBand");
            ShowStarScope(true);

            float scale = Scale;
            var hand = ControllerUtilities.GetTrueLeftHand();
            Vector3 forward = hand.forward;
            Vector3 origin = hand.position;

            // Kept upright however your hand is turned, so the picture never rolls over
            Quaternion aim = Quaternion.LookRotation(forward, Mathf.Abs(forward.y) > 0.98f ? hand.up : Vector3.up);
            float size = scale * Mathf.SmoothStep(0f, 1f, scopeRaise);

            Color brass = Color.Lerp(new Color(0.8f, 0.6f, 0.28f), Accent, 0.2f);
            PlaceScopePart(scopeEyepiece, origin, aim, -0.075f, -0.035f, 0.031f, size, Color.Lerp(brass, Color.black, 0.3f));
            PlaceScopePart(scopeTube, origin, aim, -0.035f, 0.11f, 0.019f, size, brass);
            PlaceScopePart(scopeBarrel, origin, aim, 0.11f, 0.215f, 0.025f, size, Color.Lerp(brass, Color.black, 0.15f));
            PlaceScopePart(scopeGlass, origin, aim, 0.215f, 0.219f, 0.022f, size, WithAlpha(Color.Lerp(Second, Color.white, 0.5f), 0.45f));

            // The lens sits in the near end, facing you
            Vector3 lensAt = origin + forward * (-0.077f * size);
            scopeLens.Object.transform.SetPositionAndRotation(lensAt, aim);
            scopeLens.Object.transform.localScale = Vector3.one * (0.027f * size);
            SetColor(scopeLens, Color.Lerp(Color.white, Second, 0.08f));

            PlaceRing(scopeLensRim, lensAt, forward, 0.028f * size, 0.004f * size, Accent);
            PlaceRing(scopeBand, origin + forward * (0.11f * size), forward, 0.026f * size, 0.005f * size, Color.Lerp(Accent, Color.white, 0.3f));

            scopeCamera.transform.SetPositionAndRotation(origin + forward * (0.23f * scale), aim);
            scopeCamera.nearClipPlane = 0.02f * scale;
        }

        private static void MakeScopeCamera()
        {
            if (scopeView == null)
            {
                scopeView = new RenderTexture(256, 256, 16) { name = "Eclipse_StarScopeView" };
                scopeView.Create();
            }

            scopeCamera = new GameObject("Eclipse_StarScopeCamera").AddComponent<Camera>();
            scopeCamera.enabled = false;
            scopeCamera.targetTexture = scopeView;
            scopeCamera.stereoTargetEye = StereoTargetEyeMask.None;
            scopeCamera.fieldOfView = 15f;
            scopeCamera.allowHDR = false;
            scopeCamera.allowMSAA = false;
            scopeCamera.depth = -10f;

            // Sees the world the way you do
            Camera view = Camera.main;
            if (view != null)
            {
                scopeCamera.cullingMask = view.cullingMask;
                scopeCamera.clearFlags = view.clearFlags;
                scopeCamera.backgroundColor = view.backgroundColor;
                scopeCamera.farClipPlane = view.farClipPlane;
            }

            // Only the picture: no shadows, effects or smoothing, and never drawn to the headset
            var settings = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(scopeCamera);
            settings.renderShadows = false;
            settings.renderPostProcessing = false;
            settings.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;
            settings.requiresDepthOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;
            settings.requiresColorOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;
            settings.allowXRRendering = false;
        }

        private static Piece MakeScopeLens()
        {
            // The toys' see-through material would let parts of the picture show through, so the lens gets a solid one
            Piece lens = MakeShaped(ScopeLensMesh, Color.white);
            Object.Destroy(lens.Renderer.sharedMaterial);

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            lens.Renderer.sharedMaterial = new Material(shader) { mainTexture = scopeView };
            return lens;
        }

        // Lays one length of the scope's tube along its aim, between two distances from your hand
        private static void PlaceScopePart(Piece part, Vector3 origin, Quaternion aim, float from, float to, float radius, float size, Color color)
        {
            Transform body = part.Object.transform;
            body.SetPositionAndRotation(origin + aim * new Vector3(0f, 0f, (from + to) * 0.5f * size), aim * Quaternion.Euler(90f, 0f, 0f));
            body.localScale = new Vector3(radius * 2f, (to - from) * 0.5f, radius * 2f) * size;
            SetColor(part, color);
        }

        private static void ShowStarScope(bool shown)
        {
            ShowPiece(scopeEyepiece, shown);
            ShowPiece(scopeTube, shown);
            ShowPiece(scopeBarrel, shown);
            ShowPiece(scopeGlass, shown);
            ShowPiece(scopeLens, shown);
            if (scopeLensRim != null)
                scopeLensRim.enabled = shown;
            if (scopeBand != null)
                scopeBand.enabled = shown;
        }

        /// <summary>Puts the star scope away for good, along with its camera and picture.</summary>
        public static void DisableStarScope()
        {
            DiscardPiece(ref scopeEyepiece);
            DiscardPiece(ref scopeTube);
            DiscardPiece(ref scopeBarrel);
            DiscardPiece(ref scopeGlass);
            DiscardPiece(ref scopeLens);
            DestroyLine(ref scopeLensRim);
            DestroyLine(ref scopeBand);

            if (scopeCamera != null)
            {
                scopeCamera.targetTexture = null;
                Object.Destroy(scopeCamera.gameObject);
            }
            scopeCamera = null;

            if (scopeView != null)
            {
                scopeView.Release();
                Object.Destroy(scopeView);
            }
            scopeView = null;
            scopeRaise = 0f;
        }

        // Inside the snow globe the glass is one unit round: the snowy ground, the planet sitting on it and how close snow gets to the glass
        private const int GlobeFlakes = 60;
        private const float GlobeFloor = -0.55f;
        private const float GlobePlanetSize = 0.27f;
        private const float GlobeWall = 0.9f;
        private const float GlobeFlakeSize = 0.03f;
        private static readonly Vector3 GlobePlanetAt = new Vector3(0f, GlobeFloor + GlobePlanetSize, 0f);

        private static readonly Vector3[] globeFlakeAt = new Vector3[GlobeFlakes];
        private static readonly Vector3[] globeFlakeVelocity = new Vector3[GlobeFlakes];
        private static readonly ParticleSystem.Particle[] globeFlakes = new ParticleSystem.Particle[GlobeFlakes];
        private static Piece globeGlass, globeBase, globeGround, globePlanet, globeMoon;
        private static LineRenderer globeCollar, globeRing;
        private static ParticleSystem globeSnow;
        private static Vector3 globeLastHand, globeLastVelocity;
        private static float globeStir;

        /// <summary>A snow globe with a tiny ringed planet inside sits on your right hand; shake it to swirl up glittery snow that slowly settles again.</summary>
        public static void SnowGlobe()
        {
            float now = Time.time;
            float dt = Mathf.Max(Time.deltaTime, 0.001f);
            float scale = Scale;
            float radius = 0.075f * scale;
            Vector3 hand = RightHand;
            Vector3 centre = hand + Vector3.up * (0.12f * scale);

            // Measured against your body, so running about doesn't count as shaking
            Vector3 relative = hand - BodyPosition;
            if (globeSnow == null)
            {
                globeSnow = MakeParticles("Eclipse_SnowGlobe", null, particles =>
                {
                    var main = particles.main;
                    main.loop = true;
                    main.startSpeed = 0f;
                    main.startLifetime = 100f;
                    main.maxParticles = GlobeFlakes;
                    main.scalingMode = ParticleSystemScalingMode.Local;

                    var emission = particles.emission;
                    emission.enabled = false;
                });

                globeSnow.transform.localScale = Vector3.one;
                StartGlobeSnow();
                globeLastHand = relative;
                globeLastVelocity = Vector3.zero;
            }

            Vector3 velocity = (relative - globeLastHand) / dt;
            globeLastHand = relative;
            Vector3 jolt = Vector3.ClampMagnitude(velocity - globeLastVelocity, 3f * scale);
            globeLastVelocity = velocity;

            // Small tremors are ignored; a real jolt sloshes the snow the other way and stirs it all up
            if (jolt.sqrMagnitude < 0.08f * 0.08f * scale * scale)
                jolt = Vector3.zero;
            globeStir = Mathf.Clamp01(globeStir * Mathf.Exp(-0.6f * dt) + jolt.magnitude / scale * 0.15f);
            Vector3 slosh = -jolt / radius * 0.5f;

            Color accent = Accent;
            Color second = Second;
            for (int i = 0; i < GlobeFlakes; i++)
            {
                Vector3 at = globeFlakeAt[i];
                Vector3 drift = globeFlakeVelocity[i];

                // Lighter flakes are thrown about more; stirred snow swirls round the planet, tumbling as it goes, then sinks
                drift += slosh * (0.6f + 0.4f * Mathf.Sin(i * 2.39f));
                drift += (new Vector3(-at.z, 0f, at.x) * 2.5f + Random.insideUnitSphere * 3f) * (globeStir * dt);
                drift.y -= 0.45f * dt;
                drift = Vector3.ClampMagnitude(drift * Mathf.Exp(-2.2f * dt), 4f);
                at += drift * dt;

                bool resting = false;
                if (at.y < GlobeFloor + GlobeFlakeSize)
                {
                    at.y = GlobeFloor + GlobeFlakeSize;
                    if (drift.y < 0f)
                        drift.y = 0f;
                    resting = true;
                }

                Vector3 fromPlanet = at - GlobePlanetAt;
                float clear = GlobePlanetSize + GlobeFlakeSize;
                if (fromPlanet.sqrMagnitude < clear * clear)
                {
                    float distance = fromPlanet.magnitude;
                    Vector3 normal = distance > 0.0001f ? fromPlanet / distance : Vector3.up;
                    at = GlobePlanetAt + normal * clear;
                    float into = Vector3.Dot(drift, normal);
                    if (into < 0f)
                        drift -= normal * into;
                    resting = true;
                }

                float reach = at.magnitude;
                if (reach > GlobeWall)
                {
                    Vector3 normal = at / reach;
                    at = normal * GlobeWall;
                    float outward = Vector3.Dot(drift, normal);
                    if (outward > 0f)
                        drift -= normal * (outward * 1.5f);
                }

                // Snow that has landed stays put until it is shaken loose
                if (resting)
                    drift *= Mathf.Exp(-8f * dt);

                globeFlakeAt[i] = at;
                globeFlakeVelocity[i] = drift;

                // Glitter: every flake twinkles in its own time
                float twinkle = 0.5f + 0.5f * Mathf.Sin(now * (4f + i % 5) + i * 1.7f);
                Color tint = i % 3 == 0 ? accent : i % 3 == 1 ? second : Color.white;
                ParticleSystem.Particle flake = globeFlakes[i];
                flake.position = centre + at * radius;
                flake.startSize = radius * (0.05f + 0.03f * twinkle);
                flake.startColor = WithAlpha(Color.Lerp(Color.white, tint, 0.3f + 0.4f * twinkle), 0.55f + 0.45f * twinkle);
                flake.remainingLifetime = 100f;
                globeFlakes[i] = flake;
            }

            globeSnow.transform.position = centre;
            globeSnow.SetParticles(globeFlakes, GlobeFlakes);

            Ensure(ref globeBase, PrimitiveType.Cylinder, Color.white);
            globeBase.Object.transform.position = centre + Vector3.down * (0.98f * radius);
            globeBase.Object.transform.localScale = new Vector3(1.45f, 0.16f, 1.45f) * radius;
            SetColor(globeBase, Color.Lerp(new Color(0.14f, 0.09f, 0.2f), accent, 0.3f));

            Ensure(ref globeGround, PrimitiveType.Cylinder, Color.white);
            globeGround.Object.transform.position = centre + Vector3.up * (GlobeFloor * radius);
            globeGround.Object.transform.localScale = new Vector3(1.62f, 0.01f, 1.62f) * radius;
            SetAlpha(globeGround, Color.Lerp(Color.white, second, 0.15f), 0.95f);

            Vector3 planet = centre + GlobePlanetAt * radius;
            Ensure(ref globePlanet, PrimitiveType.Sphere, Color.white);
            globePlanet.Object.transform.position = planet;
            globePlanet.Object.transform.localScale = Vector3.one * (GlobePlanetSize * 2f * radius);
            SetColor(globePlanet, Color.Lerp(accent, Color.white, 0.1f));

            float orbit = now * 1.1f;
            Ensure(ref globeMoon, PrimitiveType.Sphere, Color.white);
            globeMoon.Object.transform.position = planet + new Vector3(Mathf.Cos(orbit) * 0.48f, 0.22f, Mathf.Sin(orbit) * 0.48f) * radius;
            globeMoon.Object.transform.localScale = Vector3.one * (0.12f * radius);
            SetColor(globeMoon, Color.Lerp(Color.white, second, 0.3f));

            Ensure(ref globeGlass, PrimitiveType.Sphere, Color.white);
            globeGlass.Object.transform.position = centre;
            globeGlass.Object.transform.localScale = Vector3.one * (2f * radius);
            SetAlpha(globeGlass, Color.Lerp(Color.white, second, 0.35f), 0.13f);

            if (globeRing == null)
                globeRing = MakeRing("Eclipse_SnowGlobeRing");
            PlaceRing(globeRing, planet, Quaternion.Euler(20f, 0f, 12f) * Vector3.up, GlobePlanetSize * 1.45f * radius, 0.03f * radius, WithAlpha(second, 0.85f), now * 40f);

            if (globeCollar == null)
                globeCollar = MakeRing("Eclipse_SnowGlobeCollar");
            PlaceRing(globeCollar, centre + Vector3.down * (0.82f * radius), Vector3.up, 0.73f * radius, 0.08f * radius, Color.Lerp(accent, Color.white, 0.25f));
        }

        // Scatters the snow through the top of the globe, so it starts off settling
        private static void StartGlobeSnow()
        {
            for (int i = 0; i < GlobeFlakes; i++)
            {
                Vector3 at = Random.insideUnitSphere * 0.8f;
                at.y = Mathf.Abs(at.y);
                globeFlakeAt[i] = at;
                globeFlakeVelocity[i] = Vector3.zero;
                globeFlakes[i].startLifetime = 100f;
            }

            globeStir = 0.5f;
        }

        /// <summary>Takes away the snow globe and its snow.</summary>
        public static void DisableSnowGlobe()
        {
            DiscardPiece(ref globeGlass);
            DiscardPiece(ref globeBase);
            DiscardPiece(ref globeGround);
            DiscardPiece(ref globePlanet);
            DiscardPiece(ref globeMoon);
            DestroyLine(ref globeCollar);
            DestroyLine(ref globeRing);
            DestroyParticles(ref globeSnow);
            globeStir = 0f;
        }

        private static readonly Piece[] pinwheelBlades = new Piece[4];
        private static Piece pinwheelStick, pinwheelHub;
        private static Vector3 pinwheelLastHand;
        private static float pinwheelWind, pinwheelSpeed, pinwheelSpin;

        /// <summary>A colourful pinwheel on a stick in your right hand that spins faster the faster your hand moves through the air.</summary>
        public static void Pinwheel()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.001f);
            float scale = Scale;
            var hand = ControllerUtilities.GetTrueRightHand();

            if (pinwheelStick == null || !pinwheelStick.Alive)
            {
                pinwheelStick = MakePiece(PrimitiveType.Cylinder, Color.white, Vector3.one);
                pinwheelLastHand = hand.position;
                pinwheelWind = pinwheelSpeed = 0f;
            }

            // The blades always draw over the stick, and the pin over the blades
            if (pinwheelHub == null || !pinwheelHub.Alive)
            {
                pinwheelHub = MakePiece(PrimitiveType.Sphere, Color.white, Vector3.one);
                pinwheelHub.Renderer.sharedMaterial.renderQueue = DecalQueue + 1;
            }

            // How fast your hand moves through the air, smoothed a little
            float speed = Mathf.Min(Vector3.Distance(hand.position, pinwheelLastHand) / dt, 12f * scale);
            pinwheelLastHand = hand.position;
            pinwheelWind = Mathf.Lerp(pinwheelWind, speed, 1f - Mathf.Exp(-10f * dt));

            // A breeze keeps it turning lazily; a quick sweep whips it round, and it coasts back down
            float target = Mathf.Min(40f + pinwheelWind / scale * 700f, 2400f);
            float response = target > pinwheelSpeed ? 5f : 1.2f;
            pinwheelSpeed = Mathf.Lerp(pinwheelSpeed, target, 1f - Mathf.Exp(-response * dt));
            pinwheelSpin = (pinwheelSpin + pinwheelSpeed * dt) % 360f;

            Vector3 along = hand.forward;
            Vector3 top = hand.position + along * (0.3f * scale);

            // Turned face-on to you so you can see it spin
            Vector3 facing = Vector3.ProjectOnPlane(Head.position - top, along);
            if (facing.sqrMagnitude < 0.0001f)
                facing = hand.up;
            facing.Normalize();
            Quaternion face = Quaternion.LookRotation(facing, along);

            Vector3 bottom = hand.position - along * (0.04f * scale);
            Vector3 stickTop = top - facing * (0.008f * scale);
            pinwheelStick.Object.transform.SetPositionAndRotation((bottom + stickTop) * 0.5f, Quaternion.LookRotation(along) * Quaternion.Euler(90f, 0f, 0f));
            pinwheelStick.Object.transform.localScale = new Vector3(0.009f * scale, Vector3.Distance(bottom, stickTop) * 0.5f, 0.009f * scale);
            SetColor(pinwheelStick, Color.Lerp(new Color(0.88f, 0.78f, 0.58f), Second, 0.15f));

            Color accent = Accent;
            Color second = Second;
            for (int i = 0; i < pinwheelBlades.Length; i++)
            {
                Piece blade = pinwheelBlades[i];
                if (blade == null || !blade.Alive)
                    blade = pinwheelBlades[i] = MakeDecal(PinwheelBladeMesh, Color.white);

                blade.Object.transform.SetPositionAndRotation(top, face * Quaternion.Euler(0f, 0f, pinwheelSpin + i * 90f));
                blade.Object.transform.localScale = Vector3.one * (0.16f * scale);

                // Alternating colours, the second pair paler
                Color color = i % 2 == 0 ? accent : second;
                SetColor(blade, i < 2 ? color : Color.Lerp(color, Color.white, 0.45f));
            }

            pinwheelHub.Object.transform.position = top + facing * (0.012f * scale);
            pinwheelHub.Object.transform.localScale = Vector3.one * (0.022f * scale);
            SetColor(pinwheelHub, Color.Lerp(Color.white, accent, 0.3f));
        }

        /// <summary>Takes away the pinwheel.</summary>
        public static void DisablePinwheel()
        {
            DiscardPiece(ref pinwheelStick);
            DiscardPiece(ref pinwheelHub);
            for (int i = 0; i < pinwheelBlades.Length; i++)
                DiscardPiece(ref pinwheelBlades[i]);
            pinwheelWind = pinwheelSpeed = 0f;
        }

        private const int BalloonCordPoints = 14;
        private static readonly Vector3[] balloonCordBuffer = new Vector3[BalloonCordPoints];
        private static Piece balloonMoon, balloonCraters, balloonGlow, balloonKnot;
        private static LineRenderer balloonCord;
        private static Vector3 balloonAt, balloonVelocity;

        /// <summary>A round glowing moon balloon on a string tied to your left wrist, bobbing along and trailing behind as you move.</summary>
        public static void MoonBalloon()
        {
            float now = Time.time;
            float dt = Mathf.Max(Time.deltaTime, 0.001f);
            float scale = Scale;
            float reach = 0.6f * scale;
            float size = 0.2f * scale;
            Vector3 wrist = LeftHand;

            if (balloonMoon == null || !balloonMoon.Alive)
            {
                balloonMoon = MakePiece(PrimitiveType.Sphere, Color.white, Vector3.one);
                balloonAt = wrist + Vector3.up * reach;
                balloonVelocity = Vector3.zero;
            }

            if (balloonCraters == null || !balloonCraters.Alive)
                balloonCraters = MakeDecal(BalloonCraterMesh, Color.white);
            Ensure(ref balloonGlow, PrimitiveType.Sphere, Color.white);
            Ensure(ref balloonKnot, PrimitiveType.Sphere, Color.white);

            // Left far behind after a teleport
            if ((balloonAt - wrist).sqrMagnitude > 9f * reach * reach)
            {
                balloonAt = wrist + Vector3.up * reach;
                balloonVelocity = Vector3.zero;
            }

            // Floats upwards, drifts on a gentle breeze, and the air holds it back as you move
            Vector3 breeze = new Vector3(Mathf.Sin(now * 0.9f), 0f, Mathf.Cos(now * 0.7f)) * (0.8f * scale);
            balloonVelocity += (Vector3.up * (4f * scale) + breeze) * dt;
            balloonVelocity *= Mathf.Exp(-1.5f * dt);
            balloonAt += balloonVelocity * dt;

            // The string only ever holds the balloon back; it never pulls on you
            Vector3 fromWrist = balloonAt - wrist;
            float length = fromWrist.magnitude;
            if (length > reach)
            {
                Vector3 outwards = fromWrist / length;
                balloonAt = wrist + outwards * reach;
                float outward = Vector3.Dot(balloonVelocity, outwards);
                if (outward > 0f)
                    balloonVelocity -= outwards * outward;
                length = reach;
            }

            Vector3 along = length > 0.001f * scale ? (balloonAt - wrist) / length : Vector3.up;
            Vector3 centre = balloonAt + Vector3.up * (Mathf.Sin(now * 2.1f) * 0.015f * scale);
            Quaternion turn = Quaternion.FromToRotation(Vector3.up, along) * Quaternion.Euler(0f, now * 12f, 0f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(now * 1.8f);

            Color moon = Color.Lerp(new Color(0.93f, 0.91f, 0.84f), Second, 0.2f);
            balloonMoon.Object.transform.SetPositionAndRotation(centre, turn);
            balloonMoon.Object.transform.localScale = new Vector3(1f, 1.06f, 1f) * size;
            SetColor(balloonMoon, Color.Lerp(moon, Color.white, 0.15f * pulse));

            balloonCraters.Object.transform.SetPositionAndRotation(centre, turn);
            balloonCraters.Object.transform.localScale = balloonMoon.Object.transform.localScale;
            SetColor(balloonCraters, Color.Lerp(moon, new Color(0.35f, 0.33f, 0.42f), 0.45f));

            balloonGlow.Object.transform.position = centre;
            balloonGlow.Object.transform.localScale = Vector3.one * (size * (1.4f + 0.08f * pulse));
            SetAlpha(balloonGlow, Accent, 0.16f + 0.08f * pulse);

            // The knot at the bottom, where the string is tied on
            Vector3 knot = centre - along * (size * 0.53f);
            balloonKnot.Object.transform.position = knot;
            balloonKnot.Object.transform.localScale = Vector3.one * (0.018f * scale);
            SetColor(balloonKnot, Accent);

            if (balloonCord == null)
            {
                balloonCord = MakeLine("Eclipse_MoonBalloonString", 0.003f);
                balloonCord.positionCount = BalloonCordPoints;
            }

            // Sags a little when slack, and bows back against the way the balloon is moving
            float sag = Mathf.Lerp(0.08f, 0.012f, length / reach) * scale;
            Vector3 bow = Vector3.ClampMagnitude(-balloonVelocity * 0.03f, 0.05f * scale);
            for (int i = 0; i < BalloonCordPoints; i++)
            {
                float k = i / (float)(BalloonCordPoints - 1);
                balloonCordBuffer[i] = Vector3.Lerp(wrist, knot, k) + (Vector3.down * sag + bow) * Mathf.Sin(k * Mathf.PI);
            }

            balloonCord.SetPositions(balloonCordBuffer);
            balloonCord.startWidth = balloonCord.endWidth = 0.0025f * scale;
            balloonCord.startColor = balloonCord.endColor = new Color(1f, 1f, 1f, 0.6f);
        }

        /// <summary>Takes away the moon balloon and its string.</summary>
        public static void DisableMoonBalloon()
        {
            DiscardPiece(ref balloonMoon);
            DiscardPiece(ref balloonCraters);
            DiscardPiece(ref balloonGlow);
            DiscardPiece(ref balloonKnot);
            DestroyLine(ref balloonCord);
        }

        // How far out the star goes, how wide its loop is and how long it takes to come back
        private const float BoomerangRange = 3.2f;
        private const float BoomerangWidth = 0.9f;
        private const float BoomerangFlight = 2.4f;

        private static Piece boomerangStar, boomerangGlow;
        private static TrailRenderer boomerangTrail;
        private static Vector3 boomerangFrom, boomerangForward, boomerangSide, boomerangUp;
        private static float boomerangThrown, boomerangSpin;
        private static bool boomerangHeld = true, boomerangFlying;

        /// <summary>Press A to throw a spinning star that curves out the way your right hand points and comes back round to be caught in your hand.</summary>
        public static void BoomerangStar()
        {
            float now = Time.time;
            float dt = Time.deltaTime;
            float scale = Scale;
            var hand = ControllerUtilities.GetTrueRightHand();

            if (boomerangStar == null || !boomerangStar.Alive)
            {
                boomerangStar = MakeShaped(BoomerangStarMesh, Accent);
                boomerangFlying = false;
            }

            Ensure(ref boomerangGlow, PrimitiveType.Sphere, Color.white);

            // Its own object rather than part of the star, so throwing the star away never takes the shared line material with it
            if (boomerangTrail == null)
            {
                boomerangTrail = new GameObject("Eclipse_BoomerangTrail").AddComponent<TrailRenderer>();
                boomerangTrail.material = VertexColored;
                boomerangTrail.time = 0.4f;
                boomerangTrail.minVertexDistance = 0.03f;
                boomerangTrail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                boomerangTrail.emitting = false;
            }

            if (Pressed(rightPrimary, ref boomerangHeld) && !boomerangFlying)
                ThrowBoomerangStar(hand.position, hand.forward, now);

            Vector3 at;
            Quaternion turn;
            if (boomerangFlying)
            {
                // Out along a loop that bends left, round the far end and back in from the right, to wherever your hand is by then
                float u = Mathf.Clamp01((now - boomerangThrown) / BoomerangFlight);
                Vector3 loop = boomerangForward * (BoomerangRange * Mathf.Sin(u * Mathf.PI))
                    - boomerangSide * (BoomerangWidth * Mathf.Sin(u * Mathf.PI * 2f))
                    + boomerangUp * (0.3f * Mathf.Sin(u * Mathf.PI));
                at = Vector3.Lerp(boomerangFrom, hand.position, u * u * (3f - 2f * u)) + loop * scale;

                boomerangSpin = (boomerangSpin + 1100f * dt) % 360f;
                turn = Quaternion.LookRotation(boomerangForward, boomerangUp) * Quaternion.Euler(0f, 0f, 18f) * Quaternion.Euler(0f, boomerangSpin, 0f);

                if (u >= 1f || (u > 0.7f && (at - hand.position).sqrMagnitude < 0.12f * 0.12f * scale * scale))
                    CatchBoomerangStar(hand.position);
            }
            else
            {
                // Waits in your hand, turning slowly
                at = hand.position + hand.forward * (0.08f * scale);
                boomerangSpin = (boomerangSpin + 90f * dt) % 360f;
                turn = hand.rotation * Quaternion.Euler(0f, boomerangSpin, 0f);
            }

            float size = 0.13f * scale;
            boomerangStar.Object.transform.SetPositionAndRotation(at, turn);
            boomerangStar.Object.transform.localScale = Vector3.one * size;
            SetColor(boomerangStar, Color.Lerp(Accent, Color.white, 0.2f));

            float pulse = 1f + Mathf.Sin(now * 6f) * 0.08f;
            boomerangGlow.Object.transform.position = at;
            boomerangGlow.Object.transform.localScale = Vector3.one * (size * 1.5f * pulse);
            SetAlpha(boomerangGlow, Second, boomerangFlying ? 0.3f : 0.15f);

            boomerangTrail.transform.position = at;
            boomerangTrail.widthMultiplier = 0.05f * scale;
            boomerangTrail.startColor = WithAlpha(Accent, 0.8f);
            boomerangTrail.endColor = WithAlpha(Second, 0f);
        }

        private static void ThrowBoomerangStar(Vector3 from, Vector3 aim, float now)
        {
            // The loop's own up and sideways, worked out from the way it was thrown
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, aim);
            if (up.sqrMagnitude < 0.01f)
                up = Vector3.ProjectOnPlane(-Head.forward, aim);
            if (up.sqrMagnitude < 0.0001f)
                up = Vector3.ProjectOnPlane(Vector3.forward, aim);

            boomerangForward = aim.normalized;
            boomerangUp = up.normalized;
            boomerangSide = Vector3.Cross(boomerangUp, boomerangForward);
            boomerangFrom = from;
            boomerangThrown = now;
            boomerangFlying = true;

            boomerangTrail.transform.position = from;
            boomerangTrail.Clear();
            boomerangTrail.emitting = true;
            Burst(from, 10, 1f, false, aim);
        }

        private static void CatchBoomerangStar(Vector3 at)
        {
            boomerangFlying = false;
            if (boomerangTrail != null)
                boomerangTrail.emitting = false;

            Burst(at, 14, 1.2f, false);
            GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.tapHapticStrength / 2f, 0.06f);
        }

        /// <summary>Takes away the boomerang star and its trail.</summary>
        public static void DisableBoomerangStar()
        {
            DiscardPiece(ref boomerangStar);
            DiscardPiece(ref boomerangGlow);
            if (boomerangTrail != null)
                Object.Destroy(boomerangTrail.gameObject);
            boomerangTrail = null;
            boomerangFlying = false;
        }

        private static Piece spinnerBody, spinnerCaps, spinnerBlur;
        private static Vector3 spinnerLastHand;
        private static float spinnerSpeed, spinnerAngle;

        /// <summary>A three-lobed fidget spinner balanced on your right fingertip; flick your hand sideways to spin it up, and it slowly winds down by itself.</summary>
        public static void FidgetSpinner()
        {
            float now = Time.time;
            float dt = Mathf.Max(Time.deltaTime, 0.001f);
            float scale = Scale;
            var hand = ControllerUtilities.GetTrueRightHand();

            // Measured against your body, so only a flick of the hand counts
            Vector3 relative = hand.position - BodyPosition;
            if (spinnerBody == null || !spinnerBody.Alive)
            {
                spinnerBody = MakeShaped(SpinnerMesh, Accent);
                spinnerLastHand = relative;
            }

            if (spinnerCaps == null || !spinnerCaps.Alive)
                spinnerCaps = MakeDecal(SpinnerCapsMesh, Second);

            // A faint copy just behind the spinner as it whirls, always drawn underneath it
            if (spinnerBlur == null || !spinnerBlur.Alive)
            {
                spinnerBlur = MakeShaped(SpinnerMesh, Accent);
                spinnerBlur.Renderer.sharedMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 1;
            }

            Vector3 velocity = Vector3.ClampMagnitude((relative - spinnerLastHand) / dt, 10f * scale);
            spinnerLastHand = relative;

            Vector3 side = Vector3.ProjectOnPlane(hand.right, Vector3.up);
            if (side.sqrMagnitude < 0.01f)
                side = Vector3.ProjectOnPlane(Head.right, Vector3.up);
            float flick = Vector3.Dot(velocity, side.normalized) / scale;

            // A quick sideways flick spins it up that way, and a flick back slows it; gentle moves do nothing
            if (Mathf.Abs(flick) > 1f)
                spinnerSpeed = Mathf.Clamp(spinnerSpeed + (flick - Mathf.Sign(flick)) * 1800f * dt, -2400f, 2400f);

            // The bearings wind it down by themselves, over the best part of a minute from full speed
            spinnerSpeed = Mathf.MoveTowards(spinnerSpeed, 0f, (10f + Mathf.Abs(spinnerSpeed) * 0.06f) * dt);
            spinnerAngle = (spinnerAngle + spinnerSpeed * dt) % 360f;

            // Wobbles on your fingertip as it slows
            float spin = Mathf.Abs(spinnerSpeed);
            float wobble = 7f * (1f - Mathf.Clamp01(spin / 600f));
            Quaternion tilt = Quaternion.AngleAxis(wobble, new Vector3(Mathf.Cos(now * 2.6f), 0f, Mathf.Sin(now * 2.6f)));
            Quaternion turn = tilt * Quaternion.Euler(0f, spinnerAngle, 0f);

            Vector3 centre = hand.position + hand.forward * (0.1f * scale) + Vector3.up * (0.008f * scale);
            Vector3 size = Vector3.one * (0.06f * scale);

            spinnerBody.Object.transform.SetPositionAndRotation(centre, turn);
            spinnerBody.Object.transform.localScale = size;
            SetColor(spinnerBody, Accent);

            spinnerCaps.Object.transform.SetPositionAndRotation(centre, turn);
            spinnerCaps.Object.transform.localScale = size;
            SetColor(spinnerCaps, Color.Lerp(Second, Color.white, 0.3f));

            float blur = Mathf.Clamp01((spin - 300f) / 1200f);
            ShowPiece(spinnerBlur, blur > 0.01f);
            if (blur <= 0.01f)
                return;

            float lag = Mathf.Sign(spinnerSpeed) * Mathf.Min(spin * 0.02f, 45f);
            spinnerBlur.Object.transform.SetPositionAndRotation(centre, tilt * Quaternion.Euler(0f, spinnerAngle - lag, 0f));
            spinnerBlur.Object.transform.localScale = size;
            SetAlpha(spinnerBlur, Accent, 0.35f * blur);
        }

        /// <summary>Takes away the fidget spinner.</summary>
        public static void DisableFidgetSpinner()
        {
            DiscardPiece(ref spinnerBody);
            DiscardPiece(ref spinnerCaps);
            DiscardPiece(ref spinnerBlur);
            spinnerSpeed = 0f;
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

        private static Mesh scopeLensMesh, balloonCraterMesh, pinwheelBladeMesh, boomerangStarMesh, spinnerMesh, spinnerCapsMesh;

        /// <summary>A round lens of radius one facing back along -Z, with the scope's picture laid square across it.</summary>
        private static Mesh ScopeLensMesh
        {
            get
            {
                if (scopeLensMesh != null)
                    return scopeLensMesh;

                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                AddToyDisc(vertices, triangles, Vector3.zero, Vector3.back, 1f, 32);

                // Seen from behind, the camera's right is the lens's right, so the picture isn't mirrored
                List<Vector2> uvs = new List<Vector2>(vertices.Count);
                foreach (Vector3 vertex in vertices)
                    uvs.Add(new Vector2(0.5f + vertex.x * 0.5f, 0.5f + vertex.y * 0.5f));

                scopeLensMesh = FinishMesh("EclipseScopeLens", vertices, triangles);
                scopeLensMesh.SetUVs(0, uvs);
                return scopeLensMesh;
            }
        }

        // Which way each of the moon balloon's craters faces, and in w how wide it is
        private static readonly Vector4[] BalloonCraters =
        {
            new Vector4(0.3f, 0.5f, 0.8f, 0.11f), new Vector4(-0.6f, 0.2f, 0.7f, 0.08f), new Vector4(0.7f, -0.3f, 0.6f, 0.07f),
            new Vector4(-0.2f, -0.6f, 0.75f, 0.09f), new Vector4(-0.8f, 0.4f, -0.3f, 0.1f), new Vector4(0.5f, 0.3f, -0.8f, 0.08f),
            new Vector4(0.1f, -0.2f, -0.95f, 0.12f), new Vector4(0.9f, 0.35f, -0.1f, 0.06f), new Vector4(-0.45f, -0.25f, -0.7f, 0.07f),
            new Vector4(0.05f, 0.95f, 0.2f, 0.07f)
        };

        /// <summary>Round craters just outside a sphere primitive, which is one unit across.</summary>
        /// <remarks>Each crater faces outwards only, so the ones round the back are never drawn through the balloon.</remarks>
        private static Mesh BalloonCraterMesh
        {
            get
            {
                if (balloonCraterMesh != null)
                    return balloonCraterMesh;

                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                foreach (Vector4 crater in BalloonCraters)
                {
                    Vector3 normal = new Vector3(crater.x, crater.y, crater.z).normalized;
                    AddToyDisc(vertices, triangles, normal * 0.503f, normal, crater.w, 12);
                }

                return balloonCraterMesh = FinishMesh("EclipseBalloonCraters", vertices, triangles);
            }
        }

        /// <summary>One blade of a paper pinwheel: a flat triangle out to a corner, with a flap curling back towards the pin along +Z.</summary>
        private static Mesh PinwheelBladeMesh
        {
            get
            {
                if (pinwheelBladeMesh != null)
                    return pinwheelBladeMesh;

                Vector3 pin = Vector3.zero;
                Vector3 corner = new Vector3(0.5f, 0.5f, 0.04f);
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                AddSheet(vertices, triangles, pin, new Vector3(0.5f, 0f, 0f), corner);
                AddSheet(vertices, triangles, pin, corner, new Vector3(0.1f, 0.14f, 0.2f));
                return pinwheelBladeMesh = FinishMesh("EclipsePinwheelBlade", vertices, triangles);
            }
        }

        /// <summary>A flat five-pointed star lying across Y, raised a little in the middle on both sides.</summary>
        private static Mesh BoomerangStarMesh
        {
            get
            {
                if (boomerangStarMesh != null)
                    return boomerangStarMesh;

                Vector3 top = new Vector3(0f, 0.09f, 0f);
                Vector3 bottom = new Vector3(0f, -0.09f, 0f);
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                for (int k = 0; k < 10; k++)
                {
                    // Points and the notches between them take turns round the edge
                    float a = k * Mathf.PI / 5f;
                    float b = (k + 1) * Mathf.PI / 5f;
                    float ra = k % 2 == 0 ? 0.5f : 0.21f;
                    float rb = k % 2 == 0 ? 0.21f : 0.5f;
                    Vector3 p = new Vector3(Mathf.Sin(a) * ra, 0f, Mathf.Cos(a) * ra);
                    Vector3 q = new Vector3(Mathf.Sin(b) * rb, 0f, Mathf.Cos(b) * rb);
                    AddTriangle(vertices, triangles, top, p, q, Vector3.up);
                    AddTriangle(vertices, triangles, bottom, p, q, Vector3.down);
                }

                return boomerangStarMesh = FinishMesh("EclipseBoomerangStar", vertices, triangles);
            }
        }

        // Half the spinner's thickness, with the whole spinner a little under two units across
        private const float SpinnerHalf = 0.1f;

        /// <summary>The fidget spinner's body: a round middle and three round lobes joined into one flat piece.</summary>
        private static Mesh SpinnerMesh
        {
            get
            {
                if (spinnerMesh != null)
                    return spinnerMesh;

                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                Vector3 up = Vector3.up * SpinnerHalf;

                // Walls go in first and faces last, so where the circles overlap the faces cover the walls inside
                for (int part = 0; part < 4; part++)
                {
                    SpinnerPart(part, out Vector3 centre, out float radius);
                    for (int slice = 0; slice < 24; slice++)
                    {
                        float a = slice * Mathf.PI / 12f;
                        float b = (slice + 1) * Mathf.PI / 12f;
                        Vector3 p = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                        Vector3 q = centre + new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * radius;
                        Vector3 outward = (p + q) * 0.5f - centre;
                        AddTriangle(vertices, triangles, p - up, q - up, q + up, outward);
                        AddTriangle(vertices, triangles, p - up, q + up, p + up, outward);
                    }
                }

                for (int part = 0; part < 4; part++)
                {
                    SpinnerPart(part, out Vector3 centre, out float radius);
                    AddToyDisc(vertices, triangles, centre - up, Vector3.down, radius, 24);
                }

                for (int part = 0; part < 4; part++)
                {
                    SpinnerPart(part, out Vector3 centre, out float radius);
                    AddToyDisc(vertices, triangles, centre + up, Vector3.up, radius, 24);
                }

                return spinnerMesh = FinishMesh("EclipseSpinner", vertices, triangles);
            }
        }

        /// <summary>The round bearing caps on both faces of the fidget spinner's middle and lobes.</summary>
        private static Mesh SpinnerCapsMesh
        {
            get
            {
                if (spinnerCapsMesh != null)
                    return spinnerCapsMesh;

                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                for (int part = 0; part < 4; part++)
                {
                    SpinnerPart(part, out Vector3 centre, out float radius);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 normal = new Vector3(0f, side, 0f);
                        AddToyDisc(vertices, triangles, centre + normal * (SpinnerHalf + 0.005f), normal, radius * 0.6f, 20);
                    }
                }

                return spinnerCapsMesh = FinishMesh("EclipseSpinnerCaps", vertices, triangles);
            }
        }

        // The spinner's middle, then its three lobes a third of a turn apart
        private static void SpinnerPart(int part, out Vector3 centre, out float radius)
        {
            if (part == 0)
            {
                centre = Vector3.zero;
                radius = 0.3f;
                return;
            }

            float angle = part * Mathf.PI * 2f / 3f;
            centre = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.55f;
            radius = 0.33f;
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

        // Adds a flat round disc facing `normal`
        private static void AddToyDisc(List<Vector3> vertices, List<int> triangles, Vector3 centre, Vector3 normal, float radius, int slices)
        {
            Vector3 u = Vector3.Cross(normal, Mathf.Abs(normal.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 v = Vector3.Cross(normal, u);
            for (int slice = 0; slice < slices; slice++)
            {
                float a = slice * Mathf.PI * 2f / slices;
                float b = (slice + 1) * Mathf.PI * 2f / slices;
                AddTriangle(vertices, triangles, centre,
                    centre + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * radius,
                    centre + (u * Mathf.Cos(b) + v * Mathf.Sin(b)) * radius,
                    normal);
            }
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
