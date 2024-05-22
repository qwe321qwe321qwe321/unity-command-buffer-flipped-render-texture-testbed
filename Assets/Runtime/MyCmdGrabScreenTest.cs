 using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class MyCmdGrabScreenTest : MonoBehaviour
{
    public CameraEvent insertCommandBufferEvent = CameraEvent.AfterImageEffects;
    public BuiltinRenderTextureType sourceRenderTextureType = BuiltinRenderTextureType.CurrentActive;
    [SerializeField] private Material m_BlitMaterial;
    [SerializeField] private RawImage m_RawImage;
    [SerializeField] private RenderTexture m_RenderTexture;
    [SerializeField] private PostProcessLayer m_PostProcessLayer;

    [SerializeField] private Camera m_Camera;
    
    private CommandBuffer m_CommandBuffer;
    private CameraEvent m_LastInsertEvent;

    private void LateUpdate() {
        ValidateRenderTexture();
        SetUpCommandBuffer();
        if (m_LastInsertEvent != insertCommandBufferEvent) {
            m_Camera.RemoveCommandBuffer(m_LastInsertEvent, m_CommandBuffer);
            m_Camera.AddCommandBuffer(insertCommandBufferEvent, m_CommandBuffer);
            m_LastInsertEvent = insertCommandBufferEvent;
        }
    }

    private void OnDisable() {
        if (m_LastInsertEvent != default && m_CommandBuffer != null) {
            m_Camera.RemoveCommandBuffer(m_LastInsertEvent, m_CommandBuffer);
        }
    }

    private void OnDestroy() {
        m_RenderTexture.Release();
    }

    void SetUpCommandBuffer() {
        if (m_CommandBuffer == null) {
            m_CommandBuffer = new CommandBuffer();
            m_CommandBuffer.name = "Grab Screen";
        }
        m_CommandBuffer.Clear();
        var dest = m_RenderTexture;
        if (m_BlitMaterial) {
            m_CommandBuffer.Blit(sourceRenderTextureType, dest, m_BlitMaterial, 0);
        } else {
            m_CommandBuffer.Blit(sourceRenderTextureType, dest);
        }
        m_RawImage.texture = dest;
    }

    void ValidateRenderTexture() {
        if (m_RenderTexture && m_RenderTexture.IsCreated()) {
            return;
        }
        
        var descriptor = new RenderTextureDescriptor(Screen.width, Screen.height, GraphicsFormat.R8G8B8A8_SRGB,
            GraphicsFormat.None, 0) {
            depthBufferBits = 0,
            bindMS = false,
            msaaSamples = 1,
            graphicsFormat = GraphicsFormat.R8G8B8A8_SRGB,
            colorFormat = RenderTextureFormat.ARGB32,
            depthStencilFormat = GraphicsFormat.None,
            dimension = TextureDimension.Tex2D,
            volumeDepth = 1,
            shadowSamplingMode = ShadowSamplingMode.None,
        };
        
        m_RenderTexture = new RenderTexture(descriptor);
        m_RenderTexture.name = "ScreenRT (descriptor)";
        m_RenderTexture.Create();
        
        Debug.Log($"Create RenderTexture: {m_RenderTexture.name} {m_RenderTexture.width}x{m_RenderTexture.height} {m_RenderTexture.format}");
    }

    private Vector2 m_GuiVerticalScrollPosition;
    private void OnGUI() {
        const float PaddingY = 30f;
        Rect scrollRect = new Rect(0f, 0f, 300f, Screen.height);
        GUI.Box(scrollRect, GUIContent.none);
        m_GuiVerticalScrollPosition = GUI.BeginScrollView(
            scrollRect,
            m_GuiVerticalScrollPosition,
            new Rect(0f, 0f, 200f, 2000f)
        );
        Rect rect = new Rect(20, 20f, 300f, PaddingY);
        EnumToggleGroup(ref rect, PaddingY, ref insertCommandBufferEvent,
            CameraEvent.BeforeForwardOpaque,
            CameraEvent.AfterForwardOpaque,
            CameraEvent.BeforeForwardAlpha,
            CameraEvent.AfterForwardAlpha,
            CameraEvent.BeforeImageEffects,
            CameraEvent.AfterImageEffects,
            CameraEvent.AfterEverything
        );
        EnumToggleGroup(ref rect, PaddingY, ref sourceRenderTextureType,
            BuiltinRenderTextureType.CameraTarget,
            BuiltinRenderTextureType.CurrentActive
        );
        MSAAToggle(ref rect, PaddingY, 0, "No MSAA");
        MSAAToggle(ref rect, PaddingY, 2, "MSAA 2x");
        MSAAToggle(ref rect, PaddingY, 4, "MSAA 4x");
        MSAAToggle(ref rect, PaddingY, 8, "MSAA 8x");
        // HDR.
        m_Camera.allowHDR = GUI.Toggle(rect, m_Camera.allowHDR, "Allow HDR");
        rect.y += PaddingY;
        
        // Post Process.
        m_PostProcessLayer.enabled = GUI.Toggle(rect, m_PostProcessLayer.enabled, "Post Process");
        rect.y += PaddingY;
        
        GUI.EndScrollView();
    }

    private static void EnumToggleGroup<T>(ref Rect rect, float padding, ref T variable, params T[] options) where T : System.Enum {
        for (int i = 0; i < options.Length; i++) {
            bool insertImageEffect = variable.Equals(options[i]);
            if (GUI.Toggle(rect, insertImageEffect, options[i].ToString())) {
                variable = options[i];
            }
            rect.y += padding;
        }
    }

    private static void MSAAToggle(ref Rect rect, float paddingY, int value, string label) {
        bool msaa = QualitySettings.antiAliasing == value;
        if (GUI.Toggle(rect, msaa, label)) {
            QualitySettings.antiAliasing = value;
        }
        rect.y += paddingY;
    }
}
