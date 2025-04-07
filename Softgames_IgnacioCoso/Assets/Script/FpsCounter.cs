using System;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    private const float s_UpdateInterval = 0.5f;
    private float m_accum = 0;
    private int m_frames = 0;
    private float m_timeLeft;
    private float m_fps = 0;
    private int m_targetFrameRate;
    
    [SerializeField] private TMP_Text text;
    
    public void Awake()
    {
        m_targetFrameRate = Screen.currentResolution.refreshRate;
        Application.targetFrameRate = m_targetFrameRate;
    }


    private void Update()
    {
        m_timeLeft -= Time.deltaTime;
        m_accum += Time.timeScale / Time.deltaTime;
        m_frames++;

        if (m_timeLeft <= 0f)
        {
            m_fps = m_accum / m_frames;
            UpdateDisplay();
            m_timeLeft = s_UpdateInterval;
            m_accum = 0f;
            m_frames = 0;
        }
    }

    private void UpdateDisplay()
    {
        if (text != null)
        {
            // Color coding based on performance
            Color color;
            if (m_fps < m_targetFrameRate *0.5f) color = Color.red;
            else if (m_fps < m_targetFrameRate*0.85) color = Color.yellow;
            else color = Color.green;
            
            text.text = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{m_fps:0.} FPS</color>";
            text.text += $"\nTarget: {Application.targetFrameRate}";
        }
    }
    
}
