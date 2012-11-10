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

        private static Queue<Action> _requests;
        private static Action currentRequest;

        /// <summary>
        /// Http GET request
        /// </summary>
        /// <param name="resource">The resource name is appended to the URL. e.g. "/games"</param>
        /// <param name="eventHandler">The method that is invoked when the request has been completed.
        /// Note: Use failedRequest</param>
        public static void GET(DownloadStringCompletedEventHandler eventHandler, string resource = "", bool includeApiToken = false, int depth = 0)
        {
            //Check if any parameters should be added to the URL
            if (includeApiToken)
            {
                resource += "?apitoken=" + Apitoken;
                if (depth > 0)
                    resource += "&depth=" + depth;
            }
            else
            {
                if (depth > 0)
                    resource += "?depth=" + depth;
            }
            
            //Check if a request is currently being made already
            if (_requests.Count > 0 && IsMakingRequest)
            {
                //If it has, then queue it for later
                _requests.Enqueue(() =>
                {
                    GET(eventHandler, resource);
                });
                return;
            }
            //Store the data in case we need to retry the request
            currentRequest = () => GET(eventHandler, resource);


            WebClient wc = new WebClient();
            wc.DownloadStringAsync(new Uri(URL + resource + "&r=" + DateTime.Now.Ticks));
            wc.Headers["Cache-Control"] = "no-cache";
            wc.DownloadStringCompleted += ParseResponse;
            wc.DownloadStringCompleted += eventHandler;
            wc.DownloadStringCompleted += OnGet;
        }

        private static void OnGet(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                //This just nudges the event args into throwing an exception if the request failed
                string x = e.Result;
                nextRequest();
            }
            catch(WebException)
            {
                if (retries < MAX_RETRIES)
                {
                    currentRequest();
                    retries++;
                }
                else
                {
                    nextRequest();
                }
            }
        }

        /// <summary>
        /// The POST method should be only be used when CREATING resources on the server. Use PUT for updating!
        /// </summary>
        /// <param name="eventHandler">The method that is invoked when the request has been completed. 
        /// Note: This also includes failed requests, so make sure to catch the exception!</param>
        /// <param name="data">The json string to be sent with the request</param>
        /// <param name="resource">The extra parameter to be appended to the URL. e.g. "/games"</param>
        public static void POST(UploadStringCompletedEventHandler eventHandler, string data, string resource = "", bool includeApiToken = false, int depth = 0)
        {
            //Check if any parameters should be added to the URL
            if (includeApiToken)
            {
                resource += "?apitoken=" + Apitoken;
                if (depth > 0)
                    resource += "&depth=" + depth;
            }
            else
            {
                if (depth > 0)
                    resource += "?depth=" + depth;
            }

            if (_requests.Count > 0 && IsMakingRequest)
            {
                _requests.Enqueue(() =>
                    {
                        POST(eventHandler, data, resource);
                    });
                return;
            }


            //Store the data in case we need to retry the request
            currentRequest = () => POST(eventHandler, data, resource);

            WebClient wc = new WebClient();
            wc.Headers["Content-Type"] = "application/x-www-form-urlencoded";

            wc.UploadStringAsync(new Uri(URL + resource), "POST", data);
            wc.UploadStringCompleted += ParseResponse;
            wc.UploadStringCompleted += eventHandler;
            wc.UploadStringCompleted += OnPost;
        }

        private static void OnPost(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                //This just nudges the event args into throwing an exception if it failed
                string x = e.Result;

                nextRequest();
            }
            catch (WebException)
            {
                if (retries < MAX_RETRIES)
                {
                    currentRequest();
                    retries++;
                }
                else
                    nextRequest();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventHandler">The method that is invoked when the request has been completed. 
        /// Note: This also includes failed requests, so make sure to catch the exception!</param>
        /// <param name="resource">The extra parameter to be appended to the URL. e.g. "/games"</param>
        public static void DELETE(AsyncCallback asyncCallback, string resource = "", bool includeApiToken = false, int depth = 0)
        {
            //Check if any parameters should be added to the URL
            if (includeApiToken)
            {
                resource += "?apitoken=" + Apitoken;
                if (depth > 0)
                    resource += "&depth=" + depth;
            }
            else
            {
                if (depth > 0)
                    resource += "?depth=" + depth;
            }

            if (_requests.Count > 0 && IsMakingRequest)
            {
                _requests.Enqueue(() =>
                    {
                        DELETE(asyncCallback, resource);
                    });
                return;
            }

            //Store the data in case we need to retry the request
            currentRequest = () => DELETE(asyncCallback, resource);

            HttpWebRequest wc = WebRequest.CreateHttp(URL + resource);
            wc.Method = "DELETE";

            wc.BeginGetResponse(OnDelete + asyncCallback, "");
        }

        private static void OnDelete(IAsyncResult e)
        {
            if (e.IsCompleted)
            {
                nextRequest();
            }
            else
            {
                if (retries < MAX_RETRIES)
                {
                    currentRequest();
                    retries++;
                }
                else
                {
                    nextRequest();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asyncCallback">The method that is invoked when the request has been completed. 
        /// Note: This also includes failed requests, so make sure to catch the exception!</param>
        /// <param name="resource">The extra parameter to be appended to the URL. e.g. "/games"</param>
        public static void PUT(AsyncCallback asyncCallback, string data, string resource = "", bool includeApiToken = false)
        {
            if (includeApiToken)
                resource += "?apitoken=" + Apitoken;

            if (_requests.Count > 0 && IsMakingRequest)
            {
                _requests.Enqueue(() =>
                {
                    PUT(asyncCallback, data, resource);
                });
                return;
            }

            //Store the data in case we need to retry the request
            currentRequest = () => PUT(asyncCallback, data, resource);

            HttpWebRequest request = WebRequest.CreateHttp(new Uri(URL + resource, UriKind.Absolute));
            request.Method = "PUT";
            request.ContentType = "application/x-www-form-urlencoded"; 

            request.BeginGetRequestStream(ar =>
            {
                var requestStream = request.EndGetRequestStream(ar);
                using (var sw = new System.IO.StreamWriter(requestStream))
                {
                    sw.Write(data);
                }

                request.BeginGetResponse(a =>
                {
                    try
                    {
                        var response = request.EndGetResponse(a);
                        var responseStream = response.GetResponseStream();
                        using (var sr = new System.IO.StreamReader(responseStream))
                        {
                            // Parse the response message here
                        }
                    }
                    catch
                    {}

                }, null);

            }, null);

            //wc.BeginGetResponse(OnPut + asyncCallback);
        }


        private static void OnPut(IAsyncResult e)
        {
            if (e.IsCompleted)
            {
                nextRequest();
            }
            else
            {
                if (retries < MAX_RETRIES)
                {
                    currentRequest();
                    retries++;
                }
                else
                {
                    nextRequest();
                }
            }
        }

        private static void nextRequest()
        {
            currentRequest = null;
            retries = 0;
            if (_requests.Count > 0)
            {
                //Begin the next request in queue
                _requests.Dequeue().Invoke();
            }
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
            POST(eventHandler, data, "/apitokens");
        }

        /// <summary>
        /// Anonymous login
        /// </summary>
        /// <param name="eventHandler"></param>
        public static void Login(UploadStringCompletedEventHandler eventHandler = null)
        {
            string data = "data={}";
            //A login is a post request
            POST(eventHandler, data, "/apitokens");
        }

        public static void GetUserDetails(DownloadStringCompletedEventHandler eventHandler = null)
        {
            GET(eventHandler + OnGetUserDetails, "/users/" + LocalUser.email, true, 1);
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
            POST(eventHandler, data, "/users", true);
        }

        public static void GetGames<G>(DownloadStringCompletedEventHandler eventHandler = null)
        {
            GET(new DownloadStringCompletedEventHandler(OnGetGames<G>) + eventHandler, "/games", true, 3);
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
            GET(new DownloadStringCompletedEventHandler(OnGetGameById<G>) + eventHandler, "/games/" + id, true, 2);
        }

        private static void OnGetGameById<G>(object sender, DownloadStringCompletedEventArgs e)
        {
            if (!Networking.FailedRequest)
            {
                Game game = JsonConvert.DeserializeObject<G>(e.Result) as Game;
                for (int i = 0; i < Games.Count; i++)
                {
                    if (Games[i].id == (game).id)
                    {
                        Games[i] = game;
                        return;
                    }
                }

                //if the game wasn't found, then add it to the list
                Games.Add(game);
            }
        }

        public static void CreateGame<T>(string name, int type, UploadStringCompletedEventHandler eventHandler = null)
        {
            POST(new UploadStringCompletedEventHandler(OnCreateGame<T>) + eventHandler, 
                "data={\"name\":\"" + name + "\",\"game_type_id\":\"" + type + "\"}", "/games", true, 1);
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
            POST(new UploadStringCompletedEventHandler(OnJoinGame<P>) + eventHandler, data, "/players", true, 3);
        }

        private static void OnJoinGame<P>(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                Player p = JsonConvert.DeserializeObject<P>(e.Result) as Player;

                if (Players == null)
                    Players = new List<Player>();

                for (int i = 0; i < Players.Count; i++)
                {
                    if (p.id == Players[i].id)
                    {
                        Players[i] = p;
                        return;
                    }
                }
                Players.Add(p);

                SaveData("Players", JsonConvert.SerializeObject(Players));
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
            DELETE(aSyncCallback, "/games/" + gameindex, true);
        }

        public static void GetTeamScores(int teamid, DownloadStringCompletedEventHandler eventHandler = null)
        {
            GET(eventHandler, "/teamscore/" + teamid, true, 2);
        }

        public static void UpdateGPS(int playerid, double lattitude, double longitude, AsyncCallback asyncCallback = null)
        {
            var dict = new Dictionary<string, string>();
            dict["lat"] = lattitude.ToString("0.000");
            dict["lon"] = longitude.ToString("0.000");
            string x = JsonConvert.SerializeObject(dict);
            System.Diagnostics.Debugger.Break();
            PUT(asyncCallback, JsonConvert.SerializeObject(dict), "/player/" + playerid, true);
        }

        public static void UploadImage(System.IO.Stream s, UploadStringCompletedEventHandler eventHandler = null)
        {
            //TODO: Upload the image
            //POST(ParseResponse + eventHandler, "/images", true);
        }

        public static void Kill(Dictionary<string, object> parameters, UploadStringCompletedEventHandler eventHandler = null)
        {
            POST(eventHandler, "data=" + JsonConvert.SerializeObject(parameters), "/kills", true);
        }

        public static void Respawn(string qrcode, int playerid, AsyncCallback asyncCallBack = null)
        {
            var dict = new Dictionary<string, object>();
            dict["respawn_code"] = qrcode;
            dict["id"] = playerid;

            PUT(asyncCallBack, "data=" + JsonConvert.SerializeObject(dict), "/players/" + playerid, true);
        }
    }
}
