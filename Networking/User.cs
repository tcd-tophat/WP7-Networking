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
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Tophat
{
    public class User
    {
        public string name { get; set; }
        public string created;
        public string photo;
        public List<Game> joined_games;
        public string id { get; set; }
        public string email;
        public bool registered;

        public bool Equals(User user)
        {
            return this.id == user.id;
        }
    }
}

