using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageSetter : MonoBehaviour
#if UNITY_EDITOR
    , IAutomaticChecker
#endif
{

#if UNITY_EDITOR
    void IAutomaticChecker.Check()
    {
        var image = this.GetComponent<Image>();
        if (image != null)
        {
            image.color = Color.blue;
        }
        else
        {
            Debug.LogError($"Cannot find image: {this.gameObject.name} in {this.gameObject.scene.name}");
        }
    }
#endif
}
