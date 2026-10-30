/*
 * Eclipse Menu  Mods/EclipseArcade.cs
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
using GorillaLocomotion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Eclipse.Menu.Main;
using static Eclipse.Mods.Custom;
using Random = UnityEngine.Random;

namespace Eclipse.Mods
{
    /// <summary>Another part of the Eclipse mods: Space Games you play on your own, which only you can see.</summary>
    /// <remarks>
    /// Target Pop, Gravity Golf, Constellation Connect and Star Bowling. Each keeps its best score
    /// between sessions. Everything is drawn with your own pieces, which have no colliders, and every
    /// ball and pin is moved by hand here rather than by the game's physics.
    /// </remarks>
    public static partial class EclipseMods
    {
        // ── Space Games: shared ─────────────────────────────────────────────────

        /// <summary>Forgets held buttons, run whenever one of the Space Games with a button is turned on.</summary>
        /// <remarks>A button already held while turning a game on would otherwise count as a press.</remarks>
        public static void ResetArcade()
        {
            bowlingHeld = true;
        }

        // Hand-tap sounds, which only you hear
        private const int ArcadeClick = 66;
        private const int ArcadeTick = 148;
        private const int ArcadeDing = 244;
        private const int ArcadeChime = 213;

        private static void ArcadeSound(int sound, bool left, float volume)
        {
            if (VRRig.LocalRig != null)
                VRRig.LocalRig.PlayHandTapLocal(sound, left, volume);
        }

        private static void ArcadeBuzz(bool left, float strength, float duration)
        {
            if (GorillaTagger.Instance != null)
                GorillaTagger.Instance.StartVibration(left, GorillaTagger.Instance.tapHapticStrength * strength, duration);
        }

        /// <summary>Saves a new best score so it is still there next time you play.</summary>
        private static void SaveArcadeBest(string key, int best)
        {
            PlayerPrefs.SetInt(key, best);
            PlayerPrefs.Save();
        }

        private static void SaveArcadeBestTime(string key, float best)
        {
            PlayerPrefs.SetFloat(key, best);
            PlayerPrefs.Save();
        }

        /// <summary>Floats a game's scoreboard at a spot, sized so it reads the same however far away it is.</summary>
        private static void PlaceArcadeText(TextMeshPro text, Vector3 at, float scale)
        {
            float distance = Vector3.Distance(at, Head.position) / scale;
            PlaceToyText(text, at, Mathf.Clamp(distance * 0.045f, 0.06f, 0.5f));
        }

        // How close a point comes to the path between two others, squared. A fast hand or ball can
        // jump right past something between one frame and the next, so the whole path is checked.
        private static float ArcadePathDistance(Vector3 from, Vector3 to, Vector3 point)
        {
            Vector3 path = to - from;
            float length = path.sqrMagnitude;
            float along = length > 0.000001f ? Mathf.Clamp01(Vector3.Dot(point - from, path) / length) : 0f;
            return (from + path * along - point).sqrMagnitude;
        }

        /// <summary>
        /// Moves a ball for one frame: it falls when in the air, then rolls along the ground following
        /// its height, runs down slopes, slows with friction and bounces off walls.
        /// </summary>
        /// <param name="at">The middle of the ball.</param>
        /// <param name="friction">How much speed it loses each second while rolling.</param>
        /// <param name="slope">How strongly slopes pull it downhill; nought for none.</param>
        /// <returns>Whether it bumped into something, for a sound.</returns>
        private static bool ArcadeRoll(ref Vector3 at, ref Vector3 velocity, ref bool flying, float radius, float friction, float slope, float dt, float scale, int layers)
        {
            bool bumped = false;
            if (flying)
                velocity += Vector3.down * (9.81f * scale * dt);
            else
            {
                velocity.y = 0f;
                float speed = velocity.magnitude;
                if (speed > 0f)
                    velocity *= Mathf.Max(0f, speed - friction * dt) / speed;
            }

            Vector3 move = velocity * dt;
            float distance = move.magnitude;
            if (distance > 0.00001f)
            {
                Vector3 heading = move / distance;
                if (!Physics.SphereCast(at, radius * 0.9f, heading, out RaycastHit hit, distance, layers, QueryTriggerInteraction.Ignore))
                    at += move;
                else if (hit.normal.y < 0.6f)
                {
                    // Bounces off walls, losing some of its speed into them
                    Vector3 wall = new Vector3(hit.normal.x, 0f, hit.normal.z);
                    float into = wall.sqrMagnitude > 0.0001f ? Vector3.Dot(velocity, wall.normalized) : 0f;
                    if (into < 0f)
                    {
                        at += heading * Mathf.Max(0f, hit.distance - 0.002f * scale);
                        velocity -= wall.normalized * (into * 1.6f);
                        bumped = true;
                    }
                    else
                        at += move;
                }
                else if (flying)
                {
                    // Lands, with a little hop if it came down hard
                    at = hit.point + hit.normal * radius;
                    velocity.x *= 0.9f;
                    velocity.z *= 0.9f;
                    if (velocity.y < -2.5f * scale)
                    {
                        velocity.y *= -0.3f;
                        bumped = true;
                    }
                    else
                    {
                        velocity.y = 0f;
                        flying = false;
                    }
                }
                else
                    at += move;
            }

            if (flying)
                return bumped;

            // Sits on the ground wherever it rolls, and drops off when it runs over an edge
            float above = Mathf.Max(radius * 2f, 0.12f * scale);
            if (Physics.Raycast(at + Vector3.up * above, Vector3.down, out RaycastHit floor, above + radius + 0.15f * scale, layers, QueryTriggerInteraction.Ignore) && floor.normal.y >= 0.6f)
            {
                at.y = floor.point.y + radius;
                if (slope > 0f)
                    velocity += new Vector3(floor.normal.x, 0f, floor.normal.z) * (9.81f * slope * scale * dt);
            }
            else
                flying = true;

            return bumped;
        }

        // ── Target Pop ──────────────────────────────────────────────────────────

        private enum TargetsStage { Waiting, Playing, Over }

        private enum TargetsState { Free, Live, Popped, Missed }

        private sealed class PopTarget
        {
            public Piece Face, Eye;
            public LineRenderer Rim, Band;
            public Vector3 At;
            public float Born, Life, Gone;
            public TargetsState State;
        }

        private const string TargetsBestKey = "Eclipse_TargetPop_Best";
        private const float TargetsRoundTime = 60f;
        private const float TargetsRadius = 0.11f;
        private const float TargetsComboTime = 1f;
        private const int TargetsMaxCombo = 4;
        private static readonly PopTarget[] targetsPool = new PopTarget[6];
        private static Piece targetsStar;
        private static LineRenderer targetsStarRing;
        private static TextMeshPro targetsText;
        private static TargetsStage targetsStage;
        private static Vector3 targetsFacing = Vector3.forward;
        private static int targetsScore, targetsCombo = 1, targetsBest = -1, targetsShownScore = -1, targetsShownLeft = -1, targetsShownCombo = -1;
        private static float targetsStarted, targetsNext, targetsLastPop = -10f, targetsOver = -10f;
        private static bool targetsStarArmed;

        /// <summary>A sixty-second round of glowing targets popping up around you; hit them with your hands to pop them, scoring more for quick hits and quick runs.</summary>
        public static void TargetPop()
        {
            float now = Time.time;
            float dt = Time.deltaTime;
            float scale = Scale;
            Vector3 head = Head.position;
            Vector3 left = LeftHand;
            Vector3 right = RightHand;

            if (targetsText == null)
            {
                targetsText = MakeWristText("Eclipse_TargetPop", Color.white);
                targetsShownScore = targetsShownLeft = targetsShownCombo = -1;
                targetsFacing = LookFlat();
                targetsStarArmed = false;
                if (targetsBest < 0)
                    targetsBest = PlayerPrefs.GetInt(TargetsBestKey, 0);
            }

            // Turns slowly to follow where you look, so the targets and the score stay in front of you
            // without swinging round every time you glance at a target off to one side
            Vector3 facing = Vector3.Slerp(targetsFacing, LookFlat(), 1f - Mathf.Exp(-1.5f * dt));
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.0001f)
                targetsFacing = facing.normalized;

            if (targetsStage == TargetsStage.Playing)
            {
                float progress = Mathf.Clamp01((now - targetsStarted) / TargetsRoundTime);
                if (now - targetsStarted >= TargetsRoundTime)
                    EndTargetPop(now);
                else if (now >= targetsNext)
                    SpawnTargets(now, head, scale, progress);
            }

            for (int i = 0; i < targetsPool.Length; i++)
                UpdatePopTarget(targetsPool[i], now, head, left, right, scale);

            UpdateTargetsStar(now, head, left, right, scale);
            ShowTargetsText(now, head, scale);
        }

        private static void SpawnTargets(float now, Vector3 head, float scale, float progress)
        {
            int live = 0;
            foreach (PopTarget target in targetsPool)
                if (target != null && target.State == TargetsState.Live)
                    live++;

            // One at a time or two, and two more often as the round goes on
            if (live == 0)
            {
                SpawnPopTarget(now, head, scale, progress);
                if (Random.value < 0.2f + 0.4f * progress)
                    SpawnPopTarget(now, head, scale, progress);
            }
            else if (live == 1 && Random.value < 0.35f + 0.4f * progress)
                SpawnPopTarget(now, head, scale, progress);

            targetsNext = now + Random.Range(0.35f, 0.8f) * (1f - 0.35f * progress);
        }

        private static void SpawnPopTarget(float now, Vector3 head, float scale, float progress)
        {
            PopTarget target = null;
            for (int i = 0; i < targetsPool.Length && target == null; i++)
            {
                if (targetsPool[i] == null)
                    targetsPool[i] = new PopTarget();
                if (targetsPool[i].State == TargetsState.Free)
                    target = targetsPool[i];
            }

            if (target == null)
                return;

            // Within reach, in front of you or off to either side, high or low, and clear of the other targets
            Vector3 at = head;
            float lowest = Feet.y + 0.3f * scale;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector3 flat = Quaternion.Euler(0f, Random.Range(-80f, 80f), 0f) * targetsFacing;
                float rise = Random.Range(-40f, 15f) * Mathf.Deg2Rad;
                at = head + (flat * Mathf.Cos(rise) + Vector3.up * Mathf.Sin(rise)) * (Random.Range(0.45f, 1.1f) * scale);
                at.y = Mathf.Max(at.y, lowest);
                if (PopTargetClear(at, target, scale))
                    break;
            }

            target.At = at;
            target.Born = now;
            target.Life = Mathf.Lerp(2.4f, 1.6f, progress);
            target.State = TargetsState.Live;
            Burst(at, 4, 0.3f, false);
        }

        private static bool PopTargetClear(Vector3 at, PopTarget self, float scale)
        {
            float room = 0.32f * scale;
            foreach (PopTarget other in targetsPool)
                if (other != null && other != self && other.State == TargetsState.Live && (other.At - at).sqrMagnitude < room * room)
                    return false;
            return true;
        }

        private static void UpdatePopTarget(PopTarget target, float now, Vector3 head, Vector3 left, Vector3 right, float scale)
        {
            if (target == null)
                return;

            if (target.State == TargetsState.Free)
            {
                ShowPiece(target.Face, false);
                ShowPiece(target.Eye, false);
                if (target.Rim != null)
                    target.Rim.enabled = false;
                if (target.Band != null)
                    target.Band.enabled = false;
                return;
            }

            float radius = TargetsRadius * scale;
            float size = 1f;
            float alpha = 1f;
            switch (target.State)
            {
                case TargetsState.Live:
                    float reach = radius + 0.04f * scale;
                    bool hitLeft = (target.At - left).sqrMagnitude < reach * reach;
                    if (hitLeft || (target.At - right).sqrMagnitude < reach * reach)
                    {
                        HitPopTarget(target, now, hitLeft);
                        break;
                    }

                    float age = now - target.Born;
                    if (age > target.Life)
                    {
                        target.State = TargetsState.Missed;
                        target.Gone = now;
                        break;
                    }

                    // Springs up as it appears, and flickers when it is about to go
                    float grow = Mathf.Clamp01(age / 0.18f);
                    size = Mathf.Sin(grow * Mathf.PI * 0.5f) * (1f + 0.25f * Mathf.Sin(grow * Mathf.PI));
                    if (target.Life - age < 0.5f)
                        alpha = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(now * 18f));
                    break;
            }

            float since = now - target.Gone;
            if ((target.State == TargetsState.Popped && since > 0.25f) || (target.State == TargetsState.Missed && since > 0.4f))
            {
                target.State = TargetsState.Free;
                UpdatePopTarget(target, now, head, left, right, scale);
                return;
            }

            if (target.State == TargetsState.Missed)
            {
                alpha = 1f - since / 0.4f;
                size = 1f - 0.4f * since / 0.4f;
            }

            if (target.Face == null || !target.Face.Alive)
                target.Face = MakePiece(PrimitiveType.Cylinder, Color.white, Vector3.one);
            if (target.Eye == null || !target.Eye.Alive)
            {
                target.Eye = MakePiece(PrimitiveType.Cylinder, Color.white, Vector3.one);
                target.Eye.Renderer.sharedMaterial.renderQueue = DecalQueue;
            }

            if (target.Rim == null)
                target.Rim = MakeRing("Eclipse_TargetPopRim");
            if (target.Band == null)
                target.Band = MakeRing("Eclipse_TargetPopBand");

            Vector3 facing = head - target.At;
            facing = facing.sqrMagnitude > 0.0001f ? facing.normalized : -targetsFacing;
            Quaternion turn = Quaternion.FromToRotation(Vector3.up, facing);
            float r = radius * size;

            // A popped target is just its rim, flying outwards as it fades
            bool popped = target.State == TargetsState.Popped;
            ShowPiece(target.Face, !popped);
            ShowPiece(target.Eye, !popped);
            target.Rim.enabled = true;
            target.Band.enabled = !popped;
            if (popped)
            {
                float k = since / 0.25f;
                PlaceRing(target.Rim, target.At, facing, r * (1f + 1.5f * k), 0.012f * scale * (1f - k), WithAlpha(Color.Lerp(Accent, Color.white, 0.5f), 1f - k));
                return;
            }

            target.Face.Object.transform.SetPositionAndRotation(target.At, turn);
            target.Face.Object.transform.localScale = new Vector3(r * 2f, 0.003f * scale, r * 2f);
            SetAlpha(target.Face, Accent, 0.35f * alpha);

            target.Eye.Object.transform.SetPositionAndRotation(target.At + facing * (0.004f * scale), turn);
            target.Eye.Object.transform.localScale = new Vector3(r * 0.7f, 0.003f * scale, r * 0.7f);
            SetAlpha(target.Eye, Color.Lerp(Second, Color.white, 0.3f), 0.95f * alpha);

            PlaceRing(target.Rim, target.At, facing, r, 0.012f * scale, WithAlpha(Accent, alpha));
            PlaceRing(target.Band, target.At + facing * (0.002f * scale), facing, r * 0.62f, 0.007f * scale, WithAlpha(Color.Lerp(Accent, Color.white, 0.5f), 0.8f * alpha));
        }

        private static void HitPopTarget(PopTarget target, float now, bool left)
        {
            // Quick hits are worth more, and popping the next within a second builds up a multiplier
            targetsCombo = now - targetsLastPop <= TargetsComboTime ? Mathf.Min(targetsCombo + 1, TargetsMaxCombo) : 1;
            targetsLastPop = now;
            float quick = 1f - Mathf.Clamp01((now - target.Born) / target.Life);
            targetsScore += Mathf.RoundToInt(10f + 15f * quick) * targetsCombo;

            target.State = TargetsState.Popped;
            target.Gone = now;
            Burst(target.At, 14 + 6 * targetsCombo, 1.6f, false);
            ArcadeSound(ArcadeClick, left, 0.35f);
            ArcadeBuzz(left, 0.6f, 0.05f);
        }

        private static Vector3 TargetsStarSpot(Vector3 head, float scale) =>
            head + (targetsFacing * 0.45f + Vector3.down * 0.1f) * scale;

        // The star you touch to start a round, there before the first and a moment after each one ends
        private static void UpdateTargetsStar(float now, Vector3 head, Vector3 left, Vector3 right, float scale)
        {
            bool shown = targetsStage == TargetsStage.Waiting || (targetsStage == TargetsStage.Over && now - targetsOver > 1.5f);
            if (!shown)
            {
                ShowPiece(targetsStar, false);
                if (targetsStarRing != null)
                    targetsStarRing.enabled = false;
                return;
            }

            Piece star = EnsureStar(ref targetsStar, Color.white, 0.09f);
            ShowPiece(star, true);
            Vector3 at = TargetsStarSpot(head, scale);
            float pulse = Mathf.Sin(now * 4f);
            star.Object.transform.SetPositionAndRotation(at, Quaternion.Euler(now * 25f, now * 70f, 0f));
            star.Object.transform.localScale = Vector3.one * (0.09f * scale * (1f + 0.12f * pulse));
            SetColor(star, Color.Lerp(Accent, Color.white, 0.3f + 0.2f * pulse));

            if (targetsStarRing == null)
                targetsStarRing = MakeRing("Eclipse_TargetPopStart");
            targetsStarRing.enabled = true;
            PlaceRing(targetsStarRing, at, head - at, 0.075f * scale * (1f + 0.15f * pulse), 0.005f * scale, WithAlpha(Second, 0.7f), now * 40f);

            // Only a hand reaching in counts, not one that was already resting where the star appeared
            float reach = 0.1f * scale;
            bool touchedLeft = (left - at).sqrMagnitude < reach * reach;
            if (!touchedLeft && (right - at).sqrMagnitude >= reach * reach)
                targetsStarArmed = true;
            else if (targetsStarArmed)
                StartTargetPop(now, at, touchedLeft);
        }

        private static void StartTargetPop(float now, Vector3 at, bool left)
        {
            targetsStage = TargetsStage.Playing;
            targetsStarted = now;
            targetsNext = now + 0.6f;
            targetsScore = 0;
            targetsCombo = 1;
            targetsLastPop = -10f;
            targetsShownScore = targetsShownLeft = targetsShownCombo = -1;

            Burst(at, 30, 2f, false);
            ArcadeSound(ArcadeChime, left, 0.35f);
            ArcadeBuzz(left, 0.6f, 0.08f);
        }

        private static void EndTargetPop(float now)
        {
            targetsStage = TargetsStage.Over;
            targetsOver = now;
            targetsStarArmed = false;
            foreach (PopTarget target in targetsPool)
            {
                if (target == null || target.State != TargetsState.Live)
                    continue;

                target.State = TargetsState.Missed;
                target.Gone = now;
            }

            bool best = targetsScore > targetsBest;
            if (best)
            {
                targetsBest = targetsScore;
                SaveArcadeBest(TargetsBestKey, targetsBest);
            }

            targetsText.SafeSetText($"Time!  {targetsScore}\n<size=55%>best {targetsBest}{(best ? "  ·  new best!" : "")}\ntouch the star to play again</size>");
            targetsText.color = Accent;
            targetsShownLeft = -3;
            ArcadeSound(ArcadeDing, false, 0.4f);
        }

        private static void ShowTargetsText(float now, Vector3 head, float scale)
        {
            bool playing = targetsStage == TargetsStage.Playing;

            // The countdown and score float above the targets; otherwise the text sits over the start star
            Vector3 at = playing
                ? head + (targetsFacing * 0.95f + Vector3.up * 0.5f) * scale
                : TargetsStarSpot(head, scale) + Vector3.up * (0.2f * scale);
            PlaceToyText(targetsText, at, playing ? 0.08f : 0.06f);

            if (targetsStage == TargetsStage.Waiting && targetsShownLeft != -2)
            {
                targetsShownLeft = -2;
                targetsText.SafeSetText($"Target Pop\n<size=55%>touch the star to start  ·  best {targetsBest}</size>");
                targetsText.color = Accent;
            }

            if (!playing)
                return;

            int left = Mathf.CeilToInt(TargetsRoundTime - (now - targetsStarted));
            int combo = now - targetsLastPop <= TargetsComboTime ? targetsCombo : 1;
            if (left == targetsShownLeft && targetsScore == targetsShownScore && combo == targetsShownCombo)
                return;

            // A tick for each of the last five seconds
            if (left != targetsShownLeft && left <= 5 && targetsShownLeft > 0)
                ArcadeSound(ArcadeTick, false, 0.25f);

            targetsShownLeft = left;
            targetsShownScore = targetsScore;
            targetsShownCombo = combo;
            targetsText.SafeSetText(combo > 1
                ? $"{targetsScore}\n<size=55%>{left} s left  ·  x{combo}</size>"
                : $"{targetsScore}\n<size=55%>{left} s left</size>");
            targetsText.color = Color.Lerp(Color.white, Accent, 0.35f);
        }

        /// <summary>Takes away Target Pop's targets, start star and scoreboard.</summary>
        public static void DisableTargetPop()
        {
            foreach (PopTarget target in targetsPool)
            {
                if (target == null)
                    continue;

                DiscardPiece(ref target.Face);
                DiscardPiece(ref target.Eye);
                DestroyLine(ref target.Rim);
                DestroyLine(ref target.Band);
                target.State = TargetsState.Free;
            }

            DiscardPiece(ref targetsStar);
            DestroyLine(ref targetsStarRing);
            DestroyText(ref targetsText);
            targetsStage = TargetsStage.Waiting;
            targetsScore = 0;
            targetsCombo = 1;
            targetsStarArmed = false;
        }

        // ── Gravity Golf ────────────────────────────────────────────────────────

        private enum GolfStage { Finding, Aiming, Rolling, Sinking, Sunk }

        private const string GolfBestKey = "Eclipse_GravityGolf_Best";
        private const float GolfBallRadius = 0.035f;
        private const float GolfHandRadius = 0.07f;
        private const float GolfHoleRadius = 0.09f;
        private const float GolfWellRadius = 0.4f;
        private const float GolfPoleHeight = 0.8f;
        private static Piece golfBall, golfGlow, golfHole, golfPole, golfFlag, golfTop;
        private static LineRenderer golfRim, golfWell;
        private static TextMeshPro golfText;
        private static GolfStage golfStage;
        private static Vector3 golfAt, golfVelocity, golfFrom, golfHoleAt, golfLane = Vector3.forward;
        private static Vector3 golfLastLeft, golfLastRight, golfLeftVelocity, golfRightVelocity;
        private static float golfStageAt, golfLastHit = -10f, golfNextTry, golfOut = -10f;
        private static int golfStrokes, golfHoles, golfBest = -1, golfToGo;
        private static bool golfFlying, golfLipped, golfDirty = true, golfOutShown, golfNoGround, golfNewBest;

        /// <summary>A little hole with a glowing flag on the ground ahead and a ball at your feet; swing a hand through the ball to putt it, and sink it in as few strokes as you can.</summary>
        public static void GravityGolf()
        {
            float now = Time.time;
            float dt = Mathf.Max(Time.deltaTime, 0.001f);
            float scale = Scale;
            int layers = GTPlayer.Instance.locomotionEnabledLayers;
            Vector3 left = LeftHand;
            Vector3 right = RightHand;

            if (golfText == null)
            {
                golfText = MakeWristText("Eclipse_GravityGolf", Color.white);
                golfLastLeft = left;
                golfLastRight = right;
                golfLeftVelocity = golfRightVelocity = Vector3.zero;
                if (golfBest < 0)
                    golfBest = PlayerPrefs.GetInt(GolfBestKey, 0);
                FindGolfHole(now);
            }

            // Hand speeds, smoothed a little so one jittery frame doesn't send the ball flying
            golfLeftVelocity = Vector3.Lerp(golfLeftVelocity, (left - golfLastLeft) / dt, 0.5f);
            golfRightVelocity = Vector3.Lerp(golfRightVelocity, (right - golfLastRight) / dt, 0.5f);
            Vector3 lastLeft = golfLastLeft;
            Vector3 lastRight = golfLastRight;
            golfLastLeft = left;
            golfLastRight = right;

            // Wandered far away from the hole and the ball: a new hole is found wherever you are now
            float far = 30f * scale;
            if (golfStage != GolfStage.Finding && (golfHoleAt - BodyPosition).sqrMagnitude > far * far && (golfAt - BodyPosition).sqrMagnitude > far * far)
                FindGolfHole(now);

            switch (golfStage)
            {
                case GolfStage.Finding:
                    if (now >= golfNextTry && !PlaceGolfHole(now, scale, layers))
                    {
                        golfNextTry = now + 2.5f;
                        if (!golfNoGround)
                            golfNoGround = golfDirty = true;
                    }
                    break;

                case GolfStage.Aiming:
                    if (!SwingGolfBall(lastLeft, left, golfLeftVelocity, true, now, scale))
                        SwingGolfBall(lastRight, right, golfRightVelocity, false, now, scale);
                    break;

                case GolfStage.Rolling:
                    RollGolfBall(now, dt, scale, layers);
                    break;

                case GolfStage.Sinking:
                    if (now - golfStageAt > 0.35f)
                        SinkGolfBall(now, scale);
                    break;

                case GolfStage.Sunk:
                    if (now - golfStageAt > 3.5f)
                        FindGolfHole(now);
                    break;
            }

            DrawGolf(now, scale);
            ShowGolfText(now, scale);
        }

        private static void FindGolfHole(float now)
        {
            golfStage = GolfStage.Finding;
            golfNextTry = now;
            golfNoGround = false;
            golfDirty = true;
        }

        // Looks for fairly flat ground a few metres ahead for the hole, and puts the ball down just in front of your feet
        private static bool PlaceGolfHole(float now, float scale, int layers)
        {
            Vector3 look = LookFlat();
            if (!Physics.Raycast(Feet + (look * 0.35f + Vector3.up * 0.6f) * scale, Vector3.down, out RaycastHit tee, 1.6f * scale, layers, QueryTriggerInteraction.Ignore))
                return false;

            Vector3 lift = Vector3.up * (0.15f * scale);
            for (int attempt = 0; attempt < 16; attempt++)
            {
                Vector3 heading = Quaternion.Euler(0f, Random.Range(-50f, 50f), 0f) * look;
                Vector3 spot = tee.point + heading * (Random.Range(4f, 10f) * scale);
                if (!Physics.Raycast(spot + Vector3.up * (3f * scale), Vector3.down, out RaycastHit ground, 6f * scale, layers, QueryTriggerInteraction.Ignore))
                    continue;

                if (ground.normal.y < 0.95f || Mathf.Abs(ground.point.y - tee.point.y) > 1.5f * scale || !GolfFlatAround(ground.point, scale, layers))
                    continue;

                // The first tries also want a clear run along the ground from the ball to the hole
                if (attempt < 10 && Physics.Linecast(tee.point + lift, ground.point + lift, layers, QueryTriggerInteraction.Ignore))
                    continue;

                golfHoleAt = ground.point;
                golfLane = heading;
                golfAt = golfFrom = tee.point + Vector3.up * (GolfBallRadius * scale);
                golfVelocity = Vector3.zero;
                golfFlying = golfLipped = golfNoGround = golfNewBest = false;
                golfStrokes = 0;
                golfHoles++;
                golfStage = GolfStage.Aiming;
                golfStageAt = now;
                golfToGo = GolfToGo(scale);
                golfDirty = true;

                Burst(golfHoleAt + Vector3.up * (0.3f * scale), 20, 1.2f, false);
                Burst(golfAt, 10, 0.6f, false);
                return true;
            }

            return false;
        }

        private static bool GolfFlatAround(Vector3 centre, float scale, int layers)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f;
                Vector3 spot = centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (0.3f * scale);
                if (!Physics.Raycast(spot + Vector3.up * (0.4f * scale), Vector3.down, out RaycastHit ground, 0.8f * scale, layers, QueryTriggerInteraction.Ignore)
                    || ground.normal.y < 0.9f || Mathf.Abs(ground.point.y - centre.y) > 0.06f * scale)
                    return false;
            }

            return true;
        }

        // How far the ball has left to go, in tenths of a metre
        private static int GolfToGo(float scale)
        {
            Vector3 gap = golfHoleAt - golfAt;
            gap.y = 0f;
            return Mathf.RoundToInt(gap.magnitude / scale * 10f);
        }

        // A hand swung through the resting ball putts it along the ground the way the hand was moving
        private static bool SwingGolfBall(Vector3 from, Vector3 to, Vector3 velocity, bool left, float now, float scale)
        {
            float reach = (GolfBallRadius + GolfHandRadius) * scale;
            if (now - golfLastHit < 0.4f || ArcadePathDistance(from, to, golfAt) > reach * reach)
                return false;

            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            float speed = flat.magnitude;
            if (speed < 0.35f * scale)
                return false;

            golfVelocity = flat / speed * Mathf.Min(speed * 1.2f, 7f * scale);
            golfFrom = golfAt;
            golfLastHit = now;
            golfStrokes++;
            golfFlying = golfLipped = false;
            golfStage = GolfStage.Rolling;
            golfStageAt = now;
            golfDirty = true;

            Burst(golfAt, 8, 0.8f, false, flat);
            ArcadeSound(ArcadeClick, left, 0.3f);
            ArcadeBuzz(left, 0.5f, 0.04f);
            return true;
        }

        private static void RollGolfBall(float now, float dt, float scale, int layers)
        {
            float radius = GolfBallRadius * scale;
            Vector3 cup = golfHoleAt + Vector3.up * radius;
            Vector3 toCup = cup - golfAt;
            toCup.y = 0f;
            float distance = toCup.magnitude;

            // The hole pulls gently on a ball rolling close by
            float well = GolfWellRadius * scale;
            if (!golfFlying && distance < well && distance > 0.001f)
                golfVelocity += toCup / distance * (1.2f * scale * (1f - distance / well) * dt);

            if (ArcadeRoll(ref golfAt, ref golfVelocity, ref golfFlying, radius, 1.4f * scale, 0.7f, dt, scale, layers))
                ArcadeSound(ArcadeClick, false, 0.12f);

            toCup = cup - golfAt;
            toCup.y = 0f;
            distance = toCup.magnitude;
            float speed = new Vector2(golfVelocity.x, golfVelocity.z).magnitude;

            if (!golfFlying && distance < GolfHoleRadius * scale)
            {
                // Slow enough and it drops in; too fast and it catches the rim and spins out
                if (speed < 2.2f * scale)
                {
                    golfStage = GolfStage.Sinking;
                    golfStageAt = now;
                    ArcadeSound(ArcadeClick, false, 0.3f);
                    return;
                }

                if (!golfLipped)
                {
                    golfLipped = true;
                    golfVelocity = Quaternion.Euler(0f, Random.value < 0.5f ? -30f : 30f, 0f) * golfVelocity * 0.7f;
                    ArcadeSound(ArcadeClick, false, 0.2f);
                }
            }
            else if (distance > GolfHoleRadius * 2f * scale)
                golfLipped = false;

            // Rolled far off or fell off an edge: back to where it was hit from, with a stroke added
            if (distance > 18f * scale || golfAt.y < Mathf.Min(golfFrom.y, cup.y) - 3f * scale || (golfFlying && now - golfStageAt > 20f))
            {
                OutGolfBall(now, scale);
                return;
            }

            if ((!golfFlying && speed < 0.03f * scale) || now - golfStageAt > 25f)
            {
                golfVelocity = Vector3.zero;
                golfFlying = false;
                golfStage = GolfStage.Aiming;
                golfStageAt = now;
                golfToGo = GolfToGo(scale);
                golfDirty = true;
            }
        }

        private static void OutGolfBall(float now, float scale)
        {
            Burst(golfAt, 15, 1.2f, false);
            golfAt = golfFrom;
            golfVelocity = Vector3.zero;
            golfFlying = golfLipped = false;
            golfStrokes++;
            golfOut = now;
            golfStage = GolfStage.Aiming;
            golfStageAt = now;
            golfToGo = GolfToGo(scale);
            golfDirty = true;

            Burst(golfAt, 15, 1f, false);
            ArcadeSound(ArcadeTick, false, 0.3f);
        }

        private static void SinkGolfBall(float now, float scale)
        {
            golfStage = GolfStage.Sunk;
            golfStageAt = now;
            golfNewBest = golfBest <= 0 || golfStrokes < golfBest;
            if (golfNewBest)
            {
                golfBest = golfStrokes;
                SaveArcadeBest(GolfBestKey, golfBest);
            }

            bool ace = golfStrokes == 1;
            if (ace)
                Eclipse.Menu.Main.EclipseUnlock("Hole in One");

            Burst(golfHoleAt + Vector3.up * (0.1f * scale), ace ? 80 : 40, ace ? 3f : 2f, false, Vector3.up);
            ArcadeSound(ace ? ArcadeChime : ArcadeDing, false, 0.4f);
            ArcadeBuzz(true, 0.6f, 0.15f);
            ArcadeBuzz(false, 0.6f, 0.15f);
            golfDirty = true;
        }

        private static void DrawGolf(float now, float scale)
        {
            bool placed = golfStage != GolfStage.Finding;
            bool ball = placed && golfStage != GolfStage.Sunk;
            if (placed)
            {
                Ensure(ref golfBall, PrimitiveType.Sphere, Color.white);
                Ensure(ref golfGlow, PrimitiveType.Sphere, Color.white);
                Ensure(ref golfHole, PrimitiveType.Cylinder, Color.black);
                Ensure(ref golfPole, PrimitiveType.Cylinder, Color.white);
                EnsureStar(ref golfTop, Color.white, 0.06f);
                if (golfFlag == null || !golfFlag.Alive)
                    golfFlag = MakeShaped(GolfFlagMesh, Accent);
                if (golfRim == null)
                    golfRim = MakeRing("Eclipse_GolfRim");
                if (golfWell == null)
                    golfWell = MakeRing("Eclipse_GolfWell");
            }

            ShowPiece(golfBall, ball);
            ShowPiece(golfGlow, ball);
            ShowPiece(golfHole, placed);
            ShowPiece(golfPole, placed);
            ShowPiece(golfFlag, placed);
            ShowPiece(golfTop, placed);
            if (golfRim != null)
                golfRim.enabled = placed;
            if (golfWell != null)
                golfWell.enabled = placed;
            if (!placed)
                return;

            // The hole: a dark disc on the ground with a glowing rim, and faint rings drawn into it to show its pull
            Vector3 cup = golfHoleAt + Vector3.up * (0.002f * scale);
            float hole = GolfHoleRadius * scale;
            golfHole.Object.transform.SetPositionAndRotation(cup, Quaternion.identity);
            golfHole.Object.transform.localScale = new Vector3(hole * 2f, 0.001f * scale, hole * 2f);
            SetAlpha(golfHole, new Color(0.02f, 0.02f, 0.06f), 0.92f);
            PlaceRing(golfRim, cup + Vector3.up * (0.002f * scale), Vector3.up, hole, 0.01f * scale, Accent);

            float pull = now * 0.5f % 1f;
            PlaceRing(golfWell, cup, Vector3.up, Mathf.Lerp(GolfWellRadius * scale, hole, pull), 0.004f * scale, WithAlpha(Second, 0.35f * (1f - pull)));

            // A thin pole standing in the hole with a waving pennant and a little star on top
            float pole = GolfPoleHeight * scale;
            golfPole.Object.transform.SetPositionAndRotation(golfHoleAt + Vector3.up * (pole * 0.5f), Quaternion.identity);
            golfPole.Object.transform.localScale = new Vector3(0.012f * scale, pole * 0.5f, 0.012f * scale);
            SetColor(golfPole, Color.Lerp(Color.white, Second, 0.2f));

            Vector3 top = golfHoleAt + Vector3.up * pole;
            golfFlag.Object.transform.SetPositionAndRotation(top, Quaternion.LookRotation(golfLane) * Quaternion.Euler(0f, Mathf.Sin(now * 2.2f) * 18f, 0f));
            golfFlag.Object.transform.localScale = Vector3.one * (0.3f * scale);
            SetAlpha(golfFlag, Color.Lerp(Accent, Color.white, 0.15f + 0.1f * Mathf.Sin(now * 3f)), 0.95f);

            golfTop.Object.transform.SetPositionAndRotation(top + Vector3.up * (0.02f * scale), Quaternion.Euler(0f, now * 90f, 20f));
            golfTop.Object.transform.localScale = Vector3.one * (0.06f * scale * (1f + 0.15f * Mathf.Sin(now * 4f)));
            SetColor(golfTop, Color.Lerp(Accent, Color.white, 0.5f));

            if (!ball)
                return;

            // Drops into the hole and out of sight as it sinks
            float radius = GolfBallRadius * scale;
            Vector3 at = golfAt;
            float size = 1f;
            if (golfStage == GolfStage.Sinking)
            {
                float k = Mathf.Clamp01((now - golfStageAt) / 0.35f);
                at = Vector3.Lerp(golfAt, golfHoleAt + Vector3.down * (radius * 1.5f), k * k);
                size = 1f - 0.3f * k;
            }

            golfBall.Object.transform.position = at;
            golfBall.Object.transform.localScale = Vector3.one * (radius * 2f * size);
            SetColor(golfBall, Color.Lerp(Color.white, Accent, 0.15f));

            // Glows brighter while it is waiting to be hit
            float glow = golfStage == GolfStage.Aiming ? 0.25f + 0.12f * Mathf.Sin(now * 4f) : 0.2f;
            golfGlow.Object.transform.position = at;
            golfGlow.Object.transform.localScale = Vector3.one * (radius * 4f * size);
            SetAlpha(golfGlow, Second, glow);
        }

        private static void ShowGolfText(float now, float scale)
        {
            // The out-of-bounds note stays up for a couple of seconds
            if (golfOutShown && now - golfOut > 2f)
                golfDirty = true;

            Vector3 at = golfStage == GolfStage.Finding
                ? Head.position + (LookFlat() + Vector3.up * 0.1f) * scale
                : golfHoleAt + Vector3.up * ((GolfPoleHeight + 0.3f) * scale);
            PlaceArcadeText(golfText, at, scale);

            if (!golfDirty)
                return;

            golfDirty = false;
            golfOutShown = false;
            string best = golfBest > 0 ? golfBest.ToString() : "none yet";
            switch (golfStage)
            {
                case GolfStage.Finding:
                    golfText.SafeSetText(golfNoGround ? "No flat ground ahead\n<size=55%>face some open ground for a hole</size>" : "Finding a hole...");
                    break;

                case GolfStage.Sunk:
                    golfText.SafeSetText(golfStrokes == 1
                        ? $"Hole in one!\n<size=55%>best {best}{(golfNewBest ? "  ·  new best!" : "")}</size>"
                        : $"In the hole!\n<size=55%>{golfStrokes} strokes  ·  best {best}{(golfNewBest ? "  ·  new best!" : "")}</size>");
                    break;

                default:
                    if (now - golfOut < 2f)
                    {
                        golfOutShown = true;
                        golfText.SafeSetText("Out!  +1 stroke\n<size=55%>back to where you hit it from</size>");
                    }
                    else
                        golfText.SafeSetText($"Hole {golfHoles}  ·  {golfStrokes} {(golfStrokes == 1 ? "stroke" : "strokes")}\n<size=55%>{golfToGo / 10f:0.0} m to go  ·  best {best}</size>");
                    break;
            }

            golfText.color = golfStage == GolfStage.Sunk ? Accent : Color.Lerp(Color.white, Accent, 0.35f);
        }

        /// <summary>Takes away the golf hole, its flag, the ball and the scoreboard.</summary>
        public static void DisableGravityGolf()
        {
            DiscardPiece(ref golfBall);
            DiscardPiece(ref golfGlow);
            DiscardPiece(ref golfHole);
            DiscardPiece(ref golfPole);
            DiscardPiece(ref golfFlag);
            DiscardPiece(ref golfTop);
            DestroyLine(ref golfRim);
            DestroyLine(ref golfWell);
            DestroyText(ref golfText);
            golfStage = GolfStage.Finding;
            golfStrokes = golfHoles = 0;
            golfVelocity = Vector3.zero;
            golfFlying = golfLipped = false;
            golfOut = golfLastHit = -10f;
        }

        // ── Constellation Connect ───────────────────────────────────────────────

        private enum ConstellationStage { Showing, Running, Done }

        private sealed class SkyFigure
        {
            public string Name, About, BestKey;

            // Where each star sits, about a metre across, with east on the left as it looks in the sky
            public Vector2[] Stars;

            // The order to touch the stars in, which may come back to a star already joined.
            // A -1 lifts the line, so the star after it starts a new one.
            public int[] Path;

            // The best time, read from the saved scores the first time it is needed
            public float Best = -1f;
        }

        private static readonly SkyFigure[] ConstellationFigures =
        {
            new SkyFigure
            {
                Name = "The Big Dipper", About = "the back and tail of Ursa Major, the Great Bear", BestKey = "Eclipse_Constellation_BigDipper_Best",
                // Dubhe, Merak, Phecda, Megrez, Alioth, Mizar, Alkaid
                Stars = new[]
                {
                    new Vector2(0.426f, 0.24f), new Vector2(0.5f, 0.034f), new Vector2(0.226f, -0.132f), new Vector2(0.094f, -0.007f),
                    new Vector2(-0.12f, -0.041f), new Vector2(-0.295f, -0.055f), new Vector2(-0.5f, -0.24f)
                },
                Path = new[] { 6, 5, 4, 3, 2, 1, 0, 3 }
            },
            new SkyFigure
            {
                Name = "Orion", About = "the Hunter, with his famous belt of three stars", BestKey = "Eclipse_Constellation_Orion_Best",
                // Betelgeuse, Bellatrix, Mintaka, Alnilam, Alnitak, Saiph, Rigel, Meissa
                Stars = new[]
                {
                    new Vector2(-0.258f, 0.371f), new Vector2(0.123f, 0.316f), new Vector2(0.036f, -0.021f), new Vector2(-0.017f, -0.067f),
                    new Vector2(-0.075f, -0.104f), new Vector2(-0.164f, -0.5f), new Vector2(0.258f, -0.425f), new Vector2(-0.003f, 0.5f)
                },
                Path = new[] { 2, 1, 7, 0, 4, 5, 6, 2, 3, 4 }
            },
            new SkyFigure
            {
                Name = "Cassiopeia", About = "the Queen, a W of five bright stars", BestKey = "Eclipse_Constellation_Cassiopeia_Best",
                // Caph, Schedar, Navi, Ruchbah, Segin
                Stars = new[]
                {
                    new Vector2(0.5f, -0.058f), new Vector2(0.195f, -0.308f), new Vector2(0.013f, 0.019f), new Vector2(-0.275f, -0.006f), new Vector2(-0.5f, 0.308f)
                },
                Path = new[] { 0, 1, 2, 3, 4 }
            },
            new SkyFigure
            {
                Name = "Cygnus", About = "the Swan, also called the Northern Cross", BestKey = "Eclipse_Constellation_Cygnus_Best",
                // Deneb, Sadr, Eta Cygni, Albireo, Fawaris, Gienah
                Stars = new[]
                {
                    new Vector2(-0.357f, 0.5f), new Vector2(-0.171f, 0.196f), new Vector2(0.124f, -0.102f), new Vector2(0.472f, -0.5f),
                    new Vector2(0.219f, 0.485f), new Vector2(-0.472f, -0.149f)
                },
                Path = new[] { 0, 1, 2, 3, -1, 4, 1, 5 }
            },
            new SkyFigure
            {
                Name = "Leo", About = "the Lion, with the Sickle for its head and mane", BestKey = "Eclipse_Constellation_Leo_Best",
                // Regulus, Eta Leonis, Algieba, Adhafera, Rasalas, Epsilon Leonis, Denebola, Zosma, Chertan
                Stars = new[]
                {
                    new Vector2(0.339f, -0.24f), new Vector2(0.342f, -0.078f), new Vector2(0.238f, 0.023f), new Vector2(0.261f, 0.143f),
                    new Vector2(0.441f, 0.24f), new Vector2(0.5f, 0.168f), new Vector2(-0.5f, -0.127f), new Vector2(-0.187f, 0.055f),
                    new Vector2(-0.198f, -0.117f)
                },
                Path = new[] { 5, 4, 3, 2, 1, 0, 8, 6, 7, 2 }
            },
            new SkyFigure
            {
                Name = "The Little Dipper", About = "Ursa Minor, with Polaris, the North Star, at the end of its handle", BestKey = "Eclipse_Constellation_LittleDipper_Best",
                // Polaris, Yildun, Epsilon, Zeta, Eta, Pherkad, Kochab
                Stars = new[]
                {
                    new Vector2(-0.057f, 0.5f), new Vector2(-0.126f, 0.297f), new Vector2(-0.153f, 0.051f), new Vector2(-0.04f, -0.188f),
                    new Vector2(-0.147f, -0.289f), new Vector2(0.06f, -0.5f), new Vector2(0.153f, -0.358f)
                },
                Path = new[] { 0, 1, 2, 3, 4, 5, 6, 3 }
            },
            new SkyFigure
            {
                Name = "The Southern Cross", About = "Crux, the smallest constellation in the sky", BestKey = "Eclipse_Constellation_SouthernCross_Best",
                // Gacrux, Acrux, Mimosa, Imai
                Stars = new[] { new Vector2(-0.002f, 0.5f), new Vector2(0.089f, -0.5f), new Vector2(-0.347f, 0.057f), new Vector2(0.347f, 0.217f) },
                Path = new[] { 0, 1, -1, 2, 3 }
            }
        };

        private const int ConstellationMaxStars = 9;
        private const int ConstellationMaxLinks = 10;
        private const float ConstellationReach = 0.065f;
        private static readonly Piece[] constellationDots = new Piece[ConstellationMaxStars];
        private static readonly bool[] constellationLit = new bool[ConstellationMaxStars];
        private static readonly LineRenderer[] constellationLinks = new LineRenderer[ConstellationMaxLinks];
        private static readonly int[] constellationLinkFrom = new int[ConstellationMaxLinks];
        private static readonly int[] constellationLinkTo = new int[ConstellationMaxLinks];
        private static readonly float[] constellationLinkMade = new float[ConstellationMaxLinks];
        private static LineRenderer constellationHalo;
        private static TextMeshPro constellationText;
        private static ConstellationStage constellationStage;
        private static Vector3 constellationCentre, constellationRight = Vector3.right;
        private static int constellationIndex, constellationStep, constellationLinkCount, constellationLast = -1, constellationTouches, constellationTotal, constellationShownTenths = -1;
        private static float constellationStarted, constellationShownAt, constellationDoneAt;
        private static bool constellationLifted;

        /// <summary>A real constellation appears in front of you; touch its stars in order, the pulsing one next, to join them up as fast as you can.</summary>
        public static void ConstellationConnect()
        {
            float now = Time.time;
            float scale = Scale;
            Vector3 head = Head.position;

            if (constellationText == null)
            {
                constellationText = MakeWristText("Eclipse_ConstellationConnect", Color.white);
                ShowConstellation(constellationIndex, now, scale);
            }

            // Walked away from it: it comes back in front of you, with what you've joined so far
            float far = 3f * scale;
            if ((constellationCentre - head).sqrMagnitude > far * far)
            {
                PlaceConstellation(scale);
                Burst(constellationCentre, 20, 1f, false);
            }

            if (constellationStage == ConstellationStage.Done && now - constellationDoneAt > 4f)
                ShowConstellation((constellationIndex + 1) % ConstellationFigures.Length, now, scale);

            SkyFigure figure = ConstellationFigures[constellationIndex];
            // Stars only count once they have finished twinkling into view
            int next = constellationStage == ConstellationStage.Done ? -1 : figure.Path[constellationStep];
            if (next >= 0 && now - constellationShownAt > 0.4f)
            {
                Vector3 spot = ConstellationSpot(figure.Stars[next], scale);
                float reach = ConstellationReach * scale;
                bool left = (LeftHand - spot).sqrMagnitude < reach * reach;
                if (left || (RightHand - spot).sqrMagnitude < reach * reach)
                {
                    TouchConstellationStar(figure, next, spot, left, now, scale);
                    next = constellationStage == ConstellationStage.Done ? -1 : figure.Path[constellationStep];
                }
            }

            DrawConstellation(figure, next, now, head, scale);

            PlaceToyText(constellationText, constellationCentre + Vector3.up * (0.72f * scale), 0.065f);
            if (constellationStage == ConstellationStage.Running)
            {
                int tenths = Mathf.FloorToInt((now - constellationStarted) * 10f);
                if (tenths != constellationShownTenths)
                {
                    constellationShownTenths = tenths;
                    constellationText.SafeSetText($"{tenths / 10f:0.0} s\n<size=55%>{constellationTouches} of {constellationTotal}</size>");
                }
            }
        }

        private static Vector3 ConstellationSpot(Vector2 star, float scale) =>
            constellationCentre + (constellationRight * star.x + Vector3.up * star.y) * scale;

        // Upright in front of you, a little below eye level, so every star is within reach
        private static void PlaceConstellation(float scale)
        {
            Vector3 look = LookFlat();
            constellationCentre = Head.position + (look * 0.55f + Vector3.down * 0.1f) * scale;
            constellationRight = Vector3.Cross(Vector3.up, look);
        }

        private static void ShowConstellation(int index, float now, float scale)
        {
            constellationIndex = index;
            constellationStage = ConstellationStage.Showing;
            constellationStep = constellationLinkCount = constellationTouches = 0;
            constellationLast = constellationShownTenths = -1;
            constellationLifted = false;
            constellationShownAt = now;
            for (int i = 0; i < constellationLit.Length; i++)
                constellationLit[i] = false;

            SkyFigure figure = ConstellationFigures[index];
            constellationTotal = 0;
            foreach (int star in figure.Path)
                if (star >= 0)
                    constellationTotal++;

            if (figure.Best < 0f)
                figure.Best = PlayerPrefs.GetFloat(figure.BestKey, 0f);

            PlaceConstellation(scale);
            constellationText.SafeSetText(figure.Best > 0f
                ? $"Connect the stars\n<size=55%>touch the pulsing star to start  ·  best {figure.Best:0.0} s</size>"
                : "Connect the stars\n<size=55%>touch the pulsing star to start</size>");
            constellationText.color = Accent;
        }

        private static void TouchConstellationStar(SkyFigure figure, int star, Vector3 spot, bool left, float now, float scale)
        {
            // The clock starts with the first star
            if (constellationStage == ConstellationStage.Showing)
            {
                constellationStage = ConstellationStage.Running;
                constellationStarted = now;
                constellationText.color = Color.Lerp(Color.white, Accent, 0.35f);
            }

            if (constellationLast >= 0 && !constellationLifted && constellationLinkCount < ConstellationMaxLinks)
            {
                constellationLinkFrom[constellationLinkCount] = constellationLast;
                constellationLinkTo[constellationLinkCount] = star;
                constellationLinkMade[constellationLinkCount] = now;
                constellationLinkCount++;
            }

            constellationLit[star] = true;
            constellationLast = star;
            constellationLifted = false;
            constellationTouches++;
            constellationStep++;
            if (constellationStep < figure.Path.Length && figure.Path[constellationStep] < 0)
            {
                constellationLifted = true;
                constellationStep++;
            }

            Burst(spot, 10, 0.8f, false);
            ArcadeSound(ArcadeClick, left, 0.3f);
            ArcadeBuzz(left, 0.4f, 0.04f);

            if (constellationStep >= figure.Path.Length)
                FinishConstellation(figure, now, scale);
        }

        private static void FinishConstellation(SkyFigure figure, float now, float scale)
        {
            float time = now - constellationStarted;
            constellationStage = ConstellationStage.Done;
            constellationDoneAt = now;

            bool best = figure.Best <= 0f || time < figure.Best;
            if (best)
            {
                figure.Best = time;
                SaveArcadeBestTime(figure.BestKey, time);
            }

            constellationText.SafeSetText($"{figure.Name}\n<size=55%>{figure.About}\n{time:0.0} s  ·  best {figure.Best:0.0} s{(best ? "  ·  new best!" : "")}</size>");
            constellationText.color = Accent;

            for (int i = 0; i < figure.Stars.Length; i++)
                Burst(ConstellationSpot(figure.Stars[i], scale), 12, 1.2f, false);
            ArcadeSound(ArcadeDing, false, 0.4f);
            ArcadeBuzz(true, 0.5f, 0.12f);
            ArcadeBuzz(false, 0.5f, 0.12f);
        }

        private static void DrawConstellation(SkyFigure figure, int next, float now, Vector3 head, float scale)
        {
            bool done = constellationStage == ConstellationStage.Done;

            // Each new constellation twinkles into view
            float appear = Mathf.Clamp01((now - constellationShownAt) / 0.6f);
            appear = Mathf.Sin(appear * Mathf.PI * 0.5f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(now * 6f);

            for (int i = 0; i < ConstellationMaxStars; i++)
            {
                if (i >= figure.Stars.Length)
                {
                    ShowPiece(constellationDots[i], false);
                    continue;
                }

                Piece dot = EnsureStar(ref constellationDots[i], Color.white, 0.05f);
                ShowPiece(dot, true);

                float size;
                Color color;
                if (i == next)
                {
                    size = 0.055f + 0.025f * pulse;
                    color = Color.Lerp(Accent, Color.white, 0.3f * pulse);
                }
                else if (constellationLit[i])
                {
                    size = done ? 0.06f + 0.01f * Mathf.Sin(now * 5f + i) : 0.05f;
                    color = Color.Lerp(Second, Color.white, 0.45f);
                }
                else
                {
                    size = 0.04f + 0.005f * Mathf.Sin(now * 3f + i * 1.7f);
                    color = new Color(0.78f, 0.84f, 1f, 0.55f);
                }

                Transform body = dot.Object.transform;
                body.SetPositionAndRotation(ConstellationSpot(figure.Stars[i], scale), Quaternion.Euler(now * 30f + i * 40f, now * 50f + i * 20f, 0f));
                body.localScale = Vector3.one * (size * appear * scale);
                SetColor(dot, color);
            }

            // A ring round the star to touch next
            if (next >= 0)
            {
                if (constellationHalo == null)
                    constellationHalo = MakeRing("Eclipse_ConstellationHalo");

                Vector3 spot = ConstellationSpot(figure.Stars[next], scale);
                constellationHalo.enabled = true;
                PlaceRing(constellationHalo, spot, head - spot, (0.045f + 0.015f * pulse) * scale, 0.004f * scale, WithAlpha(Accent, 0.85f - 0.4f * pulse), now * 60f);
            }
            else if (constellationHalo != null)
                constellationHalo.enabled = false;

            // Each line grows quickly out from the star before it
            for (int k = 0; k < ConstellationMaxLinks; k++)
            {
                LineRenderer line = constellationLinks[k];
                if (k >= constellationLinkCount)
                {
                    if (line != null)
                        line.enabled = false;
                    continue;
                }

                if (line == null)
                    line = constellationLinks[k] = MakeLine("Eclipse_ConstellationLink", 0.006f);

                Vector3 from = ConstellationSpot(figure.Stars[constellationLinkFrom[k]], scale);
                Vector3 to = ConstellationSpot(figure.Stars[constellationLinkTo[k]], scale);
                float grow = Mathf.Clamp01((now - constellationLinkMade[k]) / 0.15f);
                line.enabled = true;
                line.SetPosition(0, from);
                line.SetPosition(1, Vector3.Lerp(from, to, grow));
                line.startWidth = line.endWidth = (done ? 0.008f + 0.002f * Mathf.Sin(now * 5f) : 0.006f) * scale;
                line.startColor = WithAlpha(Accent, 0.9f);
                line.endColor = WithAlpha(Second, 0.9f);
            }
        }

        /// <summary>Takes away the constellation, its lines and the timer.</summary>
        public static void DisableConstellationConnect()
        {
            for (int i = 0; i < constellationDots.Length; i++)
                DiscardPiece(ref constellationDots[i]);
            for (int k = 0; k < constellationLinks.Length; k++)
                DestroyLine(ref constellationLinks[k]);

            DestroyLine(ref constellationHalo);
            DestroyText(ref constellationText);
            constellationStage = ConstellationStage.Showing;
            constellationStep = constellationLinkCount = constellationTouches = 0;
            constellationLast = -1;
        }

        // ── Star Bowling ────────────────────────────────────────────────────────

        private enum BowlingStage { Placing, Ready, Held, Rolling, Settling, Result, Over }

        private sealed class BowlingPin
        {
            public Piece Body, Stripes;
            public Vector3 Spot, Toward;
            public float Fell = -1f, Cleared = -1f, Raised;
            public bool Down, Pushed;
        }

        private const string BowlingBestKey = "Eclipse_StarBowling_Best";
        private const float BowlingBallRadius = 0.11f;
        private const float BowlingPinHeight = 0.38f;
        private const float BowlingPinRadius = 0.06f;
        private const float BowlingSpacing = 0.3f;
        private const int BowlingTrackFrames = 6;
        private const int BowlingNormal = 0, BowlingStrike = 1, BowlingSpare = 2;
        private static readonly BowlingPin[] bowlingPins = new BowlingPin[10];
        private static readonly Vector3[] bowlingRack = new Vector3[10];
        private static readonly int[] bowlingRolls = new int[21];
        private static readonly Vector3[] bowlingTrack = new Vector3[BowlingTrackFrames];
        private static readonly float[] bowlingTrackTimes = new float[BowlingTrackFrames];

        // Which ways and how far ahead to look for a flat spot for the pins, best first
        private static readonly float[] BowlingTurns = { 0f, 12f, -12f, 25f, -25f, 40f, -40f };
        private static readonly float[] BowlingDistances = { 8f, 7f, 9f, 6f, 10f };

        private static Piece bowlingBall, bowlingHoles, bowlingGlow;
        private static TextMeshPro bowlingText;
        private static BowlingStage bowlingStage;
        private static Vector3 bowlingAt, bowlingVelocity, bowlingHeadPin, bowlingLane = Vector3.forward;
        private static Quaternion bowlingSpin = Quaternion.identity;
        private static float bowlingStageAt, bowlingLastKnock, bowlingNextTry;
        private static int bowlingFrame = 1, bowlingBallNumber = 1, bowlingRollCount, bowlingDownBefore, bowlingTrackCount, bowlingBest = -1, bowlingLastRoll, bowlingResult, bowlingShownDown = -1;
        private static bool bowlingHeld = true, bowlingFlying, bowlingFreshRack = true, bowlingTenthStrike, bowlingDirty = true, bowlingNoGround, bowlingNewBest;

        /// <summary>Ten glowing pins stand on the ground ahead; hold B to take a ball in your right hand, then swing and let go to bowl it at them, over a full ten-frame game.</summary>
        public static void StarBowling()
        {
            float now = Time.time;
            float dt = Mathf.Max(Time.deltaTime, 0.001f);
            float scale = Scale;
            int layers = GTPlayer.Instance.locomotionEnabledLayers;

            if (bowlingText == null)
            {
                bowlingText = MakeWristText("Eclipse_StarBowling", Color.white);
                if (bowlingBest < 0)
                    bowlingBest = PlayerPrefs.GetInt(BowlingBestKey, 0);
                NewBowlingGame(now);
                bowlingStage = BowlingStage.Placing;
                bowlingNextTry = now;
            }

            bool pressed = Pressed(rightSecondary, ref bowlingHeld);

            // Wandered far from the lane between balls: the pins come and stand ahead of you again
            float far = 30f * scale;
            if ((bowlingStage == BowlingStage.Ready || bowlingStage == BowlingStage.Over) && (bowlingHeadPin - BodyPosition).sqrMagnitude > far * far)
            {
                if (bowlingStage == BowlingStage.Over)
                    NewBowlingGame(now);
                bowlingStage = BowlingStage.Placing;
                bowlingNextTry = now;
                bowlingNoGround = false;
                bowlingDirty = true;
            }

            switch (bowlingStage)
            {
                case BowlingStage.Placing:
                    if (now < bowlingNextTry)
                        break;

                    if (PlaceBowlingLane(scale, layers))
                    {
                        bowlingStage = BowlingStage.Ready;
                        bowlingNoGround = false;
                        bowlingDirty = true;
                    }
                    else
                    {
                        bowlingNextTry = now + 3f;
                        if (!bowlingNoGround)
                            bowlingNoGround = bowlingDirty = true;
                    }
                    break;

                case BowlingStage.Ready:
                    if (pressed)
                        TakeBowlingBall(now, scale);
                    break;

                case BowlingStage.Held:
                    HoldBowlingBall(now, scale);
                    break;

                case BowlingStage.Rolling:
                    RollBowlingBall(now, dt, scale, layers);
                    break;

                case BowlingStage.Settling:
                    // Waits for the last pin to finish falling before counting
                    if (now - bowlingStageAt > 0.8f && now - bowlingLastKnock > 1f)
                        ScoreBowlingRoll(now, scale);
                    break;

                case BowlingStage.Result:
                    if (now - bowlingStageAt > 2f)
                        NextBowlingBall(now);
                    break;

                case BowlingStage.Over:
                    if (pressed)
                    {
                        NewBowlingGame(now);
                        bowlingStage = BowlingStage.Ready;
                    }
                    break;
            }

            DrawBowlingPins(now, scale);
            DrawBowlingBall(now, scale);
            ShowBowlingText(scale);
        }

        private static void NewBowlingGame(float now)
        {
            bowlingFrame = bowlingBallNumber = 1;
            bowlingRollCount = bowlingLastRoll = 0;
            bowlingResult = BowlingNormal;
            bowlingTenthStrike = bowlingNewBest = false;
            ResetBowlingRack(now);
            bowlingDirty = true;
        }

        // Every pin stands up again
        private static void ResetBowlingRack(float now)
        {
            foreach (BowlingPin pin in bowlingPins)
            {
                if (pin == null)
                    continue;

                pin.Down = pin.Pushed = false;
                pin.Fell = pin.Cleared = -1f;
                pin.Raised = now;
            }

            bowlingDownBefore = 0;
            bowlingFreshRack = true;
        }

        // Sweeps away the pins knocked down by the first ball, leaving the rest for the second
        private static void ClearFallenPins(float now)
        {
            foreach (BowlingPin pin in bowlingPins)
                if (pin != null && pin.Down && pin.Cleared < 0f)
                    pin.Cleared = now;

            bowlingFreshRack = false;
        }

        // Looks for a flat spot about eight metres ahead for the pins, turning a little either way if it has to
        private static bool PlaceBowlingLane(float scale, int layers)
        {
            Vector3 look = LookFlat();
            Vector3 feet = Feet;
            foreach (float distance in BowlingDistances)
                foreach (float turn in BowlingTurns)
                {
                    Vector3 lane = Quaternion.Euler(0f, turn, 0f) * look;
                    if (TryBowlingRack(feet, lane, distance * scale, scale, layers))
                    {
                        Burst(BowlingRackCentre(scale) + Vector3.up * (0.3f * scale), 30, 1.5f, false);
                        return true;
                    }
                }

            return false;
        }

        private static bool TryBowlingRack(Vector3 feet, Vector3 lane, float distance, float scale, int layers)
        {
            Vector3 lift = Vector3.up * (2f * scale);
            if (!Physics.Raycast(feet + lane * distance + lift, Vector3.down, out RaycastHit head, 5f * scale, layers, QueryTriggerInteraction.Ignore)
                || head.normal.y < 0.92f || Mathf.Abs(head.point.y - feet.y) > 2f * scale)
                return false;

            // Something to bowl along: nothing in the way between you and the pins
            Vector3 low = Vector3.up * (0.3f * scale);
            if (Physics.Linecast(feet + low, head.point + low, layers, QueryTriggerInteraction.Ignore))
                return false;

            Vector3 side = Vector3.Cross(Vector3.up, lane);
            for (int i = 0; i < bowlingRack.Length; i++)
            {
                Vector3 spot = head.point + BowlingRackOffset(i, lane, side, scale);
                if (!Physics.Raycast(spot + lift, Vector3.down, out RaycastHit ground, 5f * scale, layers, QueryTriggerInteraction.Ignore)
                    || ground.normal.y < 0.9f || Mathf.Abs(ground.point.y - head.point.y) > 0.12f * scale)
                    return false;

                bowlingRack[i] = ground.point;
            }

            bowlingHeadPin = head.point;
            bowlingLane = lane;
            for (int i = 0; i < bowlingPins.Length; i++)
            {
                if (bowlingPins[i] == null)
                    bowlingPins[i] = new BowlingPin { Raised = Time.time };
                bowlingPins[i].Spot = bowlingRack[i];
            }

            return true;
        }

        // The usual triangle: the head pin nearest you, then rows of two, three and four behind it
        private static Vector3 BowlingRackOffset(int pin, Vector3 lane, Vector3 side, float scale)
        {
            int row = pin < 1 ? 0 : pin < 3 ? 1 : pin < 6 ? 2 : 3;
            float across = pin - row * (row + 1) / 2 - row * 0.5f;
            return (lane * (row * 0.866f) + side * across) * (BowlingSpacing * scale);
        }

        private static Vector3 BowlingRackCentre(float scale) =>
            bowlingHeadPin + bowlingLane * (1.5f * 0.866f * BowlingSpacing * scale);

        private static Vector3 BowlingHandSpot(float scale) =>
            RightHand + Vector3.down * (BowlingBallRadius * 0.6f * scale);

        private static void TakeBowlingBall(float now, float scale)
        {
            bowlingStage = BowlingStage.Held;
            bowlingStageAt = now;
            bowlingTrackCount = 0;
            bowlingAt = BowlingHandSpot(scale);
            bowlingVelocity = Vector3.zero;
            bowlingDirty = true;

            Burst(bowlingAt, 12, 0.7f, false);
            ArcadeSound(ArcadeClick, false, 0.2f);
            ArcadeBuzz(false, 0.4f, 0.05f);
        }

        // The ball follows your hand while B is held, and is thrown when you let go
        private static void HoldBowlingBall(float now, float scale)
        {
            bowlingAt = BowlingHandSpot(scale);
            int slot = bowlingTrackCount % BowlingTrackFrames;
            bowlingTrack[slot] = bowlingAt;
            bowlingTrackTimes[slot] = now;
            bowlingTrackCount++;

            if (rightSecondary)
                return;

            // Thrown with the hand's speed over the last few frames, which is steadier than the last frame alone
            Vector3 velocity = Vector3.zero;
            int frames = Mathf.Min(bowlingTrackCount, BowlingTrackFrames);
            if (frames > 1)
            {
                int newest = (bowlingTrackCount - 1) % BowlingTrackFrames;
                int oldest = (bowlingTrackCount - frames) % BowlingTrackFrames;
                float span = bowlingTrackTimes[newest] - bowlingTrackTimes[oldest];
                if (span > 0.0001f)
                    velocity = (bowlingTrack[newest] - bowlingTrack[oldest]) / span;
            }

            // A quick tap of B without a swing puts the ball back rather than wasting a go
            if (now - bowlingStageAt < 0.25f && velocity.sqrMagnitude < scale * scale)
            {
                bowlingStage = BowlingStage.Ready;
                bowlingDirty = true;
                return;
            }

            bowlingVelocity = Vector3.ClampMagnitude(velocity, 10f * scale);
            bowlingFlying = true;
            bowlingStage = BowlingStage.Rolling;
            bowlingStageAt = now;
            ArcadeBuzz(false, 0.5f, 0.05f);
        }

        private static void RollBowlingBall(float now, float dt, float scale, int layers)
        {
            float radius = BowlingBallRadius * scale;
            Vector3 from = bowlingAt;
            if (ArcadeRoll(ref bowlingAt, ref bowlingVelocity, ref bowlingFlying, radius, 0.3f * scale, 0f, dt, scale, layers))
                ArcadeSound(ArcadeClick, false, 0.15f);

            // Turns over as it rolls, so you can see the finger holes go round
            Vector3 moved = bowlingAt - from;
            moved.y = 0f;
            float distance = moved.magnitude;
            if (distance > 0.0001f && !bowlingFlying)
                bowlingSpin = Quaternion.AngleAxis(distance / radius * Mathf.Rad2Deg, Vector3.Cross(Vector3.up, moved / distance)) * bowlingSpin;

            // Knocks over every pin it passes close to
            float height = BowlingPinHeight * scale;
            float reach = radius + BowlingPinRadius * scale;
            Vector3 heading = new Vector3(bowlingVelocity.x, 0f, bowlingVelocity.z);
            heading = heading.sqrMagnitude > 0.0001f ? heading.normalized : bowlingLane;
            foreach (BowlingPin pin in bowlingPins)
            {
                if (pin == null || pin.Down)
                    continue;

                Vector3 nearest = pin.Spot + Vector3.up * Mathf.Clamp(bowlingAt.y - pin.Spot.y, 0f, height);
                if (ArcadePathDistance(from, bowlingAt, nearest) > reach * reach)
                    continue;

                Vector3 away = pin.Spot - bowlingAt;
                away.y = 0f;
                Vector3 toward = heading + (away.sqrMagnitude > 0.0001f ? away.normalized * 0.8f : Vector3.zero);
                KnockBowlingPin(pin, toward, now, scale);
                bowlingVelocity *= 0.94f;
            }

            // Finished: stopped, gone past the pins, or lost somewhere
            Vector3 fromHead = bowlingAt - bowlingHeadPin;
            float along = Vector3.Dot(fromHead, bowlingLane);
            bool stopped = !bowlingFlying && bowlingVelocity.sqrMagnitude < 0.02f * scale * scale;
            bool past = along > (3f * 0.866f * BowlingSpacing + 1.2f) * scale;
            bool lost = fromHead.sqrMagnitude > 400f * scale * scale || bowlingAt.y < bowlingHeadPin.y - 3f * scale || now - bowlingStageAt > 12f;
            if (stopped || past || lost)
            {
                Burst(bowlingAt, 12, 0.8f, false);
                bowlingStage = BowlingStage.Settling;
                bowlingStageAt = now;
            }
        }

        private static void KnockBowlingPin(BowlingPin pin, Vector3 toward, float now, float scale)
        {
            toward.y = 0f;
            if (toward.sqrMagnitude < 0.0001f)
                toward = bowlingLane;

            pin.Down = true;
            pin.Pushed = false;
            pin.Fell = now;
            pin.Toward = Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f) * toward.normalized;
            bowlingLastKnock = now;
            bowlingDirty = true;

            Burst(pin.Spot + Vector3.up * (0.2f * scale), 6, 0.8f, false);
            ArcadeSound(ArcadeClick, false, 0.25f);
        }

        // A falling pin knocks down the standing pins it lands on
        private static void ToppleBowlingNeighbours(BowlingPin fallen, float now, float scale)
        {
            float height = BowlingPinHeight * scale;
            float wide = BowlingPinRadius * 2.2f * scale;
            foreach (BowlingPin pin in bowlingPins)
            {
                if (pin == null || pin == fallen || pin.Down)
                    continue;

                Vector3 offset = pin.Spot - fallen.Spot;
                offset.y = 0f;
                float along = Vector3.Dot(offset, fallen.Toward);
                if (along <= 0f || along > height * 0.95f || (offset - fallen.Toward * along).magnitude > wide)
                    continue;

                KnockBowlingPin(pin, fallen.Toward + offset.normalized, now, scale);
            }
        }

        private static void ScoreBowlingRoll(float now, float scale)
        {
            int down = 0;
            foreach (BowlingPin pin in bowlingPins)
                if (pin != null && pin.Down)
                    down++;

            bowlingLastRoll = down - bowlingDownBefore;
            bowlingDownBefore = down;
            if (bowlingRollCount < bowlingRolls.Length)
                bowlingRolls[bowlingRollCount++] = bowlingLastRoll;

            bowlingResult = down < 10 ? BowlingNormal : bowlingFreshRack ? BowlingStrike : BowlingSpare;
            Vector3 centre = BowlingRackCentre(scale) + Vector3.up * (0.4f * scale);
            if (bowlingResult == BowlingStrike)
            {
                if (bowlingFrame == 10 && bowlingBallNumber == 1)
                    bowlingTenthStrike = true;

                Burst(centre, 120, 4f, false);
                Burst(centre + Vector3.up * (0.5f * scale), 60, 2.5f, true);
                ArcadeSound(ArcadeChime, false, 0.5f);
                ArcadeBuzz(true, 0.8f, 0.25f);
                ArcadeBuzz(false, 0.8f, 0.25f);
                Eclipse.Menu.Main.EclipseUnlock("Strike!");
            }
            else if (bowlingResult == BowlingSpare)
            {
                Burst(centre, 60, 2.5f, false);
                ArcadeSound(ArcadeDing, false, 0.4f);
            }

            bowlingStage = BowlingStage.Result;
            bowlingStageAt = now;
            bowlingDirty = true;
        }

        private static void NextBowlingBall(float now)
        {
            bool cleared = bowlingDownBefore >= 10;
            if (bowlingFrame < 10)
            {
                if (bowlingBallNumber == 1 && !cleared)
                {
                    bowlingBallNumber = 2;
                    ClearFallenPins(now);
                }
                else
                {
                    bowlingFrame++;
                    bowlingBallNumber = 1;
                    ResetBowlingRack(now);
                }
            }
            else if (bowlingBallNumber == 1 || (bowlingBallNumber == 2 && (bowlingTenthStrike || cleared)))
            {
                // In the tenth frame a strike or a spare earns more balls
                bowlingBallNumber++;
                if (cleared)
                    ResetBowlingRack(now);
                else
                    ClearFallenPins(now);
            }
            else
            {
                EndBowlingGame(now);
                return;
            }

            bowlingStage = BowlingStage.Ready;
            bowlingDirty = true;
        }

        private static void EndBowlingGame(float now)
        {
            int score = BowlingScore();
            bowlingNewBest = score > bowlingBest;
            if (bowlingNewBest)
            {
                bowlingBest = score;
                SaveArcadeBest(BowlingBestKey, bowlingBest);
            }

            bowlingStage = BowlingStage.Over;
            bowlingStageAt = now;
            bowlingDirty = true;
            Burst(BowlingRackCentre(Scale) + Vector3.up * (0.8f * Scale), 50, 2f, false);
            ArcadeSound(ArcadeDing, false, 0.4f);
        }

        // The usual scoring, counting each strike's and spare's bonus balls as they are bowled
        private static int BowlingScore()
        {
            int score = 0;
            int roll = 0;
            for (int frame = 0; frame < 10 && roll < bowlingRollCount; frame++)
            {
                if (bowlingRolls[roll] == 10)
                {
                    score += 10 + BowlingRoll(roll + 1) + BowlingRoll(roll + 2);
                    roll++;
                }
                else if (BowlingRoll(roll) + BowlingRoll(roll + 1) == 10 && roll + 1 < bowlingRollCount)
                {
                    score += 10 + BowlingRoll(roll + 2);
                    roll += 2;
                }
                else
                {
                    score += BowlingRoll(roll) + BowlingRoll(roll + 1);
                    roll += 2;
                }
            }

            return score;
        }

        private static int BowlingRoll(int roll) => roll < bowlingRollCount ? bowlingRolls[roll] : 0;

        private static void DrawBowlingPins(float now, float scale)
        {
            bool placed = bowlingStage != BowlingStage.Placing;
            float height = BowlingPinHeight * scale;
            Color body = Color.Lerp(Color.white, Accent, 0.25f);
            Color stripes = Color.Lerp(Second, Color.white, 0.1f);
            foreach (BowlingPin pin in bowlingPins)
            {
                if (pin == null)
                    continue;

                // Knocked-down pins are swept away between balls, fading as they go
                float alpha = pin.Cleared < 0f ? 1f : 1f - (now - pin.Cleared) / 0.5f;
                bool shown = placed && alpha > 0f;
                if (!shown)
                {
                    ShowPiece(pin.Body, false);
                    ShowPiece(pin.Stripes, false);
                    continue;
                }

                if (pin.Body == null || !pin.Body.Alive)
                    pin.Body = MakeShaped(BowlingPinMesh, body);
                if (pin.Stripes == null || !pin.Stripes.Alive)
                    pin.Stripes = MakeDecal(BowlingStripesMesh, stripes);
                ShowPiece(pin.Body, true);
                ShowPiece(pin.Stripes, true);

                // Tips over from its base faster and faster, then bumps down onto its side
                Quaternion turn = Quaternion.identity;
                float lift = 0f;
                if (pin.Fell >= 0f)
                {
                    float since = now - pin.Fell;
                    float fall = Mathf.Clamp01(since / 0.45f);
                    float bump = since > 0.45f ? Mathf.Abs(Mathf.Sin((since - 0.45f) * 20f)) * Mathf.Exp(-(since - 0.45f) * 8f) * 8f : 0f;
                    float tilt = 90f * fall * fall - bump;
                    if (!pin.Pushed && tilt > 45f)
                    {
                        pin.Pushed = true;
                        ToppleBowlingNeighbours(pin, now, scale);
                    }

                    turn = Quaternion.AngleAxis(tilt, Vector3.Cross(Vector3.up, pin.Toward));
                    lift = Mathf.Sin(tilt * Mathf.Deg2Rad) * 0.16f * height;
                }

                // Pops up when the pins are set
                float rise = Mathf.Clamp01((now - pin.Raised) / 0.3f);
                rise = Mathf.Sin(rise * Mathf.PI * 0.5f);

                Vector3 at = pin.Spot + Vector3.up * lift;
                pin.Body.Object.transform.SetPositionAndRotation(at, turn);
                pin.Body.Object.transform.localScale = Vector3.one * (height * rise);
                SetAlpha(pin.Body, body, 0.92f * alpha);

                pin.Stripes.Object.transform.SetPositionAndRotation(at, turn);
                pin.Stripes.Object.transform.localScale = Vector3.one * (height * rise);
                SetAlpha(pin.Stripes, stripes, alpha);
            }
        }

        private static void DrawBowlingBall(float now, float scale)
        {
            bool shown = bowlingStage == BowlingStage.Held || bowlingStage == BowlingStage.Rolling;
            if (shown)
            {
                Ensure(ref bowlingBall, PrimitiveType.Sphere, Color.white);
                Ensure(ref bowlingGlow, PrimitiveType.Sphere, Color.white);
                if (bowlingHoles == null || !bowlingHoles.Alive)
                    bowlingHoles = MakeDecal(BowlingHolesMesh, Color.black);
            }

            ShowPiece(bowlingBall, shown);
            ShowPiece(bowlingHoles, shown);
            ShowPiece(bowlingGlow, shown);
            if (!shown)
                return;

            // In your hand it turns with your hand; once bowled it rolls
            if (bowlingStage == BowlingStage.Held)
                bowlingSpin = GorillaTagger.Instance.rightHandTransform.rotation;

            float size = BowlingBallRadius * 2f * scale;
            bowlingBall.Object.transform.SetPositionAndRotation(bowlingAt, bowlingSpin);
            bowlingBall.Object.transform.localScale = Vector3.one * size;
            SetColor(bowlingBall, Color.Lerp(Accent, new Color(0.08f, 0.06f, 0.2f), 0.45f));

            bowlingHoles.Object.transform.SetPositionAndRotation(bowlingAt, bowlingSpin);
            bowlingHoles.Object.transform.localScale = Vector3.one * size;
            SetAlpha(bowlingHoles, new Color(0.02f, 0.02f, 0.05f), 0.95f);

            bowlingGlow.Object.transform.position = bowlingAt;
            bowlingGlow.Object.transform.localScale = Vector3.one * (size * (1.45f + 0.05f * Mathf.Sin(now * 5f)));
            SetAlpha(bowlingGlow, Second, 0.2f);
        }

        private static void ShowBowlingText(float scale)
        {
            Vector3 at = bowlingStage == BowlingStage.Placing
                ? Head.position + (LookFlat() + Vector3.up * 0.1f) * scale
                : BowlingRackCentre(scale) + Vector3.up * (0.9f * scale);
            PlaceArcadeText(bowlingText, at, scale);

            int down = 0;
            foreach (BowlingPin pin in bowlingPins)
                if (pin != null && pin.Down)
                    down++;

            if (!bowlingDirty && down == bowlingShownDown)
                return;

            bowlingDirty = false;
            bowlingShownDown = down;
            int score = BowlingScore();
            switch (bowlingStage)
            {
                case BowlingStage.Placing:
                    bowlingText.SafeSetText(bowlingNoGround ? "No flat ground ahead\n<size=55%>face a long open floor for the pins</size>" : "Setting up the pins...");
                    break;

                case BowlingStage.Result:
                    string what = bowlingResult == BowlingStrike ? "Strike!"
                        : bowlingResult == BowlingSpare ? "Spare!"
                        : bowlingLastRoll == 0 ? "Missed them all"
                        : bowlingLastRoll == 1 ? "1 pin" : $"{bowlingLastRoll} pins";
                    bowlingText.SafeSetText($"{what}\n<size=55%>frame {bowlingFrame}  ·  score {score}</size>");
                    break;

                case BowlingStage.Over:
                    bowlingText.SafeSetText($"Game over  ·  {score}\n<size=55%>best {bowlingBest}{(bowlingNewBest ? "  ·  new best!" : "")}\npress B to play again</size>");
                    break;

                default:
                    bowlingText.SafeSetText(bowlingStage == BowlingStage.Ready && bowlingRollCount == 0
                        ? $"Frame {bowlingFrame}  ·  ball {bowlingBallNumber}\n<size=55%>press B for the ball, swing and let go\nbest {bowlingBest}</size>"
                        : $"Frame {bowlingFrame}  ·  ball {bowlingBallNumber}\n<size=55%>{down} down  ·  score {score}</size>");
                    break;
            }

            bowlingText.color = bowlingStage == BowlingStage.Result || bowlingStage == BowlingStage.Over ? Accent : Color.Lerp(Color.white, Accent, 0.35f);
        }

        /// <summary>Takes away the pins, the ball and the scoreboard, and forgets the game.</summary>
        public static void DisableStarBowling()
        {
            foreach (BowlingPin pin in bowlingPins)
            {
                if (pin == null)
                    continue;

                DiscardPiece(ref pin.Body);
                DiscardPiece(ref pin.Stripes);
            }

            DiscardPiece(ref bowlingBall);
            DiscardPiece(ref bowlingHoles);
            DiscardPiece(ref bowlingGlow);
            DestroyText(ref bowlingText);
            bowlingStage = BowlingStage.Placing;
            bowlingRollCount = 0;
            bowlingFrame = bowlingBallNumber = 1;
            bowlingShownDown = -1;
        }

        // ── Game shapes ─────────────────────────────────────────────────────────

        private static Mesh golfFlagMesh, bowlingPinMesh, bowlingStripesMesh, bowlingHolesMesh;

        // A bowling pin one unit tall, from its base up to the top of its head
        private static readonly Vector2[] BowlingPinProfile =
        {
            new Vector2(0f, 0f), new Vector2(0.07f, 0f), new Vector2(0.12f, 0.08f), new Vector2(0.155f, 0.22f), new Vector2(0.16f, 0.3f),
            new Vector2(0.14f, 0.42f), new Vector2(0.09f, 0.54f), new Vector2(0.062f, 0.63f), new Vector2(0.07f, 0.72f),
            new Vector2(0.085f, 0.8f), new Vector2(0.075f, 0.9f), new Vector2(0.045f, 0.97f), new Vector2(0f, 1f)
        };

        /// <summary>A pennant hanging from the top of a flagpole along +Y, flying out along +X, seen from both sides.</summary>
        private static Mesh GolfFlagMesh
        {
            get
            {
                if (golfFlagMesh != null)
                    return golfFlagMesh;

                // Two panels with a slight fold down the middle, so it catches the light as it waves
                Vector3 top = Vector3.zero;
                Vector3 bottom = new Vector3(0f, -0.5f, 0f);
                Vector3 fold = new Vector3(0.4f, -0.25f, 0.04f);
                Vector3 tip = new Vector3(0.8f, -0.25f, 0f);
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                AddSheet(vertices, triangles, top, bottom, fold);
                AddSheet(vertices, triangles, top, fold, tip);
                AddSheet(vertices, triangles, bottom, tip, fold);
                return golfFlagMesh = FinishMesh("EclipseGolfFlag", vertices, triangles);
            }
        }

        /// <summary>A bowling pin standing on its base, one unit tall.</summary>
        private static Mesh BowlingPinMesh
        {
            get
            {
                if (bowlingPinMesh != null)
                    return bowlingPinMesh;

                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                AddLathe(vertices, triangles, BowlingPinProfile, 20);
                return bowlingPinMesh = FinishMesh("EclipseBowlingPin", vertices, triangles);
            }
        }

        /// <summary>The two painted stripes round a bowling pin's neck, just outside its surface.</summary>
        private static Mesh BowlingStripesMesh
        {
            get
            {
                if (bowlingStripesMesh != null)
                    return bowlingStripesMesh;

                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                AddBowlingStripe(vertices, triangles, 0.575f, 0.605f);
                AddBowlingStripe(vertices, triangles, 0.655f, 0.685f);
                return bowlingStripesMesh = FinishMesh("EclipseBowlingStripes", vertices, triangles);
            }
        }

        private static void AddBowlingStripe(List<Vector3> vertices, List<int> triangles, float from, float to)
        {
            Vector2[] band =
            {
                new Vector2(ProfileRadius(BowlingPinProfile, from) + 0.004f, from),
                new Vector2(ProfileRadius(BowlingPinProfile, to) + 0.004f, to)
            };
            AddLathe(vertices, triangles, band, 20);
        }

        /// <summary>Two finger holes and a thumb hole just outside a sphere primitive, which is one unit across.</summary>
        private static Mesh BowlingHolesMesh
        {
            get
            {
                if (bowlingHolesMesh != null)
                    return bowlingHolesMesh;

                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                Vector3[] holes = { new Vector3(-0.16f, 0.95f, 0.25f), new Vector3(0.16f, 0.95f, 0.25f), new Vector3(0f, 0.8f, -0.55f) };
                float[] sizes = { 0.055f, 0.055f, 0.07f };
                for (int i = 0; i < holes.Length; i++)
                {
                    Vector3 normal = holes[i].normalized;
                    AddToyDisc(vertices, triangles, normal * 0.503f, normal, sizes[i], 12);
                }

                return bowlingHolesMesh = FinishMesh("EclipseBowlingHoles", vertices, triangles);
            }
        }
    }
}
