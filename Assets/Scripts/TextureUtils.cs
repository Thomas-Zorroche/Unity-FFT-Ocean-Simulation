using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TextureUtils
{

    public static RenderTexture CreateRenderTexture(int size, RenderTextureFormat format = RenderTextureFormat.RGFloat, bool useMips = false)
    {
        RenderTexture rt = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear);
        
        rt.useMipMap = useMips;
        rt.autoGenerateMips = false;
        rt.anisoLevel = 6;
        rt.filterMode = FilterMode.Trilinear;
        rt.wrapMode = TextureWrapMode.Repeat;
        rt.enableRandomWrite = true;
        rt.Create();
        
        return rt;
    }

    public static void SavePNG(RenderTexture rt, string fileName)
    {
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        var path = "Assets/Textures/" + fileName + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Debug.Log("Saved file to: " + path);
    }
}
