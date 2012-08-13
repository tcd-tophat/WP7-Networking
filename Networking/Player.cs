using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Tophat
{
    public class Player
    {
        public User player_user;
        public Game game;
        public string name { get; set; }
        public int? score;
        public DateTime time;
        public string photo;
        public int id { get; set; }
        public float? lattitude;
        public float? longitude;
        
    }
}
