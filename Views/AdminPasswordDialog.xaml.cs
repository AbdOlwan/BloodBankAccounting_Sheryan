using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace BloodBankAccounting.Views;

public partial class AdminPasswordDialog : Window
{
    // SHA-256 لكلمة المرور: kadkksjfkaj;ajkfjdkaj;j
    // لتغيير الكلمة: شغّل → python3 -c "import hashlib; print(hashlib.sha256('كلمتك'.encode()).hexdigest())"
    private const string CorrectHash = "3f8e20a6f47525989ccec1c73cfc0e0733863cc6abbbcdce7d146f68095a96cf";

//    لو عايز تغيّر كلمة السر مستقبلاً
//python3 -c "import hashlib; print(hashlib.sha256('كلمتك_الجديدة'.encode()).hexdigest())"
    public bool Authenticated { get; private set; }

    public AdminPasswordDialog() => InitializeComponent();

    private void BtnConfirm_Click(object sender, RoutedEventArgs e) => TryAuthenticate();
    private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

    private void PwdBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) TryAuthenticate();
        if (e.Key == Key.Escape) Close();
    }

    private void TryAuthenticate()
    {
        var input = PwdBox.Password;
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLower();

        if (hash == CorrectHash)
        {
            Authenticated = true;
            Close();
        }
        else
        {
            TxtError.Visibility = Visibility.Visible;
            PwdBox.Clear();
            PwdBox.Focus();

            // اهزّ النافذة
            ShakeWindow();
        }
    }

    // تأثير اهتزاز بسيط عند الخطأ
    private async void ShakeWindow()
    {
        var original = Left;
        int[] offsets = [10, -10, 8, -8, 5, -5, 0];
        foreach (var offset in offsets)
        {
            Left = original + offset;
            await Task.Delay(40);
        }
        Left = original;
    }
}