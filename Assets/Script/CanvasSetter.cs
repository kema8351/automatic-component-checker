using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasSetter : MonoBehaviour
#if UNITY_EDITOR
    , IAutomaticChecker
#endif
{

#if UNITY_EDITOR
    void IAutomaticChecker.Check()
    {
        var canvasScaler = this.GetComponent<CanvasScaler>();
        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1024f, 768f);
        }
        else
        {
            Debug.LogError($"Cannot find canvas scaler: {this.gameObject.name} in {this.gameObject.scene.name}");
        }
    }
#endif
}
