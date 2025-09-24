using SpectriumLive.Wpf.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace SpectriumLive.Wpf.View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel mainWindowViewModel)
    {
        InitializeComponent();
        DataContext = mainWindowViewModel;

        Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if ( DataContext is MainWindowViewModel viewModel )
        {
            if ( viewModel.StopCameraCommand.CanExecute(null) )
            {
                viewModel.StopCameraCommand.Execute(null);
            }
        }
    }
}
