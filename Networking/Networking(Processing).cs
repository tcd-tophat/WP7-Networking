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
    public partial class Networking
    {
        public static void ProcessError(WebException ex)
        {
            using (var reader = new System.IO.StreamReader(ex.Response.GetResponseStream()))
            {
                string error = reader.ReadToEnd();

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
