﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Proxer.ViewModels.Media;
using Proxer.Views.Media;

namespace Proxer.Utility
{
    public static class MediaHandler
    {
        private static readonly Regex MangaReaderUriRegex =
            new Regex(@"http[s]?:\/\/proxer.me\/read\/(?<manga_id>\d+)\/(?<chapter_id>\d+)\/(?<lang>de|en)",
                RegexOptions.ExplicitCapture);

        #region

        private static async Task<Uri> GetDailymotionStreamUri(Uri baseUri, CancellationToken cancellationToken)
        {
            string lResponse = await HttpUtility.GetRequest(baseUri, cancellationToken).ConfigureAwait(false);
            Match lFirstMatch = new Regex("\"(http[^\"]+?\\.mp4\\S+?)\"").Match(lResponse);
            if (lFirstMatch.Success) return new Uri(lFirstMatch.Captures[0].Value);
            throw new Exception();
        }

        private static async Task<Uri> GetMp4UploadStreamUri(Uri baseUri, CancellationToken cancellationToken)
        {
            string lResponse = await HttpUtility.GetRequest(baseUri, cancellationToken).ConfigureAwait(false);
            Match lFirstMatch = new Regex(@"(http[s]*:\/\/www[0-9]+\S+?video\.mp4)").Match(lResponse);
            if (lFirstMatch.Success) return new Uri(lFirstMatch.Captures[0].Value);
            throw new Exception();
        }

        private static async Task<Uri> GetProxerStreamUri(Uri baseUri, CancellationToken cancellationToken)
        {
            string lResponse = await HttpUtility.GetRequest(baseUri, cancellationToken).ConfigureAwait(false);
            Match lFirstMatch = new Regex(@"(http\S+?\.mp4)").Match(lResponse);
            if (lFirstMatch.Success) return new Uri(lFirstMatch.Captures[0].Value);
            throw new Exception();
        }

        private static async Task<Uri> GetStreamcloudStreamUri(Uri baseUri, CancellationToken cancellationToken)
        {
            string lResponse = await HttpUtility.GetRequest(baseUri, cancellationToken).ConfigureAwait(false);
            MatchCollection lPostMatches =
                new Regex("input type=\"hidden\" name=\"(?<test1>\\S+?)\" value=\"(?<test2>\\S+?)\"").Matches(lResponse);
            Dictionary<string, string> lPostArgsDictionary = new Dictionary<string, string>();
            foreach (Match match in lPostMatches)
                lPostArgsDictionary.Add(match.Groups[1].Value, match.Groups[2].Value);

            await Task.Delay(TimeSpan.FromSeconds(11), cancellationToken).ConfigureAwait(false);

            string lPostResponse =
                await HttpUtility.PostRequest(baseUri, lPostArgsDictionary, cancellationToken).ConfigureAwait(false);
            Match lFirstMatch = new Regex(@"(http\S+?video\.mp4)").Match(lPostResponse);
            if (lFirstMatch.Success) return new Uri(lFirstMatch.Captures[0].Value);

            throw new Exception();
        }

        public static async Task HandleStreamPartnerUri(Uri baseUri, CancellationToken cancellationToken)
        {
            switch (baseUri.Host.Replace("www.", string.Empty))
            {
                case "stream.proxer.me":
                    StartVideoFromUri(await GetProxerStreamUri(baseUri, cancellationToken).ConfigureAwait(false));
                    break;
                case "mp4upload.com":
                    StartVideoFromUri(await GetMp4UploadStreamUri(baseUri, cancellationToken).ConfigureAwait(false));
                    break;
                case "streamcloud.eu":
                    StartVideoFromUri(await GetStreamcloudStreamUri(baseUri, cancellationToken).ConfigureAwait(false));
                    break;
                case "dailymotion.com":
                    StartVideoFromUri(await GetDailymotionStreamUri(baseUri, cancellationToken).ConfigureAwait(false));
                    break;
                default:
                    await await Task.Factory.StartNew(() => Launcher.LaunchUriAsync(baseUri), cancellationToken,
                            TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext())
                        .ConfigureAwait(false);
                    break;
            }
        }

        private static void StartVideoFromUri(Uri videoUri)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame?.Navigate(typeof(MediaPlayerView), videoUri);
        }

        private static void StartMangaReader(ChapterInfo chapterInfo, bool isSlide)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame?.Navigate(isSlide ? typeof(MangaReaderView) : typeof(VerticalMangaReaderView), chapterInfo);
        }

        public static bool HandleMangaReaderUri(Uri uri)
        {
            Match lMatch = MangaReaderUriRegex.Match(uri.AbsoluteUri);
            if (!lMatch.Success) return false;

            ChapterInfo lChapterInfo = new ChapterInfo
            {
                MangaId = Convert.ToInt32(lMatch.Groups["manga_id"].Value),
                ChapterId = Convert.ToInt32(lMatch.Groups["chapter_id"].Value),
                Language = lMatch.Groups["lang"].Value
            };
            StartMangaReader(lChapterInfo, uri.Query.Contains("v=slide_beta"));

            return true;
        }

        #endregion
    }
}