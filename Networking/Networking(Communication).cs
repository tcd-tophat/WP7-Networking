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
            //Store the data in case we need to retry the request
            _eventHandlerD = eventHandler;
            _resource = resource;

            if (Apitoken != "")
            {
                resource = String.Format("{0}?apitoken={1}", resource, Apitoken);
            }

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
            //Store the data in case we need to retry the request
            _eventHandlerU = eventHandler;
            _data = data;
            _resource = resource;

            if (Apitoken != "")
            {
                resource = String.Format("{0}?apitoken={1}", resource, Apitoken);
            }

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

            if (Apitoken != "")
            {
                resource = String.Format("{0}?apitoken={1}", resource, Apitoken);
            }

            HttpWebRequest wc = WebRequest.CreateHttp(URL + resource);
            wc.Method = "DELETE";

            wc.BeginGetResponse(new Networking().OnDelete + aSyncCallback, "");
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
        }
        

#endregion


        /// <summary>
        /// Arranges the email and password into a valid Json string and uses Http POST to send the data to the server
        /// </summary>
        /// <param name="email">A valid email address</param>
        /// <param name="password"></param> //TODO: Check password is in correct format and length
        /// <param name="eventHandler">The method that is invoked when the request has been completed.
        /// Note: This also includes failed requests, so make sure to catch the exception!</param>
        public static void Login(string email, string password, UploadStringCompletedEventHandler eventHandler = null)
        {
            string data = "data={\"email\":\"" + email + "\",\"password\":\"" + password + "\"}";
            //A login is a post request
            POST(new Networking().ParseResponse + eventHandler, data, "/apitokens");

            //Make sure that the LocalUser is assigned a value so that the rest of the details can be retrieved later
            if (LocalUser == null)
                LocalUser = new User(email);
        }


        public static void GetUserDetails(DownloadStringCompletedEventHandler eventHandler = null)
        {
            GET(new Networking().ParseResponse + eventHandler + OnGetUserDetails, "/users/" + LocalUser.email);
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
            POST(new Networking().ParseResponse + eventHandler, data, "/users");

            //Make sure that the LocalUser is assigned a value so that the rest of the details can be retrieved later
            if (LocalUser == null)
                LocalUser = new User(email);
        }

        public static void GetGames(DownloadStringCompletedEventHandler eventHandler = null)
        {
            GET(new Networking().ParseResponse + eventHandler, "/games");
        }

        //Store the variables for use again if the request fails
        public static void CreateGame(string name, int type, UploadStringCompletedEventHandler eventHandler = null)
        {
            POST(new UploadStringCompletedEventHandler(new Networking().ParseResponse) + OnCreateGame + eventHandler, "data={\"name\":\"" + name + "\",\"game_type_id\":\"" + type + "\"}", "/games");
        }


        //Store the variables for use again if the request fails
        public static void JoinGame(string name, int gameId, string photo, UploadStringCompletedEventHandler eventHandler = null)
        {
            string data = "data={\"name\":\"" + name + "\",\"game\":\"" + gameId + "\", \"photo\":\"" + photo + "\"}";
            POST(new UploadStringCompletedEventHandler(new Networking().ParseResponse) + OnJoinGame + eventHandler, data, "/players");
        }

        private static void OnJoinGame(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                AddPlayer(JsonConvert.DeserializeObject<Player>(e.Result));
            }
            catch (WebException) { }
        }

        private static void OnCreateGame(object sender, UploadStringCompletedEventArgs e)
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
                    for (int index = 0; index < GameTypes.Length; index++)
                    {
                        if (GameTypes[index].id == id)
                            newgame = JsonConvert.DeserializeObject<Game>(e.Result);
                    }
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
            //POST(new Networking().ParseResponse + eventHandler, "/images");
        }
    }
}
