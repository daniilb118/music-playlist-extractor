using System.Collections.Generic;

namespace MusicPlaylistExtractor.Models
{
    public class MusicPlaylist
    {
        public string Name { get; set; } = string.Empty;
        public string? AvatarURL { get; set; } = null;
        public string Description { get; set; } = string.Empty;
        public List<Song> Songs { get; set; } = new List<Song>();
    }
}
