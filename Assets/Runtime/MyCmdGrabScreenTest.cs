 using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class MyCmdGrabScreenTest : MonoBehaviour
{
    public CameraEvent insertCommandBufferEvent = CameraEvent.AfterImageEffects;
    public BuiltinRenderTextureType sourceRenderTextureType = BuiltinRenderTextureType.CurrentActive;
    public bool useCustomBlit = true;
    public bool insertBuiltinBlitForCustomBlit = false;
    public bool blitShaderUvStartsAtTopKeyword = false;
    public bool blitShaderTexelSizeY = false;
    public bool blitShaderProjectionParamX = false;
    public bool blitFullTriangle = false;
    
    [SerializeField] private Material m_CustomBlitMaterial;
    [SerializeField] private Material m_CustomBlitTriangleMaterial;
    [SerializeField] private RawImage m_RawImage;
    [SerializeField] private RenderTexture m_RenderTexture;
    [SerializeField] private PostProcessLayer m_PostProcessLayer;
    [SerializeField] private PostProcessVolume m_PostProcessVolume;

    [SerializeField] private Camera m_TargetCamera;
    [SerializeField] private Camera m_SecondCamera;
    
    private AttachOnRenderImage m_AttachOnRenderImage;
    private AttachOnPrePostRender m_AttachOnPrePostRender;
    
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

    private void OnEnable() {
        if (m_TargetCamera) {
            if (!m_AttachOnRenderImage) {
                m_AttachOnRenderImage = m_TargetCamera.gameObject.AddComponent<AttachOnRenderImage>();
                m_AttachOnRenderImage.enabled = false;
            }
            if (!m_AttachOnPrePostRender) {
                m_AttachOnPrePostRender = m_TargetCamera.gameObject.AddComponent<AttachOnPrePostRender>();
                m_AttachOnPrePostRender.enabled = false;
            }
        }
        
    }

    private void OnDisable() {
        if (m_LastInsertEvent != default && m_CommandBuffer != null) {
            m_TargetCamera.RemoveCommandBuffer(m_LastInsertEvent, m_CommandBuffer);
        }

        if (m_AttachOnRenderImage) {
            m_AttachOnRenderImage.enabled = false;
        }

        if (m_AttachOnPrePostRender) {
            m_AttachOnPrePostRender.enabled = false;
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
        if (blitFullTriangle) {
            if (useCustomBlit) {
                m_CommandBuffer.BlitFullscreenTriangle(sourceRenderTextureType, dest, m_CustomBlitTriangleMaterial, 0);
            } else {
                m_CommandBuffer.BlitFullscreenTriangle(sourceRenderTextureType, dest);
            }
        } else {
            if (useCustomBlit) {
                if (insertBuiltinBlitForCustomBlit) {
                    m_CommandBuffer.GetTemporaryRT(tempScreenCopyID, GetRtDescriptor());
                    m_CommandBuffer.Blit(sourceRenderTextureType, tempScreenCopyID);
                }
                
                if (blitShaderUvStartsAtTopKeyword) {
                    m_CustomBlitMaterial.EnableKeyword("COND_UNITY_UV_STARTS_AT_TOP");
                } else {
                    m_CustomBlitMaterial.DisableKeyword("COND_UNITY_UV_STARTS_AT_TOP");
                }
                if (blitShaderTexelSizeY) {
                    m_CustomBlitMaterial.EnableKeyword("COND_TEXEL_SIZE_Y");
                } else {
                    m_CustomBlitMaterial.DisableKeyword("COND_TEXEL_SIZE_Y");
                }
                if (blitShaderProjectionParamX) {
                    m_CustomBlitMaterial.EnableKeyword("COND_PROJECTION_PARAM_X");
                } else {
                    m_CustomBlitMaterial.DisableKeyword("COND_PROJECTION_PARAM_X");
                }
                if (insertBuiltinBlitForCustomBlit) {
                    m_CommandBuffer.Blit(tempScreenCopyID, dest, m_CustomBlitMaterial, 0);
                    m_CommandBuffer.ReleaseTemporaryRT(tempScreenCopyID);
                } else {
                    m_CommandBuffer.Blit(sourceRenderTextureType, dest, m_CustomBlitMaterial, 0);
                }
            } else {
                m_CommandBuffer.Blit(sourceRenderTextureType, dest);
            }
        }
       
        m_RawImage.texture = dest;
    }

    void ValidateRenderTexture() {
        if (m_RenderTexture && m_RenderTexture.IsCreated() &&
            m_RenderTexture.width == Screen.width && m_RenderTexture.height == Screen.height) {
            return;
        }

        if (m_RenderTexture) {
            m_RenderTexture.Release();
        }
        
        m_RenderTexture = new RenderTexture(GetRtDescriptor());
        m_RenderTexture = new RenderTexture(Screen.width, Screen.height, 0);
        m_RenderTexture.name = "ScreenRT";
        m_RenderTexture.Create();
        
        Debug.Log($"Create RenderTexture: {m_RenderTexture.name} {m_RenderTexture.width}x{m_RenderTexture.height} {m_RenderTexture.format}");
    }

    private RenderTextureDescriptor GetRtDescriptor() {
        var descriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0, 0, RenderTextureReadWrite.Default);
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
        return descriptor;
    }
    
    private static readonly int tempScreenCopyID = Shader.PropertyToID("_TempScreenCopy");

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
        GUI.Label(rect, $"OS: {SystemInfo.operatingSystem}");
        rect.y += PaddingY;
        GUI.Label(rect, $"Graphics: {SystemInfo.graphicsDeviceType}");
        rect.y += PaddingY;
        GUI.Label(rect, $"Color Space: {QualitySettings.activeColorSpace}");
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
            CameraEvent.AfterSkybox,
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

        if (useCustomBlit && !blitFullTriangle) {
            insertBuiltinBlitForCustomBlit = GUI.Toggle(rect, insertBuiltinBlitForCustomBlit, "Insert Builtin Blit");
            rect.y += PaddingY;
            blitShaderUvStartsAtTopKeyword = GUI.Toggle(rect, blitShaderUvStartsAtTopKeyword, "Flip if UNITY_UV_STARTS_AT_TOP");
            rect.y += PaddingY;
            
            rect.x += Indent;
            blitShaderTexelSizeY = GUI.Toggle(rect, blitShaderTexelSizeY, "AND _MainTex_TexelSize.y < 0");
            rect.y += PaddingY;
            rect.x -= Indent;
            
            blitShaderProjectionParamX = GUI.Toggle(rect, blitShaderProjectionParamX, "Flip if _ProjectionParams.x < 0");
            rect.y += PaddingY;
        }
        
        // Blit Full Triangle.
        blitFullTriangle = GUI.Toggle(rect, blitFullTriangle, "Blit Full Triangle");
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

        // AttachOnRenderImage.
        m_AttachOnRenderImage.enabled = GUI.Toggle(rect, m_AttachOnRenderImage.enabled, "Use OnRenderImage");
        rect.y += PaddingY;
        
        // AttachOnPrePostRender.
        m_AttachOnPrePostRender.enabled = GUI.Toggle(rect, m_AttachOnPrePostRender.enabled, "Use OnPreRender & OnPostRender");
        rect.y += PaddingY;
        if (m_AttachOnPrePostRender.enabled) {
            rect.x += Indent;
            m_AttachOnPrePostRender.useRenderTexture = GUI.Toggle(rect, m_AttachOnPrePostRender.useRenderTexture, "Use Target Render Texture");
            if (m_AttachOnPrePostRender.useRenderTexture) {
                m_AttachOnPrePostRender.renderTextureDescriptor = GetRtDescriptor();
            }
            rect.y += PaddingY;
            rect.x -= Indent;
        }
        
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

public static class CommandBufferExtensions {
    /// <summary>
    /// Does a copy of source to destination using a fullscreen triangle.
    /// </summary>
    /// <param name="cmd">The command buffer to use</param>
    /// <param name="source">The source render target</param>
    /// <param name="destination">The destination render target</param>
    /// <param name="clear">Should the destination target be cleared?</param>
    /// <param name="viewport">An optional viewport to consider for the blit</param>
    /// <param name="preserveDepth">Should the depth buffer be preserved?</param>
    public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material blitMaterial, int pass, bool clear = false, Rect? viewport = null, bool preserveDepth = false)
    {
        cmd.SetGlobalTexture(ShaderIDs_MainTex, source);
        var colorLoad = viewport == null ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load;
        cmd.SetRenderTargetWithLoadStoreAction(destination, colorLoad, RenderBufferStoreAction.Store, preserveDepth ? RenderBufferLoadAction.Load : colorLoad, RenderBufferStoreAction.Store);

        if (viewport != null)
            cmd.SetViewport(viewport.Value);

        if (clear)
            cmd.ClearRenderTarget(true, true, Color.clear);

        cmd.DrawMesh(RuntimeUtilities.fullscreenTriangle, Matrix4x4.identity, blitMaterial, 0, pass);
    }
    
    internal static readonly int ShaderIDs_MainTex = Shader.PropertyToID("_MainTex");
}
