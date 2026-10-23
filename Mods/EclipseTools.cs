/*
 * Eclipse Menu  Mods/EclipseTools.cs
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
using Eclipse.Managers;
using GorillaLocomotion;
using Photon.Pun;
using TMPro;
using UnityEngine;
using static Eclipse.Menu.Main;
using static Eclipse.Mods.Custom;

namespace Eclipse.Mods
{
    /// <summary>The wrist tools: readouts and reminders only you can see, in the Wrist Tools category.</summary>
    /// <remarks>
    /// The left wrist already carries the Eclipse watches up to 0.66 and the right wrist up to 0.42,
    /// so these sit above them. The second set goes higher still, from 1.02 on the left wrist and
    /// 0.92 on the right. Anything that counts real minutes uses unscaled time, so a mod that
    /// slows the game down doesn't stretch it.
    /// </remarks>
    public static partial class EclipseMods
    {
        // ── Wrist Tools ─────────────────────────────────────────────────────────

        private static TextMeshPro batteryText;
        private static BatteryStatus batteryShownStatus;
        private static int batteryShown = -2;
        private static float batteryNext;

        /// <summary>Your headset's battery and whether it is charging, on your left wrist.</summary>
        public static void BatteryMeter()
        {
            if (batteryText == null)
            {
                batteryText = MakeWristText("Eclipse_BatteryMeter", Color.white);
                batteryShown = -2;
            }

            PlaceOnWrist(batteryText, 0.78f);
            if (Time.time < batteryNext)
                return;
            batteryNext = Time.time + 1f;

            // A computer with no battery reads -1. A laptop reads its own battery rather than the
            // headset's, so anything that isn't a standalone headset is labelled as the PC.
            float level = SystemInfo.batteryLevel;
            BatteryStatus status = SystemInfo.batteryStatus;
            int percent = level < 0f ? -1 : Mathf.RoundToInt(level * 100f);

            batteryText.color = percent < 0 ? Color.Lerp(Color.white, Second, 0.35f)
                : status == BatteryStatus.Charging ? Accent
                : percent <= 20 ? new Color(1f, 0.45f, 0.4f)
                : Color.Lerp(Color.white, Accent, 0.25f);

            if (percent == batteryShown && status == batteryShownStatus)
                return;
            batteryShown = percent;
            batteryShownStatus = status;

            bool headset = Application.platform == RuntimePlatform.Android;
            if (percent < 0)
            {
                batteryText.text = headset ? "No battery reading" : "PC  ·  no battery";
                return;
            }

            string state;
            switch (status)
            {
                case BatteryStatus.Charging: state = "charging"; break;
                case BatteryStatus.Full: state = "full"; break;
                case BatteryStatus.NotCharging: state = "plugged in"; break;
                default: state = "not charging"; break;
            }

            batteryText.text = $"{percent}%\n<size=55%>{(headset ? "" : "PC battery  ·  ")}{state}</size>";
        }

        /// <summary>Takes the battery reading off your wrist.</summary>
        public static void DisableBatteryMeter() => DestroyText(ref batteryText);

        private static TextMeshPro coordinatesText;
        private static Vector3Int coordinatesShown;
        private static float coordinatesNext;

        /// <summary>Where you are in the world, to a tenth of a metre, on your left wrist.</summary>
        public static void Coordinates()
        {
            if (coordinatesText == null)
            {
                coordinatesText = MakeWristText("Eclipse_Coordinates", Color.white);
                coordinatesShown = new Vector3Int(int.MinValue, 0, 0);
            }

            PlaceOnWrist(coordinatesText, 0.9f);
            if (Time.time < coordinatesNext)
                return;
            coordinatesNext = Time.time + 0.2f;
            coordinatesText.color = Color.Lerp(Color.white, Second, 0.35f);

            // Kept in tenths, so the text is only rebuilt when a shown digit changes
            Vector3Int tenths = Vector3Int.RoundToInt(BodyPosition * 10f);
            if (tenths == coordinatesShown)
                return;
            coordinatesShown = tenths;

            coordinatesText.text = $"<size=55%>x</size> {tenths.x / 10f:0.0}  <size=55%>y</size> {tenths.y / 10f:0.0}  <size=55%>z</size> {tenths.z / 10f:0.0}";
        }

        /// <summary>Takes your coordinates off your wrist.</summary>
        public static void DisableCoordinates() => DestroyText(ref coordinatesText);

        private static TextMeshPro handSpeedText;
        private static Vector3 handSpeedLeftFrom, handSpeedRightFrom;
        private static float handSpeedLeft, handSpeedRight, handSpeedBestLeft, handSpeedBestRight, handSpeedNext;
        private static int handSpeedFrame = -2, handSpeedShown = -1, handSpeedBestShown = -1;
        private static bool handSpeedUnlocked;

        /// <summary>How fast each of your hands is swinging, and your best this session, in metres a second on your right wrist.</summary>
        public static void HandSpeed()
        {
            if (handSpeedText == null)
            {
                handSpeedText = MakeWristText("Eclipse_HandSpeed", Color.white);
                handSpeedShown = handSpeedBestShown = -1;
            }

            // Measured from your head and at your own size, so it is how fast your real hands
            // move, not how fast you are already flying along with them.
            float scale = Mathf.Max(Scale, 0.01f);
            Vector3 left = (LeftHand - Head.position) / scale;
            Vector3 right = (RightHand - Head.position) / scale;
            float dt = Time.unscaledDeltaTime;

            // The first frame after a gap has nothing to measure against
            if (handSpeedFrame == Time.frameCount - 1 && dt > 0f)
            {
                handSpeedLeft = EaseHandSpeed(handSpeedLeft, (left - handSpeedLeftFrom).magnitude / dt, dt);
                handSpeedRight = EaseHandSpeed(handSpeedRight, (right - handSpeedRightFrom).magnitude / dt, dt);
                handSpeedBestLeft = Mathf.Max(handSpeedBestLeft, handSpeedLeft);
                handSpeedBestRight = Mathf.Max(handSpeedBestRight, handSpeedRight);

                if (!handSpeedUnlocked && Mathf.Max(handSpeedBestLeft, handSpeedBestRight) >= 10f)
                {
                    handSpeedUnlocked = true;
                    Eclipse.Menu.Main.EclipseUnlock("Speedster");
                }
            }

            handSpeedLeftFrom = left;
            handSpeedRightFrom = right;
            handSpeedFrame = Time.frameCount;

            PlaceOnWrist(handSpeedText, 0.66f, true);
            if (Time.time < handSpeedNext)
                return;
            handSpeedNext = Time.time + 0.1f;
            handSpeedText.color = Color.Lerp(Color.white, Accent, Mathf.Clamp01(Mathf.Max(handSpeedLeft, handSpeedRight) / 8f));

            // Both hands in tenths, side by side in one number, so the text is only rebuilt when a shown digit changes
            int shown = Mathf.RoundToInt(handSpeedLeft * 10f) * 1000 + Mathf.RoundToInt(handSpeedRight * 10f);
            int best = Mathf.RoundToInt(handSpeedBestLeft * 10f) * 1000 + Mathf.RoundToInt(handSpeedBestRight * 10f);
            if (shown == handSpeedShown && best == handSpeedBestShown)
                return;
            handSpeedShown = shown;
            handSpeedBestShown = best;

            handSpeedText.text = $"L {handSpeedLeft:0.0}  ·  R {handSpeedRight:0.0} m/s\n<size=55%>best {handSpeedBestLeft:0.0}  ·  {handSpeedBestRight:0.0}</size>";
        }

        // Eases towards the new reading, and skips jumps no arm could make, like a snap turn
        private static float EaseHandSpeed(float speed, float reading, float dt) =>
            reading > 20f ? speed : Mathf.Lerp(speed, reading, 1f - Mathf.Exp(-12f * dt));

        /// <summary>Takes your hand speeds off your wrist; your best stays until the game closes.</summary>
        public static void DisableHandSpeed()
        {
            DestroyText(ref handSpeedText);
            handSpeedLeft = handSpeedRight = 0f;
        }

        private static LineRenderer tapeLine;
        private static TextMeshPro tapeText;
        private static int tapeShown = -1;
        private static float tapeNext;

        /// <summary>A line between your hands with how far apart they are, in metres and feet, floating at its middle.</summary>
        public static void MeasuringTape()
        {
            if (tapeLine == null)
                tapeLine = MakeLine("Eclipse_MeasuringTape", 0.006f);

            if (tapeText == null)
            {
                tapeText = MakeWristText("Eclipse_MeasuringTapeText", Color.white);
                tapeShown = -1;
            }

            float scale = Scale;
            Vector3 left = LeftHand;
            Vector3 right = RightHand;

            tapeLine.SetPosition(0, left);
            tapeLine.SetPosition(1, right);
            tapeLine.startWidth = tapeLine.endWidth = 0.006f * scale;
            tapeLine.startColor = Accent;
            tapeLine.endColor = Second;

            tapeText.SafeSetFont(activeFont);
            tapeText.transform.position = (left + right) * 0.5f + Vector3.up * (0.06f * scale);
            tapeText.transform.localScale = Vector3.one * (0.08f * scale);
            tapeText.transform.LookAt(Head.position);
            tapeText.transform.Rotate(0f, 180f, 0f);

            if (Time.time < tapeNext)
                return;
            tapeNext = Time.time + 0.1f;
            tapeText.color = Color.Lerp(Color.white, Accent, 0.35f);

            // In whole centimetres, so the text is only rebuilt when the shown length changes
            int centimetres = Mathf.RoundToInt(Vector3.Distance(left, right) * 100f);
            if (centimetres == tapeShown)
                return;
            tapeShown = centimetres;

            tapeText.text = $"{centimetres / 100f:0.00} m\n<size=55%>{centimetres / 30.48f:0.0} ft</size>";
        }

        /// <summary>Puts the measuring tape away.</summary>
        public static void DisableMeasuringTape()
        {
            DestroyLine(ref tapeLine);
            DestroyText(ref tapeText);
        }

        private static GorillaSpeakerLoudness micSource;
        private static LineRenderer micTrack, micFill;
        private static TextMeshPro micText;
        private static float micLevel, micHeard = -10f, micNext;
        private static int micState = -1;

        private static readonly string[] MicLabels =
        {
            "<size=70%>No microphone found</size>",
            "Mic  ·  <size=70%>say something</size>",
            "Mic  ·  <size=70%>hearing you</size>",
            "Mic Meter paused\n<size=55%>Disable Mouth Movement is on</size>"
        };

        /// <summary>A little bar above your right wrist showing how loud your own microphone is, so you can check it works.</summary>
        public static void MicMeter()
        {
            if (micSource == null && VRRig.LocalRig != null)
                micSource = VRRig.LocalRig.GetComponent<GorillaSpeakerLoudness>();

            // Only ever your own rig's level, the one the game already works out to move your mouth.
            // Disable Mouth Movement stops the game updating it, so the bar would sit frozen.
            bool paused = Eclipse.Patches.Menu.LoudnessPatch.enabled;
            bool hasMic = micSource != null && micSource.IsMicEnabled;

            // A square root, so ordinary talking fills a good part of the bar
            float reading = hasMic && !paused ? Mathf.Sqrt(micSource.LoudnessNormalized) : 0f;

            // Jumps up at once and falls back gently, like a real level meter
            micLevel = reading > micLevel ? reading : Mathf.MoveTowards(micLevel, reading, 1.2f * Time.unscaledDeltaTime);
            if (reading > 0.25f)
                micHeard = Time.unscaledTime;

            int state = paused ? 3 : !hasMic ? 0 : Time.unscaledTime - micHeard < 1.5f ? 2 : 1;

            if (micTrack == null)
                micTrack = MakeLine("Eclipse_MicTrack", 0.012f);
            if (micFill == null)
                micFill = MakeLine("Eclipse_MicFill", 0.012f);

            float scale = Scale;
            Vector3 origin = RightHand + Vector3.up * (0.76f * scale);
            Vector3 toHead = Head.position - origin;
            toHead.y = 0f;
            Vector3 facing = toHead.sqrMagnitude < 0.0001f ? Vector3.forward : toHead.normalized;

            // From your left to your right as you look at it
            Vector3 across = Vector3.Cross(facing, Vector3.up);
            Vector3 start = origin - across * (0.11f * scale);

            micTrack.enabled = state != 3;
            micTrack.SetPosition(0, start);
            micTrack.SetPosition(1, start + across * (0.22f * scale));
            micTrack.startWidth = micTrack.endWidth = 0.012f * scale;
            micTrack.startColor = micTrack.endColor = WithAlpha(Second, 0.25f);

            // A hair closer to you than the track, so it always draws on top
            Vector3 fillStart = start + facing * (0.003f * scale);
            micFill.enabled = state != 3 && micLevel > 0.01f;
            micFill.SetPosition(0, fillStart);
            micFill.SetPosition(1, fillStart + across * (0.22f * scale * micLevel));
            micFill.startWidth = micFill.endWidth = 0.012f * scale;
            micFill.startColor = micFill.endColor = Accent;

            if (micText == null)
            {
                micText = MakeWristText("Eclipse_MicMeter", Color.white);
                micState = -1;
            }

            PlaceOnWrist(micText, 0.8f, true);
            if (state != micState)
            {
                micState = state;
                micText.text = MicLabels[state];
            }

            if (Time.time < micNext)
                return;
            micNext = Time.time + 0.1f;
            micText.color = state == 0 ? new Color(1f, 0.45f, 0.4f) : state == 2 ? Accent : Color.Lerp(Color.white, Second, 0.35f);
        }

        /// <summary>Takes the mic meter off your wrist.</summary>
        public static void DisableMicMeter()
        {
            DestroyLine(ref micTrack);
            DestroyLine(ref micFill);
            DestroyText(ref micText);
            micLevel = 0f;
        }

        private const int PingAlarmLimit = 200;
        private static bool pingSpiking, pingAnnounced;
        private static float pingAlarmNext, pingAlarmReady, pingLastHigh;

        /// <summary>A notification when your ping goes over 200 ms, and another once it has settled back down.</summary>
        public static void PingAlarm()
        {
            float now = Time.unscaledTime;
            if (now < pingAlarmNext || !PhotonNetwork.IsConnected)
                return;
            pingAlarmNext = now + 0.5f;

            int ping = PhotonNetwork.GetPing();
            if (ping > PingAlarmLimit)
            {
                pingLastHigh = now;
                if (pingSpiking)
                    return;

                // One alarm per spike, and none for half a minute after the last, so a shaky
                // connection doesn't bury you in them. A spike it kept quiet about ends quietly too.
                pingSpiking = true;
                pingAnnounced = now >= pingAlarmReady;
                if (pingAnnounced)
                {
                    pingAlarmReady = now + 30f;
                    NotificationManager.SendNotification($"<color=grey>[</color><color=red>PING</color><color=grey>]</color> Your ping is high: {ping} ms.", 4000);
                }
            }
            else if (pingSpiking && now - pingLastHigh > 3f)
            {
                pingSpiking = false;
                if (pingAnnounced)
                    NotificationManager.SendNotification($"<color=grey>[</color><color=green>PING</color><color=grey>]</color> Your ping is back down: {ping} ms.", 4000);
            }
        }

        /// <summary>Stops watching your ping and forgets any spike it was in the middle of.</summary>
        public static void DisablePingAlarm()
        {
            pingSpiking = pingAnnounced = false;
            pingAlarmReady = 0f;
        }

        private const float BreakEvery = 30f * 60f;
        private static float breakNext;
        private static int breakCount;

        /// <summary>Starts Break Reminder's first half hour, when it is turned on.</summary>
        public static void EnableBreakReminder()
        {
            breakNext = Time.unscaledTime + BreakEvery;
            breakCount = 0;
        }

        /// <summary>After every half hour of play since you turned it on, a friendly nudge to take a short break.</summary>
        public static void BreakReminder()
        {
            if (Time.unscaledTime < breakNext)
                return;
            breakNext = Time.unscaledTime + BreakEvery;

            int hours = ++breakCount / 2;
            string played = hours == 0 ? "half an hour"
                : (hours == 1 ? "an hour" : $"{hours} hours") + (breakCount % 2 == 0 ? "" : " and a half");

            NotificationManager.SendNotification($"<color=grey>[</color><color=green>BREAK</color><color=grey>]</color> You have played for {played}. Time for a short break: stretch, have a drink and rest your eyes.", 8000);
        }

        /// <summary>Stops the break reminders.</summary>
        public static void DisableBreakReminder() => breakCount = 0;

        private static readonly int[] CountdownMinutes = { 1, 3, 5, 10 };
        private static TextMeshPro countdownText;
        private static int countdownChoice = -1, countdownShown = -1;
        private static float countdownEnd, countdownNext;
        private static bool countdownHeld = true, countdownRunning;

        /// <summary>Forgets a held X and clears the countdown, when Countdown Timer is turned on.</summary>
        public static void EnableCountdownTimer()
        {
            countdownHeld = true;
            countdownRunning = false;
            countdownChoice = countdownShown = -1;
        }

        /// <summary>X picks 1, 3, 5 or 10 minutes and counts it down on your right wrist, with a notification when time is up.</summary>
        public static void CountdownTimer()
        {
            if (Pressed(leftPrimary, ref countdownHeld))
            {
                countdownChoice = (countdownChoice + 1) % CountdownMinutes.Length;
                countdownEnd = Time.unscaledTime + CountdownMinutes[countdownChoice] * 60f;
                countdownRunning = true;
            }

            if (countdownText == null)
            {
                countdownText = MakeWristText("Eclipse_CountdownTimer", Color.white);
                countdownShown = -1;
            }

            PlaceOnWrist(countdownText, 0.54f, true);

            // Seconds left while running, -2 once finished, -3 before X is first pressed
            int left = countdownRunning ? Mathf.CeilToInt(countdownEnd - Time.unscaledTime) : countdownChoice < 0 ? -3 : -2;
            if (countdownRunning && left <= 0)
            {
                countdownRunning = false;
                left = -2;
                NotificationManager.SendNotification($"<color=grey>[</color><color=green>TIMER</color><color=grey>]</color> Time is up! Your {CountdownMinutes[countdownChoice]} minute countdown has finished.", 6000);
            }

            if (left != countdownShown)
            {
                countdownShown = left;
                countdownText.text = left == -3 ? "Countdown\n<size=55%>X for 1, 3, 5 or 10 min</size>"
                    : left == -2 ? "Time's up!\n<size=55%>X to go again</size>"
                    : $"{left / 60}:{left % 60:00}\n<size=55%>of {CountdownMinutes[countdownChoice]} min</size>";
            }

            if (Time.time < countdownNext)
                return;
            countdownNext = Time.time + 0.1f;
            countdownText.color = left == -2 ? Accent : left == -3 ? Color.Lerp(Color.white, Second, 0.35f) : Color.Lerp(Color.white, Accent, 0.35f);
        }

        /// <summary>Takes the countdown off your wrist and stops it.</summary>
        public static void DisableCountdownTimer()
        {
            DestroyText(ref countdownText);
            countdownRunning = false;
        }

        // ── More Wrist Tools ────────────────────────────────────────────────────

        // Resting your body on something, or a hand touching, climbing or holding on: the game's own
        // test for being on the ground. The usual ground ray reaches about a metre down, so it would
        // count the first part of every jump as still standing.
        private static bool TouchingSomething() =>
            GTPlayer.Instance.IsGroundedHand || GTPlayer.Instance.IsGroundedButt;

        // Minutes, seconds and tenths, like 1:02.3
        private static string LapClock(int tenths) =>
            $"{tenths / 600}:{tenths / 10 % 60:00}.{tenths % 10}";

        private const float LapGateRadius = 1.1f;
        private static LineRenderer lapRing, lapGlow;
        private static TextMeshPro lapText;
        private static Vector3 lapCentre, lapFacing;
        private static float lapSize, lapStart, lapFlash = -10f, lapNext;
        private static int lapCount, lapLast, lapBest, lapSide, lapSessionLaps, lapShown = int.MinValue, lapShownCount;
        private static bool lapHeld = true, lapPlaced, lapArmed, lapRunning;

        /// <summary>Forgets a held A and clears the gate and laps, when Lap Timer is turned on.</summary>
        public static void EnableLapTimer()
        {
            lapHeld = true;
            lapPlaced = lapArmed = lapRunning = false;
            lapCount = 0;
            lapShown = int.MinValue;
        }

        /// <summary>A drops a glowing start gate where you stand; going through it starts the clock and every time after records a lap, on your left wrist.</summary>
        public static void LapTimer()
        {
            float now = Time.unscaledTime;

            if (Pressed(rightPrimary, ref lapHeld))
            {
                // You start out standing on the line, so a step forward through it starts the clock
                lapSize = Scale;
                lapFacing = LookFlat();
                lapCentre = Feet + Vector3.up * (LapGateRadius * lapSize);
                lapPlaced = lapArmed = true;
                lapRunning = false;
                lapSide = -1;
                lapCount = 0;
                LayLapGate();
            }

            if (lapPlaced)
            {
                if (lapRing == null || lapGlow == null)
                    LayLapGate();

                CrossLapGate(now);

                // The glow flares for a moment each time you go through
                float flare = Mathf.Clamp01(1f - (now - lapFlash) / 0.6f);
                lapRing.startColor = lapRing.endColor = Accent;
                lapGlow.startColor = lapGlow.endColor = WithAlpha(Second, 0.25f + 0.55f * flare);
                lapGlow.startWidth = lapGlow.endWidth = (0.14f + 0.1f * flare) * lapSize;
            }

            if (lapText == null)
            {
                lapText = MakeWristText("Eclipse_LapTimer", Color.white);
                lapShown = int.MinValue;
            }

            PlaceOnWrist(lapText, 1.02f);

            // Tenths into this lap while running, -2 while waiting at the gate, -3 before there is one
            int shown = lapRunning ? Mathf.FloorToInt((now - lapStart) * 10f) : lapPlaced ? -2 : -3;
            if (shown != lapShown || lapCount != lapShownCount)
            {
                lapShown = shown;
                lapShownCount = lapCount;
                lapText.text = shown == -3 ? "Lap Timer\n<size=55%>A to drop a start gate</size>"
                    : shown == -2 ? "Ready\n<size=55%>go through the gate to start</size>"
                    : lapCount == 0 ? $"{LapClock(shown)}\n<size=55%>lap 1</size>"
                    : $"{LapClock(shown)}\n<size=55%>lap {lapCount + 1}  ·  last {LapClock(lapLast)}  ·  best {LapClock(lapBest)}</size>";
            }

            if (Time.time < lapNext)
                return;
            lapNext = Time.time + 0.1f;
            lapText.color = !lapPlaced ? Color.Lerp(Color.white, Second, 0.35f) : now - lapFlash < 1f ? Accent : Color.Lerp(Color.white, Accent, 0.35f);
        }

        // Stands the gate upright where it was dropped, facing the way you were looking
        private static void LayLapGate()
        {
            if (lapRing == null)
                lapRing = MakeRing("Eclipse_LapGate");
            if (lapGlow == null)
                lapGlow = MakeRing("Eclipse_LapGateGlow");

            float radius = LapGateRadius * lapSize;
            PlaceRing(lapRing, lapCentre, lapFacing, radius, 0.04f * lapSize, Accent);
            PlaceRing(lapGlow, lapCentre, lapFacing, radius, 0.14f * lapSize, WithAlpha(Second, 0.25f));
        }

        // A crossing is your body going from one side of the gate to the other through the ring
        // itself. After each one you have to get a few metres away before the next counts, so
        // stepping back and forth on the line doesn't rack up laps.
        private static void CrossLapGate(float now)
        {
            Vector3 offset = BodyPosition - lapCentre;
            float along = Vector3.Dot(offset, lapFacing);
            float radius = LapGateRadius * lapSize;

            if (!lapArmed && offset.sqrMagnitude > radius * radius * 6.25f)
                lapArmed = true;

            // A little either side of the line still counts as on it, so standing in the gate doesn't flicker
            float edge = 0.15f * lapSize;
            int side = along > edge ? 1 : along < -edge ? -1 : lapSide;
            if (side == lapSide)
                return;
            lapSide = side;

            if (!lapArmed || (offset - lapFacing * along).sqrMagnitude > radius * radius * 1.69f)
                return;

            lapArmed = false;
            lapFlash = now;
            if (lapRunning)
            {
                int lap = Mathf.FloorToInt((now - lapStart) * 10f);
                lapBest = lapCount == 0 ? lap : Mathf.Min(lapBest, lap);
                lapLast = lap;
                lapCount++;

                if (++lapSessionLaps == 10)
                    Eclipse.Menu.Main.EclipseUnlock("Lap Legend");
            }

            lapRunning = true;
            lapStart = now;
        }

        /// <summary>Takes down the start gate and the lap times on your wrist.</summary>
        public static void DisableLapTimer()
        {
            DestroyLine(ref lapRing);
            DestroyLine(ref lapGlow);
            DestroyText(ref lapText);
            lapPlaced = lapRunning = false;
        }

        // Where along the arrow each of its points sits: the tail, the shoulders of the head, and the tip
        private static readonly float[] WaypointArrowStops = { 0f, 0.55f, 0.56f, 1f };
        private static readonly Vector3[] waypointArrowPoints = new Vector3[4];
        private static LineRenderer waypointBeam, waypointArrow;
        private static TextMeshPro waypointText;
        private static Vector3 waypointAt;
        private static float waypointSize, waypointNext;
        private static int waypointShown = int.MinValue, waypointShownRise;
        private static bool waypointHeld = true, waypointSet;

        /// <summary>Forgets a held B and clears the waypoint, when Waypoint is turned on.</summary>
        public static void EnableWaypoint()
        {
            waypointHeld = true;
            waypointSet = false;
            waypointShown = int.MinValue;
        }

        /// <summary>B saves a waypoint where you stand, marked by a tall beam of light, and your right wrist points the way to it and says how far.</summary>
        public static void Waypoint()
        {
            float scale = Scale;
            if (Pressed(rightSecondary, ref waypointHeld))
            {
                waypointAt = BodyPosition;
                waypointSize = scale;
                waypointSet = true;
                LayWaypointBeam();
            }

            Vector3 toWaypoint = waypointAt - BodyPosition;
            float distance = toWaypoint.magnitude;
            bool here = distance < 1f * scale;

            if (waypointSet)
            {
                if (waypointBeam == null)
                    LayWaypointBeam();

                waypointBeam.startColor = WithAlpha(Accent, 0.35f);
                waypointBeam.endColor = WithAlpha(Second, 0f);
            }

            if (waypointSet && !here)
            {
                if (waypointArrow == null)
                {
                    waypointArrow = MakeLine("Eclipse_WaypointArrow", 0.045f);
                    waypointArrow.positionCount = WaypointArrowStops.Length;

                    // A narrow shaft that widens into the head and comes to a point. The head is also
                    // keyed at its point's place in the list, so it draws the same however Unity reads it.
                    waypointArrow.widthCurve = new AnimationCurve(
                        new Keyframe(0f, 0.3f), new Keyframe(0.55f, 0.3f), new Keyframe(0.56f, 1f), new Keyframe(2f / 3f, 1f), new Keyframe(1f, 0f));
                }

                // Points the way you would have to go, floating above your right wrist
                Vector3 way = toWaypoint / distance;
                float length = 0.12f * scale;
                Vector3 tail = RightHand + Vector3.up * (1.02f * scale) - way * (length * 0.5f);
                for (int i = 0; i < waypointArrowPoints.Length; i++)
                    waypointArrowPoints[i] = tail + way * (length * WaypointArrowStops[i]);

                waypointArrow.enabled = true;
                waypointArrow.SetPositions(waypointArrowPoints);
                waypointArrow.widthMultiplier = 0.045f * scale;
                waypointArrow.startColor = waypointArrow.endColor = Accent;
            }
            else if (waypointArrow != null)
                waypointArrow.enabled = false;

            if (waypointText == null)
            {
                waypointText = MakeWristText("Eclipse_Waypoint", Color.white);
                waypointShown = int.MinValue;
            }

            PlaceOnWrist(waypointText, 0.92f, true);
            if (Time.time < waypointNext)
                return;
            waypointNext = Time.time + 0.1f;
            waypointText.color = !waypointSet ? Color.Lerp(Color.white, Second, 0.35f) : here ? Accent : Color.Lerp(Color.white, Accent, 0.35f);

            // Tenths of a metre up close and whole metres from ten on, -2 once you are there, -3 with no waypoint
            int shown = !waypointSet ? -3 : here ? -2 : distance < 10f ? Mathf.RoundToInt(distance * 10f) : Mathf.RoundToInt(distance) * 10;
            int rise = shown >= 0 ? Mathf.RoundToInt(toWaypoint.y) : 0;
            if (shown == waypointShown && rise == waypointShownRise)
                return;
            waypointShown = shown;
            waypointShownRise = rise;

            if (shown < 0)
            {
                waypointText.text = shown == -3 ? "Waypoint\n<size=55%>B to save one here</size>" : "You're here\n<size=55%>B to move the waypoint</size>";
                return;
            }

            string height = rise > 0 ? $"{rise} m up" : rise < 0 ? $"{-rise} m down" : "level";
            waypointText.text = shown < 100
                ? $"{shown / 10f:0.0} m\n<size=55%>{height}  ·  to your waypoint</size>"
                : $"{shown / 10} m\n<size=55%>{height}  ·  to your waypoint</size>";
        }

        // A tall, faint column of light rising from the ground where the waypoint was saved
        private static void LayWaypointBeam()
        {
            if (waypointBeam == null)
                waypointBeam = MakeLine("Eclipse_WaypointBeam", 0.25f);

            Vector3 ground = waypointAt + Vector3.down * (0.45f * waypointSize);
            waypointBeam.SetPosition(0, ground);
            waypointBeam.SetPosition(1, ground + Vector3.up * (40f * waypointSize));
            waypointBeam.startWidth = waypointBeam.endWidth = 0.25f * waypointSize;
        }

        /// <summary>Takes down the waypoint, its beam and the arrow on your wrist.</summary>
        public static void DisableWaypoint()
        {
            DestroyLine(ref waypointBeam);
            DestroyLine(ref waypointArrow);
            DestroyText(ref waypointText);
            waypointSet = false;
        }

        // Rises smaller than this are the little hops of moving about, and don't replace your last real jump
        private const float JumpHeightLeast = 0.25f;
        private static TextMeshPro jumpHeightText;
        private static float jumpHeightFrom, jumpHeightPeak, jumpHeightLast = -1f, jumpHeightBest, jumpHeightNext;
        private static int jumpHeightFrame = -2, jumpHeightShown = int.MinValue, jumpHeightBestShown;
        private static bool jumpHeightReady, jumpHeightUp;

        /// <summary>How high your current jump has taken you above where you left the ground, and your best this session, on your left wrist.</summary>
        public static void JumpHeight()
        {
            float height = BodyPosition.y;
            bool fresh = jumpHeightFrame != Time.frameCount - 1;
            jumpHeightFrame = Time.frameCount;

            if (TouchingSomething())
            {
                // Until you leave the ground, where you took off from follows you about
                jumpHeightFrom = height;
                jumpHeightPeak = 0f;
                jumpHeightReady = true;
                jumpHeightUp = false;
            }
            else if (fresh)
            {
                // Turned on in mid-air, so where this jump started was missed
                jumpHeightReady = jumpHeightUp = false;
            }
            else if (jumpHeightReady)
            {
                jumpHeightPeak = Mathf.Max(jumpHeightPeak, height - jumpHeightFrom);
                if (jumpHeightPeak >= JumpHeightLeast * Scale)
                {
                    jumpHeightUp = true;
                    jumpHeightLast = jumpHeightPeak;
                    jumpHeightBest = Mathf.Max(jumpHeightBest, jumpHeightPeak);
                }
            }

            if (jumpHeightText == null)
            {
                jumpHeightText = MakeWristText("Eclipse_JumpHeight", Color.white);
                jumpHeightShown = int.MinValue;
            }

            PlaceOnWrist(jumpHeightText, 1.14f);
            if (Time.time < jumpHeightNext)
                return;
            jumpHeightNext = Time.time + 0.1f;
            jumpHeightText.color = jumpHeightLast < 0f ? Color.Lerp(Color.white, Second, 0.35f) : jumpHeightUp ? Accent : Color.Lerp(Color.white, Accent, 0.25f);

            // In tenths, doubled with one added while still in the air, so the text is only rebuilt when it changes
            int last = Mathf.RoundToInt(jumpHeightLast * 10f);
            int shown = jumpHeightLast < 0f ? -1 : last * 2 + (jumpHeightUp ? 1 : 0);
            int best = Mathf.RoundToInt(jumpHeightBest * 10f);
            if (shown == jumpHeightShown && best == jumpHeightBestShown)
                return;
            jumpHeightShown = shown;
            jumpHeightBestShown = best;

            jumpHeightText.text = shown < 0 ? "Jump Height\n<size=55%>jump to measure</size>"
                : $"{last / 10f:0.0} m\n<size=55%>{(jumpHeightUp ? "this jump" : "last jump")}  ·  best {best / 10f:0.0} m</size>";
        }

        /// <summary>Takes your jump height off your wrist; your best stays until the game closes.</summary>
        public static void DisableJumpHeight()
        {
            DestroyText(ref jumpHeightText);
            jumpHeightReady = jumpHeightUp = false;
        }

        // Time up shorter than this is just moving about
        private const float AirTimeLeast = 0.2f;
        private static TextMeshPro airTimeText;
        private static float airTimeStart, airTimeLast = -1f, airTimeLongest, airTimeNext;
        private static int airTimeFrame = -2, airTimeShown = int.MinValue, airTimeLongestShown;
        private static bool airTimeReady, airTimeUp;

        /// <summary>How long you have been in the air, and your longest time up this session, on your left wrist.</summary>
        public static void AirTime()
        {
            float now = Time.unscaledTime;
            bool fresh = airTimeFrame != Time.frameCount - 1;
            airTimeFrame = Time.frameCount;

            if (TouchingSomething())
            {
                airTimeReady = true;
                airTimeUp = false;
            }
            else if (fresh)
            {
                // Turned on in mid-air, so when this jump started was missed
                airTimeReady = airTimeUp = false;
            }
            else if (airTimeReady && !airTimeUp)
            {
                airTimeUp = true;
                airTimeStart = now;
            }

            float flight = airTimeUp ? now - airTimeStart : 0f;
            bool counting = flight >= AirTimeLeast;
            if (counting)
            {
                airTimeLast = flight;
                airTimeLongest = Mathf.Max(airTimeLongest, flight);
            }

            if (airTimeText == null)
            {
                airTimeText = MakeWristText("Eclipse_AirTime", Color.white);
                airTimeShown = int.MinValue;
            }

            PlaceOnWrist(airTimeText, 1.26f);
            if (Time.time < airTimeNext)
                return;
            airTimeNext = Time.time + 0.1f;
            airTimeText.color = airTimeLast < 0f ? Color.Lerp(Color.white, Second, 0.35f) : counting ? Accent : Color.Lerp(Color.white, Accent, 0.25f);

            // In tenths, doubled with one added while still in the air, so the text is only rebuilt when it changes
            int last = Mathf.FloorToInt(airTimeLast * 10f);
            int shown = airTimeLast < 0f ? -1 : last * 2 + (counting ? 1 : 0);
            int longest = Mathf.FloorToInt(airTimeLongest * 10f);
            if (shown == airTimeShown && longest == airTimeLongestShown)
                return;
            airTimeShown = shown;
            airTimeLongestShown = longest;

            airTimeText.text = shown < 0 ? "Air Time\n<size=55%>jump to start</size>"
                : $"{last / 10f:0.0} s\n<size=55%>{(counting ? "in the air" : "last time up")}  ·  longest {longest / 10f:0.0} s</size>";
        }

        /// <summary>Takes your air time off your wrist; your longest stays until the game closes.</summary>
        public static void DisableAirTime()
        {
            DestroyText(ref airTimeText);
            airTimeReady = airTimeUp = false;
        }

        private static TextMeshPro roomCardText;
        private static string roomCardName, roomCardMode, roomCardRegion, roomCardWhere;
        private static int roomCardPlayers = int.MinValue, roomCardMax;
        private static bool roomCardPrivate;
        private static float roomCardNext;

        /// <summary>The room you are in, how full it is, its map and game mode and your region, on your right wrist.</summary>
        public static void RoomCard()
        {
            if (roomCardText == null)
            {
                roomCardText = MakeWristText("Eclipse_RoomCard", Color.white);
                roomCardPlayers = int.MinValue;
            }

            PlaceOnWrist(roomCardText, 1.16f, true);
            if (Time.time < roomCardNext)
                return;
            roomCardNext = Time.time + 0.5f;

            // Only what Photon already knows about the room you are in, and nothing about anyone in it but how many
            var room = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom : null;
            string region = PhotonNetwork.IsConnected ? PhotonNetwork.CloudRegion : null;
            string name = room?.Name;
            int players = room != null ? room.PlayerCount : -1;
            int max = room != null ? room.MaxPlayers : 0;
            bool hidden = room != null && !room.IsVisible;
            object mode = null;
            if (room != null && room.CustomProperties != null)
                room.CustomProperties.TryGetValue("gameMode", out mode);
            string modeText = mode as string;

            roomCardText.color = room == null ? Color.Lerp(Color.white, Second, 0.35f) : Color.Lerp(Color.white, Accent, 0.3f);
            if (players == roomCardPlayers && max == roomCardMax && hidden == roomCardPrivate && name == roomCardName && modeText == roomCardMode && region == roomCardRegion)
                return;

            if (modeText != roomCardMode)
                roomCardWhere = RoomWhere(modeText);

            roomCardPlayers = players;
            roomCardMax = max;
            roomCardPrivate = hidden;
            roomCardName = name;
            roomCardMode = modeText;
            roomCardRegion = region;

            if (room == null)
            {
                roomCardText.text = $"Not in a room\n<size=55%>{(region == null ? "offline" : "online  ·  " + RegionName(region))}</size>";
                return;
            }

            string count = max > 0 ? $"{players}/{max} players" : $"{players} {(players == 1 ? "player" : "players")}";
            string where = roomCardWhere == null ? RegionName(region) : $"{roomCardWhere}  ·  {RegionName(region)}";
            roomCardText.text = $"<size=55%>room</size> {name}\n<size=55%>{count}  ·  {(hidden ? "private" : "public")}\n{where}</size>";
        }

        // The room's game mode reads like "forest;DEFAULT;Infection;;": its map, queue and game.
        // The usual queue isn't worth saying; anything that isn't in that shape is left out.
        private static string RoomWhere(string mode)
        {
            if (string.IsNullOrEmpty(mode))
                return null;

            string[] parts = mode.Split(';');
            if (parts.Length < 3)
                return null;

            string where = null;
            for (int i = 0; i < 3; i++)
            {
                if (parts[i].Length == 0 || (i == 1 && parts[i] == "DEFAULT"))
                    continue;

                string part = Spaced(parts[i]);
                where = where == null ? part : $"{where}  ·  {part}";
            }

            return where;
        }

        // "FreezeTag", "MODDED_Infection" or "forest" as "Freeze Tag", "Modded Infection" or "Forest"
        private static string Spaced(string word)
        {
            System.Text.StringBuilder built = new System.Text.StringBuilder(word.Length + 4);
            for (int i = 0; i < word.Length; i++)
            {
                char letter = word[i];
                bool wordStart = built.Length == 0 || built[built.Length - 1] == ' ';
                if (letter == '_' || letter == ' ')
                {
                    if (!wordStart)
                        built.Append(' ');
                    continue;
                }

                if (!wordStart && char.IsUpper(letter) && char.IsLower(word[i - 1]))
                {
                    built.Append(' ');
                    wordStart = true;
                }

                built.Append(wordStart ? char.ToUpperInvariant(letter) : char.ToLowerInvariant(letter));
            }

            return built.ToString().TrimEnd();
        }

        // Photon's region codes, like "usw" or "eu/*", by name
        private static string RegionName(string region)
        {
            if (string.IsNullOrEmpty(region))
                return "unknown region";

            int slash = region.IndexOf('/');
            string code = slash >= 0 ? region.Substring(0, slash) : region;
            switch (code)
            {
                case "us": return "US East";
                case "usw": return "US West";
                case "eu": return "Europe";
                default: return code.ToUpperInvariant();
            }
        }

        /// <summary>Takes the room card off your wrist.</summary>
        public static void DisableRoomCard()
        {
            DestroyText(ref roomCardText);
            roomCardName = roomCardMode = roomCardRegion = roomCardWhere = null;
        }

        // The game only plays a tap once a hand has been off the surface for this many frames,
        // and no more than once every this many seconds per hand
        private const int TapClearFrames = 5;
        private const float TapCooldown = 0.15f;
        private static readonly int[] tapCounterSeconds = new int[60];
        private static TextMeshPro tapCounterText;
        private static float tapCounterLeftAt = -10f, tapCounterRightAt = -10f, tapCounterNext;
        private static int tapCounterTotal, tapCounterSecond = -1, tapCounterFrame = -2, tapCounterLeftOff, tapCounterRightOff;
        private static int tapCounterShown = int.MinValue, tapCounterRateShown;

        /// <summary>How many times your hands have tapped a surface this session, and how many in the last minute, on your right wrist.</summary>
        public static void TapCounter()
        {
            float now = Time.unscaledTime;
            int second = Mathf.FloorToInt(now);
            int seconds = tapCounterSeconds.Length;

            // Empties the seconds that have gone by, so the last minute only holds recent taps
            if (tapCounterSecond < 0 || second - tapCounterSecond >= seconds)
                System.Array.Clear(tapCounterSeconds, 0, seconds);
            else
                for (int past = tapCounterSecond + 1; past <= second; past++)
                    tapCounterSeconds[past % seconds] = 0;
            tapCounterSecond = second;

            // Taps are when a hand lands on something, the same touch that moves you. The first
            // frame after a gap has nothing to compare against, and the game counts none while
            // its overlay is up.
            bool fresh = tapCounterFrame != Time.frameCount - 1;
            tapCounterFrame = Time.frameCount;
            if (fresh || GTPlayer.Instance.inOverlay)
                tapCounterLeftOff = tapCounterRightOff = 0;
            else
            {
                CountTap(GTPlayer.Instance.IsHandTouching(true), ref tapCounterLeftOff, ref tapCounterLeftAt, now);
                CountTap(GTPlayer.Instance.IsHandTouching(false), ref tapCounterRightOff, ref tapCounterRightAt, now);
            }

            if (tapCounterText == null)
            {
                tapCounterText = MakeWristText("Eclipse_TapCounter", Color.white);
                tapCounterShown = int.MinValue;
            }

            PlaceOnWrist(tapCounterText, 1.28f, true);
            if (Time.time < tapCounterNext)
                return;
            tapCounterNext = Time.time + 0.1f;

            int rate = 0;
            for (int i = 0; i < seconds; i++)
                rate += tapCounterSeconds[i];

            tapCounterText.color = Color.Lerp(Color.white, Accent, Mathf.Clamp01(rate / 120f));
            if (tapCounterTotal == tapCounterShown && rate == tapCounterRateShown)
                return;
            tapCounterShown = tapCounterTotal;
            tapCounterRateShown = rate;

            tapCounterText.text = $"{tapCounterTotal} {(tapCounterTotal == 1 ? "tap" : "taps")}\n<size=55%>{rate} in the last minute</size>";
        }

        // A rising edge of a hand touching, kept only if it was clear for long enough and isn't too soon after the last
        private static void CountTap(bool touching, ref int framesOff, ref float lastTap, float now)
        {
            if (!touching)
            {
                framesOff++;
                return;
            }

            if (framesOff >= TapClearFrames && now - lastTap >= TapCooldown)
            {
                lastTap = now;
                tapCounterTotal++;
                tapCounterSeconds[tapCounterSecond % tapCounterSeconds.Length]++;
            }

            framesOff = 0;
        }

        /// <summary>Takes the tap counter off your wrist; your count stays until the game closes.</summary>
        public static void DisableTapCounter() => DestroyText(ref tapCounterText);
    }
}
