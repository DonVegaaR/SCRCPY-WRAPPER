using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ScrcpyLauncher.App;

public partial class TextViewerWindow : Window
{
    public TextViewerWindow(object dataContext, string windowTitle, string propertyPath)
    {
        InitializeComponent();

        Title = windowTitle;
        CaptionText.Text = windowTitle;
        DataContext = dataContext;

        BindingOperations.SetBinding(
            BodyTextBox,
            TextBox.TextProperty,
            new Binding(propertyPath)
            {
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            });
    }
}
