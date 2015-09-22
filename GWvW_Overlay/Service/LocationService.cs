using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.PeerResolvers;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using GWvW_Overlay.Annotations;
using GWvW_Overlay.DataModel;
using GWvW_Overlay.Properties;
using GWvW_Overlay_Location_Server_Contracts;
using MumbleLink_CSharp_GW2;

namespace GWvW_Overlay.Service
{
    [CallbackBehavior(UseSynchronizationContext = false)]
    public class LocationService : ILocationServiceCallBack, INotifyPropertyChanged
    {
        public static WvwMatch_ Match;

        public delegate void ReceivedPositionsArgs(List<GW2Link.Coordinates> positions);
        public event ReceivedPositionsArgs ReceivedPositions;

        private Guid _clientId = Guid.Empty;
        private DuplexChannelFactory<ILocationService> _channel;

        private Timer _updatePosittionTimer = new Timer(25);
        private ObservableCollection<GW2Link.Coordinates> _positions = new ObservableCollection<GW2Link.Coordinates>();


        private ILocationService _service;

        public ILocationService Service
        {
            get { return _service ?? (_service = Connection._channel.CreateChannel()); }
            private set { _service = value; }
        }


        public static LocationService Connection { get { return Nested.Instance; } }

        public ObservableCollection<GW2Link.Coordinates> Positions
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
            try
            {
                _channel = new DuplexChannelFactory<ILocationService>(this, "default");
                _channel.Faulted += ResetChannel;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            _updatePosittionTimer.Elapsed += PushPosition;
        }

        public void StartSending()
        {
            if (Settings.Default.player_api_key == String.Empty ||
                String.IsNullOrWhiteSpace(Settings.Default.player_api_key) || !Settings.Default.player_position_sharing)
            {
                return;
            }
            var coords = MainWindow.DataLink.GetCoordinates();
            try
            {
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
            }
            catch (Exception)
            {
                ResetChannel(this, EventArgs.Empty);
            }
            _updatePosittionTimer.Start();
        }

        private void PushPosition(object sender, EventArgs e)
        {
            var coords = MainWindow.DataLink.GetCoordinates();
            try
            {
                Service.SendPosition(_clientId, new Position()
                {
                    MapId = coords.MapId,
                    X = coords.X,
                    Y = coords.Y
                });
            }
            catch (Exception)
            {
                ResetChannel(this, EventArgs.Empty);
            }

        }

        public static void ResetChannel(object sender, EventArgs eventArgs)
        {
            try
            {
                Connection._clientId = Guid.Empty;
                Connection._channel = new DuplexChannelFactory<ILocationService>(sender, "default");
                Connection._channel.Faulted += ResetChannel;
                Connection._channel.Closed += ResetChannel;
                Connection.Service = Connection._channel.CreateChannel();
                Connection.StartSending();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        public void ReceivePositions(List<Position> positions)
        {
            if (Match == null)
            {
                return;
            }
            var correctedPositions = positions.AsParallel()
                                              .Select(p => Match.PlayerPositions.Transform(new GW2Link.Coordinates
            {
                X = p.X,
                Y = p.Y,
                MapId = p.MapId
            })).ToList();


            if (ReceivedPositions != null)
            {
                ReceivedPositions(correctedPositions);
            }
            Positions = new ObservableCollection<GW2Link.Coordinates>(correctedPositions);
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