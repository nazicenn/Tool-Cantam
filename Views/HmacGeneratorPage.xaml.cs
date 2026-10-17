using System.ComponentModel;
using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

/// <summary>
/// HMAC Generator sayfası.
/// <para>
/// Anahtar varsayılan olarak <see cref="System.Windows.Controls.PasswordBox"/> ile maskelenir;
/// parola kutusu bağlanamadığı için değer code-behind üzerinden ViewModel'e aktarılır.
/// "Anahtarı göster" seçildiğinde normal metin kutusu görünür ve aynı özelliğe bağlıdır.
/// </para>
/// </summary>
public partial class HmacGeneratorPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.HmacGeneratorViewModel>
{
    public ViewModels.HmacGeneratorViewModel ViewModel { get; }

    public HmacGeneratorPage(ViewModels.HmacGeneratorViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = this;

        ViewModel.RequestKeyFocus += FocusKey;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ViewModels.HmacGeneratorViewModel.SecretKey):
                // ViewModel'den gelen değer (temizleme) parola alanına yansıtılır; döngü oluşmaz.
                if (KeyPasswordBox.Password != ViewModel.SecretKey)
                    KeyPasswordBox.Password = ViewModel.SecretKey;
                break;

            case nameof(ViewModels.HmacGeneratorViewModel.ShowKey):
                if (!ViewModel.ShowKey)
                    KeyPasswordBox.Password = ViewModel.SecretKey;
                break;
        }
    }

    private void KeyPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e) =>
        ViewModel.SecretKey = KeyPasswordBox.Password;

    private void FocusKey()
    {
        if (!IsLoaded)
            return;

        if (ViewModel.ShowKey)
            KeyTextBox.Focus();
        else
            KeyPasswordBox.Focus();
    }
}
