using System.Linq;
using System.Windows;

namespace tool_cantam.Helpers;

/// <summary>
/// Dosya sürükle-bırak işlevini XAML tarafında bildirimsel olarak kullanmaya olanak tanır.
/// Code-behind'da tekrar eden Border_Drop/MouseLeftButtonDown kodunu ortadan kaldırır.
///
/// Kullanım örneği:
///   <Border helpers:FileDragDropBehavior.IsEnabled="True"
///           helpers:FileDragDropBehavior.FilePath="{Binding ViewModel.FilePath, Mode=TwoWay}" />
///
/// Alternatif: Microsoft.Xaml.Behaviors.Wpf paketi ile ayni sonuc elde edilebilir.
/// Bu yaklasim ek bagimliligi onlemek icin saf Attached Property kullanir.
/// </summary>
public static class FileDragDropBehavior
{
    #region IsEnabled Attached Property

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(FileDragDropBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    #endregion

    #region FilePath Attached Property

    public static readonly DependencyProperty FilePathProperty =
        DependencyProperty.RegisterAttached(
            "FilePath",
            typeof(string),
            typeof(FileDragDropBehavior),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static string GetFilePath(DependencyObject obj) => (string)obj.GetValue(FilePathProperty);
    public static void SetFilePath(DependencyObject obj, string value) => obj.SetValue(FilePathProperty, value);

    #endregion

    /// <summary>
    /// IsEnabled degistiginde olay dinleyicilerini baglar veya koparir.
    /// </summary>
    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // UIElement yerine FrameworkElement kontrolü yapıyoruz aşkım
        if (d is not FrameworkElement element)
            return;

        if ((bool)e.NewValue)
        {
            element.AllowDrop = true;
            element.Drop += OnDrop;
            element.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;

            // Artık Unloaded olayına erişebiliriz
            element.Unloaded += OnElementUnloaded;
        }
        else
        {
            element.AllowDrop = false;
            element.Drop -= OnDrop;
            element.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
            element.Unloaded -= OnElementUnloaded;
        }
    }

    // Temizlik işlemini yapan yardımcı metot
    private static void OnElementUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.Drop -= OnDrop;
            element.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
            element.Unloaded -= OnElementUnloaded;
        }
    }

    /// <summary>
    /// Dosya surukleme ile birakildiginda ilk dosyanin yolunu FilePath property'sine yazar.
    /// </summary>
    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is not DependencyObject obj)
            return;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Any())
            {
                SetFilePath(obj, files.First());
            }
        }
    }

    /// <summary>
    /// Sol tıklama ile OpenFileDialog açar ve seçilen dosyanın yolunu FilePath property'sine yazar.
    /// IFileExportService yerine burada doğrudan diyalog kullanılır çünkü
    /// bu bir UI-davranış (behavior) sınıfıdır, ViewModel değil.
    /// </summary>
    private static void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not DependencyObject obj)
            return;

        var openFileDialog = new Microsoft.Win32.OpenFileDialog();
        if (openFileDialog.ShowDialog() == true)
        {
            SetFilePath(obj, openFileDialog.FileName);
        }
    }
}
