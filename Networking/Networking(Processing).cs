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
using System.IO;
using System.IO.IsolatedStorage;

namespace Tophat
{
    public class NetworkingNotInitializedException : Exception 
    {
        public NetworkingNotInitializedException(string msg) : base(msg) { }
    }

    public partial class Networking
    {
        public static User LocalUser { get; private set; }

        public static List<Player> Players { get; private set; }
        /// <summary>
        /// Set the local copy of players using a json string
        /// </summary>
        /// <param name="text"></param>
        public static void SetPlayers_Local<P>(string text)
        {
            Players = JsonConvert.DeserializeObject<List<P>>(text) as List<Player>;
        }
        /// <summary>
        /// Set the local copy of players using a list of players
        /// </summary>
        /// <param name="players"></param>
        public static void SetPlayers_Local(List<Player> players)
        {
            Players = players;
        }

        /// <summary>
        /// Get the Json string representation of the players
        /// </summary>
        /// <returns></returns>
        public static string GetPlayersJson()
        {
            return JsonConvert.SerializeObject(Players);
        }

        /// <summary>
        /// Returns the player associated with the given game id.
        /// If none are associated, it returns null
        /// </summary>
        /// <param name="GameId"></param>
        /// <returns></returns>
        public static Player GetPlayerByGameId_Local(int GameId)
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

        /// <summary>
        /// Gets one of the game from the locally stored list
        /// </summary>
        /// <param name="GameId"></param>
        /// <returns></returns>
        public static Game GetGameById_Local(int GameId)
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
            get { return retries >= MAX_RETRIES && FailedRequest; }
        }

        public static bool FailedRequest { get; private set; }
        private static int retries;
        private const int MAX_RETRIES = 0;

        private static int Requests;
        private static bool noResponse;
        public static bool IsMakingRequest
        {
            get { return Requests > 0;}
        }


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
        public static void Init<P>(string url, int port)
        {
            _url = url + ":" + port;
            _results = new Dictionary<string, object>();
            Requests = 0;
            Apitoken = "";
            Players = new List<Player>();
            Games = new List<Game>();

            //Load the saved data
            string data = LoadData("User");
            MessageBox.Show(data);
            if (data != "")
                LocalUser = JsonConvert.DeserializeObject<User>(data);
            else
                ;//CreateDirectory("User");

            data = LoadData("Apitoken");
            if (data != "")
                Networking.Apitoken = data;
            else
                ;// CreateDirectory("Apitoken");

            data = LoadData("Players");
            if (data != "" && data != "null")
            {
                List<P> spplayers = JsonConvert.DeserializeObject(data, typeof(List<P>), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }) as List<P>;

                //Cast each player
                foreach (P player in spplayers)
                {
                    Players.Add(player as Player);
                }
            }
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
                        {
                            MessageBox.Show("There was no response from the server. Please check that you are connected to the internet");
                            noResponse = true;
                        }
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

        private static void ParseResponse(object sender, EventArgs e)
        {
            noResponse = false;
            try
            {
                Dictionary<string, object> dict;

                //First cast the event handler argument to the relevant type so that we can get the result
                if (e.GetType() == typeof(UploadStringCompletedEventArgs))
                {
                    var ev = (UploadStringCompletedEventArgs)e;
                    dict = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(ev.Result);
                }
                else
                {
                    var ev = (DownloadStringCompletedEventArgs)e;
                    dict = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(ev.Result);
                }

                foreach (var keypair in dict)
                {
                    if (keypair.Key == "apitoken")
                    {
                        Apitoken = (string)keypair.Value;
                        SaveData("Apitoken", Apitoken);
                    }
                    else if (keypair.Key == "user")
                    {
                        //The key "user" points to an object, so it has to be parsed
                        //to a string first and then converted to a User
                        LocalUser = JsonConvert.DeserializeObject<User>(dict["user"].ToString());
                        SaveData("User", Newtonsoft.Json.JsonConvert.SerializeObject(LocalUser));
                    }
                    else
                        Results[keypair.Key] = keypair.Value;
                }

                FailedRequest = false;
            }
            catch (WebException ex)
            {
                FailedRequest = true;
                Networking.ProcessError(ex);
            }
        }

        public static void Logout()
        {
            Apitoken = "";
            LocalUser = null;
            SaveData("Apitoken.txt", "");
            SaveData("User.txt", "");
        }



        private static void CreateDirectory()
        {
            IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreForApplication();

            if (!f.DirectoryExists("data"))
                f.CreateDirectory("data");
        }

        public static string LoadData(string path)
        {
            IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreForApplication();

            if (!f.FileExists("data\\" + path))
                return "";
            else
            {
                string Text;

                using (var fs = new IsolatedStorageFileStream("data\\" + path, FileMode.Open, f))
                {
                    using (var isoFileReader = new StreamReader(fs))
                    {
                        Text = isoFileReader.ReadToEnd();
                    }
                }
                return Text;
            }
        }

        public static void DeleteFile(string path)
        {
            IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreForApplication();
            f.DeleteFile(path);
        }

        public static void SaveData(string path, string data)
        {
            IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreForApplication();

            using (var fs = new IsolatedStorageFileStream("data\\" + path, FileMode.Create, f))
            {
                using (var isoFileWriter = new StreamWriter(fs))
                {
                    isoFileWriter.Write(data);
                }
            }
        }
    }
}