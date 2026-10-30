/*
 * Eclipse Menu  Mods/EclipseGames.cs
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
using TMPro;
using UnityEngine;
using static Eclipse.Menu.Main;
using static Eclipse.Mods.Custom;
using Random = UnityEngine.Random;

namespace Eclipse.Mods
{
    /// <summary>The Space Games: little games only you can see and play, in the Space Games category.</summary>
    /// <remarks>
    /// Each game keeps a scoreboard floating where you play and remembers your best score between
    /// sessions, saved only when you beat it. Everything a game makes is your own and can't be
    /// bumped into, and nothing here touches another player or sends anything over the network.
    /// </remarks>
    public static partial class EclipseMods
    {
        // ── Ring Run ────────────────────────────────────────────────────────────

        private const int RingRunRings = 8;
        private const float RingRunRadius = 0.9f;
        private const string RingRunBestKey = "Eclipse_RingRun_Best";

        private static readonly LineRenderer[] ringRunRings = new LineRenderer[RingRunRings];
        private static readonly Vector3[] ringRunCentres = new Vector3[RingRunRings];
        private static readonly Vector3[] ringRunFacings = new Vector3[RingRunRings];
        private static LineRenderer ringRunGlow, ringRunGuide;
        private static TextMeshPro ringRunText;
        private static Vector3 ringRunLastHead, ringRunLastBody;
        private static float ringRunSize = 1f, ringRunStart, ringRunTime, ringRunPassed = -10f, ringRunBest;

        // The ring to go through next: -1 before there is a course, and one past the last once it is finished
        private static int ringRunNext = -1, ringRunFrame = -2, ringRunShown = int.MinValue, ringRunShownRing = -2;
        private static bool ringRunHeld = true, ringRunRecord;

        /// <summary>Forgets a held A and clears the course, when Ring Run is turned on.</summary>
        public static void EnableRingRun()
        {
            ringRunHeld = true;
            ringRunNext = -1;
            ringRunShown = int.MinValue;
        }

        /// <summary>Press A to lay out a winding course of eight glowing rings ahead of you, then go through them in order as fast as you can.</summary>
        public static void RingRun()
        {
            float now = Time.time;
            float scale = Scale;
            Vector3 head = Head.position;
            Vector3 body = BodyPosition;

            if (ringRunText == null)
            {
                ringRunText = MakeWristText("Eclipse_RingRun", Color.white);
                ringRunBest = PlayerPrefs.GetFloat(RingRunBestKey, 0f);
                ringRunShown = int.MinValue;
            }

            if (Pressed(rightPrimary, ref ringRunHeld))
                RingRunLay(body, scale);

            // Only from one frame to the next, so a gap while the mod was off never counts as going through
            if (ringRunNext >= 0 && ringRunNext < RingRunRings && ringRunFrame == Time.frameCount - 1)
                RingRunCross(head, body, now);
            ringRunLastHead = head;
            ringRunLastBody = body;
            ringRunFrame = Time.frameCount;

            bool racing = ringRunNext >= 0 && ringRunNext < RingRunRings;
            float size = ringRunSize;
            float radius = RingRunRadius * size;
            float pulse = 0.5f + 0.5f * Mathf.Sin(now * 6f);
            float flare = Mathf.Clamp01(1f - (now - ringRunPassed) / 0.5f);

            for (int i = 0; i < RingRunRings; i++)
            {
                // The rings still to come, and the one just gone through while it flares out
                bool shown = ringRunNext >= 0 && (i >= ringRunNext || (i == ringRunNext - 1 && flare > 0f));
                LineRenderer ring = ringRunRings[i];
                if (!shown)
                {
                    if (ring != null)
                        ring.enabled = false;
                    continue;
                }

                if (ring == null)
                    ring = ringRunRings[i] = MakeRing("Eclipse_RingRunRing");
                ring.enabled = true;

                if (i == ringRunNext)
                    PlaceRing(ring, ringRunCentres[i], ringRunFacings[i], radius * (1f + 0.03f * pulse), (0.05f + 0.025f * pulse) * size, Color.Lerp(Accent, Color.white, 0.35f * pulse));
                else if (i < ringRunNext)
                    PlaceRing(ring, ringRunCentres[i], ringRunFacings[i], radius * (1.6f - 0.6f * flare), 0.07f * size * flare, WithAlpha(Color.Lerp(Accent, Color.white, 0.5f), flare));
                else
                    PlaceRing(ring, ringRunCentres[i], ringRunFacings[i], radius, 0.025f * size, WithAlpha(Second, i == ringRunNext + 1 ? 0.4f : 0.18f));
            }

            if (racing)
            {
                Vector3 centre = ringRunCentres[ringRunNext];
                if (ringRunGlow == null)
                    ringRunGlow = MakeRing("Eclipse_RingRunGlow");
                if (ringRunGuide == null)
                    ringRunGuide = MakeLine("Eclipse_RingRunGuide", 0.01f);
                ringRunGlow.enabled = ringRunGuide.enabled = true;
                PlaceRing(ringRunGlow, centre, ringRunFacings[ringRunNext], radius, (0.14f + 0.08f * pulse) * size, WithAlpha(Second, 0.15f + 0.15f * pulse));

                // A faint line from your chest to the next ring, so it can always be found
                Vector3 way = centre - head;
                float distance = way.magnitude;
                Vector3 from = head + Vector3.down * (0.25f * scale);
                if (distance > 0.001f)
                    from += way * (0.3f * scale / distance);
                ringRunGuide.SetPosition(0, from);
                ringRunGuide.SetPosition(1, centre);
                ringRunGuide.startWidth = 0.006f * scale;
                ringRunGuide.endWidth = 0.03f * size;
                ringRunGuide.startColor = WithAlpha(Accent, 0.4f);
                ringRunGuide.endColor = WithAlpha(Accent, 0.1f);
            }
            else
            {
                if (ringRunGlow != null)
                    ringRunGlow.enabled = false;
                if (ringRunGuide != null)
                    ringRunGuide.enabled = false;
            }

            // Tenths into the run while the clock is going, -2 waiting at the first ring, -3 with no course and -4 finished
            int shownTime = !racing ? (ringRunNext < 0 ? -3 : -4) : ringRunNext == 0 ? -2 : Mathf.FloorToInt((now - ringRunStart) * 10f);
            if (shownTime != ringRunShown || ringRunNext != ringRunShownRing)
            {
                ringRunShown = shownTime;
                ringRunShownRing = ringRunNext;
                string best = ringRunBest > 0f ? $"best {LapClock(Mathf.FloorToInt(ringRunBest * 10f))}" : "no best yet";
                ringRunText.SafeSetText(shownTime == -3 ? $"Ring Run\n<size=55%>A to lay out a course  ·  {best}</size>"
                    : shownTime == -2 ? $"Ring 1 of {RingRunRings}\n<size=55%>go through it to start the clock  ·  {best}</size>"
                    : shownTime == -4 ? $"{(ringRunRecord ? "New best!" : "Finished!")}  {LapClock(Mathf.FloorToInt(ringRunTime * 10f))}\n<size=55%>{best}  ·  A for a new course</size>"
                    : $"{LapClock(shownTime)}\n<size=55%>ring {ringRunNext + 1} of {RingRunRings}  ·  {best}</size>");
            }

            // Over the next ring, growing with distance so it can be read from afar; otherwise above your right hand
            if (racing)
            {
                Vector3 centre = ringRunCentres[ringRunNext];
                float textSize = Mathf.Clamp(Vector3.Distance(head, centre) / scale * 0.03f, 0.06f, 0.3f);
                PlaceToyText(ringRunText, centre + Vector3.up * (radius + (0.08f + textSize * 0.45f) * scale), textSize);
            }
            else
                PlaceToyText(ringRunText, RightHand + Vector3.up * (0.16f * scale), 0.05f);

            ringRunText.color = shownTime == -4 && ringRunRecord ? Accent : Color.Lerp(Color.white, Accent, 0.35f);
        }

        // Lays a new course out ahead of you. Each ring is a few metres on from the one before, turning a
        // little left or right but never back on itself, and stands 1 to 4 m above the ground under it,
        // higher further along, so it can be reached by climbing and jumping as usual. A spot with no
        // ground under it, inside something solid or behind a wall from the ring before is passed over.
        private static void RingRunLay(Vector3 body, float scale)
        {
            int layers = GTPlayer.Instance.locomotionEnabledLayers;
            Vector3 ahead = LookFlat();
            Vector3 previous = body;
            float floor = Feet.y;
            float bend = 0f;
            float radius = RingRunRadius * scale;

            for (int i = 0; i < RingRunRings; i++)
            {
                Vector3 centre = Vector3.zero;
                bool found = false;
                for (int attempt = 0; attempt < 12 && !found; attempt++)
                {
                    // Later tries may turn harder, to find a way round whatever is in the way
                    float widest = attempt < 4 ? 40f : 85f;
                    float turn = Mathf.Clamp(bend + Random.Range(-widest, widest), -80f, 80f);
                    float step = (i == 0 ? Random.Range(3f, 3.5f) : Random.Range(2.4f, 3.1f)) * scale;
                    Vector3 spot = previous + Quaternion.Euler(0f, turn, 0f) * ahead * step;

                    if (!Physics.Raycast(new Vector3(spot.x, floor + 5f * scale, spot.z), Vector3.down, out RaycastHit hit, 10f * scale, layers, QueryTriggerInteraction.Ignore))
                        continue;

                    float lift = (Random.Range(1.1f, 2f) + Random.Range(0f, 1.8f) * i / (RingRunRings - 1f)) * scale;
                    centre = new Vector3(spot.x, hit.point.y + lift, spot.z);
                    if (Physics.CheckSphere(centre, radius * 0.6f, layers, QueryTriggerInteraction.Ignore))
                        continue;
                    if (attempt < 8 && Physics.Linecast(previous, centre, layers, QueryTriggerInteraction.Ignore))
                        continue;

                    bend = turn;
                    floor = hit.point.y;
                    found = true;
                }

                // Nowhere better: straight on, at a height that suits the last ground found
                if (!found)
                {
                    centre = previous + Quaternion.Euler(0f, bend, 0f) * ahead * (2.8f * scale);
                    centre.y = floor + 1.5f * scale;
                }

                ringRunCentres[i] = centre;
                previous = centre;
            }

            // Each ring stands upright, turned halfway between the way in and the way on to the next
            for (int i = 0; i < RingRunRings; i++)
            {
                Vector3 into = ringRunCentres[i] - (i == 0 ? body : ringRunCentres[i - 1]);
                Vector3 onward = i + 1 < RingRunRings ? ringRunCentres[i + 1] - ringRunCentres[i] : into;
                Vector3 facing = into.normalized + onward.normalized;
                facing.y = 0f;
                ringRunFacings[i] = facing.sqrMagnitude > 0.0001f ? facing.normalized : ahead;
            }

            ringRunSize = scale;
            ringRunNext = 0;
            ringRunPassed = -10f;
            ringRunShown = int.MinValue;
            Burst(ringRunCentres[0], 20, 1.2f, false);
        }

        // Going through a ring means your head or body crossing its flat face inside its edge since the last
        // frame, from either side. A jump of metres in a single frame is a teleport and never counts.
        private static void RingRunCross(Vector3 head, Vector3 body, float now)
        {
            float jump = 4f * ringRunSize;
            if ((body - ringRunLastBody).sqrMagnitude > jump * jump)
                return;

            Vector3 centre = ringRunCentres[ringRunNext];
            Vector3 facing = ringRunFacings[ringRunNext];
            float radius = RingRunRadius * ringRunSize;
            if (RingRunThrough(ringRunLastHead, head, centre, facing, radius) || RingRunThrough(ringRunLastBody, body, centre, facing, radius))
                RingRunPass(now);
        }

        // Whether a point moving from `from` to `to` changed sides of the ring's face, and where it crossed was inside the ring
        private static bool RingRunThrough(Vector3 from, Vector3 to, Vector3 centre, Vector3 facing, float radius)
        {
            float before = Vector3.Dot(from - centre, facing);
            float after = Vector3.Dot(to - centre, facing);
            if ((before > 0f) == (after > 0f))
                return false;

            Vector3 crossed = Vector3.Lerp(from, to, before / (before - after));
            return (crossed - centre).sqrMagnitude < radius * radius;
        }

        private static void RingRunPass(float now)
        {
            Vector3 centre = ringRunCentres[ringRunNext];
            if (ringRunNext == 0)
                ringRunStart = now;
            ringRunNext++;
            ringRunPassed = now;
            GorillaTagger.Instance.StartVibration(true, GorillaTagger.Instance.tapHapticStrength / 2f, 0.05f);
            GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.tapHapticStrength / 2f, 0.05f);

            if (ringRunNext < RingRunRings)
            {
                Burst(centre, 14, 1.5f, false);
                VRRig.LocalRig.PlayHandTapLocal(244, false, 0.3f);
                return;
            }

            // Through the last ring: the run is over, and lower times are better
            ringRunTime = now - ringRunStart;
            ringRunRecord = ringRunBest <= 0f || ringRunTime < ringRunBest;
            if (ringRunRecord)
            {
                ringRunBest = ringRunTime;
                PlayerPrefs.SetFloat(RingRunBestKey, ringRunBest);
                PlayerPrefs.Save();
            }

            if (ringRunTime < 40f)
                Eclipse.Menu.Main.EclipseUnlock("Ring Racer");

            Burst(centre, 70, 3.5f, false);
            VRRig.LocalRig.PlayHandTapLocal(213, false, 0.4f);
            ringRunShown = int.MinValue;
        }

        /// <summary>Takes away the course of rings and the Ring Run scoreboard.</summary>
        public static void DisableRingRun()
        {
            for (int i = 0; i < ringRunRings.Length; i++)
                DestroyLine(ref ringRunRings[i]);
            DestroyLine(ref ringRunGlow);
            DestroyLine(ref ringRunGuide);
            DestroyText(ref ringRunText);
            ringRunNext = -1;
            ringRunShown = int.MinValue;
        }

        // ── Asteroid Dodge ──────────────────────────────────────────────────────

        private const int DodgeRocks = 14;
        private const float DodgeReach = 0.35f;
        private const float DodgeRange = 25f;
        private const string DodgeBestKey = "Eclipse_AsteroidDodge_Best";

        // Each rock's Velocity is how it flies, Size how big it is, Anchor its lumpy shape and Seed how it tumbles
        private static readonly Piece[] dodgeRocks = new Piece[DodgeRocks];
        private static Piece dodgeFlash;
        private static LineRenderer dodgeField;
        private static TextMeshPro dodgeText;
        private static Vector3 dodgeAhead = Vector3.forward, dodgeLastHead;
        private static float dodgeStart, dodgeNext, dodgeOver = -1f, dodgeTime, dodgeBest;
        private static int dodgeShown = int.MinValue;
        private static bool dodgeRecord, dodgeSurvivor;

        /// <summary>Rocks fly at you from out in front, faster and more often the longer you last; dodge them for as long as you can.</summary>
        public static void AsteroidDodge()
        {
            float now = Time.time;
            float dt = Time.deltaTime;
            float scale = Scale;
            Vector3 head = Head.position;

            if (dodgeText == null)
            {
                dodgeText = MakeWristText("Eclipse_AsteroidDodge", Color.white);
                dodgeBest = PlayerPrefs.GetFloat(DodgeBestKey, 0f);
                dodgeLastHead = head;
                DodgeBegin(now);
            }

            // After a hit your time stays up for a few seconds, then a new game starts by itself
            if (dodgeOver >= 0f && now - dodgeOver > 3f)
                DodgeBegin(now);
            bool playing = dodgeOver < 0f;
            float survived = playing ? now - dodgeStart : dodgeTime;

            if (playing && now >= dodgeNext)
            {
                dodgeNext = now + Mathf.Max(0.35f, 1.5f - survived * 0.02f);
                DodgeLaunch(head, survived, scale);
            }

            if (playing && survived >= 60f && !dodgeSurvivor)
            {
                dodgeSurvivor = true;
                Eclipse.Menu.Main.EclipseUnlock("Survivor");
                VRRig.LocalRig.PlayHandTapLocal(244, false, 0.35f);
            }

            // A jump of metres in one frame is a teleport, so then only where your head is now counts
            Vector3 lastHead = (head - dodgeLastHead).sqrMagnitude > 9f * scale * scale ? head : dodgeLastHead;
            dodgeLastHead = head;

            float reach = DodgeReach * scale;
            Color stone = Color.Lerp(new Color(0.62f, 0.55f, 0.48f), Accent, 0.25f);
            for (int i = 0; i < dodgeRocks.Length; i++)
            {
                Piece rock = dodgeRocks[i];
                if (rock == null || !rock.Alive || !rock.Object.activeSelf)
                    continue;

                Transform body = rock.Object.transform;
                Vector3 from = body.position;
                Vector3 to = from + rock.Velocity * dt;
                float grow = Mathf.Clamp01((now - rock.Born) / 0.4f);
                body.SetPositionAndRotation(to, Quaternion.Euler(now * 70f + rock.Seed, now * 45f + rock.Seed * 2f, rock.Seed * 3f));
                body.localScale = rock.Anchor * (rock.Size * scale * grow);
                SetColor(rock, stone);

                // Back in the pool once it has gone a few metres past you, or been left far behind by a teleport
                float speed = rock.Velocity.magnitude;
                float past = speed > 0.001f ? Vector3.Dot(to - head, rock.Velocity) / speed : 0f;
                if (past > 4f * scale || (to - head).sqrMagnitude > 2500f * scale * scale)
                {
                    rock.Object.SetActive(false);
                    continue;
                }

                if (Random.value < 0.25f)
                    Burst(to, 1, 0.3f, false, -rock.Velocity);

                // Measured from your head as it moved this frame, so a fast rock can't skip past it
                if (playing && DodgeMiss(from - lastHead, to - head) < reach)
                {
                    DodgeCrash(now, to);
                    playing = false;
                }
            }

            // A red flash all round you when you're hit
            float flash = dodgeOver >= 0f ? Mathf.Clamp01(1f - (now - dodgeOver) / 0.6f) : 0f;
            if (flash > 0f)
            {
                if (dodgeFlash == null || !dodgeFlash.Alive)
                {
                    dodgeFlash = MakePiece(PrimitiveType.Sphere, Color.red, Vector3.one);

                    // Drawn from the inside too, since you are in the middle of it
                    dodgeFlash.Renderer.sharedMaterial.SetFloat("_Cull", 0f);
                }

                ShowPiece(dodgeFlash, true);
                dodgeFlash.Object.transform.position = head;
                dodgeFlash.Object.transform.localScale = Vector3.one * (0.6f * scale);
                SetAlpha(dodgeFlash, new Color(1f, 0.12f, 0.1f), 0.45f * flash);
            }
            else
                ShowPiece(dodgeFlash, false);

            // A faint ring far out in front, where the rocks come from
            if (dodgeField == null)
                dodgeField = MakeRing("Eclipse_AsteroidField");
            PlaceRing(dodgeField, head + (dodgeAhead * DodgeRange + Vector3.up * 1.2f) * scale, dodgeAhead, 6f * scale, 0.12f * scale, WithAlpha(Second, playing ? 0.22f : 0.08f));

            // Whole seconds while playing, -2 once you've been hit
            int shown = playing ? Mathf.FloorToInt(survived) : -2;
            if (shown != dodgeShown)
            {
                dodgeShown = shown;
                string best = dodgeBest > 0f ? $"best {dodgeBest:0.0} s" : "no best yet";
                dodgeText.SafeSetText(playing
                    ? $"{shown} s\n<size=55%>{best}</size>"
                    : $"{(dodgeRecord ? "New best!" : "Hit!")}  {dodgeTime:0.0} s\n<size=55%>{best}  ·  again in a moment</size>");
            }

            // Up and out in front of you, the way the rocks come from
            PlaceToyText(dodgeText, head + (dodgeAhead * 1.4f + Vector3.up * 0.5f) * scale, 0.08f);
            dodgeText.color = playing ? Color.Lerp(Color.white, Accent, 0.35f) : dodgeRecord ? Accent : new Color(1f, 0.45f, 0.4f);
        }

        // How close a rock came to your head between one frame and the next, both ends measured from your head
        private static float DodgeMiss(Vector3 from, Vector3 to)
        {
            Vector3 step = to - from;
            float length = step.sqrMagnitude;
            float t = length > 1e-8f ? Mathf.Clamp01(-Vector3.Dot(from, step) / length) : 0f;
            return (from + step * t).magnitude;
        }

        private static void DodgeBegin(float now)
        {
            dodgeAhead = LookFlat();
            dodgeStart = now;
            dodgeNext = now + 1.5f;
            dodgeOver = -1f;
            dodgeSurvivor = false;
            dodgeShown = int.MinValue;
        }

        // Sends a rock from somewhere out in front, aimed at your head give or take a little.
        // The rocks get faster the longer you last, up to a limit.
        private static void DodgeLaunch(Vector3 head, float survived, float scale)
        {
            for (int i = 0; i < dodgeRocks.Length; i++)
            {
                Piece rock = dodgeRocks[i];
                if (rock != null && rock.Alive && rock.Object.activeSelf)
                    continue;

                rock = SkyEnsureShape(ref dodgeRocks[i], BeltRockMesh, SkyFacetShade, SkySolidQueue, true);
                Vector3 side = Vector3.Cross(Vector3.up, dodgeAhead);
                Vector3 from = head + (dodgeAhead * DodgeRange + side * Random.Range(-7f, 7f) + Vector3.up * Random.Range(-1.5f, 4f)) * scale;
                Vector3 aim = head + Random.insideUnitSphere * (0.6f * scale);
                float speed = Mathf.Min(5f + survived * 0.2f, 16f) * Random.Range(0.9f, 1.1f) * scale;

                rock.Velocity = (aim - from).normalized * speed;
                rock.Size = Random.Range(0.22f, 0.42f);
                rock.Anchor = new Vector3(Random.Range(0.8f, 1.2f), Random.Range(0.75f, 1.1f), Random.Range(0.8f, 1.2f));
                rock.Seed = Random.value * 360f;
                rock.Born = Time.time;
                rock.Object.transform.position = from;
                rock.Object.transform.localScale = Vector3.zero;
                rock.Object.SetActive(true);
                return;
            }
        }

        private static void DodgeCrash(float now, Vector3 at)
        {
            dodgeOver = now;
            dodgeTime = now - dodgeStart;
            dodgeRecord = dodgeTime > dodgeBest;
            if (dodgeRecord)
            {
                dodgeBest = dodgeTime;
                PlayerPrefs.SetFloat(DodgeBestKey, dodgeBest);
                PlayerPrefs.Save();
            }

            Burst(at, 40, 3f, false);
            foreach (Piece rock in dodgeRocks)
            {
                if (rock == null || !rock.Alive || !rock.Object.activeSelf)
                    continue;

                Burst(rock.Object.transform.position, 12, 1.5f, false);
                rock.Object.SetActive(false);
            }

            GorillaTagger.Instance.StartVibration(true, GorillaTagger.Instance.tagHapticStrength, 0.3f);
            GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.tagHapticStrength, 0.3f);
            VRRig.LocalRig.PlayHandTapLocal(68, false, 0.5f);
            dodgeShown = int.MinValue;
        }

        /// <summary>Takes away the rocks, the red flash and the Asteroid Dodge scoreboard.</summary>
        public static void DisableAsteroidDodge()
        {
            for (int i = 0; i < dodgeRocks.Length; i++)
                DiscardPiece(ref dodgeRocks[i]);
            DiscardPiece(ref dodgeFlash);
            DestroyLine(ref dodgeField);
            DestroyText(ref dodgeText);
            dodgeOver = -1f;
            dodgeShown = int.MinValue;
        }

        // ── Comet Squash ────────────────────────────────────────────────────────

        // The ball and your hands in metres, the wall's size and how far off it stands, and a pull a bit lighter than real
        private const float SquashBallRadius = 0.08f;
        private const float SquashHandRadius = 0.07f;
        private const float SquashWallDistance = 3f;
        private const float SquashWallWidth = 3.2f;
        private const float SquashWallHeight = 3f;
        private const float SquashGravity = 6f;
        private const string SquashBestKey = "Eclipse_CometSquash_Best";

        private static readonly Vector3[] squashCorners = new Vector3[4];
        private static Piece squashBall, squashGlow, squashWall;
        private static LineRenderer squashEdge, squashSpot;
        private static TrailRenderer squashTrail;
        private static TextMeshPro squashText;
        private static Vector3 squashBase, squashFacing, squashVelocity, squashLastLeft, squashLastRight, squashSpotAt;
        private static float squashSize = 1f, squashServeAt = -1f, squashLastHit = -10f, squashWallHit = -10f;
        private static int squashRally, squashBest, squashShown = int.MinValue;

        // squashToWall: you hit it and it is on its way to the wall. squashReturn: it came back off the wall and is yours to return.
        private static bool squashPlaced, squashInPlay, squashPlayed, squashToWall, squashReturn, squashRecord;

        /// <summary>Squash against a glowing see-through wall in front of you: hit the comet ball at the wall with your hands and keep the rally going.</summary>
        public static void CometSquash()
        {
            float now = Time.time;
            float dt = Mathf.Max(Time.deltaTime, 0.001f);
            float scale = Scale;
            Vector3 left = LeftHand;
            Vector3 right = RightHand;

            if (squashText == null)
            {
                squashText = MakeWristText("Eclipse_CometSquash", Color.white);
                squashBest = PlayerPrefs.GetInt(SquashBestKey, 0);
                squashShown = int.MinValue;
            }

            // The wall goes up once, in front of you, and again only if you end up far away from it
            if (!squashPlaced || (BodyPosition - squashBase).sqrMagnitude > 225f * scale * scale)
                SquashPlaceWall(now, scale);

            if (squashBall == null || !squashBall.Alive)
            {
                squashBall = MakePiece(PrimitiveType.Sphere, Color.white, Vector3.one);
                squashInPlay = false;
                if (squashServeAt < 0f)
                    squashServeAt = now + 0.5f;
            }

            Ensure(ref squashGlow, PrimitiveType.Sphere, Color.white);

            // Its own object rather than part of the ball, so taking the ball away never takes the shared line material with it
            if (squashTrail == null)
            {
                squashTrail = new GameObject("Eclipse_CometSquashTrail").AddComponent<TrailRenderer>();
                squashTrail.material = VertexColored;
                squashTrail.time = 0.35f;
                squashTrail.minVertexDistance = 0.03f;
                squashTrail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                squashTrail.emitting = false;
            }

            Vector3 leftVelocity = Vector3.ClampMagnitude((left - squashLastLeft) / dt, 14f * scale);
            Vector3 rightVelocity = Vector3.ClampMagnitude((right - squashLastRight) / dt, 14f * scale);

            Transform ball = squashBall.Object.transform;
            float radius = SquashBallRadius * scale;
            if (!squashInPlay)
            {
                if (squashServeAt >= 0f && now >= squashServeAt)
                    SquashServe(ball, scale);
            }
            else
            {
                // Moved in steps small enough that neither the ball nor a fast swing can jump right past the
                // other, with each hand partway along its own path for each step
                float reach = radius + SquashHandRadius * scale;
                float travel = (squashVelocity.magnitude + Mathf.Max(leftVelocity.magnitude, rightVelocity.magnitude)) * dt;
                int steps = Mathf.Clamp(Mathf.CeilToInt(travel / (radius * 0.75f)), 1, 12);
                float step = dt / steps;
                float drag = Mathf.Exp(-0.1f * step);
                Vector3 position = ball.position;
                for (int s = 1; s <= steps; s++)
                {
                    float u = (float)s / steps;
                    squashVelocity += Vector3.down * (SquashGravity * scale * step);
                    squashVelocity *= drag;
                    position += squashVelocity * step;

                    SquashOffWall(ref position, now, radius, scale);
                    SquashOffHand(ref position, Vector3.Lerp(squashLastLeft, left, u), leftVelocity, true, now, reach, scale);
                    SquashOffHand(ref position, Vector3.Lerp(squashLastRight, right, u), rightVelocity, false, now, reach, scale);
                }

                ball.position = position;

                // The rally is over if it falls below your feet, gets well behind you, goes past the wall or gets away
                float depth = Vector3.Dot(position - squashBase, squashFacing);
                float you = Vector3.Dot(BodyPosition - squashBase, squashFacing);
                if (position.y < Mathf.Min(Feet.y, squashBase.y) - 0.1f * scale || depth > you + 1.5f * scale || depth < -1f * scale
                    || (position - BodyPosition).sqrMagnitude > 144f * scale * scale)
                    SquashMiss(now, position);
            }

            squashLastLeft = left;
            squashLastRight = right;

            ShowPiece(squashBall, squashInPlay);
            ShowPiece(squashGlow, squashInPlay);
            if (squashInPlay)
            {
                float hit = Mathf.Exp(-(now - squashLastHit) * 8f);
                ball.localScale = Vector3.one * (radius * 2f * (1f + 0.2f * hit));
                SetColor(squashBall, Color.Lerp(Color.Lerp(Accent, Color.white, 0.4f), Color.white, 0.5f * hit));

                squashGlow.Object.transform.position = ball.position;
                squashGlow.Object.transform.localScale = Vector3.one * (radius * 3.4f * (1f + 0.06f * Mathf.Sin(now * 9f)));
                SetAlpha(squashGlow, Second, 0.25f + 0.2f * hit);
            }

            squashTrail.transform.position = ball.position;
            squashTrail.widthMultiplier = radius * 1.6f;
            squashTrail.startColor = WithAlpha(Accent, 0.75f);
            squashTrail.endColor = WithAlpha(Second, 0f);

            // The wall: a faint glowing pane with a bright edge, flaring where the ball hits it
            float flare = Mathf.Clamp01(1f - (now - squashWallHit) / 0.5f);
            Ensure(ref squashWall, PrimitiveType.Cube, Color.white);
            squashWall.Object.transform.SetPositionAndRotation(squashBase + Vector3.up * (SquashWallHeight * 0.5f * squashSize), Quaternion.LookRotation(squashFacing));
            squashWall.Object.transform.localScale = new Vector3(SquashWallWidth, SquashWallHeight, 0.02f) * squashSize;
            SetAlpha(squashWall, Second, 0.08f + 0.1f * flare);

            if (squashEdge == null)
            {
                squashEdge = MakeLine("Eclipse_CometSquashEdge", 0.03f);
                squashEdge.positionCount = squashCorners.Length;
                squashEdge.loop = true;
            }

            squashEdge.SetPositions(squashCorners);
            squashEdge.startWidth = squashEdge.endWidth = 0.03f * squashSize;
            squashEdge.startColor = squashEdge.endColor = Color.Lerp(Accent, Color.white, 0.4f * flare);

            if (squashSpot == null)
                squashSpot = MakeRing("Eclipse_CometSquashSpot");
            squashSpot.enabled = flare > 0f;
            if (flare > 0f)
                PlaceRing(squashSpot, squashSpotAt, squashFacing, (0.1f + 0.4f * (1f - flare)) * squashSize, 0.04f * squashSize * flare, WithAlpha(Color.Lerp(Accent, Color.white, 0.5f), flare));

            // The rally so far while the ball is in play, -2 between rallies and -3 before the first serve
            int shown = squashInPlay ? squashRally : squashPlayed ? -2 : -3;
            if (shown != squashShown)
            {
                squashShown = shown;
                squashText.SafeSetText(shown >= 0 ? $"{squashRally}\n<size=55%>rally  ·  best {Mathf.Max(squashBest, squashRally)}</size>"
                    : shown == -2 ? $"{(squashRecord ? "New best!" : "Rally over")}\n<size=55%>{squashRally} in the rally  ·  best {squashBest}</size>"
                    : $"Comet Squash\n<size=55%>hit the comet at the wall  ·  best {squashBest}</size>");
            }

            // High on the wall, where it can be read from where you stand
            PlaceToyText(squashText, squashBase + Vector3.up * (2.35f * squashSize) + squashFacing * (0.05f * squashSize), 0.2f);
            squashText.color = shown == -2 ? Accent : Color.Lerp(Color.white, Accent, 0.35f);
        }

        // Stands the wall up a few metres in front of you and facing you, from the ground under it up to about 3 m
        private static void SquashPlaceWall(float now, float scale)
        {
            squashFacing = -LookFlat();
            squashSize = scale;
            Vector3 spot = BodyPosition - squashFacing * (SquashWallDistance * scale);

            // On the ground under the wall if there is some near your own height, otherwise level with your feet
            float floor = Feet.y;
            if (Physics.Raycast(new Vector3(spot.x, Head.position.y + 0.5f * scale, spot.z), Vector3.down, out RaycastHit hit, 4f * scale, GTPlayer.Instance.locomotionEnabledLayers, QueryTriggerInteraction.Ignore)
                && hit.point.y < floor + 1f * scale)
                floor = hit.point.y;
            squashBase = new Vector3(spot.x, floor, spot.z);

            Vector3 across = Vector3.Cross(Vector3.up, squashFacing) * (SquashWallWidth * 0.5f * scale);
            Vector3 top = Vector3.up * (SquashWallHeight * scale);
            squashCorners[0] = squashBase - across;
            squashCorners[1] = squashBase - across + top;
            squashCorners[2] = squashBase + across + top;
            squashCorners[3] = squashBase + across;

            squashPlaced = true;
            squashInPlay = false;
            squashServeAt = now + 1f;
            squashLastLeft = LeftHand;
            squashLastRight = RightHand;
            squashShown = int.MinValue;
            if (squashTrail != null)
                squashTrail.emitting = false;
        }

        // Lobs the ball off the wall to drop in about chest high just in front of you
        private static void SquashServe(Transform ball, float scale)
        {
            Vector3 across = Vector3.Cross(Vector3.up, squashFacing);
            Vector3 from = squashBase + squashFacing * (0.3f * squashSize) + Vector3.up * (1.8f * squashSize) + across * (Random.Range(-0.6f, 0.6f) * squashSize);
            Vector3 to = Head.position - squashFacing * (0.55f * scale) + Vector3.down * (0.25f * scale) + across * (Random.Range(-0.2f, 0.2f) * scale);
            const float flight = 1.1f;
            squashVelocity = (to - from) / flight + Vector3.up * (0.5f * SquashGravity * scale * flight);

            ball.position = from;
            squashInPlay = true;
            squashServeAt = -1f;
            squashToWall = squashReturn = false;
            squashRally = 0;
            squashShown = int.MinValue;

            squashTrail.transform.position = from;
            squashTrail.Clear();
            squashTrail.emitting = true;
            Burst(from, 12, 1f, false, squashFacing);
        }

        // Bounces the ball back off the wall's face, if it reaches the wall inside its edges while heading into it
        private static void SquashOffWall(ref Vector3 position, float now, float radius, float scale)
        {
            Vector3 offset = position - squashBase;
            float depth = Vector3.Dot(offset, squashFacing);
            float closing = Vector3.Dot(squashVelocity, squashFacing);
            if (depth >= radius || depth < -0.5f * scale || closing >= 0f)
                return;

            float across = Vector3.Dot(offset, Vector3.Cross(Vector3.up, squashFacing));
            if (Mathf.Abs(across) > SquashWallWidth * 0.5f * squashSize + radius || offset.y < -radius || offset.y > SquashWallHeight * squashSize + radius)
                return;

            // Back off the wall a little slower than it came in, but always with enough pace to reach you
            position += squashFacing * (radius - depth);
            float back = Mathf.Max(-closing * 0.85f, 2.5f * scale);
            squashVelocity += squashFacing * (back - closing);

            if (squashToWall)
            {
                squashToWall = false;
                squashReturn = true;
            }

            squashWallHit = now;
            squashSpotAt = position - squashFacing * radius;
            Burst(squashSpotAt, 10, 1f, false, squashFacing);
            VRRig.LocalRig.PlayHandTapLocal(67, false, 0.3f);
        }

        // Bats the ball off one hand, taking on that hand's speed. Returning it after it came off the wall
        // scores a point, as long as it is hit from a proper distance rather than pinned against the wall.
        private static void SquashOffHand(ref Vector3 position, Vector3 hand, Vector3 handVelocity, bool left, float now, float reach, float scale)
        {
            Vector3 away = position - hand;
            float distance = away.magnitude;
            if (distance >= reach)
                return;

            Vector3 normal = distance > 0.0001f ? away / distance : -squashFacing;
            position = hand + normal * reach;

            float closing = Vector3.Dot(squashVelocity - handVelocity, normal);
            if (closing >= 0f)
                return;

            squashVelocity -= normal * (closing * 1.6f);

            // A little help: some of any sideways pace on a shot towards the wall is taken off
            Vector3 across = Vector3.Cross(Vector3.up, squashFacing);
            if (Vector3.Dot(squashVelocity, squashFacing) < 0f)
                squashVelocity -= across * (Vector3.Dot(squashVelocity, across) * 0.3f);
            squashVelocity = Vector3.ClampMagnitude(squashVelocity, 12f * scale);

            // The same swing brushing the ball twice only counts once
            if (now - squashLastHit < 0.12f)
                return;
            squashLastHit = now;

            if (squashReturn && Vector3.Dot(position - squashBase, squashFacing) > 1f * squashSize)
            {
                squashRally++;
                Burst(position, 10, 1f, false);
            }

            squashReturn = false;
            squashToWall = true;
            GorillaTagger.Instance.StartVibration(left, GorillaTagger.Instance.tapHapticStrength / 2f, 0.04f);
            VRRig.LocalRig.PlayHandTapLocal(84, left, 0.3f);
        }

        private static void SquashMiss(float now, Vector3 at)
        {
            squashInPlay = false;
            squashPlayed = true;
            squashServeAt = now + 1.6f;
            squashRecord = squashRally > squashBest;
            if (squashRecord)
            {
                squashBest = squashRally;
                PlayerPrefs.SetInt(SquashBestKey, squashBest);
                PlayerPrefs.Save();
            }

            if (squashTrail != null)
                squashTrail.emitting = false;
            Burst(at, 20, 1.5f, false);
            VRRig.LocalRig.PlayHandTapLocal(48, false, 0.3f);
            squashShown = int.MinValue;
        }

        /// <summary>Takes away the wall, the comet ball and the Comet Squash scoreboard.</summary>
        public static void DisableCometSquash()
        {
            DiscardPiece(ref squashBall);
            DiscardPiece(ref squashGlow);
            DiscardPiece(ref squashWall);
            DestroyLine(ref squashEdge);
            DestroyLine(ref squashSpot);
            if (squashTrail != null)
                Object.Destroy(squashTrail.gameObject);
            squashTrail = null;
            DestroyText(ref squashText);
            squashPlaced = squashInPlay = squashPlayed = false;
            squashServeAt = -1f;
            squashRally = 0;
            squashShown = int.MinValue;
        }

        // ── Simon Stars ─────────────────────────────────────────────────────────

        private enum SimonStage { Waiting, Showing, Turn, Cleared, Over }

        private const int SimonCount = 4;
        private const int SimonLongest = 100;
        private const float SimonStarSize = 0.15f;
        private const string SimonBestKey = "Eclipse_SimonStars_Best";

        // Upper left, upper right, lower left and lower right, each with its own colour and note
        private static readonly Vector2[] SimonPlaces = { new Vector2(-1f, 1f), new Vector2(1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f) };
        private static readonly Color[] SimonStarColors = { new Color(1f, 0.8f, 0.25f), new Color(0.3f, 0.8f, 1f), new Color(1f, 0.4f, 0.85f), new Color(0.4f, 1f, 0.5f) };
        private static readonly int[] SimonStarNotes = { 296, 299, 302, 305 };

        private static readonly Piece[] simonStars = new Piece[SimonCount];
        private static readonly Piece[] simonGlows = new Piece[SimonCount];
        private static readonly Vector3[] simonSpots = new Vector3[SimonCount];
        private static readonly float[] simonStarLit = new float[SimonCount];
        private static readonly int[] simonOrder = new int[SimonLongest];

        // The star each hand is in, left then right, or -1
        private static readonly int[] simonTouching = { -1, -1 };
        private static TextMeshPro simonBoard;
        private static Vector3 simonAnchor, simonAhead = Vector3.forward;
        private static SimonStage simonStage;
        private static float simonAt, simonOverAt = -10f;
        private static int simonRound, simonStep, simonBest, simonStartBest, simonShown = int.MinValue;
        private static bool simonPlaced, simonFollow, simonTurning, simonWon, simonRecord;

        /// <summary>Four big coloured stars float within reach in front of you and light up in a growing sequence; touch them in the same order with either hand.</summary>
        public static void SimonStars()
        {
            float now = Time.time;
            float dt = Time.deltaTime;
            float scale = Scale;
            Vector3 head = Head.position;

            if (simonBoard == null)
            {
                simonBoard = MakeWristText("Eclipse_SimonStars", Color.white);
                simonBest = PlayerPrefs.GetInt(SimonBestKey, 0);
                simonShown = int.MinValue;
            }

            if (!simonPlaced)
            {
                simonAhead = LookFlat();
                simonAnchor = SimonHome(head, scale);
                simonPlaced = true;
                simonStage = SimonStage.Waiting;
                simonAt = now + 1.2f;
                simonRound = simonStep = 0;
                simonTouching[0] = simonTouching[1] = -1;
            }

            // The stars hold still while you reach for them, and only come after you once you have moved
            // off or turned well away
            Vector3 look = LookFlat();
            float turned = Vector3.Angle(simonAhead, look);
            if (turned > 70f)
                simonTurning = true;
            if (simonTurning)
            {
                simonAhead = Vector3.Slerp(simonAhead, look, 1f - Mathf.Exp(-3f * dt));
                if (turned < 5f)
                    simonTurning = false;
            }

            Vector3 home = SimonHome(head, scale);
            float off = (simonAnchor - home).magnitude;
            if (off > 0.5f * scale)
                simonFollow = true;
            if (simonFollow)
            {
                simonAnchor = off > 5f * scale ? home : Vector3.Lerp(simonAnchor, home, 1f - Mathf.Exp(-4f * dt));
                if (off < 0.05f * scale)
                    simonFollow = false;
            }

            Vector3 across = Vector3.Cross(Vector3.up, simonAhead);
            for (int i = 0; i < SimonCount; i++)
                simonSpots[i] = simonAnchor + (across * (SimonPlaces[i].x * 0.24f) + Vector3.up * (SimonPlaces[i].y * 0.17f)) * scale;

            // Either hand can touch; the left is checked first, so both at once counts as the left
            float reach = (SimonStarSize * 0.5f + 0.06f) * scale;
            int touched = SimonTouch(0, LeftHand, reach);
            int touchedRight = SimonTouch(1, RightHand, reach);
            int hand = 0;
            if (touched < 0)
            {
                touched = touchedRight;
                hand = 1;
            }

            switch (simonStage)
            {
                case SimonStage.Waiting:
                    if (now >= simonAt)
                    {
                        simonRound = 0;
                        simonStartBest = simonBest;
                        SimonNextRound(now);
                    }
                    break;

                case SimonStage.Showing:
                    if (now >= simonAt)
                    {
                        if (simonStep < simonRound)
                        {
                            // Longer sequences play a little faster
                            float lit = Mathf.Max(0.25f, 0.5f - simonRound * 0.015f);
                            SimonLight(simonOrder[simonStep], lit, -1);
                            simonStep++;
                            simonAt = now + lit * 1.5f;
                        }
                        else
                        {
                            simonStage = SimonStage.Turn;
                            simonStep = 0;
                        }
                    }
                    break;

                case SimonStage.Turn:
                    if (touched < 0)
                        break;

                    SimonLight(touched, 0.25f, hand);
                    if (touched != simonOrder[simonStep])
                        SimonEnd(now, false);
                    else if (++simonStep == simonRound)
                    {
                        simonStage = SimonStage.Cleared;
                        simonAt = now + 0.9f;
                        Burst(simonAnchor, 18, 1.2f, false);
                    }
                    break;

                case SimonStage.Cleared:
                    if (now >= simonAt)
                        SimonNextRound(now);
                    break;

                case SimonStage.Over:
                    // Any star touched once the red has faded starts a new game
                    if (touched >= 0 && now - simonOverAt > 1f)
                    {
                        SimonLight(touched, 0.25f, hand);
                        simonStage = SimonStage.Waiting;
                        simonAt = now + 0.8f;
                    }
                    break;
            }

            // After a wrong touch every star flashes red for a moment
            float overFor = now - simonOverAt;
            bool red = simonStage == SimonStage.Over && !simonWon && overFor < 1.2f;
            bool idle = simonStage == SimonStage.Over || simonStage == SimonStage.Waiting;
            for (int i = 0; i < SimonCount; i++)
            {
                if (simonStars[i] == null || !simonStars[i].Alive)
                    simonStars[i] = MakeShaped(BoomerangStarMesh, Color.white);
                Piece glow = Ensure(ref simonGlows[i], PrimitiveType.Sphere, Color.white);
                Vector3 at = simonSpots[i];

                Color color = SimonStarColors[i];
                float bright = now < simonStarLit[i] ? 1f : Mathf.Clamp01(1f - (now - simonStarLit[i]) / 0.15f);
                if (red)
                {
                    color = new Color(1f, 0.15f, 0.12f);
                    bright = Mathf.Sin(overFor * 18f) > 0f ? 1f : 0.3f;
                }
                else if (idle)
                    bright = Mathf.Max(bright, 0.2f + 0.15f * Mathf.Sin(now * 2.5f + i * 1.6f));

                // Facing you, point up, rocking gently
                float size = (SimonStarSize + 0.04f * bright) * scale;
                Vector3 toYou = head - at;
                Quaternion face = Quaternion.LookRotation(toYou.sqrMagnitude > 0.0001f ? toYou : -simonAhead) * Quaternion.Euler(-90f, 0f, 0f);
                simonStars[i].Object.transform.SetPositionAndRotation(at, face * Quaternion.AngleAxis(Mathf.Sin(now * 1.3f + i) * 12f, Vector3.up));
                simonStars[i].Object.transform.localScale = Vector3.one * size;
                Color dim = new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, 0.85f);
                SetColor(simonStars[i], Color.Lerp(dim, Color.Lerp(color, Color.white, 0.35f), bright));

                glow.Object.transform.position = at;
                glow.Object.transform.localScale = Vector3.one * (size * (1.5f + 0.5f * bright));
                SetAlpha(glow, color, 0.06f + 0.3f * bright);
            }

            // What the board says depends only on the stage, the round and how far into it you are
            int shown = (int)simonStage * 100000 + simonRound * 1000 + simonStep;
            if (shown != simonShown)
            {
                simonShown = shown;
                switch (simonStage)
                {
                    case SimonStage.Waiting:
                        simonBoard.SafeSetText($"Simon Stars\n<size=55%>watch the stars, then touch them in order  ·  best {simonBest}</size>");
                        break;
                    case SimonStage.Showing:
                        simonBoard.SafeSetText($"Round {simonRound}\n<size=55%>watch the stars</size>");
                        break;
                    case SimonStage.Turn:
                        simonBoard.SafeSetText($"Round {simonRound}\n<size=55%>your turn  ·  {simonStep} of {simonRound}</size>");
                        break;
                    case SimonStage.Cleared:
                        simonBoard.SafeSetText($"Round {simonRound} cleared!\n<size=55%>best {simonBest}</size>");
                        break;
                    default:
                        simonBoard.SafeSetText(simonWon
                            ? $"All {SimonLongest} rounds!\n<size=55%>touch a star to play again</size>"
                            : $"Wrong star!\n<size=55%>you reached round {simonRound}  ·  {(simonRecord ? "a new best!" : $"best {simonBest}")}\ntouch a star to play again</size>");
                        break;
                }
            }

            PlaceToyText(simonBoard, simonAnchor + Vector3.up * (0.38f * scale), 0.05f);
            simonBoard.color = simonStage == SimonStage.Over ? (red ? new Color(1f, 0.45f, 0.4f) : Accent)
                : simonStage == SimonStage.Cleared ? Accent : Color.Lerp(Color.white, Accent, 0.35f);
        }

        // Where the stars sit: within arm's reach in front of you, a little below your eyes
        private static Vector3 SimonHome(Vector3 head, float scale) =>
            head + (simonAhead * 0.45f + Vector3.down * 0.15f) * scale;

        // The star a hand has just reached, or -1. A hand has to leave a star before it can touch it again,
        // with a little room to spare so a hand resting on its edge doesn't count over and over.
        private static int SimonTouch(int hand, Vector3 at, float reach)
        {
            int on = simonTouching[hand];
            if (on >= 0 && (at - simonSpots[on]).sqrMagnitude < reach * reach * 2f)
                return -1;

            simonTouching[hand] = -1;
            for (int i = 0; i < SimonCount; i++)
            {
                if ((at - simonSpots[i]).sqrMagnitude >= reach * reach)
                    continue;

                simonTouching[hand] = i;
                return i;
            }

            return -1;
        }

        // Lights a star for a moment, with its own note and a tick in the hand that touched it, or both when it is the game's turn
        private static void SimonLight(int star, float time, int hand)
        {
            simonStarLit[star] = Time.time + time;
            VRRig.LocalRig.PlayHandTapLocal(SimonStarNotes[star], hand == 0, 0.4f);

            float tick = GorillaTagger.Instance.tapHapticStrength / 3f;
            if (hand != 1)
                GorillaTagger.Instance.StartVibration(true, tick, 0.03f);
            if (hand != 0)
                GorillaTagger.Instance.StartVibration(false, tick, 0.03f);
        }

        // Adds one more star to the sequence and plays it all through
        private static void SimonNextRound(float now)
        {
            if (simonRound == SimonLongest)
            {
                SimonEnd(now, true);
                return;
            }

            simonOrder[simonRound++] = Random.Range(0, SimonCount);
            if (simonRound > simonBest)
            {
                simonBest = simonRound;
                PlayerPrefs.SetInt(SimonBestKey, simonBest);
                PlayerPrefs.Save();
            }

            if (simonRound == 10)
                Eclipse.Menu.Main.EclipseUnlock("Starry Memory");

            simonStage = SimonStage.Showing;
            simonStep = 0;
            simonAt = now + 0.7f;
        }

        private static void SimonEnd(float now, bool won)
        {
            simonStage = SimonStage.Over;
            simonOverAt = now;
            simonWon = won;
            simonRecord = simonRound > simonStartBest;
            simonShown = int.MinValue;

            if (won)
            {
                Burst(simonAnchor, 60, 3f, false);
                return;
            }

            GorillaTagger.Instance.StartVibration(true, GorillaTagger.Instance.tagHapticStrength, 0.25f);
            GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.tagHapticStrength, 0.25f);
            VRRig.LocalRig.PlayHandTapLocal(68, false, 0.45f);
        }

        /// <summary>Takes away the four stars and the Simon Stars scoreboard.</summary>
        public static void DisableSimonStars()
        {
            for (int i = 0; i < SimonCount; i++)
            {
                DiscardPiece(ref simonStars[i]);
                DiscardPiece(ref simonGlows[i]);
                simonStarLit[i] = 0f;
            }

            DestroyText(ref simonBoard);
            simonPlaced = simonFollow = simonTurning = false;
            simonTouching[0] = simonTouching[1] = -1;
            simonRound = simonStep = 0;
            simonOverAt = -10f;
            simonShown = int.MinValue;
        }
    }
}
