/*
 * Eclipse Menu  Classes/Menu/EclipseMeshes.cs
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

using Eclipse.Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Eclipse.Classes.Menu
{
    /// <summary>
    /// Eclipse's own 3D menu model. Every flat cube of the menu is swapped for a rounded tile with
    /// a bevelled front edge, built in code at the right proportions for that piece, so the
    /// panel, the buttons and the tabs all get soft corners without changing where anything is.
    /// </summary>
    public static class EclipseMeshes
    {
        private const int CornerSegments = 6;
        private static readonly Dictionary<long, Mesh> roundedCache = new Dictionary<long, Mesh>();
        private static Mesh star;

        // The size of the menu's root before it is scaled for the player, in depth, width, height
        private static readonly Vector3 RootScale = new Vector3(0.1f, 0.3f, 0.3825f);

        /// <summary>
        /// A box in unit space (-0.5 to 0.5) whose front (+X) face is a rounded rectangle with a
        /// bevelled edge. The radii are given in unit space per axis so the corners stay round
        /// after the object's own non-uniform scale is applied.
        /// </summary>
        public static Mesh RoundedBox(float ry, float rz, float by, float bz, float bx)
        {
            ry = Mathf.Clamp(ry, 0.001f, 0.5f);
            rz = Mathf.Clamp(rz, 0.001f, 0.5f);
            by = Mathf.Clamp(by, 0f, ry * 0.9f);
            bz = Mathf.Clamp(bz, 0f, rz * 0.9f);
            bx = Mathf.Clamp(bx, 0f, 0.9f);

            long key = Quantize(ry) | Quantize(rz) << 12 | Quantize(by) << 24 | Quantize(bz) << 36 | Quantize(bx) << 48;
            if (roundedCache.TryGetValue(key, out Mesh cached) && cached != null)
                return cached;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            List<Vector2> outer = Ring(0.5f, 0.5f, ry, rz);
            List<Vector2> inner = Ring(0.5f - by, 0.5f - bz, Mathf.Max(ry - by, ry * 0.35f), Mathf.Max(rz - bz, rz * 0.35f));

            float back = -0.5f;
            float middle = 0.5f - bx;
            float front = 0.5f;

            Cap(vertices, triangles, outer, back, -1f);
            Band(vertices, triangles, outer, back, outer, middle, 0f);
            Band(vertices, triangles, outer, middle, inner, front, 1f);
            Cap(vertices, triangles, inner, front, 1f);

            Mesh mesh = new Mesh { name = "EclipseRoundedBox" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);

            Vector2[] uv = new Vector2[vertices.Count];
            for (int i = 0; i < uv.Length; i++)
                uv[i] = new Vector2(vertices[i].z + 0.5f, vertices[i].y + 0.5f);
            mesh.uv = uv;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
            roundedCache[key] = mesh;
            return mesh;
        }

        private static long Quantize(float value) => (long)Mathf.Clamp(Mathf.RoundToInt(value * 4000f), 0, 4095);

        // The outline of a rounded rectangle in the YZ plane, going around once
        private static List<Vector2> Ring(float halfY, float halfZ, float ry, float rz)
        {
            List<Vector2> points = new List<Vector2>(4 * (CornerSegments + 1));
            Vector2[] centres =
            {
                new Vector2(halfY - ry, halfZ - rz),
                new Vector2(-halfY + ry, halfZ - rz),
                new Vector2(-halfY + ry, -halfZ + rz),
                new Vector2(halfY - ry, -halfZ + rz)
            };

            for (int corner = 0; corner < 4; corner++)
            {
                float start = corner * 90f;
                for (int s = 0; s <= CornerSegments; s++)
                {
                    float angle = (start + 90f * s / CornerSegments) * Mathf.Deg2Rad;
                    points.Add(centres[corner] + new Vector2(Mathf.Cos(angle) * ry, Mathf.Sin(angle) * rz));
                }
            }

            return points;
        }

        // Adds a triangle facing `outward`
        private static void Triangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
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

        // A flat face filling a ring, facing +X (side 1) or -X (side -1)
        private static void Cap(List<Vector3> vertices, List<int> triangles, List<Vector2> ring, float x, float side)
        {
            Vector3 centre = new Vector3(x, 0f, 0f);
            Vector3 outward = new Vector3(side, 0f, 0f);
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i];
                Vector2 b = ring[(i + 1) % ring.Count];
                Triangle(vertices, triangles, centre, new Vector3(x, a.x, a.y), new Vector3(x, b.x, b.y), outward);
            }
        }

        // The wall joining two rings; `forward` tilts the facing toward +X for the bevel
        private static void Band(List<Vector3> vertices, List<int> triangles, List<Vector2> ringA, float xA, List<Vector2> ringB, float xB, float forward)
        {
            for (int i = 0; i < ringA.Count; i++)
            {
                int j = (i + 1) % ringA.Count;
                Vector3 a0 = new Vector3(xA, ringA[i].x, ringA[i].y);
                Vector3 a1 = new Vector3(xA, ringA[j].x, ringA[j].y);
                Vector3 b0 = new Vector3(xB, ringB[i].x, ringB[i].y);
                Vector3 b1 = new Vector3(xB, ringB[j].x, ringB[j].y);

                Vector2 mid = (ringA[i] + ringA[j]) * 0.5f;
                Vector3 outward = new Vector3(forward, mid.x, mid.y);

                Triangle(vertices, triangles, a0, a1, b1, outward);
                Triangle(vertices, triangles, a0, b1, b0, outward);
            }
        }

        /// <summary>A small three-dimensional star for the pointer.</summary>
        public static Mesh Star
        {
            get
            {
                if (star != null)
                    return star;

                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                Vector3[] axes = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
                const float tip = 0.5f;
                const float waist = 0.13f;

                foreach (Vector3 axis in axes)
                {
                    // The four inner points around this spike, in order around the axis
                    Vector3 u = Mathf.Abs(axis.x) > 0.5f ? Vector3.up : Vector3.right;
                    Vector3 v = Vector3.Cross(axis, u);
                    Vector3[] ring =
                    {
                        (axis + u + v) * waist,
                        (axis + u - v) * waist,
                        (axis - u - v) * waist,
                        (axis - u + v) * waist
                    };

                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 a = ring[i];
                        Vector3 b = ring[(i + 1) % 4];
                        Triangle(vertices, triangles, axis * tip, a, b, axis + (a + b) * 0.5f);
                    }
                }

                star = new Mesh { name = "EclipseStar" };
                star.SetVertices(vertices);
                star.SetTriangles(triangles, 0);
                star.RecalculateNormals();
                star.RecalculateBounds();
                star.hideFlags = HideFlags.DontUnloadUnusedAsset;
                return star;
            }
        }

        private static Mesh torus;

        /// <summary>A small ring (a torus) for the pointer.</summary>
        public static Mesh Torus
        {
            get
            {
                if (torus != null)
                    return torus;

                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                const int around = 24, tube = 8;
                const float major = 0.38f, minor = 0.12f;

                Vector3 Point(int u, int v)
                {
                    float a = u * Mathf.PI * 2f / around, b = v * Mathf.PI * 2f / tube;
                    float reach = major + minor * Mathf.Cos(b);
                    return new Vector3(Mathf.Cos(a) * reach, minor * Mathf.Sin(b), Mathf.Sin(a) * reach);
                }

                for (int u = 0; u < around; u++)
                {
                    float middle = (u + 0.5f) * Mathf.PI * 2f / around;
                    Vector3 core = new Vector3(Mathf.Cos(middle) * major, 0f, Mathf.Sin(middle) * major);
                    for (int v = 0; v < tube; v++)
                    {
                        Vector3 a = Point(u, v), b = Point(u + 1, v), c = Point(u + 1, v + 1), d = Point(u, v + 1);
                        Vector3 outward = (a + b + c + d) * 0.25f - core;
                        Triangle(vertices, triangles, a, b, c, outward);
                        Triangle(vertices, triangles, a, c, d, outward);
                    }
                }

                torus = new Mesh { name = "EclipseTorus" };
                torus.SetVertices(vertices);
                torus.SetTriangles(triangles, 0);
                torus.RecalculateNormals();
                torus.RecalculateBounds();
                torus.hideFlags = HideFlags.DontUnloadUnusedAsset;
                return torus;
            }
        }

        /// <summary>
        /// Swaps every cube in the menu for a rounded tile. Positions, sizes, colliders and
        /// colours stay exactly as they were.
        /// </summary>
        /// <param name="skip">An object to leave as a cube (a textured background, for example).</param>
        public static void Remodel(GameObject root, GameObject skip = null)
        {
            if (root == null)
                return;

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null || filter.sharedMesh == null || filter.gameObject == skip || filter.gameObject == root)
                    continue;

                // Only the built-in cube; text, cylinders and anything custom keep their mesh
                if (filter.sharedMesh.name != "Cube")
                    continue;

                Vector3 size = SizeUnder(root.transform, filter.transform);
                float sx = Mathf.Abs(size.x), sy = Mathf.Abs(size.y), sz = Mathf.Abs(size.z);
                if (sx < 1e-5f || sy < 1e-5f || sz < 1e-5f)
                    continue;

                float smaller = Mathf.Min(sy, sz);
                float aspect = Mathf.Max(sy, sz) / smaller;
                // Long thin pieces become pills, small square ones become discs
                float roundness = aspect > 2.5f ? 0.45f : aspect < 1.15f && smaller < 0.03f ? 0.5f : 0.3f;
                float radius = Mathf.Min(smaller * roundness, 0.035f);
                float bevel = Mathf.Min(radius * 0.5f, sx * 0.6f);

                filter.sharedMesh = RoundedBox(radius / sy, radius / sz, bevel / sy, bevel / sz, bevel / sx);
            }
        }

        private static readonly Dictionary<(int, int, int, int, int), Mesh> haloCache = new Dictionary<(int, int, int, int, int), Mesh>();

        /// <summary>
        /// A flat ring that fades out around a rounded tile's outline, for a soft glow. It is built
        /// in the tile's own unit space, so it goes on a child of the tile, and it spreads the same
        /// distance on every side after the menu's uneven scale.
        /// </summary>
        /// <param name="spread">How far the glow reaches past the tile's edge, in metres.</param>
        /// <param name="strength">How opaque the glow is at the tile's edge.</param>
        public static Mesh Halo(Transform root, Transform piece, float spread, float strength)
        {
            Vector3 size = SizeUnder(root, piece);
            float sx = Mathf.Abs(size.x), sy = Mathf.Abs(size.y), sz = Mathf.Abs(size.z);
            if (sx < 1e-5f || sy < 1e-5f || sz < 1e-5f || spread <= 0f)
                return null;

            // Tenths of a millimetre, and hundredths of strength
            var key = (Mathf.RoundToInt(sx * 10000f), Mathf.RoundToInt(sy * 10000f), Mathf.RoundToInt(sz * 10000f), Mathf.RoundToInt(spread * 10000f), Mathf.RoundToInt(strength * 100f));
            if (haloCache.TryGetValue(key, out Mesh cached) && cached != null)
                return cached;

            // The same corners Remodel gave the tile
            float smaller = Mathf.Min(sy, sz);
            float aspect = Mathf.Max(sy, sz) / smaller;
            float roundness = aspect > 2.5f ? 0.45f : aspect < 1.15f && smaller < 0.03f ? 0.5f : 0.3f;
            float radius = Mathf.Min(smaller * roundness, 0.035f);

            // Out from the edge: full strength, a quick drop, then nothing
            float[] reach = { 0f, 0.3f, 1f };
            float[] alpha = { strength, strength * 0.35f, 0f };
            float front = 0.5f + 0.0002f / sx;

            List<Vector3> vertices = new List<Vector3>();
            List<Color> colors = new List<Color>();
            for (int r = 0; r < reach.Length; r++)
            {
                float y = spread * reach[r] / sy, z = spread * reach[r] / sz;
                foreach (Vector2 point in Ring(0.5f + y, 0.5f + z, radius / sy + y, radius / sz + z))
                {
                    vertices.Add(new Vector3(front, point.x, point.y));
                    colors.Add(new Color(1f, 1f, 1f, alpha[r]));
                }
            }

            List<int> triangles = new List<int>();
            int count = vertices.Count / reach.Length;
            for (int r = 0; r < reach.Length - 1; r++)
            {
                int inner = r * count, outer = (r + 1) * count;
                for (int i = 0; i < count; i++)
                {
                    int j = (i + 1) % count;
                    triangles.Add(inner + i);
                    triangles.Add(outer + i);
                    triangles.Add(outer + j);
                    triangles.Add(inner + i);
                    triangles.Add(outer + j);
                    triangles.Add(inner + j);
                }
            }

            Mesh mesh = new Mesh { name = "EclipseHalo" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
            haloCache[key] = mesh;
            return mesh;
        }

        // The object's size in metres as if the menu were at its base scale
        private static Vector3 SizeUnder(Transform root, Transform child)
        {
            Vector3 size = Vector3.one;
            for (Transform t = child; t != null && t != root; t = t.parent)
                size = Vector3.Scale(size, t.localScale);
            return Vector3.Scale(size, RootScale);
        }

        // ── Press animation ─────────────────────────────────────────────────────

        private static string pressedName;
        private static float pressedAt = -10f;

        /// <summary>Remembers which button was just pressed, so the redrawn menu can show it pushing in.</summary>
        public static void NotePress(string buttonName)
        {
            pressedName = buttonName;
            pressedAt = Time.time;
        }

        /// <summary>Plays the push-in on the button that was just pressed, if it is in this menu.</summary>
        public static void AnimatePress(GameObject root)
        {
            if (root == null || pressedName == null || Time.time - pressedAt > 0.3f || CoroutineManager.instance == null)
                return;

            foreach (ButtonCollider button in root.GetComponentsInChildren<ButtonCollider>(true))
            {
                if (button != null && button.relatedText == pressedName)
                {
                    CoroutineManager.instance.StartCoroutine(PushIn(button.transform));
                    break;
                }
            }

            pressedName = null;
        }

        private static IEnumerator PushIn(Transform button)
        {
            const float duration = 0.18f;
            Vector3 rest = button.localPosition;
            Vector3 pressed = rest - new Vector3(0.035f, 0f, 0f);

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (button == null)
                    yield break;

                // Starts pushed in, springs back out with a small overshoot
                float k = t / duration;
                float spring = 1f - Mathf.Exp(-6f * k) * Mathf.Cos(9f * k);
                button.localPosition = Vector3.LerpUnclamped(pressed, rest, spring);
                yield return null;
            }

            if (button != null)
                button.localPosition = rest;
        }
    }

    /// <summary>Turns the pointer star slowly.</summary>
    public class EclipseSpin : MonoBehaviour
    {
        public float speed = 90f;

        private void Update() =>
            transform.localRotation = Quaternion.Euler(Time.time * speed, Time.time * speed * 0.6f, 0f);
    }
}
