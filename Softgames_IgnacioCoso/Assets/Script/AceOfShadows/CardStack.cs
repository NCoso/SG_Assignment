using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardStack : MonoBehaviour
{
    private const int s_StackSize = 144;
    
    [SerializeField] private Card m_CardPrefab;
    [SerializeField] private RectTransform m_CardContainer;
    [SerializeField] private bool m_IsFaceDownStack;
    [SerializeField] private int m_MaxOffsetedCards = 5;
    
    
    protected Stack<Card> m_currentCards = new Stack<Card>();

    public bool IsFaceDownStack => m_IsFaceDownStack;
    public Vector3 CurrentCardOffset => CardTotalOffsetByCardIndex(CardCount - 1);
    public Vector3 CurrentCardPosition => transform.TransformVector(CurrentCardOffset) + transform.position;
    public int CardCount => m_currentCards.Count;
    
    public Vector3 OffsetBetweenCards
    {
        get
        {
            // set stacked cards offset as 1/75 of card height
            if (m_OffsetBetweenCards == Vector3.zero)
                m_OffsetBetweenCards = Vector3.down * (transform as RectTransform).rect.height / 75; 

            return m_OffsetBetweenCards;
        }
    }
    protected Vector3 CardTotalOffsetByCardIndex(int index) => (Math.Min(index, m_MaxOffsetedCards)) * OffsetBetweenCards;
        



    private Vector3 m_OffsetBetweenCards = Vector3.zero;


    public Card TopCard() => CardCount == 0 ? null : m_currentCards.Peek();
    public Card Pop() => CardCount == 0 ? null : m_currentCards.Pop();
    public void Push(Card _card)
    {
        _card.transform.SetParent(m_CardContainer, worldPositionStays: true);
        m_currentCards.Push(_card);
    }

    
    
    [ContextMenu("Clear Stack")]
    public void ClearStack()
    {
        GameObject currentChild = null;
        for (int i = 0; i < m_CardContainer.childCount; i++)
        {
            currentChild = m_CardContainer.GetChild(i).gameObject;
            currentChild.SetActive(false);
            Destroy(currentChild);
        }
        m_currentCards.Clear();
    }
    
    [ContextMenu("Fill Stack")]
    public void FillStack()
    {
        ClearStack();
        for (int i = 0; i < s_StackSize; i++)
        {
            InstantiateRandomCard();
        }
        UpdateAllCardsPositionOffset();
    }

    private Card InstantiateRandomCard()
    {
        Card newCard = Instantiate(m_CardPrefab, m_CardContainer.position, Quaternion.identity, m_CardContainer);
        newCard.RandomizeCardContent();
            
        // adapt the card-orientation to match the stack-orientation
        if (m_IsFaceDownStack)
            newCard.ImmediateBackFlip();
        else
            newCard.ImmediateFrontFlip();
        
        // make the card-size match the stack-size
        RectTransform newCardTransform = newCard.transform as RectTransform;
        newCardTransform.anchorMin = Vector2.zero;     
        newCardTransform.anchorMax = Vector2.one;       
        newCardTransform.offsetMin = Vector2.zero;      
        newCardTransform.offsetMax = Vector2.zero;      
            
        m_currentCards.Push(newCard);
        
        return newCard;
    }
    
    public void UpdateAllCardsPositionOffset()
    {
        Transform currentChild = null;
        int enabledCount = 0;
        for (int i = 0; i < m_CardContainer.childCount; i++)
        {
            currentChild = m_CardContainer.GetChild(i).transform;
            
            // Avoid considering disabled cards (disabled due delayed destroy)
            if (currentChild.gameObject.activeSelf)
            {
                enabledCount += 1;
            }
            
            currentChild.localPosition = CardTotalOffsetByCardIndex(enabledCount); 
        }
    }
    
}
