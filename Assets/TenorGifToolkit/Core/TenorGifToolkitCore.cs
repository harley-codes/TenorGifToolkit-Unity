using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

namespace TenorGifToolkit.Core
{
    /* TenoreServiceAPI has been setup for easy use with the Tenor API
     * When you create a new instance of this class, an API key must be provided.
     * 
     * There is a static function called TestApiKey(), this is useful for development stages.
     * However not recommended for builds. As there is no need. And this has no callback function like the other API calls,
     * Unity will hold until a response from the server is returned, or the request times out.
     * 
     * If you have chosen to exclude the Helper set when importing, and do not know where to get an API key.
     * Visit this link, https://tenor.com/developer/dashboard
     * 
     * All search request require a callback method to be provided. For simple use, a lambda expression is recommended. For example...
     
     * Lambada way
        StartCoroutine(tenoreServiceAPI.GetSearchResults(searchPrams, (SearchResults searchResults) =>
        {
            //Do work here.
        }));

     * Normal Way
        void SomeFunction()
        {
            StartCoroutine(tenoreServiceAPI.GetSearchResults(searchPrams, ReturnFunction));
        }
        void ReturnFunction(SearchResults searchResults)
        {
            //Do work Here
        }
     */

    public class TenoreServiceAPI
    {
        public string ApiKey { get; protected set; }
        private event Action<SearchResults> SearchResultsEvent;
        private event Action<SearchResults.Result> SearchResultsResultEvent;
        private event Action<SearchResults.Result[]> SearchResultsResultsEvent;

        public TenoreServiceAPI(string TenorApiKey)
        {
            ApiKey = TenorApiKey;
        }

        public IEnumerator GetSearchResults(SearchPramsAPI prams, Action<SearchResults> callback)
        {
            string url = TenorAPI.ApiAddress + $"search?q={prams.SearchQuery}&key={ApiKey}&media_filter=basic&limit={prams.ResultsLimit}&pos={prams.NextPagePos}&contentfilter={prams.ContentFilter}";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError || webRequest.isHttpError)
                {
                    Debug.LogError($"Error Requesting GIF Results: {webRequest.error}");
                }
                else
                {
                    SearchResults results = JsonUtility.FromJson<SearchResults>(webRequest.downloadHandler.text);
                    SearchResultsEvent += callback;
                    SearchResultsEvent(results);
                    SearchResultsEvent = null;
                }
            }
        }

        public IEnumerator GetRandomSearchResults(SearchPramsAPI prams, Action<SearchResults> callback)
        {
            string url = TenorAPI.ApiAddress + $"random?q={prams.SearchQuery}&key={ApiKey}&media_filter=basic&limit={prams.ResultsLimit}&pos={prams.NextPagePos}&contentfilter={prams.ContentFilter}";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError || webRequest.isHttpError)
                {
                    Debug.LogError($"Error Requesting GIF Results: {webRequest.error}");
                }
                else
                {
                    SearchResults results = JsonUtility.FromJson<SearchResults>(webRequest.downloadHandler.text);
                    SearchResultsEvent += callback;
                    SearchResultsEvent(results);
                    SearchResultsEvent = null;
                }
            }
        }

        public IEnumerator GetGifByID(int gifApiID, Action<SearchResults.Result> callback)
        {
            string url = TenorAPI.ApiAddress + $"gifs?key={ApiKey}&ids={gifApiID}&media_filter=basic&limit=1";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError || webRequest.isHttpError)
                {
                    Debug.LogError($"Error Requesting GIF Results: {webRequest.error}");
                }
                else
                {
                    SearchResults results = JsonUtility.FromJson<SearchResults>(webRequest.downloadHandler.text);
                    SearchResultsResultEvent += callback;
                    SearchResultsResultEvent(results.results[0] ?? null);
                    SearchResultsResultEvent = null;
                }
            }
        }

        public IEnumerator GetGifsByIDs(int[] gifApiIDs, Action<SearchResults.Result[]> callback)
        {
            if (gifApiIDs.Length > 50)
            {
                Debug.LogWarning($"GetGifsById() can search a max of 50 GIFs, you have supplied {gifApiIDs.Length}. Results will be automatically trimmed");
                gifApiIDs = gifApiIDs.Take(50).ToArray();
            }

            string gifIDs = string.Join(",", gifApiIDs);

            string url = TenorAPI.ApiAddress + $"gifs?key={ApiKey}&ids={gifIDs}&media_filter=basic&limit={gifApiIDs.Length}";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError || webRequest.isHttpError)
                {
                    Debug.LogError($"Error Requesting GIF Results: {webRequest.error}");
                }
                else
                {
                    SearchResults results = JsonUtility.FromJson<SearchResults>(webRequest.downloadHandler.text);
                    SearchResultsResultsEvent += callback;
                    SearchResultsResultsEvent(results.results);
                    SearchResultsResultsEvent = null;
                }
            }
        }

        public static void TestApiKey(string apiKey)
        {
            string url = TenorAPI.ApiAddress + $"random?key={apiKey}";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SendWebRequest();

                while (!webRequest.isDone) { }

                if (webRequest.isNetworkError || webRequest.isHttpError)
                {
                    Debug.Log("Tenor API Check: Network error occurred, check your connection");
                }
                else
                {
                    string result = webRequest.downloadHandler.text.Contains("invalid key") ? "Invalid Key" : "Success";
                    Debug.Log($"Tenor API Check: {result}");
                }
            }
        }
    }

    public static class TenorAPI
    {
        private const string apiAddress = "https://api.tenor.com/v1/";
        public static string ApiAddress => apiAddress;
    }

    public class SearchPramsAPI
    {
        private int resultsLimit = 1;
        public string SearchQuery { get; set; }
        public int ResultsLimit { get => resultsLimit; set => resultsLimit = Mathf.Clamp(value, 1, 50); }
        public ContentFilters ContentFilter { get; set; }
        /// <summary>
        /// https://tenor.com/gifapi/documentation#contentfilter
        /// </summary>
        public enum ContentFilters
        {
            high = 1,
            medium = 2,
            low = 3,
            off = 4,
        }
        /// <summary> Page is not an index. If you want the next page, this should be set to the "next" values returned from your last search. Leave blank for a fresh search.</summary>
        public string NextPagePos { get; set; }
    }
}