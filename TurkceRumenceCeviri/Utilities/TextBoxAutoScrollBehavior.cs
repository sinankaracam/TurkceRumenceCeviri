using System;
using System.Windows;
using System.Windows.Controls;

namespace TurkceRumenceCeviri.Utilities;

public static class TextBoxAutoScrollBehavior
{
    public static readonly DependencyProperty AutoScrollToEndProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollToEnd",
            typeof(bool),
            typeof(TextBoxAutoScrollBehavior),
            new PropertyMetadata(false, OnAutoScrollToEndChanged));

    public static void SetAutoScrollToEnd(DependencyObject element, bool value) =>
        element.SetValue(AutoScrollToEndProperty, value);

    public static bool GetAutoScrollToEnd(DependencyObject element) =>
        (bool)element.GetValue(AutoScrollToEndProperty);

    private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb)
            return;

        if ((bool)e.NewValue)
        {
            tb.TextChanged += TextBox_TextChanged;
            tb.Loaded += TextBox_Loaded;
        }
        else
        {
            tb.TextChanged -= TextBox_TextChanged;
            tb.Loaded -= TextBox_Loaded;
        }
    }

    private static void TextBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
            tb.ScrollToEnd();
    }

    private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
            tb.ScrollToEnd();
    }
}
