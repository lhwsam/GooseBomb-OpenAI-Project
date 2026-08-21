using UnityEditor;
using UnityEngine;

namespace BombSwap.Editor.UI
{
    public sealed class PixelFontShaderGui : ShaderGUI
    {
        public override void OnGUI(
            MaterialEditor materialEditor,
            MaterialProperty[] properties)
        {
            MaterialProperty atlas = FindProperty("_MainTex", properties);
            MaterialProperty faceColor = FindProperty("_FaceColor", properties);
            MaterialProperty outlineColor = FindProperty("_OutlineColor", properties);
            MaterialProperty outlineWidth = FindProperty("_OutlineWidth", properties);
            MaterialProperty padding = FindProperty("_Padding", properties);

            EditorGUILayout.LabelField("Bomb Swap Pixel Font", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                materialEditor.TexturePropertySingleLine(
                    new GUIContent("Font Atlas"),
                    atlas);
            }

            materialEditor.ColorProperty(faceColor, "Text Tint");
            materialEditor.ColorProperty(outlineColor, "Outline Color");
            materialEditor.ShaderProperty(outlineWidth, "Outline Width (0-2 px)");
            materialEditor.ShaderProperty(padding, "TMP Mesh Padding");

            EditorGUILayout.HelpBox(
                "Keep mesh padding at least as large as the outline width. " +
                "For gradients, enable Vertex Color Gradient on the TextMeshProUGUI component " +
                "and assign the PixelWarmGradient preset or edit its four vertex colors.",
                MessageType.Info);
        }
    }
}
