using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

public static class RtsActionReplaySettingsWindow
{
    public static void Show(RtsUI ui, string theme)
    {
        Exception error = null;
        var t = new System.Threading.Thread(() =>
        {
            try
            {
                var build = typeof(RtsUI).GetMethod("BuildWindow",
                    BindingFlags.Instance | BindingFlags.NonPublic, null,
                    Type.EmptyTypes, null);
                if (build == null) throw new MissingMethodException("RtsUI.BuildWindow");

                var w = (Window)build.Invoke(ui, null);
                var applySections = typeof(RtsUI).GetMethod("ApplySections",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (applySections != null)
                    applySections.Invoke(ui, new object[] { w });

                ApplyTransferTheme(w, theme);
                ApplyTransferCards(w);
                w.ShowDialog();
            }
            catch (Exception ex) { error = ex.InnerException ?? ex; }
        });
        t.SetApartmentState(System.Threading.ApartmentState.STA);
        t.IsBackground = false;
        t.Start();
        t.Join();
        if (error != null) throw error;
    }

    static void ApplyTransferTheme(Window w, string theme)
    {
        var light = !string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase);
        w.Background = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(light ? "#FFFFFF" : "#1E1E1E"));
        w.Tag = light ? "Light" : "Dark";
        RtsUITheme.Initialize();
        RtsUITheme.Apply(w, light);
    }

    static void ApplyTransferCards(Window w)
    {
        var sections = new System.Collections.Generic.List<GroupBox>();
        CollectSections(w, sections);
        foreach (var box in sections) ReplaceSection(box, w);
    }

    static void CollectSections(DependencyObject root,
        System.Collections.Generic.List<GroupBox> result)
    {
        foreach (var childObject in LogicalTreeHelper.GetChildren(root))
        {
            var child = childObject as DependencyObject;
            if (child == null) continue;
            var box = child as GroupBox;
            if (box != null &&
                Convert.ToString(box.Tag).StartsWith("__rts_section:",
                    StringComparison.Ordinal))
                result.Add(box);
            CollectSections(child, result);
        }
    }

    static void ReplaceSection(GroupBox box, Window w)
    {
        var sectionBg = w.Resources["duhBuhSectionBackground"] as Brush;
        var sectionBorder = w.Resources["duhBuhSectionBorder"] as Brush;
        var sectionText = w.Resources["duhBuhSectionText"] as Brush;
        var accent = w.Resources["duhBuhAccent"] as Brush;
        var title = (box.Header as TextBlock)?.Text ?? Convert.ToString(box.Header);

        var content = new StackPanel();
        content.Children.Add(new Border
        {
            Height = 3,
            Background = accent,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 8)
        });
        content.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = sectionText,
            Background = sectionBg,
            Padding = new Thickness(10, 7, 10, 7),
            Margin = new Thickness(0, 0, 0, 10),
            HorizontalAlignment = HorizontalAlignment.Stretch
        });

        var body = box.Content as UIElement;
        if (body != null) content.Children.Add(body);

        var card = new Border
        {
            Background = sectionBg,
            BorderBrush = sectionBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 8, 14, 10),
            Margin = new Thickness(0, 0, 0, 14),
            Child = content,
            Tag = box.Tag
        };

        var parent = LogicalTreeHelper.GetParent(box) as Panel;
        if (parent == null) return;
        var index = parent.Children.IndexOf(box);
        parent.Children.RemoveAt(index);
        parent.Children.Insert(index, card);
    }
}
