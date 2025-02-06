using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Snapshot : MonoBehaviour
{
    public int width = 720;
    public int height = 720;
    public string path = "Assets/Recordings";

    public void DoTakeSnapshot(string id, Action callback)
    {
        Camera cam = gameObject.GetComponent<Camera>();
        var tex = TakeSnapshot(cam, width, height);
        SaveTexture(tex, path, id);
        callback.Invoke();
    }

    static Texture2D TakeSnapshot(Camera cam, int width, int height)
    {
        cam.targetTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 8);
        cam.Render();

        RenderTexture previousRt = RenderTexture.active;
        RenderTexture.active = cam.targetTexture;

        Texture2D texture = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.ARGB32, false);
        texture.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        texture.Apply(false);

        RenderTexture.active = previousRt;

        RenderTexture.ReleaseTemporary(cam.targetTexture);
        cam.targetTexture = null;

        return texture;
    }

    static void MakePNGAlpha(Texture2D tex)
    {
        var color = tex.GetPixels32();
        for (int i = color.Length - 1; i >= 0; --i)
        {
            var c = color[i];
            if (c.a == 0)
            {
                c.a = (byte)Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                color[i] = c;
            }
        }
        tex.SetPixels32(color);
    }

    static void SaveTexture(Texture2D texture, string path, string name)
    {
        MakePNGAlpha(texture);
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes($"{path}/{name}.png", pngData);
    }
}
