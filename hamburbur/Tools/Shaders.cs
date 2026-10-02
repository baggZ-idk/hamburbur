using UnityEngine;
// ReSharper disable ShaderLabShaderReferenceNotResolved

namespace hamburbur.Tools;

public abstract class Shaders
{
    public static Shader UberShader
    {
        get
        {
            if (field == null)
                field = Shader.Find("GorillaTag/UberShader");

            return field;
        }
    }
    
    public static Shader ScaryGlassShader
    {
        get
        {
            if (field == null)
                field = Shader.Find("GorillaTag/URPScryGlass");

            return field;
        }
    }
    
    public static Shader HatlasShader
    {
        get
        {
            if (field == null)
                field = Shader.Find("hatlas");

            return field;
        }
    }

    public static Shader TextShader
    {
        get
        {
            if (field == null)
                field = Shader.Find("GUI/Text Shader");

            return field;
        }
    }

    public static Shader UniversalUnlitShader
    {
        get
        {
            if (field == null)
                field = Shader.Find("Universal Render Pipeline/Unlit");

            return field;
        }
    }
    
    public static Shader UniversalLitShader
    {
        get
        {
            if (field == null)
                field = Shader.Find("Universal Render Pipeline/Lit");

            return field;
        }
    }
    
    public static Shader UnlitTextureShader
    {
        get
        {
            if (field == null)
                field = Shader.Find("Unlit/Texture");

            return field;
        }
    }
    
    public static Shader SpritesDefaultShader
    {
        get
        {
            if (field == null)
                field = Shader.Find("Sprites/Default");

            return field;
        }
    }
}