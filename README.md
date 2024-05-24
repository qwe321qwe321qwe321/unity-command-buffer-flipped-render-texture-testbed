# unity-command-buffer-flipped-render-texture-testbed

![image](https://github.com/qwe321qwe321qwe321/unity-command-buffer-flipped-render-texture-testbed/assets/23000374/c44506a2-34a8-4b9d-9f5d-d2e4658d99aa)

# Discussion
* https://forum.unity.com/threads/commandbuffer-rendering-scene-flipped-upside-down-in-forward-rendering.415922/
* https://forum.unity.com/threads/unity-flipping-render-textures.1030057/
* https://forum.unity.com/threads/command-buffer-blit-render-texture-result-is-upside-down.1463063/
* https://forum.unity.com/threads/screen-upside-down-with-commandbuffer-blit.683143/
* https://forum.unity.com/threads/command-buffer-blit-flipping-render-textures-in-scene-view-or-game-view.1185322/
* [Unity Doc: Writing shaders for different graphics APIs](https://docs.unity3d.com/2019.4/Documentation/Manual/SL-PlatformDifferences.html)

# Shorthand
Q: 如何正確使用 Command Buffer 抓螢幕畫面（ post processing 結束後）而不要有上下顛倒的結果?

A: 目前結論如下

主要還是第一篇的這句
> So it seems that when OnRenderImage is being used, or when HDR rendering is on, or when anything that needs the camera to render to an intermediate buffer is enabled, the camera automatically renders to an intermediate buffer instead of the backbuffer (screen) directly. This is nice.

重點在於 `BuiltinRenderTextureType.CurrentActive` 或 `BuiltinRenderTextureType.CameraTarget` 為 Back Buffer (Screen) 的時候就會發生一連串問題。

而 Command Buffer 可插入的鏡頭事件 `CameraEvent.AfterImageEffect` 和 `CameraEvent.AfterEverything` 這兩個時機點的 Render Target 幾乎一定是 Back Buffer （目前測出的唯一例外是多鏡頭的情況，後面補充）。

Blit 使用 Back Buffer 當作 source 會有以下問題：
1. 可能上下顛倒（OpenGL 不會顛倒）
2. 自己寫的 Blit Shader 無法正確運行（無法 assign 進 `_MainTex`，但 Unity Editor 內的 Game View RT 可以被 access 所以在 Editor 內能夠正確運行）
 * 僅有內建的無 material `Blit()` 才能抓到 Back Buffer 資料（看 Frame Debugger 感覺這部分指令跟 GrabPass 一樣），但可能會上下顛倒。

目前結論是：**一切萬惡之源都是試圖把 Back Buffer 當成 Blit 的 source**。

然後這串前面提的一大堆「影響因素」全都是在影響「Blit 當下的 source 到底是否為 Back Buffer」這件事情。

列一下 `CameraEvent` 的執行順序和相關內部事件的順序

https://docs.unity3d.com/Manual/GraphicsCommandBuffers.html

只看幾個重要的，括號是我測試結果，不在文檔上
* BeforeForwardAlpha
* Unity renders transparent geometry, and UI Canvases with a Rendering Mode of Screen Space - Camera.
* AfterForwardAlpha
* (若後續沒有額外 RT 需求且 camera.targetRenderTexture == null 的話直接繪製到 back buffer 上)
* (`OnPostRender` 會在這裡觸發)
* BeforeImageEffects
* Unity applies post-processing effects. (PPv2 在這處理)
* (若前面沒做且後續也沒有其他 RT 需求的話，現在繪製到 back buffer 上)
* AfterImageEffects
* AfterEverything
* Unity renders UI Canvases with a Rendering Mode that is not Screen Space - Camera. (這段是所有 camera 繪製完畢後開始畫 UI Overlay)

再來解釋「額外 RT 需求」的達成條件，以下任意條件成立都算：
1. Post Processing Stack
2. 有掛 `OnRenderImage()`
3. 開啟 HDR
4. 開啟 MSAA
5. Camera.forceIntoRenderTexture == true
6. 是否有別的全螢幕（viewport 0, 0, 1, 1）的 Camera 晚於這個 Camera 後才繪製

其他還有甚麼條件我不知道也不覺得有辦法全部抓到

具體檢查方法可以從 Frame Debugger 的 RenderTarget 看是不是 `<No name>` 就知道他是不是畫在 back buffer 還是額外的 RT 上。

總之以上任一條件成立的話就不怕在 `AfterForwardAlpha` 的時候就已經繪製到 Back Buffer 了，可以避免上面那堆問題。

其他有測試過不會造成影響的：
* `OnPreRender()`
* `OnPostRender()`

  ---
回到本串開頭的問題： 所以如何在判斷要不要翻轉？

目前結論：如果在 source 是 back buffer + 非 openGL = 上下顛倒。

問題是沒有一個完美解可以提前知道 source 會不會是 back buffer（除了把上面條件全部列出來做判斷，但還是無法保證有沒有漏）。

不過雖然沒辦法判斷 Source 是不是 Back Buffer，但有辦法讓 Source 一定不是 Back Buffer：
1. 死都不要用 `CameraEvent.AfterImageEffect` 和 `CameraEvent.AfterEverything`
2. 強制對目標 Camera 開啟 `forceIntoRenderTexture = true`

回到問題原點是我希望抓取 Post Processing 後的圖，而現在要符合以上條件的能用的最晚時間點也是 `CameraEvent.BeforeImageEffect`，但這又跟 Post Processing 有衝突。

所以要解的話，目前只想到
1. Integrate into Post Processing Stack，寫成 PostProcessingEffect 插入進去流程尾巴。
2. 想辦法讓 Post processing 最後輸出至某 RT 上然後去讀它。
3. 某種 hack 讓自己的 command buffer 永遠位於 `CameraEvent.BeforeImageEffect` queue 的最後。


至於 `UNITY_UV_STARTS_AT_TOP`、`_MainTex_TexelSize.y`、`_ProjectionParams.x < 0` 經測試在這個問題上毫無用處。
1. `UNITY_UV_STARTS_AT_TOP` 在非 OpenGL 以外 Graphics API 都為 true。
 * N 主機和 P 主機上也為 true
2. `_MainTex_TexelSize.y < 0` 各平台、各 Graphics API 都為 false。（所以我還是搞不懂這是幹嘛的
3. `_ProjectionParams.x < 0` 在非 OpenGL 以外 Graphics API 都為 true。
 * N 主機和 P 主機上也為 true

毫無用處的理由：這幾個條件跟 source 是不是 Back Buffer 這個問題的根本原因一點關聯都沒有，它們就是個環境變數。

假設你現在的 setup 是 D3D11 + Blit source 是 Back Buffer，那你本來會得到上下顛倒的結果，而你照著官方文檔隨便加了
```
#if UNITY_UV_STARTS_AT_TOP
if (_MainTex_TexelSize.y < 0)
        uv.y = 1-uv.y;
#endif
```
你會發現完全沒用，因為 `_MainTex_TexelSize.y < 0` == false。

然後你再去試 `_ProjectionParams.x < 0` 或是單純只用 `UNITY_UV_STARTS_AT_TOP` 會發現欸！修好了！

實則不然，這只是因為這個 setup 下你讀到 Back Buffer 而翻轉，假設你環境或開發過程中改到上面扯的一堆因素導致你在那個時間點又沒抓到 back buffer 了，這時這邊的冗餘判斷式反而又會翻轉成上下顛倒的樣子。

我之所以感覺三不五時上下又顛倒了就是在其他平台的 branch 下的優化過程中導致 rendering path 改變（拔掉多相機或 Post Processing）而又顛倒回來。

---
# Environment
- Unity 2021.3.36f1 
- Built-in RP
- 只測以下平台:
  - Windows D3D11、D3D12、OpenGLCore、Vulkan
  - N 主機 、 P 主機

手機都沒測，希望有緣人自己開專案試 

