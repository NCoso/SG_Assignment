using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class AceOfShadows : MonoBehaviour
{
    private const float s_DealDelay = 1f;
    private const float s_MoveTime = 2f;
    
    [SerializeField]
    private CardStack m_OrignStack;
    [SerializeField]
    private CardStack[] m_TargetStacks;
    private int m_dealtCards = 0;

    private CardStack CurrentTargetStack => m_TargetStacks[m_dealtCards % m_TargetStacks.Length];

    protected void Start()
    {
        InitializeAndDealAllCards();
    }


    [ContextMenu("Initialize And Deal All Cards")]
    public void InitializeAndDealAllCards()
    {
        InitializeStacks();
        DOVirtual.DelayedCall(1f, DealAllCardsWithCustomDelay);
    }
    
    [ContextMenu("Initialize Stacks")]
    public void InitializeStacks()
    {
        m_OrignStack.FillStack();
        foreach (var stack in m_TargetStacks)
        {
            stack.ClearStack();
        }

        m_dealtCards = 0;
    }

    [ContextMenu("Deal top card")]
    public void DealTopCard()
    {
        DealTopCard(_onCompleteAction: null, _onStartAction: null);
    }

    [ContextMenu("Deal all cards")]
    public void DealAllCards()
    {
        DealTopCard(_onCompleteAction: DealAllCards, _onStartAction: null);
    }

    [ContextMenu("Deal all cards with custom delay")]
    public void DealAllCardsWithCustomDelay()
    {
        DealTopCard(_onCompleteAction: null, _onStartAction: () => 
            DOVirtual.DelayedCall(s_DealDelay, DealAllCardsWithCustomDelay)
            );
    }

    public void DealTopCard(Action _onCompleteAction, Action _onStartAction)
    {
        //Check if there are more cards to deal
        if (m_OrignStack == null || CurrentTargetStack == null || m_OrignStack.CardCount <= 0)
            return;

        bool flipFront = m_OrignStack.IsFaceDownStack == true  && CurrentTargetStack.IsFaceDownStack == false;
        bool flipBack  = m_OrignStack.IsFaceDownStack == false && CurrentTargetStack.IsFaceDownStack == true;
        Vector3 targetPosition = CurrentTargetStack.CurrentCardPosition;
        
        Card topCard = m_OrignStack.Pop();
        CurrentTargetStack.Push(topCard);
        m_dealtCards += 1;

        Sequence sequence = DOTween.Sequence();
        sequence.Join(topCard.transform.DOMoveX(targetPosition.x, s_MoveTime)
            .SetEase(Ease.Linear)
            .OnStart(() => _onStartAction?.Invoke()));
        sequence.Join(topCard.transform.DOMoveY(targetPosition.y, s_MoveTime)
            .SetEase(Ease.OutBack));

        if (flipFront)
            sequence.Append(DOVirtual.DelayedCall(0.1f, topCard.AnimateFrontFlip));
        else if (flipBack)
            sequence.Append(DOVirtual.DelayedCall(0.1f, topCard.AnimateBackFlip));

        sequence.OnComplete(() => _onCompleteAction?.Invoke());
    }
    
    
    
    

}
