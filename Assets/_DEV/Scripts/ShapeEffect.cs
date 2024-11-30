using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ShapeEffect : MonoBehaviour
{
    public float scaleMultiplier = 1.2f; 
    public float animationDuration = 0.5f; 

    void Start()
    {
        GetComponent<RectTransform>().DOScale(Vector3.one * scaleMultiplier, animationDuration)
            .SetEase(Ease.InOutSine) 
            .SetLoops(-1, LoopType.Yoyo); 
    }
}
