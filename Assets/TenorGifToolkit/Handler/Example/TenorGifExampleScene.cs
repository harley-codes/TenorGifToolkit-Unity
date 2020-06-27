using System;
using TenorGifToolkit.Handler;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/* Thanks for purchasing, I hope you enjoy :)
 * This example script will show you how to integrate with the TenorGifSearcher class
 * TenorGifSearcher is a helper that will simplify Tenor API core tasks!!
 * 
 * For full control however, check out the documentation for TenorGifToolkit.Core
 * With that you can handle raw requests and manage your own callbacks, logic etc.
 * 
 * This example script has all the code to handle the example scene, right down to scripted UI objects.
 * That way you can see how everything is done, and hopefully get some ideas yourself.
 * Half the stuff in here can be simplified down/converted to a few prefabs and some helper classes. So don't be overwhelmed by the amount of code. Its super easy :) 
 * The main things to work with though is just the TenorGifSearcher class to make calls, and a function that will receive search requests.
 * 
 * Addressing the debug warning you may see, "AudioSampleProvider buffer overflow. X sample frames discarded."
 * This is not an issue with Unity, or the code. This is a result of the GIF videos provided by Tenor.
 * As we are loading MP4s they may come with sound, and they may not be created correctly by Tenor or The Up-loader.
 * This is not an issue however, and it will not affect your game in any way :)
 * I have research this any the easiest way to explain is..
 * If a video has say 100 frames, but there are 150 frames worth of audio. The video player must discard 50 frames worth of audio.
 */

public class TenorGifExampleScene : MonoBehaviour
{
    public TenorGifSearcher tenorGifSearcher;
    private TenorFormattedResults SearchResults { get; set; }
    public GifQualities gifSearchQuality = GifQualities.Nano;

    public GameObject panelChat, panelGif;
    public GameObject buttonLoadMore;
    public GameObject scrollViewContainerChat, scrollViewContainerGif;
    private RectTransform scrollViewContainerChatViewport, scrollViewContainerGifViewport;
    public Transform spinningCube;
    public ScrollRect scrollRectChat;
    public Sprite gifMask;

    private void Start()
    {
        panelChat.SetActive(true);
        panelGif.SetActive(false);
        buttonLoadMore.SetActive(false);
        scrollViewContainerChatViewport = scrollViewContainerChat.transform.parent.GetComponent<RectTransform>();
        scrollViewContainerGifViewport = scrollViewContainerGif.transform.parent.GetComponent<RectTransform>();
        InvokeRepeating("VramControllCaller", 0.1f, 0.1f);
    }

    private void Update()
    {
        //The spinning cube is purely an example of how the API calls will not lock up the screen when sending requests.
        spinningCube.Rotate(Vector3.up, 50 * Time.deltaTime);
    }

    // Invoked repeatedly from start()
    // The actions will check the GIF's view condition of the scroll view,
    // ensuring if the GIF is not in view, that it is not playing.
    // Make sure you do a similar thing with your code.
    // Invoking will help with the amount of checks done and increase performance.
    // If you have to many video players running all at once,
    // you will start to get memory errors and they wont play.
    void VramControllCaller()
    {
        VramControllAction(scrollViewContainerGifViewport);
        VramControllAction(scrollViewContainerChatViewport);
    }

    // See VramControllCaller() for comments.
    void VramControllAction(RectTransform viewport)
    {
        Rect viewWorldRect = viewport.rect;
        viewWorldRect.center = viewport.TransformPoint(viewWorldRect.center);
        viewWorldRect.size = viewport.TransformVector(viewWorldRect.size);

        foreach (VideoPlayer gif in viewport.GetComponentsInChildren<VideoPlayer>(true))
        {
            RectTransform gifRect = gif.GetComponent<RectTransform>();
            Rect gifWorldRect = gifRect.rect;
            gifWorldRect.center = gifRect.TransformPoint(gifWorldRect.center);
            gifWorldRect.size = gifRect.TransformVector(gifWorldRect.size);

            bool overlaps = gifWorldRect.Overlaps(viewWorldRect, true);

            if (overlaps && !gif.isPlaying && gif.gameObject.activeInHierarchy)
            {
                gif.Play();
            }

            if (!overlaps && gif.isPlaying && gif.gameObject.activeInHierarchy)
            {
                gif.Stop();
            }
        }
    }

    // Called from the Input Field event in GIF panel.
    // The Search GIFs function could easily be called from an InputField event,
    // It has been shown here to demonstrate the function.
    public void InputFieldEventSearchGIFs(Text text)
    {
        tenorGifSearcher.SearchGifs(text.text);
    }

    // Called from the Load More Button in GIF Panel
    // When TenorGifSearcher.SearchGifs has been called, a reference to the next page requirements is stored.
    // Simply call GetNextPage and the same callback Event as SearchGifs() will be called with the next lot of results
    public void ButtonEventSearchNextGIFs()
    {
        tenorGifSearcher.GetNextPage();
    }

    // This is called from the TenorGifSearcher, its is executed on a callback method once the Tenor API returns results.
    // To receive the callback event from TenorGifSearcher, you must reference a public function just like this.
    // EG: public Function(TenorFormattedResults ..., boolean ...)
    public void SearchResultsCallback(TenorFormattedResults searchResults, bool nextPages)
    {
        // This will check if the User has requested to load more results, or if it is a new search.
        // A reference to the results is stored in SearchResults so that the details such is URL can be used later when posting a GIF
        if (nextPages && searchResults != null)
        {
            SearchResults.AddPages(searchResults);
        }
        else
        {
            SearchResults = searchResults;
            foreach (RectTransform child in scrollViewContainerGif.transform)
                if (child.name != buttonLoadMore.name)
                    Destroy(child.gameObject);
        }

        DisplayGifSearchResults(searchResults);
    }

