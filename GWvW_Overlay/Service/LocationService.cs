using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using GWvW_Overlay.Annotations;
using GWvW_Overlay_Location_Server_Contracts;

namespace GWvW_Overlay.Service
{
    [CallbackBehavior(UseSynchronizationContext = false)]
    public class LocationService : ILocationServiceCallBack, INotifyPropertyChanged
    {
        public delegate void ReceivedPositionsArgs(List<Position> positions);
        public event ReceivedPositionsArgs ReceivedPositions;

        private DuplexChannelFactory<ILocationService> _channel;


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