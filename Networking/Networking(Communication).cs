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
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;

namespace Tophat
{
    public partial class Networking
    {
        #region HttpRequests

        private static AsyncCallback _aSyncCallback;
        private static UploadStringCompletedEventHandler _eventHandlerU;
        private static DownloadStringCompletedEventHandler _eventHandlerD;
        private static string _data;
        private static string _resource;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resource">The resource name is appended to the URL. e.g. "/games"</param>
        /// <param name="eventHandler">The method that is invoked when the request has been completed. 
        /// Note: This also includes failed requests, so make sure to catch the exception!</param>
        public static void GET(DownloadStringCompletedEventHandler eventHandler, string resource = "")
        {
            Requests++;

            //Store the data in case we need to retry the request
            _eventHandlerD = eventHandler;
            _resource = resource;

            WebClient wc = new WebClient();
            wc.DownloadStringAsync(new Uri(URL + resource));
            wc.DownloadStringCompleted += eventHandler;
            wc.DownloadStringCompleted += OnGet;

        }

        private static void OnGet(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                //This just nudges the event args into throwing an exception if it failed
                string x = e.Result;
                retries = 0;
            }
            catch(WebException)
            {
                if (retries < MAX_RETRIES)
                {
                    GET(_eventHandlerD, _resource);
                    retries++;
                }
                else
                    retries = 0;
            }
            //The request has been completed; whether it failed or not doesn't matter
            Requests--;
        }

        /// <summary>
        /// The POST method should be only be used when CREATING resources on the server. Use PUT for updating!
        /// </summary>
        /// <param name="eventHandler">The method that is invoked when the request has been completed. 
        /// Note: This also includes failed requests, so make sure to catch the exception!</param>
        /// <param name="data">The json string to be sent with the request</param>
        /// <param name="resource">The extra parameter to be appended to the URL. e.g. "/games"</param>
        public static void POST(UploadStringCompletedEventHandler eventHandler, string data, string resource = "")
        {
            Requests++;

            //Store the data in case we need to retry the request
            _eventHandlerU = eventHandler;
            _data = data;
            _resource = resource;

            WebClient wc = new WebClient();
            wc.Headers["Content-Type"] = "application/x-www-form-urlencoded";

            wc.UploadStringAsync(new Uri(URL + resource), "POST", data);
            wc.UploadStringCompleted += eventHandler;
            wc.UploadStringCompleted += OnPost;
        }

        private static void OnPost(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                //This just nudges the event args into throwing an exception if it failed
                string x = e.Result;
            
                retries = 0;
            }
            catch (WebException)
            {
                if (retries < MAX_RETRIES)
                {
                    POST(_eventHandlerU, _data, _resource);
                    retries++;
                }
                else
                    retries = 0;
            }

            //The request has been completed; whether it failed or not doesn't matter
            Requests--;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventHandler">The method that is invoked when the request has been completed. 
        /// Note: This also includes failed requests, so make sure to catch the exception!</param>
        /// <param name="resource">The extra parameter to be appended to the URL. e.g. "/games"</param>
        public static void DELETE(AsyncCallback aSyncCallback, string resource = "")
        {
            //Store the data in case we need to retry the request
            _aSyncCallback = aSyncCallback;
            _resource = resource;

            HttpWebRequest wc = WebRequest.CreateHttp(URL + resource);
            wc.Method = "DELETE";

            wc.BeginGetResponse(new Networking().OnDelete + aSyncCallback, "");

            Requests++;
        }

        private void OnDelete(IAsyncResult e)
        {
            lock (this)
            {
                //This just nudges the event args into throwing an exception if it failed
                if (e.IsCompleted)
                {
                    retries = 0;
                }
                else
                {
                    if (retries < MAX_RETRIES)
                    {
                        DELETE(_aSyncCallback, _resource);
                        retries++;
                    }
                    else
                        retries = 0;
                }
            }
            //The request has been completed; whether it failed or not doesn't matter
            Requests--;
        }
        

#endregion


        /// <summary>
        /// Sends the username and password to the server using a POST request. Retrieves an apitoken
        /// </summary>
        /// <param name="email">A valid email address</param>
        /// <param name="password"></param> //TODO: Check password is in correct format and length
        /// <param name="eventHandler">The method that is invoked when the request has been completed.
        /// Note: This also includes failed requests, so make sure to catch the exception!</param>
        public static void Login(string email, string password, UploadStringCompletedEventHandler eventHandler = null)
        {
            string data = "data={\"email\":\"" + email + "\",\"password\":\"" + password + "\"}";
            //A login is a post request
            POST(ParseResponse + eventHandler, data, "/apitokens");
        }

        /// <summary>
        /// Anonymous login
        /// </summary>
        /// <param name="eventHandler"></param>
        public static void Login(UploadStringCompletedEventHandler eventHandler = null)
        {
            string data = "data={}";
            //A login is a post request
            POST(ParseResponse + eventHandler, data, "/apitokens");
        }

        public static void GetUserDetails(DownloadStringCompletedEventHandler eventHandler = null)
        {
            GET(ParseResponse + eventHandler + OnGetUserDetails, "/users/" + LocalUser.email + "?apitoken=" + Apitoken);
        }

