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
    public bool useCustomBlit = true;
    [SerializeField] private Material m_CustomBlitMaterial;
    [SerializeField] private RawImage m_RawImage;
    [SerializeField] private RenderTexture m_RenderTexture;
    [SerializeField] private PostProcessLayer m_PostProcessLayer;

    [SerializeField] private Camera m_TargetCamera;
    [SerializeField] private Camera m_SecondCamera;
    
    private CommandBuffer m_CommandBuffer;
    private CameraEvent m_LastInsertEvent;

    private void LateUpdate() {
        ValidateRenderTexture();
        SetUpCommandBuffer();
        if (m_LastInsertEvent != insertCommandBufferEvent) {
            m_TargetCamera.RemoveCommandBuffer(m_LastInsertEvent, m_CommandBuffer);
            m_TargetCamera.AddCommandBuffer(insertCommandBufferEvent, m_CommandBuffer);
            m_LastInsertEvent = insertCommandBufferEvent;
        }
    }

    private void OnDisable() {
        if (m_LastInsertEvent != default && m_CommandBuffer != null) {
            m_TargetCamera.RemoveCommandBuffer(m_LastInsertEvent, m_CommandBuffer);
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
        if (useCustomBlit) {
            m_CommandBuffer.Blit(sourceRenderTextureType, dest, m_CustomBlitMaterial, 0);
        } else {
            m_CommandBuffer.Blit(sourceRenderTextureType, dest);
        }
        m_RawImage.texture = dest;
    }

    void ValidateRenderTexture() {
        if (m_RenderTexture && m_RenderTexture.IsCreated()) {
            return;
        }

        var descriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
        // {
        //     depthBufferBits = 0,
        //     bindMS = false,
        //     msaaSamples = 1,
        //     graphicsFormat = GraphicsFormat.R8G8B8A8_SRGB,
        //     colorFormat = RenderTextureFormat.ARGB32,
        //     depthStencilFormat = GraphicsFormat.None,
        //     dimension = TextureDimension.Tex2D,
        //     volumeDepth = 1,
        //     shadowSamplingMode = ShadowSamplingMode.None,
        // };
        
        m_RenderTexture = new RenderTexture(descriptor);
        m_RenderTexture.name = "ScreenRT (descriptor)";
        m_RenderTexture.Create();
        
        Debug.Log($"Create RenderTexture: {m_RenderTexture.name} {m_RenderTexture.width}x{m_RenderTexture.height} {m_RenderTexture.format}");
    }

    private Vector2 m_GuiVerticalScrollPosition;
    private void OnGUI() {
        const float PaddingY = 30f;
        const float Indent = 10f;
        Rect scrollRect = new Rect(0f, 0f, 300f, Screen.height);
        GUI.Box(scrollRect, GUIContent.none);
        m_GuiVerticalScrollPosition = GUI.BeginScrollView(
            scrollRect,
            m_GuiVerticalScrollPosition,
            new Rect(0f, 0f, 200f, 2000f)
        );
        Rect rect = new Rect(20, 20f, 300f, PaddingY);
        GUI.Label(rect, $"Graphics: {SystemInfo.graphicsDeviceType}");
        rect.y += PaddingY;
        if (m_RenderTexture) {
            GUI.Label(rect, $"RT Format: {m_RenderTexture.format}");
            rect.y += PaddingY;
            GUI.Label(rect, $"graphicsFormat: {m_RenderTexture.graphicsFormat}");
            rect.y += PaddingY;
            GUI.Label(rect, $"depthStencilFormat: {m_RenderTexture.depthStencilFormat}");
            rect.y += PaddingY;

        }
        
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
        // Blit Material.
        useCustomBlit = GUI.Toggle(rect, useCustomBlit, "Custom Blit Shader");
        rect.y += PaddingY;
        
        MSAAToggle(ref rect, PaddingY, 0, "No MSAA");
        MSAAToggle(ref rect, PaddingY, 2, "MSAA 2x");
        MSAAToggle(ref rect, PaddingY, 4, "MSAA 4x");
        MSAAToggle(ref rect, PaddingY, 8, "MSAA 8x");
        
        // HDR.
        m_TargetCamera.allowHDR = GUI.Toggle(rect, m_TargetCamera.allowHDR, "Allow HDR");
        rect.y += PaddingY;
        
        // Post Process.
        m_PostProcessLayer.enabled = GUI.Toggle(rect, m_PostProcessLayer.enabled, "Post Process");
        rect.y += PaddingY;
        
        // forceIntoRenderTexture.
        m_TargetCamera.forceIntoRenderTexture = GUI.Toggle(rect, m_TargetCamera.forceIntoRenderTexture, "forceIntoRenderTexture");
        rect.y += PaddingY;
        
        // Second Camera.
        SecondCameraOptions(ref rect, PaddingY, Indent);
        
        GUI.EndScrollView();
    }

    private void SecondCameraOptions(ref Rect rect, float paddingY, float indent) {
        GUI.Label(rect, "Second Camera:");
        rect.y += paddingY;
        rect.x += indent;
        
        m_SecondCamera.enabled = GUI.Toggle(rect, m_SecondCamera.enabled, "enabled");
        rect.y += paddingY;
        
        // Second Camera's viewport rect.
        Rect FullscreenViewport = new Rect(0f, 0f, 1f, 1f);
        Rect NonFullscreenViewport = new Rect(0f, 0f, 0.5f, 0.5f);
        bool isFullscreenViewport = m_SecondCamera.rect == FullscreenViewport;
        isFullscreenViewport = GUI.Toggle(rect, isFullscreenViewport, "viewport: Fullscreen");
        if (isFullscreenViewport && m_SecondCamera.rect != FullscreenViewport) {
            m_SecondCamera.rect = FullscreenViewport;
        } else if (!isFullscreenViewport && m_SecondCamera.rect != NonFullscreenViewport) {
            m_SecondCamera.rect = NonFullscreenViewport;
        }
        rect.y += paddingY;
        
        // Second Camera's depth.
        const float PriorDepth = -10f;
        const float NotPriorDepth = 10f;
        bool secondCameraPrior = m_SecondCamera.depth == PriorDepth;
        secondCameraPrior = GUI.Toggle(rect, secondCameraPrior, "Prior to MainCamera");
        if (secondCameraPrior && m_SecondCamera.depth != PriorDepth) {
            m_SecondCamera.depth = PriorDepth;
        } else if (!secondCameraPrior && m_SecondCamera.depth != NotPriorDepth) {
            m_SecondCamera.depth = NotPriorDepth;
        }

        rect.x -= indent;
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
