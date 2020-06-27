using System.Collections.Generic;
using System.Reflection;
using TenorGifToolkit.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TenorGifToolkit.Handler
{
    /* It is highly recommended you don't modify this script unless you absolutely have to.
     * Keep in mind this is tied to the TenorGifSearcherEditor script and so any new variables will need to be scripted for the inspector.
     * 
     * As such there is little commenting on this script. However if you are intending on pulling this apart, here is a rundown...
     * TenorGifSearcher handles calls to the TenoreServiceAPI Core API controller.
     * Making calls to the API Core requires an API key to be provided when an instance is made,
     * and most searches require a TenorAPI.SearchPramsAPI which defines things such as search limit, content filter etc.
     * 
     * All results from the Core API are returning in a callback method, look into the below functions and corresponding Core API call for an example.
     * This makes things a little more complicated, however it stops the possibility of locking up the UI on requests.
     *      Target Callbacks are split between text search and ID search. Giving the option to handle differently.
     *      For example, the search callback would be for the user searching a GIF, the ID callback would be for the person receiving a GIF ID to display on their client.
     * 
     * This script handles those calls through the following functions.
     *  -SearchGifs()
     *  -GetNextPage()
     *  -RandomGifs()
     *  -SearchGifsByIDs()
     * 
     * SearchGifs() is simple, it takes the search prams and returns the results.
     * 
     * GetNextPage() takes no search text. When SearchGifs() or RandomGifs() is searched, a reference to the searchText is stored,
     * along with the required property for getting the next page. Thus this function can be called after either, and a new set of results will be returned.
     * 
     * RandomGifs() is like SearchGifs(), however is does not take a search text. It takes the "randomSearchKeyword" defined in the inspector.
     * Great for displaying GIFs straight away, before the user inputs a search result.
     * Note, this is not a basic search on "randomSearchKeyword", the results will still be random.
     * 
     * SearchGifsByIDs() is not demonstrated in the example script. This function is slightly different to the others.
     * This function takes a list of IDs; which can be found in the results from any of the above listed functions, along with this one.
     * This is great for receiving GIFs in chat across a server. When a user posts a GIF, you can send an ID across to all other users.
     * Then they can make an API call them-selfs to request GIF data. Thus reducing network traffic on your game.
     * 
     * It is also useful if you wish to restrict the use of GIFs to a predefined list.
     */
    [AddComponentMenu("Tenor Toolkit/GIF Searcher")]
    public class TenorGifSearcher : MonoBehaviour
    {
        //Private Fields
#pragma warning disable CS0649 // Disable not used warnings, they are used...
        [SerializeField] private GameObject onSearchedTargetObject;
        [SerializeField] private Component onSearchedTargetComponent;
        [SerializeField] private Component onSearchedByIdTargetComponent;
        [SerializeField] private byte[] onSearchedTargetMethodInfoData;
        [SerializeField] private string onSearchedTargetMethodSelector;
        [SerializeField] private byte[] onSearchedByIdTargetMethodInfoData;
        [SerializeField] private string onSearchedByIdTargetMethodSelector;
        [SerializeField] private string tenorApiKey = "LIVDSRZULELA";
#pragma warning restore CS0649
        private string lastSearchResultsNext = string.Empty;
        private string lastSearch = string.Empty;

        private SimpleBinaryStreamer binaryStreamer = new SimpleBinaryStreamer();
        private TenoreServiceAPI api;

        //Public Properties
        /// <summary> Don't modify, this is public purely for the Editor Inspector Script. </summary>
        public GameObject OnSearchedTargetObject { get => onSearchedTargetObject; set => onSearchedTargetObject = value; }
        public SearchContentFilters contentFilter = SearchContentFilters.Low;
        public int searchLimit = 1;
        public string randomSearchKeyword = "random";
        /// <summary> Don't modify, this is public purely for the Editor Inspector Script. </summary>
        public MethodInfo OnSearchedTargetMethod
        {
            get => onSearchedTargetMethodInfoData != null ? binaryStreamer.GetIt<MethodInfo>(ref onSearchedTargetMethodInfoData) : null;
            set { if (value != null) binaryStreamer.SetItClass(ref onSearchedTargetMethodInfoData, ref value); else onSearchedTargetMethodInfoData = new byte[] { }; }
        }
        /// <summary> Don't modify, this is public purely for the Editor Inspector Script. </summary>
        public MethodInfo OnSearchedByIdTargetMethod
        {
            get => onSearchedByIdTargetMethodInfoData != null ? binaryStreamer.GetIt<MethodInfo>(ref onSearchedByIdTargetMethodInfoData) : null;
            set { if (value != null) binaryStreamer.SetItClass(ref onSearchedByIdTargetMethodInfoData, ref value); else onSearchedByIdTargetMethodInfoData = new byte[] { }; }
        }

        private void Start()
        {
            api = new TenoreServiceAPI(tenorApiKey);
            lastSearchResultsNext = string.Empty;
            lastSearch = string.Empty;
        }

        #region SearchRequests
        public void SearchGifs(Text searchText) => SearchGifs(searchText.text);

        public void SearchGifs(string searchText)
        {
            lastSearch = searchText;
            HandleSearchRequest(searchText, isRandom: false, nextPages: false);
        }

        public void SearchGifsRandom(Text searchText) => SearchGifsRandom(searchText.text);

        public void SearchGifsRandom(string searchText)
        {
            lastSearch = searchText;
            HandleSearchRequest(searchText, isRandom: true, nextPages: false);
        }

        public void GetNextPage()
        {
            HandleSearchRequest(lastSearch, isRandom: false, nextPages: true);
        }

        public void RandomGifs()
        {
            lastSearch = randomSearchKeyword;
            HandleSearchRequest(randomSearchKeyword, isRandom: true, nextPages: false);
        }

        public void GetGifByID(int ID)
        {
            if (!CheckPreSearchConditionsSuccess(byID: true)) return;
            int[] IDs = new int[] { ID };
            StartCoroutine(api.GetGifsByIDs(IDs, (SearchResults.Result[] searchResults) =>
            {
                TenorFormattedResults results = new TenorFormattedResults(searchResults);
                OnSearchedByIdTargetMethod.Invoke(onSearchedByIdTargetComponent, new object[] { results });
            }));
        }

        public void GetGifsByIDs(int[] IDs)
        {
            if (!CheckPreSearchConditionsSuccess(byID: true)) return;

            StartCoroutine(api.GetGifsByIDs(IDs, (SearchResults.Result[] searchResults) =>
            {
                TenorFormattedResults results = new TenorFormattedResults(searchResults);
                OnSearchedByIdTargetMethod.Invoke(onSearchedByIdTargetComponent, new object[] { results });
            }));
        }

        public void GetGifsByIDs(List<int> IDs)
        {
            if (!CheckPreSearchConditionsSuccess(byID: true)) return;

            StartCoroutine(api.GetGifsByIDs(IDs.ToArray(), (SearchResults.Result[] searchResults) =>
            {
                TenorFormattedResults results = new TenorFormattedResults(searchResults);
                OnSearchedByIdTargetMethod.Invoke(onSearchedByIdTargetComponent, new object[] { results });
            }));
        }

        private void HandleSearchRequest(string searchString, bool isRandom, bool nextPages)
        {
            if (!CheckPreSearchConditionsSuccess()) return;

            if (!nextPages) lastSearchResultsNext = string.Empty;

            SearchPramsAPI searchPrams = new SearchPramsAPI()
            {
                ContentFilter = (SearchPramsAPI.ContentFilters)(int)contentFilter,
                ResultsLimit = searchLimit,
                NextPagePos = lastSearchResultsNext,
                SearchQuery = searchString
            };

            if (isRandom)
            {
                StartCoroutine(api.GetRandomSearchResults(searchPrams, (SearchResults searchResults) =>
                {
                    lastSearchResultsNext = searchResults.next;
                    TenorFormattedResults results = new TenorFormattedResults(searchResults.results);
                    OnSearchedTargetMethod.Invoke(onSearchedTargetComponent, new object[] { results, nextPages });
                }));
            }
            else
            {
                StartCoroutine(api.GetSearchResults(searchPrams, (SearchResults searchResults) =>
                {
                    lastSearchResultsNext = searchResults.next;
                    TenorFormattedResults results = new TenorFormattedResults(searchResults.results);
                    OnSearchedTargetMethod.Invoke(onSearchedTargetComponent, new object[] { results, nextPages });
                }));
            }
        }

        private bool CheckPreSearchConditionsSuccess(bool byID = false)
        {
            if (onSearchedTargetObject is null || onSearchedTargetComponent is null || (!byID && OnSearchedTargetMethod is null) || (byID && OnSearchedByIdTargetMethod is null))
            {
                Debug.LogWarning("TenorGifSearcher: Cannot do Event Search without Object/Function setup in inspector.");
                return false;
            }
            return true;
        }
        #endregion
    }
}