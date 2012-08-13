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
    public class GameType
    {
        public readonly string name;
        public readonly Type type;
        public readonly int id;

        public GameType(Type type, string name = "default", int id = 0)
        {
            this.type = type;
            this.name = name;
            this.id = id;
        }
        public GameType(string name = "default", int id = 0)
        {
            this.type = typeof(Game);
            this.name = name;
            this.id = id;
        }
    }
}
