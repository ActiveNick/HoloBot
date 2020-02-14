﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.UI
{
    // Basic value types within a shader
    public enum ShaderPropertyType { Color, Float, Range, TexEnv, Vector, None }

    /// <summary>
    /// property format for each property
    /// </summary>
    [System.Serializable]
    public struct ShaderProperties
    {
        public string Name;
        public ShaderPropertyType Type;
        public Vector2 Range;
    }

    /// <summary>
    /// collection of properties found in a shader
    /// </summary>
    public struct ShaderInfo
    {
        public ShaderProperties[] ShaderOptions;
        public string Name;
    }


    /// <summary>
    /// Collection of shader and material utilities
    /// </summary>

    public class InteractableThemeShaderUtils : MonoBehaviour
    {
        /// <summary>
        /// Get a MaterialPropertyBlock and copy the designated properties
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="props"></param>
        /// <returns></returns>
        public static MaterialPropertyBlock GetMaterialPropertyBlock(GameObject gameObject, ShaderProperties[] props)
        {
            MaterialPropertyBlock materialBlock = GetPropertyBlock(gameObject);
            Renderer renderer = gameObject.GetComponent<Renderer>();

            float value;
            if (renderer != null)
            {
                Material material = GetValidMaterial(renderer);
                if (material != null)
                {
                    for (int i = 0; i < props.Length; i++)
                    {
                        ShaderProperties prop = props[i];
                        switch (props[i].Type)
                        {
                            case ShaderPropertyType.Color:
                                Color color = material.GetVector(prop.Name);
                                materialBlock.SetColor(prop.Name, color);
                                break;
                            case ShaderPropertyType.Float:
                                value = material.GetFloat(prop.Name);
                                materialBlock.SetFloat(prop.Name, value);
                                break;
                            case ShaderPropertyType.Range:
                                value = material.GetFloat(prop.Name);
                                materialBlock.SetFloat(prop.Name, value);
                                break;
                            default:
                                break;
                        }
                    }
                }
                gameObject.GetComponent<Renderer>().SetPropertyBlock(materialBlock);
            }

            return materialBlock;
        }

        /// <summary>
        /// Get the MaterialPropertyBlock from a renderer on a gameObject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static MaterialPropertyBlock GetPropertyBlock(GameObject gameObject)
        {
            MaterialPropertyBlock materialBlock = new MaterialPropertyBlock();
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.GetPropertyBlock(materialBlock);
            }
            return materialBlock;
        }

        /// <summary>
        /// Grab the shared material to avoid creating new material instances and breaking batching.
        /// Because MaterialPropertyBlocks are used for setting material properties the shared material is
        /// used to set the initial state of the MaterialPropertyBlock(s) before mutating state.
        /// </summary>
        /// <param name="renderer"></param>
        /// <returns></returns>
        public static Material GetValidMaterial(Renderer renderer)
        {
            Material material = null;

            if (renderer != null)
            {
                material = renderer.sharedMaterial;
            }
            return material;
        }
    }
}
