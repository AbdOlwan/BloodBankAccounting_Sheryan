using System.Windows;
using System.Windows.Controls;

namespace BloodBankAccounting.Views;

/// <summary>
/// نافذة إدخال نص بسيطة — تُستخدم لطلب سبب الإلغاء وما شابهه.
/// تُعيد null إذا ضغط المستخدم إلغاء، أو النص المُدخل إذا أكّد.
/// </summary>
public static class InputDialog
{
    public static string? Show(string prompt, string title = "إدخال", Window? owner = null)
    {
        var dlg = new Window
        {
            Title = title,
            Width = 420,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,
            ResizeMode = ResizeMode.NoResize,
            FlowDirection = FlowDirection.RightToLeft,
            WindowStyle = WindowStyle.ToolWindow
        };

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var lbl = new TextBlock
        {
            Text = prompt,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14
        };
        Grid.SetRow(lbl, 0);

        var txt = new TextBox
        {
            FontSize = 14,
            Padding = new Thickness(6),
            MaxLength = 200
        };
        Grid.SetRow(txt, 2);

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left
            
        };
        Grid.SetRow(btnPanel, 4);

        string? result = null;

        var btnOk = new Button
        {
            Content = "تأكيد",
            Width = 90,
            Height = 34,
            FontSize = 13,
            IsDefault = true
        };
        btnOk.Click += (_, _) =>
        {
            result = txt.Text;
            dlg.DialogResult = true;
        };

        var btnCancel = new Button
        {
            Content = "إلغاء",
            Width = 90,
            Height = 34,
            FontSize = 13,
            IsCancel = true
        };
        btnCancel.Click += (_, _) => dlg.DialogResult = false;
        btnCancel.Margin = new Thickness(10, 0, 0, 0);

        btnPanel.Children.Add(btnOk);
        btnPanel.Children.Add(btnCancel);

        grid.Children.Add(lbl);
        grid.Children.Add(txt);
        grid.Children.Add(btnPanel);

        dlg.Content = grid;
        dlg.Loaded += (_, _) => txt.Focus();

        var confirmed = dlg.ShowDialog();
        return confirmed == true ? result : null;
    }
}