using ReactiveUI;
using System.Collections.Generic;
using System.Linq;
using MusicPlaylistExtractor.Models;
using System.Collections.ObjectModel;

namespace MusicPlaylistExtractor.ViewModels
{
    public class MusicPlaylistViewModel : ReactiveObject
    {
        public ObservableCollection<Song> Songs { get; }
        public MusicPlaylistViewModel(MusicPlaylist playlist)
        {
            Name = playlist.Name;
            AvatarURL = playlist.AvatarURL;
            Description = playlist.Description;
            Songs = new(playlist.Songs);
        }

        public string Name { get; }
        public string? AvatarURL { get; }
        public string Description { get; }
    }
}