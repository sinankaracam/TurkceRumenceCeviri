using System.Windows;
using TurkceRumenceCeviri.ViewModels;

namespace TurkceRumenceCeviri;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void SetDataContext(MainViewModel viewModel)
    {
        DataContext = viewModel;
    }
}