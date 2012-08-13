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
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Tophat
{
    public class NetworkingNotInitializedException : Exception 
    {
        public NetworkingNotInitializedException(string msg) : base(msg) { }
    }

    public delegate void Request(EventHandler eventHandler);

    public partial class Networking
    {
        public static GameType[] GameTypes { get; private set; }

        public static User LocalUser { get; private set; }
        public static void SetLocalUser(string text)
        {
            LocalUser = JsonConvert.DeserializeObject<User>(text);
        }
        public static void SetLocalUser(User user)
        {
            LocalUser = user;
        }

        public static List<Player> Players { get; private set; }
        public static void SetPlayers(string text)
        {
            Players = JsonConvert.DeserializeObject<List<Player>>(text);
        }
        public static void SetPlayers(List<Player> players)
        {
            Players = players;
        }
        public static string GetPlayersJson()
        {
            return JsonConvert.SerializeObject(Players);
        }

        /// <summary>
        /// Makes sure a duplicate player isn't added
        /// </summary>
        /// <param name="p">The player to be added/updated</param>
        public static void AddPlayer(Player p)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                if (p.id == Players[i].id)
                {
                    Players[i] = p;
                    return;
                }
            }
            Players.Add(p);
        }

        public static Player GetPlayerByGameId(int GameId)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                if ((Players[i].game as Game).id == GameId)
                {
                    return Players[i];
                }
            }
            return null;
        }


        public static Game GetGameById(int GameId)
        {
            for (int i = 0; i < Games.Count; i++)
            {
                if (Games[i].id == GameId)
                {
                    return Games[i];
                }
            }
            return null;
        }

        public static bool isStopped
        {
            get { return retries >= MAX_RETRIES; }
        }
        private static int retries;
        private const int MAX_RETRIES = 0;

        private static Queue<Request> Requests;
        private static Queue<EventHandler> RequestCompletedEventHandlers;

        private static string _apitoken;
        public static string Apitoken
        {
            get
            {
                if (_apitoken != null)
                    return _apitoken;
                else
                    throw new NetworkingNotInitializedException("You must invoke the Init method first!");
            }
            set { _apitoken = value; }
        }

        private static string _url;
        public static string URL
        {
            get
            {
                if (_url != null)
                    return _url;
                else
                    throw new NetworkingNotInitializedException("You must invoke the Init method first!");
            }
        }

        public static Dictionary<string, object> _results;
        public static Dictionary<string, object> Results
        {
            get 
            {
                if (_results != null)
                    return _results;
                else
                    throw new NetworkingNotInitializedException("You must invoke the Init method first!");
            }
        }

        public static List<Game> Games { get; set; }

        /// <summary>
        /// This method must be invoked before any 
        /// </summary>
        /// <param name="url">The domain name of the server</param>
        /// <param name="port">The port number to access</param>
        public static void Init(string url, int port, GameType[] gameTypes)
        {
            _url = url + ":" + port;
            _results = new Dictionary<string, object>();
            if (gameTypes == null)
                GameTypes = new GameType[1] { new GameType() };
            else
                GameTypes = gameTypes;
            Requests = new Queue<Request>();
            RequestCompletedEventHandlers = new Queue<EventHandler>();
            Apitoken = "";
            Players = new List<Player>();
            Games = new List<Game>();
        }


        public static void ProcessError(WebException ex)
        {
            //ex.Response appeared as null here when the screen was locked and then unlocked
            //This prevents an unhandled exception
            if (ex.Response != null)
            {
                using (var reader = new System.IO.StreamReader(ex.Response.GetResponseStream()))
                {
                    string error = reader.ReadToEnd();

                    if (retries >= MAX_RETRIES - 1)
                    {
                        if (error == "") //No data implies no response from the server
                            MessageBox.Show("There was no response from the server. Please check that you are connected to the internet");
                        else
                        {
                            Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(error);
                            //Simply show the error for now
                            MessageBox.Show(String.Format("{0}:\n{1}",
                                dict["error_code"],
                                dict["error_message"]));
                        }
                    }
                }
            }
        }

        private void ParseResponse(object sender, EventArgs e)
        {
            lock (this)
            {
                try
                {
                    Dictionary<string, object> dict;

                    if (e.GetType() == typeof(UploadStringCompletedEventArgs))
                    {
                        var ev = (UploadStringCompletedEventArgs)e;
                        dict = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(ev.Result);
                        MessageBox.Show(ev.Result);
                    }
                    else
                    {
                        var ev = (DownloadStringCompletedEventArgs)e;
                        dict = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(ev.Result);
                        MessageBox.Show(ev.Result);
                    }

                    foreach (var keypair in dict)
                    {
                        if (keypair.Key == "apitoken")
                        {
                            Apitoken = (string)keypair.Value;
                        }
                        else if (keypair.Key == "user")
                        {
                            //The key "user" doesn't point to a string, so it has to be parsed
                            //to a string first and then converted to a string
                            LocalUser = JsonConvert.DeserializeObject<User>(dict["user"].ToString());
                        }
                        else if (keypair.Key == "games")
                        {
                            //The key "games" doesn't point to a string, so it has to be parsed
                            //as a list of objects and then converted to a string
                            object[] jsonGameList = JsonConvert.DeserializeObject<object[]>(dict["games"].ToString());
                            Games = new List<Game>();

                            for (int i = 0; i < jsonGameList.Length; i++)
                            {
                                try
                                {
                                    Games.Add(JsonConvert.DeserializeObject<Game>(jsonGameList[i].ToString()));
                                }
                                catch (Exception ex)
                                {
                                    if (MessageBox.Show("The game: \n" + jsonGameList[i].ToString() + "\n is invalid. \nView More Info?", "Error", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                                        MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                                }
                            }
                        }
                        else
                            Results[keypair.Key] = keypair.Value;
                    }
                }
                catch (WebException ex)
                {
                    Networking.ProcessError(ex);
                }
            }
        }

    }
}
