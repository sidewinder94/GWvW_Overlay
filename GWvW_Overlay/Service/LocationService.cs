using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.PeerResolvers;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using GWvW_Overlay.Annotations;
using GWvW_Overlay.Properties;
using GWvW_Overlay_Location_Server_Contracts;

namespace GWvW_Overlay.Service
{
    [CallbackBehavior(UseSynchronizationContext = false)]
    public class LocationService :ILocationServiceCallBack, INotifyPropertyChanged
    {
        public delegate void ReceivedPositionsArgs(List<Position> positions);
        public event ReceivedPositionsArgs ReceivedPositions;

        private Guid _clientId;
        private DuplexChannelFactory<ILocationService> _channel;

        private Timer _updatePosittionTimer = new Timer(25);
        private ObservableCollection<Position> _positions = new ObservableCollection<Position>();

        public ILocationService Service { get; private set; }


        public static LocationService Connection { get { return Nested.Instance; } }

        public ObservableCollection<Position> Positions
        {
            get { return _positions; }
            set
            {
                _positions = value;
                OnPropertyChanged();
            }

        }

        private static class Nested
        {
            public static readonly LocationService Instance = new LocationService();
        }

        private LocationService()
        {
            ResetChannel(null, null);
            _updatePosittionTimer.Elapsed += PushPosition;
            StartSending();
        }

        public void StartSending()
        {
            if (Settings.Default.player_api_key == String.Empty ||
                String.IsNullOrWhiteSpace(Settings.Default.player_api_key) || !Settings.Default.player_position_sharing)
            {
                return;
            }
            var coords = MainWindow.DataLink.GetCoordinates();
            _clientId = Service.SubscribeClient(new Client()
            {
                AnetAccountApiKey = Settings.Default.player_api_key,
                Position = new Position()
                {
                    X = coords.X,
                    Y = coords.Y,
                    MapId = coords.MapId
                }
            });

            _updatePosittionTimer.Start();
        }

        private void PushPosition(object sender, EventArgs e)
        {
            var coords = MainWindow.DataLink.GetCoordinates();
            Service.SendPosition(_clientId, new Position()
            {
                MapId = coords.MapId,
                X = coords.X,
                Y = coords.Y
            });

        }

        private static void ResetChannel(object sender, EventArgs eventArgs)
        {
            try
            {
                Connection._channel = new DuplexChannelFactory<ILocationService>(Connection, "default");
                Connection._channel.Faulted += ResetChannel;
                Connection.Service = Connection._channel.CreateChannel();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        public void ReceivePositions(List<Position> positions)
        {
            if (ReceivedPositions != null)
            {
                ReceivedPositions(positions);
            }
            Positions = new ObservableCollection<Position>(positions);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}