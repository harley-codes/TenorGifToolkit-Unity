using System.Collections.Generic;
using System.Linq;
using GifToolkit.Core;
using UnityEngine;
using UnityEngine.Video;
using System.ComponentModel;

namespace GifToolkit.Handler
{
    /* Just some cool stuff to help out, and reduce your code :)
     * 
     * SelectGifByQuality()
     * Rather that doing a conditional check in your function to determine the quality you want,
     * This extension method will return the quality you want based on provided enum
     * 
     * TenorFormattedResults
     * Just a much more please adaptation of the Core API search results.
     * Has function AddPages(), useful if you want to hold a reference of old/new after getting new search results.
     * 
     * PrepareGifPlayer
     * Configures the video player for you and spits out a Render Texture that can be applied to a UI image.
     */

    public enum SearchContentFilters
    {
        /// <summary>
        /// Show only content rated: G
        /// </summary>
        [Description("Only show content rated: G")]
        High = 1,
        /// <summary>
        /// Only show content rated: G and PG
        /// </summary>
        [Description("Only show content rated: G and PG")]
        Medium = 2,
        /// <summary>
        /// Only show content rated: G, PG and PG-13
        /// </summary>
        [Description("Only show content rated: G, PG and PG-13")]
        Low = 3,
        /// <summary>
        /// Only show content: G, PG, PG-13, and R (no nudity)
        /// </summary
        [Description("Only show content rated: G, PG, PG-13, and R (no nudity)")]
        Off = 4
    }

    public enum GifQualities
    {
        Nano = 1,
        Tiny = 2,
        Normal = 3
    }

    public static class Extentions
    {
        public static TenorFormattedResults.Gif SelectGifByQuality(this TenorFormattedResults.Media media, GifQualities quality)
        {
            switch (quality)
            {
                case GifQualities.Nano:
                    return media.NanoSize;
                case GifQualities.Tiny:
                    return media.TinySize;
                case GifQualities.Normal:
                    return media.NormalSize;
                default:
                    return media.NanoSize;
            }
        }
    }

    public class TenorFormattedResults
    {
        /// <summary>
        /// The Dictionary Key represents the GIF's ID associated with the Tenor Database.
        /// </summary>
        public Dictionary<int, Media> GifResults { get; set; }

        public class Media
        {
            public Media(SearchResults.Result result)
            {
                ApiID = result.id;
                Title = result.title;
                NanoSize = new Gif(result.Media.nanomp4, result.id, result.title, result.hasaudio);
                TinySize = new Gif(result.Media.tinymp4, result.id, result.title, result.hasaudio);
                NormalSize = new Gif(result.Media.mp4, result.id, result.title, result.hasaudio);
                HasAudio = result.hasaudio;

            }
            public int ApiID { get; protected set; }
            public string Title { get; protected set; }
            public bool HasAudio { get; protected set; }
            public Gif NanoSize { get; protected set; }
            public Gif TinySize { get; protected set; }
            /// <summary>
            /// Warning, not only may NormalSize slow search and load speeds,
            /// Tenor does not always supply a usable image format usable by Unity for the thumbnail.
            /// </summary>
            public Gif NormalSize { get; protected set; }
        }

        public class Gif
        {
            public Gif(SearchResults.MediaItem item, int iD, string title, bool hasAudio)
            {
                VideoURL = item.url;
                ThumnailURL = item.preview;
                PixelSize = item.Dimentions;
                ApiID = iD;
                Title = title;
                AspectRacio = (float)item.Dimentions.x / (float)item.Dimentions.y;
                HasAudio = hasAudio;
            }
            public string Title { get; protected set; }
            public int ApiID { get; protected set; }
            public string VideoURL { get; protected set; }
            public string ThumnailURL { get; protected set; }
            public Vector2Int PixelSize { get; protected set; }
            public float AspectRacio { get; protected set; }
            public bool HasAudio { get; set; }
        }

        public TenorFormattedResults(SearchResults.Result result)
        {
            GifResults = new Dictionary<int, Media>();
            GifResults.Add(result.id, new Media(result));
        }

        public TenorFormattedResults(SearchResults.Result[] results)
        {
            GifResults = results.ToDictionary(
                key => key.id,
                value => new Media(value)
            );
        }

        public void AddPages(SearchResults.Result[] results)
        {
            results.ToList().ForEach(result => {
                if (!GifResults.ContainsKey(result.id))
                {
                    GifResults.Add(result.id, new Media(result));
                }
                else
                    Debug.LogWarning($"\"{result.title}:{result.id}\" cannot be added, " +
                        $"it's ID conflicts with previously added GIF \"{GifResults[result.id].Title}:{GifResults[result.id].ApiID}\".");
            });
        }

        public void AddPages(TenorFormattedResults results)
        {
            results.GifResults.ToList().ForEach(keyPair => {
                if (!GifResults.ContainsKey(keyPair.Key))
                {
                    GifResults.Add(keyPair.Key, keyPair.Value);
                }
            });
        }
    }

    public static class HelperFunctions
    {
        public static void PrepareGifPlayer_NewRender(ref VideoPlayer videoPlayer, out RenderTexture renderTexture, TenorFormattedResults.Gif gif, bool loopGif = true, bool disableAudio = true)
        {
            renderTexture = new RenderTexture(gif.PixelSize.x, gif.PixelSize.y, 0);
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = gif.VideoURL;
            videoPlayer.isLooping = loopGif;
            videoPlayer.targetTexture = renderTexture;
            if (disableAudio)
            {
                for (ushort i = 0; i < videoPlayer.controlledAudioTrackCount; i++)
                {
                    videoPlayer.SetDirectAudioMute(i, true);
                    videoPlayer.EnableAudioTrack(i, false);
                }
            }
        }

        public static void PrepareGifPlayer_ExisitngRender(ref VideoPlayer videoPlayer, ref RenderTexture renderTexture, TenorFormattedResults.Gif gif, bool loopGif = true, bool disableAudio = true)
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = gif.VideoURL;
            videoPlayer.isLooping = loopGif;
            videoPlayer.targetTexture = renderTexture;
            if (disableAudio)
            {
                for (ushort i = 0; i < videoPlayer.controlledAudioTrackCount; i++)
                {
                    videoPlayer.SetDirectAudioMute(i, true);
                    videoPlayer.EnableAudioTrack(i, false);
                }
            }
        }
    }
}
