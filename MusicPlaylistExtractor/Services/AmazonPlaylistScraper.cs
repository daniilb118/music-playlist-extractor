using MusicPlaylistExtractor.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlaylistExtractor.Services
{
    enum PlaylistContentType {
        Album,
        Playlist,
    }

    public class AmazonPlaylistScraper
    {
        public static async Task<MusicPlaylist> ScrapePlaylistAsync(string url)
        {
            var uri = new Uri(url);
            var lastSegment = uri.Segments[^1];
            var playlistId = lastSegment.Trim('/');

            PlaylistContentType type;
            if (url.Contains("/albums/"))
            {
                type = PlaylistContentType.Album;
            }
            else if (url.Contains("/playlists/"))
            {
                type = PlaylistContentType.Playlist;
            }
            else
            {
                throw new Exception("Got invalid link.");
            }

            var client = new HttpClient();

            var response = await client.SendAsync(RequestTemplate(playlistId, type));
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync();

            return ExtractPlaylistFromJsonResponse(jsonResponse, type);
        }

        private static MusicPlaylist MakePlaylistFromJsonEntries(JToken items, JToken template)
        {
            var playlist = new MusicPlaylist();
            playlist.Name = (string?)template["headerImageAltText"] ?? "Unknown Name";
            playlist.Description = (string?)template["headerSecondaryText"] ?? "Unknown Description";
            playlist.AvatarURL = (string?)template["headerImage"];

            foreach (var item in items)
            {
                playlist.Songs.Add(new Song
                {
                    Name = (string?)item["primaryText"] ?? "Unkown Name",
                    Artist = (string?)item["secondaryText1"] ?? "Unkown Artist",
                    Album = (string?)item["secondaryText2"] ?? "Unkown Album",
                    Duration = (string?)item["secondaryText3"] ?? "Unkown Duration",
                });
            }
            return playlist;
        }
        
        private static MusicPlaylist MakeAlbumFromJsonEntries(JToken items, JToken template)
        {
            var playlist = new MusicPlaylist();
            playlist.Name = (string?)template["headerImageAltText"] ?? "Unknown Name";
            playlist.Description = (string?)template["headerPrimaryText"] ?? "Unknown Description";
            playlist.AvatarURL = (string?)template["headerImage"];

            foreach (var item in items)
            {
                playlist.Songs.Add(new Song
                {
                    Name = (string?)item["primaryText"] ?? "Unkown Name",
                    Artist = (string?)template["headerImageAltText"] ?? "Unkown Artist",
                    Album = (string?)template["headerPrimaryText"] ?? "Unkown Album",
                    Duration = (string?)item["secondaryText3"] ?? "Unkown Duration",
                });
            }
            return playlist;
        }

        private static MusicPlaylist ExtractPlaylistFromJsonResponse(string jsonResponse, PlaylistContentType contentType)
        {
            var jObject = JObject.Parse(jsonResponse);

            var method = jObject["methods"]
                ?.AsJEnumerable()
                .Where(m => m != null)
                .FirstOrDefault(m => m["interface"]?.ToObject<string>() == "TemplateListInterface.v1_0.CreateAndBindTemplateMethod");

            var items = method?["template"]?["widgets"]?[0]?["items"];

            var template = method?["template"];

            if (items == null || template == null)
                throw new Exception("Couldn't parse playlist from URL. Probably used API is outdated so contact developers.");

            return contentType switch
            {
                PlaylistContentType.Album => MakeAlbumFromJsonEntries(items, template),
                PlaylistContentType.Playlist => MakePlaylistFromJsonEntries(items, template),
                _ => throw new NotImplementedException(), //unreachable
            };
        }

        private static HttpRequestMessage RequestTemplate(string playlistId, PlaylistContentType contentType)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://na.mesk.skill.music.a2z.com/api/showHome");
            request.Headers.Add("authority", "na.mesk.skill.music.a2z.com");
            request.Headers.Add("accept", "*/*");
            request.Headers.Add("accept-language", "en-US,en;q=0.9");
            request.Headers.Add("origin", "https://music.amazon.com");
            request.Headers.Add("referer", "https://music.amazon.com/");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
            request.Headers.Add("sec-fetch-dest", "empty");
            request.Headers.Add("sec-fetch-mode", "cors");
            request.Headers.Add("sec-fetch-site", "cross-site");
            request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36");
            request.Headers.Add("Cookie", "AWSALB=ODtTt52aGCCWmmKx+6HCIImovB+3hm0PRBHqN5J+KLEVZgOLE9wORidxR7g+jMFmu2zotjFd+CPv+mBfNkMNDzt4PWDC7fYD4IT2I78XIQMa5cNvslrIl5KGyY5X; AWSALBCORS=ODtTt52aGCCWmmKx+6HCIImovB+3hm0PRBHqN5J+KLEVZgOLE9wORidxR7g+jMFmu2zotjFd+CPv+mBfNkMNDzt4PWDC7fYD4IT2I78XIQMa5cNvslrIl5KGyY5X");

            //TODO: find out session data source and retrieve it properly
            var contentTypePlaceholder = contentType switch
            {
                PlaylistContentType.Album => "albums",
                PlaylistContentType.Playlist => "playlists",
                _ => throw new NotImplementedException(), //unreachable
            };
            var requestBodyTemplate = "{\"deeplink\":\"{\\\"interface\\\":\\\"DeeplinkInterface.v1_0.DeeplinkClientInformation\\\",\\\"deeplink\\\":\\\"/_PLAYLIST_TYPE_PLACEHOLDER_/_PLAYLIST_ID_PLACEHOLDER_\\\"}\",\"headers\":\"{\\\"x-amzn-authentication\\\":\\\"{\\\\\\\"interface\\\\\\\":\\\\\\\"ClientAuthenticationInterface.v1_0.ClientTokenElement\\\\\\\",\\\\\\\"accessToken\\\\\\\":\\\\\\\"\\\\\\\"}\\\",\\\"x-amzn-device-model\\\":\\\"WEBPLAYER\\\",\\\"x-amzn-device-width\\\":\\\"1920\\\",\\\"x-amzn-device-family\\\":\\\"WebPlayer\\\",\\\"x-amzn-device-id\\\":\\\"14406708175336113\\\",\\\"x-amzn-user-agent\\\":\\\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36\\\",\\\"x-amzn-session-id\\\":\\\"144-0670817-5336113\\\",\\\"x-amzn-device-height\\\":\\\"1080\\\",\\\"x-amzn-request-id\\\":\\\"400e8ee0-7deb-45a4-80dd-249462983610\\\",\\\"x-amzn-device-language\\\":\\\"en_US\\\",\\\"x-amzn-currency-of-preference\\\":\\\"USD\\\",\\\"x-amzn-os-version\\\":\\\"1.0\\\",\\\"x-amzn-application-version\\\":\\\"1.0.8241.0\\\",\\\"x-amzn-device-time-zone\\\":\\\"Europe/Kiev\\\",\\\"x-amzn-timestamp\\\":\\\"1762790434977\\\",\\\"x-amzn-csrf\\\":\\\"{\\\\\\\"interface\\\\\\\":\\\\\\\"CSRFInterface.v1_0.CSRFHeaderElement\\\\\\\",\\\\\\\"token\\\\\\\":\\\\\\\"GA5v4lexK+TLLdfguDeHNEnW9kmVCQC2Izxfn4B0pAA=\\\\\\\",\\\\\\\"timestamp\\\\\\\":\\\\\\\"1762790420\\\\\\\",\\\\\\\"rndNonce\\\\\\\":\\\\\\\"162978770\\\\\\\"}\\\",\\\"x-amzn-music-domain\\\":\\\"music.amazon.com\\\",\\\"x-amzn-referer\\\":\\\"\\\",\\\"x-amzn-affiliate-tags\\\":\\\"\\\",\\\"x-amzn-ref-marker\\\":\\\"\\\",\\\"x-amzn-page-url\\\":\\\"https://music.amazon.com/_PLAYLIST_TYPE_PLACEHOLDER_/_PLAYLIST_ID_PLACEHOLDER_\\\",\\\"x-amzn-weblab-id-overrides\\\":\\\"\\\",\\\"x-amzn-video-player-token\\\":\\\"\\\",\\\"x-amzn-feature-flags\\\":\\\"\\\",\\\"x-amzn-has-profile-id\\\":\\\"\\\",\\\"x-amzn-age-band\\\":\\\"\\\"}\"}0";
            var finalBodyJson = requestBodyTemplate.Replace("_PLAYLIST_ID_PLACEHOLDER_", playlistId).Replace("_PLAYLIST_TYPE_PLACEHOLDER_", contentTypePlaceholder);
            request.Content = new StringContent(finalBodyJson, Encoding.UTF8, "text/plain");
            return request;
        }
    }
}
