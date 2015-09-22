using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using GWvW_Overlay.Annotations;
using MumbleLink_CSharp_GW2;

namespace GWvW_Overlay.DataModel
{
    public class Positions : INotifyPropertyChanged
    {

        private double _canvasHeight;
        private double _canvasWidth;
        private readonly Timer _refreshData = new Timer(20.0)
        {
            AutoReset = true,
        };
        public GW2Link.Coordinates Player
        {
            get { return Transform(MainWindow.DataLink.GetCoordinates()); }
        }


        public double PlayerRotation
        {
            get
            {
                return PlayerRotationAngle();
            }
        }

        public Visibility PlayerVisibility
        {
            get
            {
                if (Map.KnownMap(MainWindow.DataLink.GetCoordinates().MapId) &&
                    Properties.Settings.Default.player_position)
                {
                    return Visibility.Visible;
                }
                return Visibility.Hidden;
            }
        }

        public double CanvasHeight
        {
            get { return _canvasHeight; }
            set
            {
                _canvasHeight = value;
                OnPropertyChanged();
            }
        }

        public double CanvasWidth
        {
            get { return _canvasWidth; }
            set
            {
                _canvasWidth = value;
                OnPropertyChanged();
            }
        }


        public Positions()
        {

            _refreshData.Enabled = true;
            _refreshData.Elapsed += (sender, args) =>
            {
                OnPropertyChanged("Player");
                OnPropertyChanged("PlayerRotation");
                OnPropertyChanged("PlayerVisibility");
            };
            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName != "Player" && args.PropertyName != "PlayerRotation" && args.PropertyName != "PlayerVisibility")
                {
                    OnPropertyChanged("Player");

                }
            };
        }


        public double PlayerRotationAngle()
        {
            var data = MainWindow.DataLink.Read();

            var defaultVector = new Point(0, 1);
            var playerVector = new Point(data.FAvatarFront[0], data.FAvatarFront[2]);

            var result = Math.Atan2(defaultVector.Y, defaultVector.X) - Math.Atan2(playerVector.Y, playerVector.X);

            if (result < 0)
            {
                result += 2 * Math.PI;
            }

            return result * (180.0 / Math.PI);
        }



        public GW2Link.Coordinates Transform(GW2Link.Coordinates nativeCoordinates)
        {
            if (nativeCoordinates.MapId == 0) return nativeCoordinates;

            var mapId = nativeCoordinates.MapId;

            var mapSize = MapInfo.GetMapInfo(mapId).MapRect;

            var mapSizeX = Math.Abs(mapSize[0][0]) + Math.Abs(mapSize[1][0]);
            var mapSizeY = Math.Abs(mapSize[0][1]) + Math.Abs(mapSize[1][1]);

            nativeCoordinates.X = Math.Abs(CanvasWidth * ((nativeCoordinates.X + mapSizeX / 2.0) / mapSizeX)) - 25.0 / 2.0;
            nativeCoordinates.Y = Math.Abs(CanvasHeight * ((nativeCoordinates.Y - mapSizeY / 2.0) / mapSizeY)) - 29.0 / 2.0;

            return nativeCoordinates;
        }



        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}