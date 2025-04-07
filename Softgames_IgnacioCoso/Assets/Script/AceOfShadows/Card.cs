using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Card : MonoBehaviour
{
    [SerializeField] protected FlippableCard m_FlippableCard;

    [SerializeField] protected Image m_CardIcon;

    
    public void AnimateFrontFlip() => m_FlippableCard.FrontViewFlip();
    public void AnimateBackFlip() => m_FlippableCard.BackViewFlip();
    public void ImmediateFrontFlip() => m_FlippableCard.SetFrontViewImmediate();
    public void ImmediateBackFlip() => m_FlippableCard.SetBackViewImmediate();
    

    [ContextMenu("Randomize Card Content")]
    public void RandomizeCardContent()
    {
        RandomizeCardColor();
    }
    
    protected void RandomizeCardColor()
    {
        m_CardIcon.color = Random.ColorHSV(
            0f, 1f, // Hue range (0-1 covers all colors)
            0.5f, 1f, // Saturation (avoid grays)
            0.8f, 1f // Value (avoid dark colors)
        );
    }

    
}
