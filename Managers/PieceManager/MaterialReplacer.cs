using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MistrootTamer;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PieceManager
{
    [PublicAPI]
    public static class MaterialReplacer
    {
        private static readonly Dictionary<GameObject, bool> ObjectToSwap = new Dictionary<GameObject, bool>();
        private static readonly Dictionary<string, Material> OriginalMaterials = new Dictionary<string, Material>();
        private static readonly Dictionary<GameObject, ShaderType> ObjectsForShaderReplace = new Dictionary<GameObject, ShaderType>();
        private static bool hasRun = false;

        static MaterialReplacer()
        {
            Harmony harmony = new Harmony("org.bepinex.helpers.PieceManager");
            harmony.Patch(AccessTools.DeclaredMethod(typeof(ZoneSystem), nameof(ZoneSystem.Start)), postfix: new HarmonyMethod(typeof(MaterialReplacer), nameof(ReplaceAllMaterialsWithOriginal)));
        }

        public enum ShaderType
        {
            PieceShader,
            VegetationShader,
            RockShader,
            RugShader,
            GrassShader,
            CustomCreature,
            UseUnityShader
        }

        public static void RegisterGameObjectForShaderSwap(GameObject go, ShaderType type)
        {
            if (go == null)
            {
                Debug.LogWarning("Attempted to register a null GameObject for shader swap.");
                return;
            }

            if (!ObjectsForShaderReplace.ContainsKey(go))
            {
                ObjectsForShaderReplace.Add(go, type);
            }
        }

        public static void RegisterGameObjectForMatSwap(GameObject go, bool isJotunnMock = false)
        {
            if (go == null)
            {
                Debug.LogWarning("Attempted to register a null GameObject for material swap.");
                return;
            }

            if (!ObjectToSwap.ContainsKey(go))
            {
                ObjectToSwap.Add(go, isJotunnMock);
            }
        }

        private static void GetAllMaterials()
        {
            foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (!OriginalMaterials.ContainsKey(material.name) && material.shader?.name != "Hidden/InternalErrorShader")
                {
                    OriginalMaterials[material.name] = material;
                }
            }
        }

        [HarmonyPriority(Priority.VeryHigh)]
        private static void ReplaceAllMaterialsWithOriginal()
        {
            if (UnityEngine.SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null || hasRun)
            {
                return;
            }

            if (OriginalMaterials.Count == 0)
            {
                GetAllMaterials();
            }

            foreach (var kvp in ObjectToSwap)
            {
                var go = kvp.Key;
                var isJotunnMock = kvp.Value;
                ProcessGameObjectMaterials(go, isJotunnMock);
            }

            foreach (var kvp in ObjectsForShaderReplace)
            {
                var go = kvp.Key;
                var shaderType = kvp.Value;
                ProcessGameObjectShaders(go, shaderType);
            }

            hasRun = true;
        }

        private static void ProcessGameObjectMaterials(GameObject go, bool isJotunnMock)
        {
            if (go == null) return;

            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var newMaterials = renderer.sharedMaterials.Select(material => ReplaceMaterial(material, isJotunnMock)).ToArray();
                renderer.sharedMaterials = newMaterials;
            }
        }

        private static Material ReplaceMaterial(Material originalMaterial, bool isJotunnMock)
        {
            if (originalMaterial == null) return null;

            string cleanName = originalMaterial.name.Replace(" (Instance)", "");
            if (OriginalMaterials.TryGetValue(cleanName, out var replacementMaterial))
            {
                MistrootTamerPlugin.MistrootTamerLogger.LogWarning($"Found {replacementMaterial.name}. Replacing {cleanName} with {replacementMaterial.name}, shader is: {replacementMaterial.shader}");
                return replacementMaterial;
            }

            Debug.LogWarning($"No suitable material found to replace: {cleanName}");
            return originalMaterial;
        }

        private static void ProcessGameObjectShaders(GameObject go, ShaderType shaderType)
        {
            if (go == null) return;

            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        material.shader = GetShaderForType(material.shader, shaderType, material.shader.name);
                    }
                }
            }
        }

        private static Shader GetShaderForType(Shader orig, ShaderType shaderType, string originalShaderName)
        {
            var shaderName = GetShaderNameByType(shaderType, originalShaderName);
            Shader shader = FindShaderWithName(orig, shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"Shader not found: {shaderName}. Using original shader.");
                return orig;
            }
            return shader;
        }

        public static Shader FindShaderWithName(Shader origShader, string name)
        {
            foreach (Shader shader in Resources.FindObjectsOfTypeAll<Shader>())
            {
                if (shader.name == name)
                {
                    return shader;
                }
            }

            return origShader;
        }

        private static string GetShaderNameByType(ShaderType type, string defaultName)
        {
            switch (type)
            {
                case ShaderType.PieceShader: return "Custom/Piece";
                case ShaderType.VegetationShader: return "Custom/Vegetation";
                case ShaderType.RockShader: return "Custom/StaticRock";
                case ShaderType.RugShader: return "Custom/Rug";
                case ShaderType.GrassShader: return "Custom/Grass";
                case ShaderType.CustomCreature: return "Custom/Creature";
                case ShaderType.UseUnityShader: return defaultName;
                default: return "Standard";
            }
        }
    }
}
