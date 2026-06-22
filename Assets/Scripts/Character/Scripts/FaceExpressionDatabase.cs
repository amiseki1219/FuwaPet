using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PokoChan/Face Expression Database", fileName = "FaceExpressionDatabase")]
public class FaceExpressionDatabase : ScriptableObject
{
    public List<FaceExpressionData> expressions = new List<FaceExpressionData>();

    public FaceExpressionData GetExpression(string key)
    {
        foreach (var expr in expressions)
        {
            if (expr.expressionKey == key)
                return expr;
        }
        Debug.LogWarning($"[FaceExpressionDatabase] Expression not found: {key}");
        return null;
    }
}
