/*
 * Eclipse Menu  Mods/EclipseMotion.cs
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

using Eclipse.Utilities;
using GorillaLocomotion;
using UnityEngine;
using static Eclipse.Menu.Main;
using static Eclipse.Mods.Custom;
using Random = UnityEngine.Random;

namespace Eclipse.Mods
{
    /// <summary>The third part of the Eclipse mods: the Space Movement set, new ways of getting about that move only you.</summary>
    public static partial class EclipseMods
    {
        // ── Space Movement ──────────────────────────────────────────────────────

        /// <summary>Forgets held buttons for the Space Movement mods, run whenever one of them is turned on.</summary>
        /// <remarks>A button already held while turning a mod on would otherwise count as a press.</remarks>
        public static void ResetMotion() =>
            chargeJumpHeld = zipHeld = orbitHeld = wellHeld = moonZoneHeld = jabHeld = strokeHeld = true;

        private const float FullCharge = 2.5f;
        private static bool chargeJumpHeld = true, jumpCharging;
        private static float jumpCharge, chargeLastGround = -10f;
        private static Piece chargeGlow;
        private static LineRenderer chargeRing, chargeHalo;

        /// <summary>Hold your left trigger on the ground to charge a jump and let go to leap, higher the longer you charged.</summary>
        public static void ChargeJump()
        {
            if (Extras.Grounded())
                chargeLastGround = Time.time;

            // A moment's grace, so the little hops of moving about don't count as leaving the ground
            bool onGround = Time.time - chargeLastGround < 0.2f;
            bool down = leftTrigger > 0.5f;

            // Holding the trigger as you land starts a charge too, but one already held when
            // the mod was turned on has to be let go of first.
            if (!down)
                chargeJumpHeld = false;
            else if (!chargeJumpHeld && !jumpCharging && onGround)
            {
                jumpCharging = true;
                jumpCharge = 0f;
            }

            if (!jumpCharging)
                return;

            if (down)
            {
                if (onGround)
                    jumpCharge = Mathf.Min(FullCharge, jumpCharge + Time.deltaTime);

                DrawCharge(jumpCharge / FullCharge);
                return;
            }

            float power = jumpCharge / FullCharge;
            bool ready = onGround && jumpCharge > 0.15f;
            ClearCharge();
            if (!ready)
                return;

            Vector3 velocity = Extras.Body.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, Mathf.Lerp(4.5f, 15f, power));
            Extras.Body.linearVelocity = velocity;
            Burst(Feet, Mathf.RoundToInt(Mathf.Lerp(10f, 60f, power)), Mathf.Lerp(1.5f, 4f, power), true, Vector3.up);
        }

        private static void DrawCharge(float power)
        {
            float scale = Scale;
            Vector3 centre = Feet + Vector3.up * (0.02f * scale);

            // Throbs once it is full, so you know holding on any longer won't help
            float throb = power >= 1f ? 1f + Mathf.Sin(Time.time * 14f) * 0.08f : 1f;
            float radius = Mathf.Lerp(0.12f, 0.5f, power) * scale * throb;

            Ensure(ref chargeGlow, PrimitiveType.Cylinder, Accent);
            chargeGlow.Object.transform.SetPositionAndRotation(centre, Quaternion.identity);
            chargeGlow.Object.transform.localScale = new Vector3(radius * 2f, 0.004f * scale, radius * 2f);
            SetAlpha(chargeGlow, Accent, Mathf.Lerp(0.08f, 0.4f, power));

            if (chargeRing == null)
                chargeRing = MakeRing("Eclipse_ChargeRing");
            if (chargeHalo == null)
                chargeHalo = MakeRing("Eclipse_ChargeHalo");

            PlaceRing(chargeRing, centre, Vector3.up, radius, 0.02f * scale, Accent);
            PlaceRing(chargeHalo, centre, Vector3.up, radius * 1.3f, 0.05f * scale, WithAlpha(Second, 0.4f * power));

            if (Random.value < 0.15f + power * 0.4f)
                Burst(centre, 1, 0.5f + power * 1.5f, false, Vector3.up);
        }

        private static void ClearCharge()
        {
            jumpCharging = false;
            jumpCharge = 0f;
            DiscardPiece(ref chargeGlow);
            DestroyLine(ref chargeRing);
            DestroyLine(ref chargeHalo);
        }

        public static void DisableChargeJump() => ClearCharge();

        private static bool zipHeld = true, zipStrung, zipRiding, zipForward, zipArrived;
        private static Vector3 zipFrom, zipTo;
        private static float zipSpeed;
        private static LineRenderer zipLine, zipGlow;
        private static Piece zipStarFrom, zipStarTo;

        /// <summary>Right trigger strings a glowing line to wherever your right hand points; hold your right grip on it to ride it to the far end.</summary>
        public static void Zipline()
        {
            float scale = Scale;
            if (Pressed(rightTrigger > 0.5f, ref zipHeld))
            {
                // Every pull takes the old line down; pointing at a surface strings the new one
                TakeDownZipline();

                var hand = ControllerUtilities.GetTrueRightHand();
                if (Physics.Raycast(hand.position, hand.forward, out RaycastHit hit, 40f * scale, GTPlayer.Instance.locomotionEnabledLayers) && hit.distance > 2f * scale)
                {
                    zipFrom = hand.position;
                    zipTo = hit.point;
                    zipStrung = true;
                    Burst(hit.point, 20, 2f, false);
                }
            }

            if (!zipStrung)
                return;

            DrawZipline(scale);

            Vector3 along = zipTo - zipFrom;
            float length = along.magnitude;
            Vector3 direction = along / length;
            Vector3 grip = RightHand;
            float travelled = Mathf.Clamp(Vector3.Dot(grip - zipFrom, direction), 0f, length);
            Vector3 closest = zipFrom + direction * travelled;
            float offLine = (grip - closest).sqrMagnitude;

            // Letting go, or being knocked well off the line, drops you with the speed you had
            if (!rightGrab || zipRiding && offLine > scale * scale)
            {
                zipRiding = false;
                return;
            }

            if (!zipRiding)
            {
                if (offLine > 0.3f * 0.3f * scale * scale)
                    return;

                // Off to whichever end is further away, starting at the speed you already had along it
                zipRiding = true;
                zipArrived = false;
                zipForward = travelled < length * 0.5f;
                zipSpeed = Mathf.Max(0f, Vector3.Dot(Extras.Body.linearVelocity, zipForward ? direction : -direction));
            }

            Vector3 heading = zipForward ? direction : -direction;
            float left = Mathf.Max(0f, (zipForward ? length - travelled : travelled) - 0.6f * scale);

            // Picks up speed smoothly to a cruise, then eases to a stop just short of the end
            zipSpeed = Mathf.MoveTowards(zipSpeed, 10f, 8f * Time.deltaTime);
            float speed = Mathf.Min(zipSpeed, left * 3f);

            // Set fresh every frame, so gravity has no say while you ride; the second part keeps your hand on the line
            Extras.Body.linearVelocity = heading * speed + Vector3.ClampMagnitude((closest - grip) * 10f, 5f);

            if (speed > 1f && Random.value < 0.4f)
                Burst(grip, 1, 1f, true, -heading);

            // Once per ride, on coming to rest at the far end
            if (!zipArrived && left < 0.05f * scale)
            {
                zipArrived = true;
                Eclipse.Menu.Main.EclipseUnlock("Zipliner");
            }
        }

        private static void DrawZipline(float scale)
        {
            if (zipLine == null)
                zipLine = MakeLine("Eclipse_Zipline", 0.015f);
            if (zipGlow == null)
                zipGlow = MakeLine("Eclipse_ZiplineGlow", 0.05f);

            zipLine.SetPosition(0, zipFrom);
            zipLine.SetPosition(1, zipTo);
            zipLine.startWidth = zipLine.endWidth = 0.015f * scale;
            zipLine.startColor = Accent;
            zipLine.endColor = Second;

            zipGlow.SetPosition(0, zipFrom);
            zipGlow.SetPosition(1, zipTo);
            zipGlow.startWidth = zipGlow.endWidth = (0.05f + Mathf.Sin(Time.time * 4f) * 0.01f) * scale;
            zipGlow.startColor = zipGlow.endColor = WithAlpha(Second, 0.25f);

            PlaceZipStar(ref zipStarFrom, zipFrom, scale, 0f);
            PlaceZipStar(ref zipStarTo, zipTo, scale, 45f);
        }

        private static void PlaceZipStar(ref Piece star, Vector3 at, float scale, float offset)
        {
            Transform body = EnsureStar(ref star, Color.white, 0.1f).Object.transform;
            body.position = at;
            body.rotation = Quaternion.Euler(0f, Time.time * 90f + offset, 0f);
            body.localScale = Vector3.one * (0.1f * scale);
            SetColor(star, Color.Lerp(Color.white, Accent, 0.5f));
        }

        private static void TakeDownZipline()
        {
            zipStrung = zipRiding = zipArrived = false;
            DestroyLine(ref zipLine);
            DestroyLine(ref zipGlow);
            DiscardPiece(ref zipStarFrom);
            DiscardPiece(ref zipStarTo);
        }

        public static void DisableZipline() => TakeDownZipline();

        private static float strafeAirTime;

        /// <summary>In the air, the way you are moving slowly bends round to the way you look, like strafing in an old shooter, without making you any faster.</summary>
        public static void AirStrafe()
        {
            if (Extras.Grounded() || GTPlayer.Instance.IsHandTouching(true) || GTPlayer.Instance.IsHandTouching(false))
            {
                strafeAirTime = 0f;
                return;
            }

            strafeAirTime += Time.deltaTime;

            Vector3 velocity = Extras.Body.linearVelocity;
            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            if (flat.sqrMagnitude < 1f)
                return;

            // Glancing back over your shoulder isn't a turn, so past this you carry straight on
            float angle = Vector3.SignedAngle(flat, LookFlat(), Vector3.up);
            if (Mathf.Abs(angle) > 110f)
                return;

            // Eases in over the first moments of a jump, so the little hops of running are left alone.
            // Turning the velocity round rather than adding to it keeps your speed exactly as it was.
            float most = 90f * Mathf.Clamp01(strafeAirTime / 0.3f) * Time.deltaTime;
            flat = Quaternion.AngleAxis(Mathf.Clamp(angle, -most, most), Vector3.up) * flat;
            Extras.Body.linearVelocity = new Vector3(flat.x, velocity.y, flat.z);
        }

        public static void DisableAirStrafe() => strafeAirTime = 0f;

        private const float OrbitMinSpeed = 3f;
        private static bool orbitHeld = true, orbiting;
        private static Vector3 orbitCentre, orbitAxis;
        private static float orbitRadius, orbitSpeed;
        private static LineRenderer orbitRing;
        private static Piece orbitStar;

        /// <summary>Hold your right grip to circle the spot where your right hand was, and let go to fly off the way you were heading.</summary>
        public static void OrbitMode()
        {
            float scale = Scale;
            if (Pressed(rightGrab, ref orbitHeld))
                StartOrbit(scale);

            if (!orbiting)
                return;

            Vector3 offset = Vector3.ProjectOnPlane(BodyPosition - orbitCentre, orbitAxis);
            float distance = offset.magnitude;
            Vector3 radial = distance > 0.001f
                ? offset / distance
                : Vector3.Cross(orbitAxis, Mathf.Abs(orbitAxis.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 tangent = Vector3.Cross(orbitAxis, radial);

            if (!rightGrab)
            {
                Extras.Body.linearVelocity = tangent * orbitSpeed;
                EndOrbit();
                return;
            }

            // Aims for where the circle carries you by next frame rather than straight along the
            // tangent, which would drift you outward; being off the circle is eased back gently.
            float step = Mathf.Max(Time.deltaTime, 0.001f);
            Vector3 onCircle = radial * orbitRadius;
            Vector3 next = Quaternion.AngleAxis(orbitSpeed * step / orbitRadius * Mathf.Rad2Deg, orbitAxis) * onCircle;
            Vector3 drift = orbitCentre + onCircle - BodyPosition;
            Extras.Body.linearVelocity = (next - onCircle) / step + Vector3.ClampMagnitude(drift * 8f, 6f);

            if (orbitRing == null)
                orbitRing = MakeRing("Eclipse_OrbitRing");
            PlaceRing(orbitRing, orbitCentre, orbitAxis, orbitRadius, 0.015f * scale, WithAlpha(Accent, 0.35f));

            Transform star = EnsureStar(ref orbitStar, Color.white, 0.08f).Object.transform;
            star.position = orbitCentre;
            star.rotation = Quaternion.LookRotation(orbitAxis) * Quaternion.Euler(0f, 0f, Time.time * 120f);
            star.localScale = Vector3.one * (0.08f * scale);
            SetColor(orbitStar, Second);
        }

        private static void StartOrbit(float scale)
        {
            Vector3 spot = RightHand;
            Vector3 offset = BodyPosition - spot;
            Vector3 velocity = Extras.Body.linearVelocity;
            Vector3 swing = Vector3.ProjectOnPlane(velocity, offset);

            if (swing.magnitude >= OrbitMinSpeed && offset.sqrMagnitude > 0.0001f)
                // Already moving: round in the plane you were moving in
                orbitAxis = Vector3.Cross(offset, swing).normalized;
            else
            {
                // Barely moving: round level with the ground, off to your right, or ahead if the spot is beside you
                Vector3 flat = new Vector3(offset.x, 0f, offset.z);
                if (flat.sqrMagnitude < 0.0001f)
                    flat = -LookFlat();

                Vector3 heading = Vector3.Cross(flat, Vector3.up);
                if (Vector3.Dot(heading, LookFlat()) < -0.2f)
                    heading = -heading;

                orbitAxis = Vector3.Cross(flat, heading).normalized;
            }

            // The circle runs through you, round the line through the spot along the axis
            orbitCentre = spot + orbitAxis * Vector3.Dot(offset, orbitAxis);
            orbitRadius = Mathf.Max(Vector3.ProjectOnPlane(offset, orbitAxis).magnitude, 0.75f * scale);
            orbitSpeed = Mathf.Max(velocity.magnitude, OrbitMinSpeed);
            orbiting = true;
            Burst(spot, 20, 1.5f, false);
        }

        private static void EndOrbit()
        {
            orbiting = false;
            DestroyLine(ref orbitRing);
            DiscardPiece(ref orbitStar);
        }

        public static void DisableOrbitMode() => EndOrbit();

        private static bool wellHeld = true;
        private static Vector3? wellPoint;
        private static Piece wellCore;
        private static LineRenderer wellSwirl, wellSwirlB;
        private static readonly Piece[] wellStars = new Piece[3];

        /// <summary>Y drops a little black hole where your right hand points that gently pulls you in while you are near it, and Y again takes it away.</summary>
        public static void GravityWell()
        {
            float scale = Scale;
            if (Pressed(leftSecondary, ref wellHeld))
            {
                if (wellPoint != null)
                    RemoveGravityWell();
                else
                {
                    // On a surface it floats just off it; with nothing in reach it hangs a few metres out
                    var hand = ControllerUtilities.GetTrueRightHand();
                    wellPoint = Physics.Raycast(hand.position, hand.forward, out RaycastHit hit, 30f * scale, GTPlayer.Instance.locomotionEnabledLayers)
                        ? hit.point + hit.normal * (0.4f * scale)
                        : hand.position + hand.forward * (5f * scale);
                    Burst(wellPoint.Value, 40, 3f, false);
                }
            }

            if (wellPoint == null)
                return;

            Vector3 at = wellPoint.Value;
            DrawGravityWell(at, scale);

            Vector3 offset = at - BodyPosition;
            float distance = offset.magnitude;
            float reach = 8f * scale, calm = 0.5f * scale;
            if (distance > reach)
                return;

            Vector3 velocity = Extras.Body.linearVelocity;
            if (distance > calm)
            {
                // Stronger the closer you are, up to a cap. It fades in at the edge of its reach and
                // just outside the calm middle, so neither edge gives you a jolt.
                float pull = Mathf.Min(12f, 16f * scale / distance)
                    * Mathf.Clamp01((reach - distance) / (1.5f * scale))
                    * Mathf.Clamp01((distance - calm) / (0.5f * scale));
                velocity += offset * (pull * Time.deltaTime / distance);
            }

            // A little drag close in, so you settle beside it instead of swinging through it for ever
            if (distance < 1.5f * scale)
                velocity *= 1f - Mathf.Clamp01(1.5f * Time.deltaTime);

            Extras.Body.linearVelocity = velocity;
        }

        private static void DrawGravityWell(Vector3 at, float scale)
        {
            float t = Time.time;
            Ensure(ref wellCore, PrimitiveType.Sphere, Color.black);
            wellCore.Object.transform.position = at;
            wellCore.Object.transform.localScale = Vector3.one * (0.3f * scale);
            SetColor(wellCore, new Color(0.01f, 0f, 0.03f, 1f));

            if (wellSwirl == null)
                wellSwirl = MakeRing("Eclipse_WellSwirl");
            if (wellSwirlB == null)
                wellSwirlB = MakeRing("Eclipse_WellSwirlB");

            Vector3 tilt = Quaternion.Euler(Mathf.Sin(t * 0.8f) * 18f, 0f, Mathf.Cos(t * 0.6f) * 18f) * Vector3.up;
            PlaceRing(wellSwirl, at, tilt, 0.28f * scale, 0.035f * scale, Accent, t * 160f);
            PlaceRing(wellSwirlB, at, Quaternion.Euler(50f, t * 30f, 0f) * Vector3.up, 0.4f * scale, 0.06f * scale, WithAlpha(Second, 0.4f), -t * 110f);

            // Little stars spiralling in over the edge, fading as they fall
            Quaternion disc = Quaternion.FromToRotation(Vector3.up, tilt);
            for (int i = 0; i < wellStars.Length; i++)
            {
                float fall = (t * 0.4f + i / (float)wellStars.Length) % 1f;
                float radius = Mathf.Lerp(0.9f, 0.16f, fall) * scale;
                float angle = t * 1.5f + i * Mathf.PI * 2f / wellStars.Length + fall * 7f;

                Piece star = EnsureStar(ref wellStars[i], Color.white, 0.06f);
                star.Object.transform.position = at + disc * new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                star.Object.transform.rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
                star.Object.transform.localScale = Vector3.one * (Mathf.Lerp(0.07f, 0.02f, fall) * scale);
                SetAlpha(star, Color.Lerp(Second, Accent, fall), Mathf.Sin(fall * Mathf.PI));
            }
        }

        private static void RemoveGravityWell()
        {
            wellPoint = null;
            DiscardPiece(ref wellCore);
            DestroyLine(ref wellSwirl);
            DestroyLine(ref wellSwirlB);
            for (int i = 0; i < wellStars.Length; i++)
                DiscardPiece(ref wellStars[i]);
        }

        public static void DisableGravityWell() => RemoveGravityWell();

        private const float WallSlideSpeed = 1.5f;

        // How flat a surface can be and still count as a wall: the up part of its normal stays under this
        private const float WallSlideSteep = 0.45f;
        private static float wallSlideSparks;

        /// <summary>Falling with a hand on or just against a wall slows you to a gentle slide down it, with sparks at your hand.</summary>
        public static void WallSlide()
        {
            Vector3 velocity = Extras.Body.linearVelocity;
            if (velocity.y > -WallSlideSpeed || Extras.Grounded())
                return;

            bool left = WallSlideTouch(true, out Vector3 leftPoint);
            bool right = WallSlideTouch(false, out Vector3 rightPoint);
            if (!left && !right)
                return;

            // Brakes hard but not all at once, so catching a wall mid-fall feels like friction rather than glue
            velocity.y = Mathf.MoveTowards(velocity.y, -WallSlideSpeed, 30f * Time.deltaTime);
            Extras.Body.linearVelocity = velocity;

            if (Time.time < wallSlideSparks)
                return;

            wallSlideSparks = Time.time + 0.06f;
            if (left)
                Burst(leftPoint, 2, 1.2f, true, Vector3.up);
            if (right)
                Burst(rightPoint, 2, 1.2f, true, Vector3.up);
        }

        private static bool WallSlideTouch(bool left, out Vector3 point)
        {
            GTPlayer player = GTPlayer.Instance;

            // The game already knows the surface a touching hand is on
            if (player.IsHandTouching(left))
            {
                RaycastHit touched = left ? player.LeftHandRef.lastHitInfo : player.RightHandRef.lastHitInfo;
                point = touched.point;
                return Mathf.Abs(touched.normal.y) < WallSlideSteep;
            }

            // A hand held just off the wall never counts as touching, so a short sweep from your side of
            // the hand out through it looks for one. The hand here is where the game keeps it, never inside a wall.
            float scale = Scale;
            Vector3 hand = player.GetHandPosition(left);
            Vector3 outward = hand - BodyPosition;
            outward.y = 0f;
            point = hand;
            if (outward.sqrMagnitude < 0.01f * scale * scale)
                return false;

            outward.Normalize();
            if (!Physics.SphereCast(hand - outward * (0.1f * scale), 0.05f * scale, outward, out RaycastHit hit, 0.18f * scale, player.locomotionEnabledLayers, QueryTriggerInteraction.Ignore))
                return false;

            point = hit.point;
            return Mathf.Abs(hit.normal.y) < WallSlideSteep && Vector3.Dot(hit.normal, outward) < -0.3f;
        }

        /// <summary>Forgets when Wall Slide last threw sparks.</summary>
        public static void DisableWallSlide() => wallSlideSparks = 0f;

        private const float MoonZoneRadius = 6f;
        private const int MoonZoneArcPoints = 25;
        private static bool moonZoneHeld = true;
        private static Vector3? moonZonePoint;
        private static float moonZoneSize;
        private static LineRenderer moonZoneEdge;
        private static readonly LineRenderer[] moonZoneRings = new LineRenderer[3];
        private static readonly LineRenderer[] moonZoneArcs = new LineRenderer[3];
        private static readonly Vector3[] moonZoneArcBuffer = new Vector3[MoonZoneArcPoints];

        /// <summary>X drops a big see-through dome where you stand with Moon gravity inside it; X again inside it picks it up, and outside it brings it to you.</summary>
        public static void MoonZone()
        {
            float scale = Scale;
            if (Pressed(leftPrimary, ref moonZoneHeld))
            {
                if (moonZonePoint != null && MoonZoneInside(scale, out _))
                {
                    Burst(moonZonePoint.Value, 30, 2f, false, Vector3.up);
                    RemoveMoonZone();
                }
                else
                    DropMoonZone(scale);
            }

            if (moonZonePoint == null)
                return;

            bool inside = MoonZoneInside(scale, out float depth);
            DrawMoonZone(moonZonePoint.Value, inside, scale);
            if (!inside || Extras.Grounded())
                return;

            // The same pull as Moon Gravity, faded in over the last half metre inside the edge so crossing it is no jolt
            float strength = Mathf.Clamp01(depth / (0.5f * scale));
            Extras.Body.linearVelocity -= Physics.gravity * (5f / 6f * strength * Time.deltaTime);
        }

        /// <summary>Whether you are inside the dome, and how far in from its edge.</summary>
        private static bool MoonZoneInside(float scale, out float depth)
        {
            Vector3 offset = BodyPosition - moonZonePoint.Value;
            depth = moonZoneSize - offset.magnitude;

            // A little below its floor still counts, for domes dropped on a slope
            return depth > 0f && offset.y > -scale;
        }

        private static void DropMoonZone(float scale)
        {
            // Rests on the ground under you; in mid-air it is centred on your feet
            moonZonePoint = Physics.Raycast(BodyPosition, Vector3.down, out RaycastHit hit, 3f * scale, GTPlayer.Instance.locomotionEnabledLayers, QueryTriggerInteraction.Ignore)
                ? hit.point
                : Feet;
            moonZoneSize = MoonZoneRadius * scale;
            Burst(moonZonePoint.Value, 40, 3f, false, Vector3.up);
        }

        private static void DrawMoonZone(Vector3 centre, bool inside, float scale)
        {
            float t = Time.time;
            float radius = moonZoneSize;

            if (moonZoneEdge == null)
                moonZoneEdge = MakeRing("Eclipse_MoonZoneEdge");
            PlaceRing(moonZoneEdge, centre + Vector3.up * (0.03f * scale), Vector3.up, radius, 0.06f * scale, WithAlpha(Accent, inside ? 0.7f : 0.4f));

            // Faint rings up the dome and arcs over the top, so you can see where it is and still see through it
            for (int i = 0; i < moonZoneRings.Length; i++)
            {
                if (moonZoneRings[i] == null)
                    moonZoneRings[i] = MakeRing("Eclipse_MoonZoneRing");

                float rise = (i + 1) * 22.5f * Mathf.Deg2Rad;
                float shimmer = 0.8f + Mathf.Sin(t * 1.5f + i * 1.3f) * 0.2f;
                PlaceRing(moonZoneRings[i], centre + Vector3.up * (Mathf.Sin(rise) * radius), Vector3.up, Mathf.Cos(rise) * radius, 0.03f * scale, WithAlpha(Second, 0.2f * shimmer));
            }

            for (int i = 0; i < moonZoneArcs.Length; i++)
            {
                if (moonZoneArcs[i] == null)
                {
                    moonZoneArcs[i] = MakeLine("Eclipse_MoonZoneArc", 0.025f);
                    moonZoneArcs[i].positionCount = MoonZoneArcPoints;
                }

                // Each arc runs from one side of the floor ring over the top to the other, turning slowly
                Vector3 across = Quaternion.Euler(0f, i * 60f + t * 4f, 0f) * Vector3.right;
                for (int p = 0; p < MoonZoneArcPoints; p++)
                {
                    float angle = p * Mathf.PI / (MoonZoneArcPoints - 1);
                    moonZoneArcBuffer[p] = centre + (across * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle)) * radius;
                }

                LineRenderer arc = moonZoneArcs[i];
                arc.SetPositions(moonZoneArcBuffer);
                arc.startWidth = arc.endWidth = 0.025f * scale;
                arc.startColor = arc.endColor = WithAlpha(Accent, inside ? 0.22f : 0.14f);
            }
        }

        private static void RemoveMoonZone()
        {
            moonZonePoint = null;
            DestroyLine(ref moonZoneEdge);
            for (int i = 0; i < moonZoneRings.Length; i++)
                DestroyLine(ref moonZoneRings[i]);
            for (int i = 0; i < moonZoneArcs.Length; i++)
                DestroyLine(ref moonZoneArcs[i]);
        }

        /// <summary>Takes the Moon Zone dome away.</summary>
        public static void DisableMoonZone() => RemoveMoonZone();

        /// <summary>Follows how fast a hand is moving next to your body, smoothed a little, for Comet Jab and Star Stroke.</summary>
        /// <remarks>Measured against your body, so flying along doesn't count as moving your hand.</remarks>
        private static void TrackSwing(Vector3 hand, ref Vector3 last, ref Vector3 velocity, bool fresh, float scale)
        {
            Vector3 offset = hand - BodyPosition;
            if (fresh)
                velocity = Vector3.zero;
            else
            {
                Vector3 moved = (offset - last) / Mathf.Max(Time.deltaTime, 0.001f);

                // Further in one frame than any arm can swing is a snap turn or a teleport, so that frame is left out
                float most = 15f * scale;
                if (moved.sqrMagnitude < most * most)
                    velocity = Vector3.Lerp(velocity, moved, Mathf.Clamp01(Time.deltaTime * 30f));
            }

            last = offset;
        }

        /// <summary>Adds speed along a heading without taking it past a top speed, and turns round any speed the other way first.</summary>
        private static void PushAlong(Vector3 heading, float boost, float topSpeed)
        {
            Vector3 velocity = Extras.Body.linearVelocity;
            float along = Vector3.Dot(velocity, heading);
            float target = Mathf.Max(along, Mathf.Min(Mathf.Max(along, 0f) + boost, topSpeed));
            Extras.Body.linearVelocity = velocity + heading * (target - along);
        }

        private const float JabPunch = 3f, JabBoost = 9f, JabTopSpeed = 16f;
        private static bool jabHeld = true, jabTracking;
        private static Vector3 jabLastHand, jabHandVelocity;
        private static float jabReady, jabTrailUntil;
        private static TrailRenderer jabTrail;

        /// <summary>Hold B and punch your right hand forward fast to launch yourself the way you punched, trailing a short comet tail.</summary>
        public static void CometJab()
        {
            float scale = Scale;
            TrackSwing(RightHand, ref jabLastHand, ref jabHandVelocity, !jabTracking, scale);
            jabTracking = true;

            // B already held when the mod was turned on has to be let go of first
            if (!rightSecondary)
                jabHeld = false;
            else if (!jabHeld && Time.time >= jabReady)
            {
                // A fast punch roughly the way you are looking; it carries you a little more towards your view than the punch itself
                Vector3 punch = jabHandVelocity;
                float speed = punch.magnitude;
                if (speed > JabPunch * scale && Vector3.Dot(punch, Head.forward) > speed * 0.6f)
                {
                    Vector3 heading = Vector3.Slerp(punch / speed, Head.forward, 0.35f).normalized;

                    // The cap stops a string of jabs stacking up for ever, and the wait between them stops one punch counting twice
                    PushAlong(heading, JabBoost, JabTopSpeed);
                    jabReady = Time.time + 0.6f;
                    jabTrailUntil = Time.time + 0.4f;
                    Burst(RightHand, 25, 3f, false, -heading);
                }
            }

            DrawJabTrail(scale);
        }

        private static void DrawJabTrail(float scale)
        {
            bool burning = Time.time < jabTrailUntil;
            if (jabTrail == null)
            {
                if (!burning)
                    return;

                jabTrail = new GameObject("Eclipse_JabTrail").AddComponent<TrailRenderer>();
                jabTrail.material = VertexColored;
                jabTrail.time = 0.35f;
                jabTrail.minVertexDistance = 0.05f;
                jabTrail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                jabTrail.transform.position = BodyPosition;
                jabTrail.Clear();
            }

            // Follows you all the time but only draws just after a jab, so the old tail fades out on its own
            jabTrail.transform.position = BodyPosition;
            jabTrail.emitting = burning;
            jabTrail.widthMultiplier = 0.25f * scale;
            jabTrail.startColor = WithAlpha(Accent, 0.85f);
            jabTrail.endColor = WithAlpha(Second, 0f);

            if (burning && Random.value < 0.5f)
                Burst(BodyPosition, 1, 0.6f, false);
        }

        /// <summary>Takes away Comet Jab's tail and forgets how your hand was moving.</summary>
        public static void DisableCometJab()
        {
            jabTracking = false;
            jabTrailUntil = 0f;
            if (jabTrail != null)
                Object.Destroy(jabTrail.gameObject);
            jabTrail = null;
        }

        private const float StrokePull = 1.8f, StrokeTopSpeed = 9f;
        private static bool strokeHeld = true, strokeTracking, strokeSweeping;
        private static Vector3 strokeLastLeft, strokeLastRight, strokeLeftVelocity, strokeRightVelocity;
        private static float strokeReady;

        /// <summary>Hold A in the air and sweep both hands back together like a breaststroke to push yourself forward a little with each stroke.</summary>
        public static void StarStroke()
        {
            float scale = Scale;
            TrackSwing(LeftHand, ref strokeLastLeft, ref strokeLeftVelocity, !strokeTracking, scale);
            TrackSwing(RightHand, ref strokeLastRight, ref strokeRightVelocity, !strokeTracking, scale);
            strokeTracking = true;

            // A already held when the mod was turned on has to be let go of first
            if (!rightPrimary)
                strokeHeld = false;

            GTPlayer player = GTPlayer.Instance;
            Vector3 together = strokeLeftVelocity + strokeRightVelocity;
            if (!rightPrimary || strokeHeld || Extras.Grounded() || player.IsHandTouching(true) || player.IsHandTouching(false) || together.sqrMagnitude < 0.01f)
            {
                strokeSweeping = false;
                return;
            }

            // The arms spreading apart cancel out, leaving the way both hands sweep together. The
            // weaker hand's speed that way is the stroke's strength, so one hand alone does nothing.
            Vector3 sweep = together.normalized;
            float weaker = Mathf.Min(Vector3.Dot(strokeLeftVelocity, sweep), Vector3.Dot(strokeRightVelocity, sweep));
            bool backward = Vector3.Dot(sweep, Head.forward) < -0.3f;

            // One push per stroke: the hands have to ease off before the next one counts
            if (!backward || weaker < StrokePull * scale * 0.5f)
            {
                strokeSweeping = false;
                return;
            }

            if (strokeSweeping || weaker < StrokePull * scale || Time.time < strokeReady)
                return;

            strokeSweeping = true;
            strokeReady = Time.time + 0.3f;
            PushAlong(-sweep, Mathf.Lerp(2.5f, 4f, Mathf.Clamp01((weaker - StrokePull * scale) / (2f * scale))), StrokeTopSpeed);
            Burst(LeftHand, 6, 1.2f, false, sweep);
            Burst(RightHand, 6, 1.2f, false, sweep);
        }

        /// <summary>Forgets how your hands were moving for Star Stroke.</summary>
        public static void DisableStarStroke() => strokeTracking = strokeSweeping = false;
    }
}
