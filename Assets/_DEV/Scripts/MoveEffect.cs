using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class MoveEffect : MonoBehaviour
{
    public float moveDistance = 50f; 
    public float animationDuration = 1f; 

    void Start()
    {
        GetComponent<RectTransform>().DOAnchorPosY(GetComponent<RectTransform>().anchoredPosition.y + moveDistance, animationDuration)
            .SetEase(Ease.InOutSine) 
            .SetLoops(-1, LoopType.Yoyo); 
    }
}
