using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PhoenixFlame : MonoBehaviour
{
    [Header("Particles")]
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private ParticleSystem embersParticles;
    [SerializeField] private ParticleSystem smokeParticles;

    [Header("UI")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private Animator fireAnimator;
    [SerializeField] private string toggleParam = "IsBurning";

    private bool isBurning = true;

    private void Start()
    {
        // Initialize all particles
        fireParticles.Play();
        embersParticles.Play();
        smokeParticles.Play();

        // Button setup
        toggleButton.onClick.AddListener(ToggleFire);
        UpdateButtonText();
    }

    public void ToggleFire()
    {
        isBurning = !isBurning;
        
        // Animate transition
        fireAnimator.SetBool(toggleParam, isBurning);
        
        
        // // Smooth particle transitions
        // DOTween.To(() => fireParticles.emission.rateOverTime.constant, 
        //     x => fireParticles.emission.rateOverTime = x, 
        //     isBurning ? 50f : 0f, 1f);
        
        UpdateButtonText();
    }

    private void UpdateButtonText()
    {
        toggleButton.GetComponentInChildren<TMP_Text>().text = 
            isBurning ? "EXTINGUISH" : "IGNITE";
    }
}