using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachOnRenderImage : MonoBehaviour
{
    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(source, destination);
    }
}
