using System;
using System.Net;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Text;
using System.Collections.Generic;

namespace Tophat
{
    public class Game
    {
        public int id { get; private set; }
        public List<Player> players { get; private set; }
        public int maxplayers { get; private set; }
        public User creator { get; private set; }
        public DateTime time { get; private set; }
        public int gameType { get; private set; }
        public string name { get; private set; }
        public System.Collections.Generic.Dictionary<string, object> Field;

        public bool isPlaying { get; set; }

        public Game(int id, User creator, DateTime time, int gameType, string name, List<Player> players, int maxplayers)
        {
            this.id = id;
            this.players = players;
            this.creator = creator;
            this.time = time;
            this.gameType = gameType;
            this.name = name;
            this.maxplayers = maxplayers;
        }

        public bool Equals(Game game)
        {
            return this.id == game.id;
        }
    }

}