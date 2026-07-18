using System.Windows;
using JellyfinPotPlayerShell.Core.Jellyfin;

namespace JellyfinPotPlayerShell.App;

public partial class MediaSourceSelectionWindow : Window
{
    public MediaSourceSelectionWindow(IReadOnlyList<JellyfinMediaSource> mediaSources)
    {
        InitializeComponent();
        MediaSourcesList.ItemsSource = mediaSources;
        MediaSourcesList.SelectedIndex = mediaSources.Count > 0 ? 0 : -1;
    }

    public JellyfinMediaSource? SelectedMediaSource { get; private set; }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (MediaSourcesList.SelectedItem is not JellyfinMediaSource mediaSource)
        {
            MessageBox.Show(
                this,
                "请先选择一个媒体版本。",
                "请选择播放版本",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        SelectedMediaSource = mediaSource;
        DialogResult = true;
    }
}
