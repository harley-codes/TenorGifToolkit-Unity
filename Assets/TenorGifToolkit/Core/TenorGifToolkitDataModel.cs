using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace TenorGifToolkit.Core
{
    /* WARNING!!!
     * These classes, and variables have been setup/named specifically for the Tenor API.
     * Changing this script could break any API calls.
     * 
     * If you are intending on expanding on the API search results.
     * Check out the Tenor documentation, and use a tool like PostMan to first test your API returns.
     * https://tenor.com/gifapi/documentation
     * https://www.postman.com/
     */

    [Serializable]
    public class SearchResults
    {
        protected SearchResults() { }
        public string weburl;
        public Result[] results;
        /// <summary> Next represents the necessary POS val for search queries when wanting the next page </summary>
        public string next;

        [Serializable]
        public class Result
        {
            protected Result() { }
            public int id;
            public string title;
            public List<Media> media;
            public bool hasaudio;
            /// <summary> Media returns the first index of media, as the Tenor API nests a value in a list. media's length is always 1 </summary>
            public Media Media => media[0];
        }

        [Serializable]
        public class Media
        {
            protected Media() { }
            public MediaItem nanomp4;
            public MediaItem tinymp4;
            public MediaItem mp4;
        }

        [Serializable]
        public class MediaItem
        {
            protected MediaItem() { }
            public string url;
            public string preview;
            public int[] dims;
            /// <summary>Turns dims into a Vector, dimms has an array length of 2, storing the X and Y pixels. </summary>
            public Vector2Int Dimentions => new Vector2Int(dims[0], dims[1]);
        }
    }
}