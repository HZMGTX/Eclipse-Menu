/*
 * Eclipse Menu  Classes/Menu/EclipseLook.cs
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

using Eclipse.Menu;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eclipse.Classes.Menu
{
    /// <summary>
    /// The lit side of the Eclipse model: shiny tiles that catch the light but never go fully dark,
    /// and accent pieces (the rims, the corner brackets, the lit strips) that glow, with a soft
    /// halo spreading around them.
    /// </summary>
    public static class EclipseLook
    {
        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string UberShaderName = "GorillaTag/UberShader";

        // How shiny the tiles are, and how much of their own colour they give off so they still
        // show in dark maps (in linear light)
        private const float Smoothness = 0.45f;
        private const float SelfLight = 0.14f;

        private static Shader litShader;
        private static Shader haloShader;

        /// <summary>Marks a piece of the menu to glow when the menu is finished.</summary>
        /// <param name="spread">How far its halo reaches past its edge, in metres.</param>
        /// <param name="strength">How opaque the halo is at its edge.</param>
        /// <param name="emission">How brightly the piece itself glows (in linear light).</param>
        public static GameObject MarkGlow(GameObject piece, float spread, float strength, float emission)
        {
            if (piece == null)
                return null;

            EclipseGlow glow = piece.AddComponent<EclipseGlow>();
            glow.spread = spread;
            glow.strength = strength;
            glow.emission = emission;
            return piece;
        }

        /// <summary>
        /// Gives the finished menu its lit look. Runs after <see cref="EclipseMeshes.Remodel"/>, on
        /// the rounded tiles only.
        /// </summary>
        /// <param name="skip">An object to leave alone (a textured background, for example).</param>
        public static void Apply(GameObject root, GameObject skip = null)
        {
            if (root == null || Main.transparentMenu)
                return;

            // The crystal look keeps its own shader
            bool shine = !Main.disableEclipseShine && !Main.crystallizeMenu;
            bool glow = !Main.disableEclipseGlow;
            if (!shine && !glow)
                return;

            if (litShader == null)
                litShader = Shader.Find(LitShaderName);
            if (haloShader == null)
                haloShader = Shader.Find("Sprites/Default");

            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null || renderer.gameObject == skip)
                    continue;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || filter.sharedMesh.name != "EclipseRoundedBox")
                    continue;

                Material material = renderer.sharedMaterial;
                if (material == null || material == Main.CrystalMaterial)
                    continue;

                EclipseGlow mark = renderer.GetComponent<EclipseGlow>();
                float emission = 0f;

                string shader = material.shader != null ? material.shader.name : "";
                if (shine && litShader != null && (shader == UberShaderName || shader == LitShaderName))
                {
                    if (shader != LitShaderName)
                        material.shader = litShader;

                    material.SetFloat("_Smoothness", Smoothness);
                    material.SetFloat("_Metallic", 0f);
                    emission = SelfLight;
                }

                if (glow && mark != null && material.shader == litShader)
                    emission += mark.emission;

                MeshRenderer halo = null;
                if (glow && mark != null && haloShader != null)
                    halo = AddHalo(root.transform, renderer, mark);

                if (emission <= 0f && halo == null)
                    continue;

                if (emission > 0f)
                {
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }

                EclipseLight light = renderer.gameObject.AddComponent<EclipseLight>();
                light.Init(renderer, material, emission, halo);
            }
        }

        private static MeshRenderer AddHalo(Transform root, MeshRenderer piece, EclipseGlow mark)
        {
            Mesh mesh = EclipseMeshes.Halo(root, piece.transform, mark.spread, mark.strength);
            if (mesh == null)
                return null;

            GameObject halo = new GameObject("Eclipse_Halo");
            halo.transform.SetParent(piece.transform, false);
            halo.layer = piece.gameObject.layer;
            halo.AddComponent<MeshFilter>().sharedMesh = mesh;

            // Drawn just before the menu's text, so the text always sits on top of the glow
            Material material = new Material(haloShader) { renderQueue = (int)RenderQueue.Transparent - 10 };
            MeshRenderer renderer = halo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }
    }

    /// <summary>Marks a menu piece that glows. Read by <see cref="EclipseLook.Apply"/>.</summary>
    public class EclipseGlow : MonoBehaviour
    {
        public float spread;
        public float strength;
        public float emission;
    }

    /// <summary>
    /// Keeps a tile's glow in step with its colour as the theme fades, and shows or hides its halo
    /// along with it. Owns the halo's material.
    /// </summary>
    public class EclipseLight : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private Renderer pieceRenderer;
        private Renderer haloRenderer;
        private Material material;
        private Material halo;
        private float emission;
        private Color last = new Color(-1f, -1f, -1f, -1f);
        private bool lastShown;

        public void Init(Renderer renderer, Material pieceMaterial, float linearEmission, Renderer haloPiece)
        {
            pieceRenderer = renderer;
            material = pieceMaterial;
            haloRenderer = haloPiece;
            halo = haloPiece != null ? haloPiece.sharedMaterial : null;

            // Material colours are set in gamma space, so the strength is converted to match
            emission = linearEmission > 0f ? Mathf.LinearToGammaSpace(linearEmission) : 0f;
            lastShown = !renderer.enabled;
            LateUpdate();
        }

        // After ColorChanger has set this frame's colour
        private void LateUpdate()
        {
            if (pieceRenderer == null)
                return;

            if (haloRenderer != null && lastShown != pieceRenderer.enabled)
            {
                lastShown = pieceRenderer.enabled;
                haloRenderer.enabled = lastShown;
            }

            // Moving gradients swap in their own material; the glow keeps its last colour then
            if (material == null)
                return;
            if (pieceRenderer.sharedMaterial != material)
            {
                material = null;
                return;
            }

            Color color = material.color;
            if (color == last)
                return;
            last = color;

            if (emission > 0f)
                material.SetColor(EmissionColor, new Color(color.r * emission, color.g * emission, color.b * emission, 1f));

            if (halo != null)
                halo.color = new Color(color.r, color.g, color.b, 1f);
        }

        private void OnDestroy()
        {
            if (halo != null)
                Destroy(halo);
        }
    }
}
