﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DesignScript.Editor.Common
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        string eulaFilePath = string.Empty;
        bool ignoreClose = false;
        ILoggerWrapper logger = null;

        public AboutWindow(ILoggerWrapper logger)
        {
            InitializeComponent();
            this.logger = logger;
            this.InstallNewUpdate = false;
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);
        }

        public bool InstallNewUpdate { get; private set; }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //Request a check for update version info
            DisplayVersionInformation(null);
            UpdateManager.Instance.UpdateDownloaded += new UpdateDownloadedEventHandler(OnUpdatePackageDownloaded);
            UpdateManager.Instance.CheckForProductUpdate();

            string executingAssemblyPathName = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string rootModuleDirectory = System.IO.Path.GetDirectoryName(executingAssemblyPathName);
            eulaFilePath = System.IO.Path.Combine(rootModuleDirectory, "DesignScriptLauncher.exe");
            if (!File.Exists(eulaFilePath))
                ViewLicenseTextBlock.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void OnWindowClick(object sender, MouseButtonEventArgs e)
        {
            if (ignoreClose)
                ignoreClose = false;
            else
                Close();
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.ignoreClose = true;
        }

        private void OnClickLink(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://labs.autodesk.com/utilities/designscript");
        }

        private void OnViewLicense(object sender, RoutedEventArgs e)
        {
            if (File.Exists(eulaFilePath))
                System.Diagnostics.Process.Start(eulaFilePath, "EULA");
        }

        private void OnUpdatePackageDownloaded(object sender, UpdateDownloadedEventArgs e)
        {
            DisplayVersionInformation(e);
        }

        private void OnUpdateInfoMouseUp(object sender, MouseButtonEventArgs e)
        {
            logger.LogInfo("AboutWindow-OnUpdateInfoMouseUp", "AboutWindow-OnUpdateInfoMouseUp");
            this.InstallNewUpdate = true;
            this.Close();
        }

        private void DisplayVersionInformation(UpdateDownloadedEventArgs e)
        {
            UpdateManager manager = UpdateManager.Instance;
            string version = manager.ProductVersion.ToString();

#if (DEBUG)
            this.VersionNumber.Text = string.Format("Version: {0} (Debug)", version);
#else
            this.VersionNumber.Text = string.Format("Version: {0}", version);
#endif

            if ((null != e) && e.UpdateAvailable)
            {
                string latest = manager.AvailableVersion.ToString();
                this.UpdateInfo.Foreground = Brushes.OrangeRed;
                this.UpdateInfo.Text = string.Format("[Latest vesion: {0}]", latest);

                this.UpdateInfo.Cursor = Cursors.Hand;
                this.UpdateInfo.MouseUp += new MouseButtonEventHandler(OnUpdateInfoMouseUp);
            }
            else
            {
                this.UpdateInfo.Foreground = Brushes.Green;
                this.UpdateInfo.Text = "[Up-to-date]";

                this.UpdateInfo.Cursor = Cursors.Arrow;
                this.UpdateInfo.MouseUp -= new MouseButtonEventHandler(OnUpdateInfoMouseUp);
            }
        }
    }
}