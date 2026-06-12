using System;
using UnityEngine;

[Serializable]
public class FaceExpressionData
{
    public string expressionKey;
    public Texture2D leftEyeTexture;
    public Texture2D rightEyeTexture;
    public Texture2D mouthTexture;
    public bool showCheek;
}
