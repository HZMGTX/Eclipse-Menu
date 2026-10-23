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
    /// so these sit above them. Anything that counts real minutes uses unscaled time, so a mod that
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
    }
}
