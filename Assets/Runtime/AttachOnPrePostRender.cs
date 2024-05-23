using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachOnPrePostRender : MonoBehaviour {
    public bool useRenderTexture = false;
    public RenderTextureDescriptor renderTextureDescriptor;
    [SerializeField] private RenderTexture m_RenderTexture;
    private Camera m_Camera;
    public void OnPreRender() {
        if (useRenderTexture) {
            m_RenderTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.Default);
            if (!m_Camera) {
                m_Camera = GetComponent<Camera>();
            }
            m_Camera.targetTexture = m_RenderTexture;
        }
    }

    public void OnPostRender() {
        if (useRenderTexture) {
            if (m_RenderTexture) {
                Graphics.Blit(m_RenderTexture, null as RenderTexture);
            }
            if (m_Camera) {
                m_Camera.targetTexture = null;
            }
        }
        if (m_RenderTexture) {
            RenderTexture.ReleaseTemporary(m_RenderTexture);
            m_RenderTexture = null;
        }
    }
}