    // SearchResultsCallback() for additional comments...
    // SearchResultsByIdCallback() is the callback from when a searched GIF is clicked.
    // In this example scene it is not necessary, but this is an example of how you would display a chosen GIF on another clients PC.
    // For example, after choosing a GIF. You would broadcast relevant info including a GIF ID, the end client would then search the API based on GIF ID
    // You wouldn't want to send the hole GIF data across server.
    public void SearchResultsByIdCallback(TenorFormattedResults searchResults)
    {
        foreach (var result in searchResults.GifResults)
        {
            TenorFormattedResults.Gif gif = result.Value.SelectGifByQuality(gifSearchQuality);
            SendChatGIF(gif);
        }
    }

    // Called form SearchResultsCallback() after a GIFs are searched.
    // This will iterate through the provided results and append the Scroll-view with GIF buttons.
    // Clicking a GIF button re-search the GIF by ID to be pasted in chat. See SearchResultsByIdCallback() comments for an explanation as to why.
    private void DisplayGifSearchResults(TenorFormattedResults searchResults)
    {
        foreach (var result in searchResults.GifResults)
        {
            TenorFormattedResults.Gif gif = result.Value.SelectGifByQuality(gifSearchQuality);

            GameObject go = new GameObject(gif.ApiID.ToString(), typeof(Button), typeof(Image), typeof(Mask));
            go.transform.SetParent(scrollViewContainerGif.transform);

            GameObject goChild = new GameObject("GIF", typeof(VideoPlayer), typeof(RectTransform), typeof(RawImage));
            goChild.transform.SetParent(go.transform);

            RawImage rawImage = goChild.GetComponent<RawImage>();
            VideoPlayer videoPlayer = goChild.GetComponent<VideoPlayer>();
            RectTransform rectTransform = goChild.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = Vector2.one * 0.5f;
            rectTransform.position = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            HelperFunctions.PrepareGifPlayer_NewRender(ref videoPlayer, out RenderTexture renderTexture, gif, loopGif: true, disableAudio: true);
            rawImage.texture = renderTexture;
            try { videoPlayer.SetDirectAudioMute(0, true); } catch { }
            videoPlayer.Play();

            Button button = go.GetComponent<Button>();
            Image image = go.GetComponent<Image>();
            button.targetGraphic = image;
            image.sprite = gifMask;
            button.onClick.AddListener(() =>
            {
                tenorGifSearcher.GetGifByID(gif.ApiID);
                button.onClick.RemoveAllListeners();
                foreach (RectTransform child in scrollViewContainerGif.transform)
                    if (child.name != buttonLoadMore.name)
                        Destroy(child.gameObject);
            });
        }
        buttonLoadMore.SetActive(true);
        buttonLoadMore.transform.SetAsLastSibling();
    }

    // Called from SearchResultsByIdCallback()
    // Displays the GIF in chat.
    private void SendChatGIF(TenorFormattedResults.Gif gif)
    {
        ShowChatPanel();
        SendChatMessage("");
        GameObject go = new GameObject($"ChatGif: {DateTime.Now} - ID{gif.ApiID}", typeof(VideoPlayer), typeof(RawImage), typeof(AspectRatioFitter));
        go.transform.SetParent(scrollViewContainerChat.transform);
        RawImage image = go.GetComponent<RawImage>();
        VideoPlayer videoPlayer = go.GetComponent<VideoPlayer>();
        HelperFunctions.PrepareGifPlayer_NewRender(ref videoPlayer, out RenderTexture renderTexture, gif, loopGif: true, disableAudio: true);

        AspectRatioFitter fitter = go.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        fitter.aspectRatio = gif.AspectRacio;
        RectTransform rectTransform = go.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0, 150);

        image.texture = renderTexture;
        videoPlayer.Play();
        Invoke("ScrollToBottom", 0.1f);
    }

    // Create a new Text game object in the Chat scroll view.
    // Called from the Input Field's event in Panel Chat
    public void SendChatMessage(Text message) => SendChatMessage(message.text);

    private void SendChatMessage(string message)
    {
        GameObject go = new GameObject($"ChatMessage: {DateTime.Now}", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
        go.transform.SetParent(scrollViewContainerChat.transform);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 20;
        text.text = $"[{DateTime.Now:hh:mm tt}] : {message}";
        ContentSizeFitter csf = go.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        Invoke("ScrollToBottom", 0.1f);
    }

    // Called from the Search GIF Button Event in Chat panel.
    public void ButtonEventShowGifSearcher()
    {
        buttonLoadMore.SetActive(false);
        panelChat.SetActive(false);
        panelGif.SetActive(true);
        tenorGifSearcher.RandomGifs();
    }

    // Called after a GIF is posted to chat
    public void ShowChatPanel()
    {
        panelChat.SetActive(true);
        panelGif.SetActive(false);
    }

    //Ensure scroll views are always at bottom as content expands.
    private void ScrollToBottom()
    {
        scrollRectChat.verticalScrollbar.value = 0f;
    }
}
