using System;
using System.IO;
using UnityEditor;

namespace BombSwap.Editor.Verification
{
    internal sealed class ResponsiveWebGLTemplateScope : IDisposable
    {
        internal const string TemplateAssetPath = "Assets/WebGLTemplates/BombSwap/index.html";
        internal const string TemplateSetting = "PROJECT:BombSwap";

        private readonly string _previousTemplate;
        private bool _disposed;

        private ResponsiveWebGLTemplateScope()
        {
            if (!File.Exists(TemplateAssetPath))
            {
                throw new FileNotFoundException(
                    "The Bomb Swap WebGL template is missing.",
                    TemplateAssetPath);
            }

            _previousTemplate = PlayerSettings.WebGL.template;
            PlayerSettings.WebGL.template = TemplateSetting;
            if (!string.Equals(
                    PlayerSettings.WebGL.template,
                    TemplateSetting,
                    StringComparison.Ordinal))
            {
                PlayerSettings.WebGL.template = _previousTemplate;
                throw new InvalidOperationException(
                    $"Unity did not activate WebGL template '{TemplateSetting}'.");
            }
        }

        public static ResponsiveWebGLTemplateScope Activate()
        {
            return new ResponsiveWebGLTemplateScope();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            PlayerSettings.WebGL.template = _previousTemplate;
            AssetDatabase.SaveAssets();
            _disposed = true;
        }
    }
}
