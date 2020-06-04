using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Gladkoe.NavisGeometryListener.Server;
using Gladkoe.NavisGeometryListener.Server.Entities;

namespace Gladkoe.NavisGeometryListener.Views
{
    /// <summary>
    /// Interaction logic for ServerView.xaml
    /// </summary>
    public partial class ServerView : Window
    {
        private readonly WcfServer _server;

        public ServerView()
        {
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