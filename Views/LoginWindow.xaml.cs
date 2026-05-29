using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SwanCode.Core.Branding;
using SwanCode.Core.Services.Api;

namespace SwanCode.Core.Views
{
    public partial class LoginWindow : Window
    {
        private static readonly Regex UserKeyFormat =
            new(@"^swan_uk_[a-f0-9]{8}_[a-f0-9]{32}$", RegexOptions.IgnoreCase);

        private readonly ApiClient _api;
        private readonly IProductBranding _branding;
        private bool _isBusy;

        /// <summary>
        /// После успешного логина — введённый user-ключ. Сохраняется в config.
        /// </summary>
        public string UserKey { get; private set; } = string.Empty;

        public LoginWindow(IProductBranding branding, ApiClient api)
        {
            _branding = branding ?? throw new ArgumentNullException(nameof(branding));
            _api = api ?? throw new ArgumentNullException(nameof(api));
            InitializeComponent();
            // Заголовок окна — из брендинга (Core не знает имени конкретного продукта).
            Title = _branding.DisplayName;
            Loaded += (_, _) => KeyBox.Focus();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await DoLoginAsync();
        }

        private void KeyBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                _ = DoLoginAsync();
        }

        private async Task DoLoginAsync()
        {
            if (_isBusy) return;

            var key = KeyBox.Password.Trim();

            if (!UserKeyFormat.IsMatch(key))
            {
                ShowError(Localize("str_InvalidKeyFormat"));
                return;
            }

            _isBusy = true;
            LoginButton.IsEnabled = false;
            ErrorText.Visibility = Visibility.Collapsed;
            StatusText.Text = Localize("str_LoggingIn");
            StatusText.Visibility = Visibility.Visible;

            // Временно ставим ключ и проверяем через GetMeAsync. При неудаче откатываем.
            var previousKey = _api.UserKey;
            _api.UserKey = key;

            try
            {
                var me = await _api.GetMeAsync();
                if (me.ServiceInfo.Success)
                {
                    UserKey = key;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    _api.UserKey = previousKey;
                    ShowError(MapErrorCode(me.ServiceInfo.ErrorCode));
                }
            }
            catch (ApiException ex)
            {
                _api.UserKey = previousKey;
                ShowError(MapErrorCode(ex.ErrorCode, ex.Message));
            }
            catch (Exception ex)
            {
                _api.UserKey = previousKey;
                ShowError(ex.Message);
            }
            finally
            {
                _isBusy = false;
                LoginButton.IsEnabled = true;
                StatusText.Visibility = Visibility.Collapsed;
            }
        }

        private string MapErrorCode(string? code, string? fallback = null)
        {
            return code switch
            {
                "UNAUTHORIZED" or "USER_REQUIRED" => Localize("str_InvalidUserKey"),
                "USER_DELETED" => Localize("str_UserDeleted"),
                "PRODUCT_REQUIRED" => Localize("str_NoProductKey"),
                "CONNECTION_ERROR" => Localize("str_ErrorConnection"),
                _ => fallback ?? code ?? Localize("str_InvalidUserKey")
            };
        }

        private string Localize(string key)
        {
            return (string)FindResource(key) ?? key;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
