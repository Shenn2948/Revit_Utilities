using System;
using System.Windows;
using RevitUtils.Geometry.NavisGeometryListener.Server;
using RevitUtils.Geometry.NavisGeometryListener.Server.Entities;

namespace RevitUtils.Geometry.NavisGeometryListener.Views
{
    /// <summary>
    /// Interaction logic for ServerView.xaml
    /// </summary>
    public partial class ServerView : Window
    {
        private readonly WcfServer _server;

        public ServerView()
        {
            InitializeComponent();

            _server = new WcfServer();
            _server.Received += ServerOnReceived;
        }

        private void ServerOnReceived(object sender, DataReceivedEventArgs e)
        {
            var s = e.Data;
        }

        private void StopServerBtn_OnClick(object sender, RoutedEventArgs e)
        {
            _server.Stop();
        }

        private void StartServerBtn_OnClick(object sender, RoutedEventArgs e)
        {
            _server.Start();
        }

        private void ServerView_OnClosed(object sender, EventArgs e)
        {
            _server.Stop();
        }
    }
}