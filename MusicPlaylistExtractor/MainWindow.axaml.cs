using Avalonia.Controls;
using MusicPlaylistExtractor.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using MusicPlaylistExtractor.ViewModels;

namespace MusicPlaylistExtractor;

public partial class MainWindow : Window
{
    private readonly TextBox _urlTextBox;
    private readonly Button _loadButton;
    private readonly Image _playlistAvatar;
    private readonly TextBlock _playlistName;
    private readonly TextBlock _playlistDescription;
    private readonly DataGrid _songsGrid;
    private readonly Grid _playlistInfoGrid;
    private readonly AmazonPlaylistScraper _scraper;

    public MainWindow()
    {
        InitializeComponent();

        _urlTextBox = this.FindControl<TextBox>("UrlTextBox") ?? throw new InvalidOperationException("Cannot find UrlTextBox");
        _loadButton = this.FindControl<Button>("LoadButton") ?? throw new InvalidOperationException("Cannot find LoadButton");
        _playlistAvatar = this.FindControl<Image>("PlaylistAvatar") ?? throw new InvalidOperationException("Cannot find PlaylistAvatar");
        _playlistName = this.FindControl<TextBlock>("PlaylistName") ?? throw new InvalidOperationException("Cannot find PlaylistName");
        _playlistDescription = this.FindControl<TextBlock>("PlaylistDescription") ?? throw new InvalidOperationException("Cannot find PlaylistDescription");
        _songsGrid = this.FindControl<DataGrid>("SongsGrid") ?? throw new InvalidOperationException("Cannot find SongsGrid");
        _playlistInfoGrid = this.FindControl<Grid>("PlaylistInfoGrid") ?? throw new InvalidOperationException("Cannot find PlaylistInfoGrid");
        try
        {
            _scraper = new AmazonPlaylistScraper();
            _loadButton.Click += LoadButton_Click;
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"An error occurred: {ex.Message}");
        }
    }

    private async Task LoadPlaylist()
    {
        var url = _urlTextBox.Text;
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            var playlist = await _scraper.ScrapePlaylistAsync(url);
            var viewModel = new MusicPlaylistViewModel(playlist);
            DataContext = viewModel;
            _playlistName.Text = playlist.Name;
            _playlistDescription.Text = playlist.Description;
            if (playlist.AvatarURL != null)
            {
                _playlistAvatar.Source = await LoadImageFromUrl(playlist.AvatarURL);
            }

            _playlistInfoGrid.IsVisible = true;
            _songsGrid.IsVisible = true;

        }
        catch (Exception ex)
        {
            ShowErrorMessage($"An error occurred: {ex.Message}");
        }
    }

    public void ShowErrorMessage(string message)
    {
        _playlistInfoGrid.IsVisible = true;
        _playlistName.Text = message;
        _playlistDescription.Text = null;
        _playlistAvatar.Source = null;
        _songsGrid.IsVisible = false;
    }

    private async void LoadButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await LoadPlaylist();
    }

    private static async Task<Bitmap?> LoadImageFromUrl(string url)
    {
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            return new Bitmap(stream);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
