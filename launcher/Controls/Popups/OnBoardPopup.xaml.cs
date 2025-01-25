using launcher.Classes.BranchUtils;
using System.Windows;
using System.Windows.Controls;
using launcher.Classes.Global;
using launcher.Classes.Game;
using launcher.Classes.Utilities;
using launcher.Classes.Managers;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using Hardcodet.Wpf.TaskbarNotification;
using static launcher.Classes.Global.References;
using System.Windows.Media;
using System.Numerics;
using System.Windows.Media.Animation;
using System;

namespace launcher
{
    public partial class OnBoardPopup : UserControl
    {
        private int currentIndex = 0;

        public OnBoardPopup()
        {
            InitializeComponent();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (currentIndex + 1 >= DataCollections.OnBoardControlPos.Count)
            {
                OnBoard_Control.Visibility = Visibility.Hidden;
                OnBoardingRect.Visibility = Visibility.Hidden;
                AppState.OnBoarding = false;
                Main_Window.ResizeMode = ResizeMode.CanResize;
                SetItem(0);
                return;
            }

            SetItem(currentIndex + 1);
        }

        public void SetItem(int index)
        {
            currentIndex = index;

            Next.Content = "Next";
            Skip.Visibility = Visibility.Visible;

            if (currentIndex == DataCollections.OnBoardControlPos.Count - 1)
            {
                Next.Content = "Finish";
                Skip.Visibility = Visibility.Hidden;
            }

            Title.Text = DataCollections.OnBoardTitles[index];
            Desc.Text = DataCollections.OnBoardDescs[index];
            Page.Text = $"{index + 1} of {DataCollections.OnBoardControlPos.Count}";

            if (OnBoard_Control.RenderTransform is TransformGroup transformGroup)
            {
                var translateTransform = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();
                AnimateTranslate(translateTransform, DataCollections.OnBoardControlPos[index]);
                AnimateGeoRect(OnBoardingClip, DataCollections.OnboardGeoRects[index]);
            }
        }

        private void AnimateTranslate(TranslateTransform translateTransform, Vector2 xy)
        {
            if (translateTransform == null)
            {
                System.Diagnostics.Debug.WriteLine("TranslateTransform is null. Cannot animate.");
                return;
            }

            // Determine animation speed
            double speed = (bool)Ini.Get(Ini.Vars.Disable_Transitions) ? 1 : 400;

            // Animate X property
            var moveXAnimation = new DoubleAnimation
            {
                From = translateTransform.X,
                To = xy.X,
                Duration = new Duration(TimeSpan.FromMilliseconds(speed)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            // Animate Y property
            var moveYAnimation = new DoubleAnimation
            {
                From = translateTransform.Y,
                To = xy.Y,
                Duration = new Duration(TimeSpan.FromMilliseconds(speed)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            // Apply animations directly
            translateTransform.BeginAnimation(TranslateTransform.XProperty, moveXAnimation);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, moveYAnimation);
        }

        private void AnimateGeoRect(RectangleGeometry geo, Rect newRect)
        {
            if (geo == null)
            {
                System.Diagnostics.Debug.WriteLine("RectangleGeometry is null. Cannot animate.");
                return;
            }

            // Ensure the geometry is not frozen
            if (geo.IsFrozen)
            {
                // Clone the geometry to make it unfrozen
                geo = geo.Clone();
            }

            double speed = (bool)Ini.Get(Ini.Vars.Disable_Transitions) ? 1 : 400;

            var rectAnimation = new RectAnimation
            {
                From = geo.Rect,
                To = newRect,
                Duration = new Duration(TimeSpan.FromMilliseconds(speed)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            // Apply the animation directly
            geo.BeginAnimation(RectangleGeometry.RectProperty, rectAnimation);
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            OnBoard_Control.Visibility = Visibility.Hidden;
            OnBoardingRect.Visibility = Visibility.Hidden;
            AppState.OnBoarding = false;
            Main_Window.ResizeMode = ResizeMode.CanResize;
            SetItem(0);
        }
    }
}