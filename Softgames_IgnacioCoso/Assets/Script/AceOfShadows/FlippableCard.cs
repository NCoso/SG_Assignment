using System;
using DG.Tweening;
using UnityEngine;

public class FlippableCard : MonoBehaviour
{
    [SerializeField] protected RectTransform m_CardFront, m_CardBack;

    protected Tween m_FlipTween;
    
    
    protected static Vector3 s_FontViewRotation => Vector3.zero;
    protected static Vector3 s_BackViewRotation => Vector3.up * 180;
    protected static Vector3 ViewToRotation(bool _isFrontView) => _isFrontView ? s_FontViewRotation : s_BackViewRotation;
   
    
    protected bool IsFrontVisible => transform.eulerAngles.y < 90 || transform.eulerAngles.y > 270;
    
    
    
    [ContextMenu("Immediate front view")]
    public void SetFrontViewImmediate()
    {
        SetViewImmediate(_isFrontView: true);
    }
    
    [ContextMenu("Immediate back view")]
    public void SetBackViewImmediate()
    {
        SetViewImmediate(_isFrontView: false);
    }
    
    [ContextMenu("Front view flip")]
    public void FrontViewFlip()
    {
        FlipCard(_frontView: true);
    }
    
    [ContextMenu("Back view flip")]
    public void BackViewFlip()
    {
        FlipCard(_frontView: false);
    }

    public void SetViewImmediate(bool _isFrontView)
    {
        transform.eulerAngles = ViewToRotation(_isFrontView);
        UpdateFrontAndBackVisibility();
    }

    protected void UpdateFrontAndBackVisibility()
    {
        bool isFrontVisible = IsFrontVisible;
        m_CardFront.gameObject.SetActive(isFrontVisible == true);
        m_CardBack .gameObject.SetActive(isFrontVisible == false);
        
        //Debug.Log($"UpdateFrontAndBackVisibility - isFrontVisible: {isFrontVisible} ({transform.eulerAngles.y})");
    }

    protected void FlipCard(bool _frontView, float _duration = 1f)
    {
        CleanTween();
            
        m_FlipTween = transform.DOLocalRotate(ViewToRotation(_frontView), _duration)
            .OnUpdate(UpdateFrontAndBackVisibility)
            .SetEase(Ease.InOutCirc);
    }

    private void CleanTween()
    {
        if (m_FlipTween != null && m_FlipTween.IsActive())
            m_FlipTween.Kill();
    }

    private void OnDestroy()
    {
        CleanTween();
    }
}
