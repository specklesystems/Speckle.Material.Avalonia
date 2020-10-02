﻿using System;
using System.Globalization;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace MaterialXamlToolKit.Avalonia.Assists {
    public static class ShadowProvider {
        public static Color MaterialShadowColor { get; set; } = Color.FromArgb(107, 0, 0, 0);

        public static BoxShadows ToBoxShadows(this ShadowDepth shadowDepth, Color? overridedColor = null) {
            return shadowDepth switch {
                ShadowDepth.Depth0 => new BoxShadows(new BoxShadow()),
                ShadowDepth.Depth1 => new BoxShadows(new BoxShadow {Blur = 5, OffsetX = 1, OffsetY = 1, Color = overridedColor ?? MaterialShadowColor}),
                ShadowDepth.Depth2 => new BoxShadows(new BoxShadow {Blur = 8, OffsetX = 1.5, OffsetY = 1.5, Color = overridedColor ?? MaterialShadowColor}),
                ShadowDepth.Depth3 => new BoxShadows(new BoxShadow {Blur = 14, OffsetX = 4.5, OffsetY = 4.5, Color = overridedColor ?? MaterialShadowColor}),
                ShadowDepth.Depth4 => new BoxShadows(new BoxShadow {Blur = 25, OffsetX = 8, OffsetY = 8, Color = overridedColor ?? MaterialShadowColor}),
                ShadowDepth.Depth5 => new BoxShadows(new BoxShadow {Blur = 35, OffsetX = 13, OffsetY = 13, Color = overridedColor ?? MaterialShadowColor}),
                _                  => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public enum ShadowDepth {
        Depth0,
        Depth1,
        Depth2,
        Depth3,
        Depth4,
        Depth5
    }

    public static class ShadowAssist {
        static ShadowAssist() {
            ShadowDepthProperty.Changed.Subscribe(ShadowDepthChangedCallback);
            DarkenProperty.Changed.Subscribe(DarkenPropertyChangedCallback);
        }
        
        public static readonly AvaloniaProperty<ShadowDepth> ShadowDepthProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, ShadowDepth>(
            "ShadowDepth", typeof(ShadowAssist), ShadowDepth.Depth0, true);

        private static void ShadowDepthChangedCallback(AvaloniaPropertyChangedEventArgs args) {
            if (args.Sender is Border border) {
                border.BoxShadow = (args.NewValue as ShadowDepth? ?? ShadowDepth.Depth0).ToBoxShadows();
            }
        }
        
        public static void SetShadowDepth(AvaloniaObject element, ShadowDepth value) {
            element.SetValue(ShadowDepthProperty, value);
        }

        public static ShadowDepth GetShadowDepth(AvaloniaObject element) {
            return (ShadowDepth) element.GetValue(ShadowDepthProperty);
        }

        public static readonly AvaloniaProperty<bool> DarkenProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, bool>(
            "Darken", typeof(ShadowAssist));

        private static void DarkenPropertyChangedCallback(AvaloniaPropertyChangedEventArgs obj) {
            var border = obj.Sender as Border;
            var boxShadow = border?.BoxShadow;

            if (boxShadow == null) return;

            var targetBoxShadows = (bool?) obj.NewValue == true
                ? GetShadowDepth((AvaloniaObject) obj.Sender).ToBoxShadows(Color.FromArgb(255, 0, 0, 0))
                : GetShadowDepth((AvaloniaObject) obj.Sender).ToBoxShadows();

            var animation = new Animation() {Duration = TimeSpan.FromMilliseconds(350), FillMode = FillMode.Both};
            animation.Children.Add(
                new KeyFrame {
                    Cue = Cue.Parse("0%", CultureInfo.CurrentCulture),
                    Setters = {
                        new Setter {Property = Border.BoxShadowProperty, Value = boxShadow}
                    }
                });
            animation.Children.Add(
                new KeyFrame {
                    Cue = Cue.Parse("100%", CultureInfo.CurrentCulture),
                    Setters = {
                        new Setter {Property = Border.BoxShadowProperty, Value = targetBoxShadows}
                    }
                });
            animation.RunAsync(border);
        }

        public static void SetDarken(AvaloniaObject element, bool value) {
            element.SetValue(DarkenProperty, value);
        }

        public static bool GetDarken(AvaloniaObject element) {
            return (bool) element.GetValue(DarkenProperty);
        }
    }
}