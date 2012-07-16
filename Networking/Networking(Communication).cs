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
        private static string URL;
        public static Dictionary<string, string> Results;

        public static void Init(string url, int port)
        {
            URL = url + ":" + port;
            Results = new Dictionary<string, string>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item">The part that is appended to the URL</param>
        /// <param name="eventHandler">The method that will be invoked when a response is given from the server or if the request fails</param>
        public static void GET(DownloadStringCompletedEventHandler eventHandler, string item = "")
        {
            WebClient wc = new WebClient();
            if (Results.ContainsKey("apitoken"))
                item = String.Format("{0}?apitoken={1}", item, Results["apitoken"]);

            wc.DownloadStringAsync(new Uri(URL + item));
            wc.DownloadStringCompleted += eventHandler;
        }

        public static void POST(UploadStringCompletedEventHandler eventHandler, string data, string item = "")
        {
            WebClient wc = new WebClient();
            wc.Headers["Content-Type"] = "application/x-www-form-urlencoded";
            wc.UploadStringAsync(new Uri(URL + item), "POST", data);
            wc.UploadStringCompleted += eventHandler;
        }

        public static void Login(string email, string password, UploadStringCompletedEventHandler eventHandler)
        {
            string data = "data={\"username\":\"" + email + "\",\"password\":\"" + password + "\"}";
            //A login is a post request
            POST(new Networking().GetUserDetails + eventHandler, data, "/apitokens");
        }

        public static void CreateUser(string email, string password, string name, string photo, UploadStringCompletedEventHandler eventHandler)
        {
            var dict = new Dictionary<string, string>();
            dict["email"] = email;
            dict["password"] = password;
            dict["name"] = name;
            dict["photo"] = photo;

            //Create a temporary dictionary so it can be packaged easily into a string
            string data = "data=" + JsonConvert.SerializeObject(dict);

            //Creating a user is a post request
            POST(new Networking().GetUserDetails + eventHandler, data, "/users");
        }

        public static void GetGames()
        {

        }


        public void GetUserDetails(object sender, UploadStringCompletedEventArgs e)
        {
            lock (this)
            {
                try
                {
                    var dict = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(e.Result);

                    if (dict.ContainsKey("apitoken"))
                    {
                        Results["apitoken"] = (string)dict["apitoken"];
                        MessageBox.Show(Results["apitoken"]);
                    }

                    if (dict.ContainsKey("user"))
                    {
                        //The key "user" doesn't point to a string, so it has to be parsed
                        //as an object and then converted to a string
                        Results["user"] = dict["user"].ToString();
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