        private static void OnGetUserDetails(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                LocalUser = JsonConvert.DeserializeObject<User>(e.Result);
            }
            catch (WebException) { }
        }

        /// <summary>
        /// Arranges the user data into a valid Json string and uses Http POST to send the data to the server
        /// </summary>
        /// <param name="email">Enter a valid email address here</param>
        /// <param name="password">Enter the password associated with that email address</param>
        /// <param name="name">The username that is to be associated with this account</param>
        /// <param name="photo">TODO: Pass in an actual photo(Could use this to get the location of the file to send)</param>
        /// <param name="eventHandler">The method that is invoked when the request has been completed. 
        /// Note: This also includes failed requests, so make sure to catch the exception!</param>
        public static void CreateUser(string email, string password, string name, string photo, UploadStringCompletedEventHandler eventHandler = null)
        {
            var dict = new Dictionary<string, string>();
            dict["email"] = email;
            dict["password"] = password;
            dict["name"] = name;
            dict["photo"] = photo;

            //Create a temporary dictionary so it can be packaged easily into a string
            string data = "data=" + JsonConvert.SerializeObject(dict);

            //Creating a user is a post request
            POST(ParseResponse + eventHandler, data, "/users?apitoken=" + Apitoken);
        }

        public static void GetGames<G>(DownloadStringCompletedEventHandler eventHandler = null)
        {
            GET(new DownloadStringCompletedEventHandler(OnGetGames<G>) + eventHandler, "/games?apitoken=" + Apitoken);
        }


        private static void OnGetGames<G>(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(e.Result);

                //The key "games" doesn't point to a string, so it has to be parsed
                //as a list of objects first
                object[] jsonGameList = JsonConvert.DeserializeObject<object[]>(dict["games"].ToString());
                Games = new List<Game>();

                //The games are parsed one by one so that at least some of the games can be salvaged if something goes wrong
                for (int i = 0; i < jsonGameList.Length; i++)
                {
                    try
                    {
                        Games.Add(JsonConvert.DeserializeObject<G>(jsonGameList[i].ToString()) as Game);
                    }
                    catch (Exception ex)
                    {
                        if (MessageBox.Show("The game: \n" + jsonGameList[i].ToString() + "\n is invalid. \nView More Info?", "Error", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                            MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                    }
                }
            }
            catch (WebException)
            {

            }
        }

        public static void GetGameById<G>(int id, DownloadStringCompletedEventHandler eventHandler = null)
        {
            GET(ParseResponse + new DownloadStringCompletedEventHandler(OnGetGameById<G>) + eventHandler, "/games/" + id + "?apitoken=" + Apitoken);
        }

        private static void OnGetGameById<G>(object sender, DownloadStringCompletedEventArgs e)
        {
            if (!Networking.FailedRequest)
            {
                Game game = JsonConvert.DeserializeObject<G>(e.Result) as Game;
                for (int i = 0; i < Games.Count; i++)
                {
                    if (Games[i].id == game.id)
                        Games[i] = game;
                }
            }
        }

        public static void CreateGame<T>(string name, int type, UploadStringCompletedEventHandler eventHandler = null)
        {
            POST(new UploadStringCompletedEventHandler(ParseResponse) + new UploadStringCompletedEventHandler(OnCreateGame<T>) + eventHandler, 
                "data={\"name\":\"" + name + "\",\"game_type_id\":\"" + type + "\"}", "/games?apitoken=" + Apitoken);
        }


        public static void JoinGame<P>(string playername, int gameId, Dictionary<string, object> extras = null, UploadStringCompletedEventHandler eventHandler = null)
        {
            if (extras == null)
                extras = new Dictionary<string, object>();


            //create a temporary string because it doesnt seem to compile the escape characters properly in 1 line
            var temp = new Dictionary<string, string>();
            temp["id"] = gameId.ToString();

            //use the extras dictionary to hold all the info rather than creating a new one
            extras["name"] = playername;
            extras["game"] = temp;


            string data = "data=" + JsonConvert.SerializeObject(extras);
            POST(new UploadStringCompletedEventHandler(ParseResponse) + new UploadStringCompletedEventHandler(OnJoinGame<P>) + eventHandler, 
                data, "/players?apitoken=" + Apitoken);
        }

        private static void OnJoinGame<P>(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                MessageBox.Show(e.Result);
                Player p = JsonConvert.DeserializeObject<P>(e.Result) as Player;

                for (int i = 0; i < Players.Count; i++)
                {
                    if (p.id == Players[i].id)
                    {
                        Players[i] = p;
                        return;
                    }
                }
                Players.Add(p);

                SaveData("Players", JsonConvert.SerializeObject(Players as List<P>));
            }
            catch (WebException) { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">The game type</typeparam>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnCreateGame<T>(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                //This just nudges the event args into throwing an exception if it failed
                string x = e.Result;

                Game newgame;
                //This event handler is invoked AFTER ParseResponse, meaning the dictionary will contain the relevant item
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(Results["game_type"].ToString());
                int id;

                if (int.TryParse(dict["id"].ToString(), out id))
                {
                    newgame = JsonConvert.DeserializeObject<T>(e.Result) as Game;
                }
            }
            catch (WebException) { }
        }

        public static void DeleteGame(int gameindex, AsyncCallback aSyncCallback = null)
        {
            DELETE(aSyncCallback, "/games/" + gameindex);
        }



        public static void UploadImage(System.IO.Stream s, UploadStringCompletedEventHandler eventHandler = null)
        {
            //TODO: Upload the image
            //POST(new Networking().ParseResponse + eventHandler, "/images?apitoken=" + Apitoken);
        }

        public static void Kill(Dictionary<string, string> parameters, UploadStringCompletedEventHandler eventHandler = null)
        {
            POST(ParseResponse, "data=" + JsonConvert.SerializeObject(parameters), "/kills?apitoken=" + Apitoken);
        }
    }
}
