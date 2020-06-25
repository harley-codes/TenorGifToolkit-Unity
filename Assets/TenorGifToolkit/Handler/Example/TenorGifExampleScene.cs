using System;
using TenorGifToolkit.Helpers;
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
 */

public class TenorGifExampleScene : MonoBehaviour
{
    public TenorGifSearcher tenorGifSearcher;
    private TenorFormattedResults SearchResults { get; set; }
    public GifQualities gifSearchQuality = GifQualities.Nano;

    public GameObject panelChat, panelGif;
    public GameObject buttonLoadMore;
    public GameObject scrollViewContainerChat, scrollViewContainerGif;
    public Transform spinningCube;
    public ScrollRect scrollRectChat;
    public Sprite gifMask;

    private void Start()
    {
        panelChat.SetActive(true);
        panelGif.SetActive(false);
        buttonLoadMore.SetActive(false);
    }

    private void Update()
    {
        //The spinning cube is purely an example of how the API calls will not lock up the screen when sending requests.
        spinningCube.Rotate(Vector3.up, 50 * Time.deltaTime);
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

        DissplayGifSearchResults(searchResults);
    }

    // This will iterate through the provided results and append the Scroll-view with GIF buttons.
    // Clicking a GIF button will make a call to SendChatGIF();
    // 
    private void DissplayGifSearchResults(TenorFormattedResults searchResults)
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

            HelperFunctions.PrepareGifPlayer_NewRender(ref videoPlayer, out RenderTexture renderTexture, gif, loopGif: true);
            rawImage.texture = renderTexture;
            try { videoPlayer.SetDirectAudioMute(0, true); } catch { }
            videoPlayer.Play();

            Button button = go.GetComponent<Button>();
            Image image = go.GetComponent<Image>();
            button.targetGraphic = image;
            image.sprite = gifMask;
            button.onClick.AddListener(() =>
            {
                SendChatGIF(gif);
                button.onClick.RemoveAllListeners();
                foreach (RectTransform child in scrollViewContainerGif.transform)
                    if (child.name != buttonLoadMore.name)
                        Destroy(child.gameObject);
            });
        }
        buttonLoadMore.SetActive(true);
        buttonLoadMore.transform.SetAsLastSibling();
    }


    // Called from DissplayGifSearchResults butoon.onClick lambada expression.
    private void SendChatGIF(TenorFormattedResults.Gif gif)
    {
        ShowChatPanel();
        SendChatMessage("");
        GameObject go = new GameObject($"ChatGif: {DateTime.Now} - ID{gif.ApiID}", typeof(VideoPlayer), typeof(RawImage), typeof(AspectRatioFitter));
        go.transform.SetParent(scrollViewContainerChat.transform);
        RawImage image = go.GetComponent<RawImage>();
        VideoPlayer videoPlayer = go.GetComponent<VideoPlayer>();
        HelperFunctions.PrepareGifPlayer_NewRender(ref videoPlayer, out RenderTexture renderTexture, gif, loopGif: true);

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
    public void SendChatMessage(Text message) { SendChatMessage(message.text); }

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
