﻿using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Web.Services.Description;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Media3D;
using System.Xml;
using Xceed.Wpf.Toolkit;

namespace X_IPTV
{
    /*
     * player_api.php?action=get_live_streams
     * 
     * player_api.php?action=get_live_categories
     * 
     * xmltv.php xml data channel ids, display name, icon src. Has the desc for channels
     * 
     * get.php
     */
    public class REST_Ops
    {
        private static readonly HttpClient _client = new HttpClient();

        private static readonly string _user = Instance.currentUser.username;
        private static readonly string _pass = Instance.currentUser.password;
        private static readonly string _server = Instance.currentUser.server;
        private static readonly string _port = Instance.currentUser.port;
        private static readonly bool _useHttps = Instance.currentUser.useHttps;
        //use get_live_categories for categories

        //not needed other than to get basic account info
        //keep for misc reasons and base url
        public static async Task<bool> CheckLoginConnection()
        {
            // Create a request for the URL. 		
            WebRequest request = WebRequest.Create($"{(_useHttps ? "https" : "http")}://{_server}:{_port}/player_api.php?username={_user}&password={_pass}");
            // If required by the server, set the credentials.
            request.Credentials = CredentialCache.DefaultCredentials;
            
            try
            {
                // Get the response.
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = await reader.ReadToEndAsync();
                // Display the content.
                Debug.WriteLine(responseFromServer);

                PlayerInfo info = Newtonsoft.Json.JsonConvert.DeserializeObject<PlayerInfo>(responseFromServer);

                Instance.PlayerInfo = info;

                // Cleanup the streams and the response.
                reader.Close();
                dataStream.Close();
                response.Close();

                return true; // Connection was successful
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse response = (HttpWebResponse)ex.Response;
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        // Handle the 404 Not Found response
                        System.Windows.MessageBox.Show("404 Not Found");
                    }

                }
                return false; // Connection was not successful
            }
        }

        //testing for rewrite
        public static async Task GetEPGDataForIndividualChannel(string stream_id)
        {
            // Create a request for the URL. 		
            WebRequest request = WebRequest.Create($"{(_useHttps ? "https" : "http")}://{_server}:{_port}/player_api.php?username={_user}&password={_pass}&action=get_simple_data_table&stream_id={stream_id}");
            // If required by the server, set the credentials.
            request.Credentials = CredentialCache.DefaultCredentials;
            // Get the response.
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = await reader.ReadToEndAsync();

            Channel24hrEPG myDeserializedClass = JsonConvert.DeserializeObject<Channel24hrEPG>(responseFromServer);

            if (myDeserializedClass.epg_listings.Count == 0)
            {
                Debug.WriteLine("epg_listings is an empty list");
                return;
            }
            else
                Debug.WriteLine("epg_listings is not an empty list");



            //MessageBox.Show(DecodeFrom64(myDeserializedClass.epg_listings[0].title));

            Debug.WriteLine("title: " + DecodeFrom64(myDeserializedClass.epg_listings[0].title));
            Debug.WriteLine("description: " + DecodeFrom64(myDeserializedClass.epg_listings[0].description));


            //Instance.allChannelEPG_24HRS_Dict.Add(stream_id, myDeserializedClass);

            // Cleanup the streams and the response.
            reader.Close();
            dataStream.Close();
            response.Close();
        }

        public static string DecodeFrom64(string encodedData)
        {
            try
            {
                byte[] encodedDataAsBytes = System.Convert.FromBase64String(encodedData);
                string returnValue = System.Text.Encoding.UTF8.GetString(encodedDataAsBytes);
                return returnValue;
            }
            catch (FormatException e)
            {
                Console.WriteLine("An error occurred while decoding the base64 encoded string: " + e.Message);
                return null;
            }
        }


        //Retrieves each individual channel data
        //keep pass action as parameter
        public static async Task RetrieveChannelData()//maybe pass in the action as a string and use this for all action calls
        {
            //action=get_live_streams  use this to get all the channels and their data
            //action=get_live_categories    use this to get the categories (already is used, should be fine)
            //action=get_simple_data_table&stream_id=X  use this to get the channel data for a specific channel (EPG)


            // Create a request for the URL. 	
            WebRequest request = WebRequest.Create($"{(_useHttps ? "https" : "http")}://{_server}:{_port}/player_api.php?username={_user}&password={_pass}&action=get_live_streams");
            // If required by the server, set the credentials.
            request.Credentials = CredentialCache.DefaultCredentials;
            // Get the response.
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = await reader.ReadToEndAsync();
            // Display the content.
            Debug.WriteLine(responseFromServer);

            //Channels are loaded here
            ChannelEntry[] channelInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<ChannelEntry[]>(responseFromServer);

            //add each ChannelEntry to categoryToChannelMap based on the category_id
            foreach (ChannelEntry channel in channelInfo)
            {
                if (Instance.categoryToChannelMap.ContainsKey(channel.category_id))
                    Instance.categoryToChannelMap[channel.category_id].Add(channel);
                else
                    Instance.categoryToChannelMap.Add(channel.category_id, new List<ChannelEntry>() { channel });
            }

            Instance.ChannelsArray = channelInfo;

            // Cleanup the streams and the response.
            reader.Close();
            dataStream.Close();
            response.Close();

            //await LoadPlaylistChannelData(user, pass, server, port);//hide this call from where RetrieveChannelData is called

            await LoadPlaylistDataAsync();//hide this call from where RetrieveChannelData is called
        }

        //keep but join in other functions, pass the action as a parameter
        public static async Task RetrieveCategories()
        {

            // Create a request for the URL.
            WebRequest request = WebRequest.Create($"{(_useHttps ? "https" : "http")}://{_server}:{_port}/player_api.php?username={_user}&password={_pass}&action=get_live_categories");
            // If required by the server, set the credentials.
            request.Credentials = CredentialCache.DefaultCredentials;
            // Get the response.
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = await reader.ReadToEndAsync();
            // Display the content.
            Debug.WriteLine(responseFromServer);

            ChannelGroups[] info = Newtonsoft.Json.JsonConvert.DeserializeObject<ChannelGroups[]>(responseFromServer);

            //add each category_id to the categoryToChannelMap as a key and the values null
            foreach (ChannelGroups entry in info)
            {
                if (!Instance.categoryToChannelMap.ContainsKey(entry.category_id))
                    Instance.categoryToChannelMap.Add(entry.category_id, new List<ChannelEntry>());
                else
                    Debug.WriteLine("Key already exists in categoryToChannelMap dictionary");
            }


            Instance.ChannelGroupsArray = info;

            // Cleanup the streams and the response.
            reader.Close();
            dataStream.Close();
            response.Close();
        }

        //seems the get.php api call is the only one that includes the stream url in the response
        //public static async Task LoadPlaylistChannelData(string user, string pass, string server, string port)
        //{
        //    //This method uses parsing instead of json Deserializing because this response doesn't return in json format/json formattable

        //    //playlistDataMap is a dictionary containing the xui_id as the key and value being the PlaylistData object
        //    Instance.playlistDataMap = new Dictionary<string, ChannelStreamData>();

        //    //*** Maybe change this from httpclient to webrequest eventually ***

        //    //retrieve channel data from client
        //    _client.DefaultRequestHeaders.Accept.Clear();
        //    _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.0.0 Safari/537.36");

        //    Task<string> stringTask = null;
        //    if ((bool)ul.protocolCheckBox.IsChecked)//use the https protocol
        //        stringTask = _client.GetStringAsync($"https://{server}:{port}/get.php?username={user}&password={pass}");
        //    else//use the http protocol
        //        stringTask = _client.GetStringAsync($"http://{server}:{port}/get.php?username={user}&password={pass}");

        //    var serverResponse = await stringTask;

        //    //parse the m3u playlist and split into an array. playlist array contains the unparsed data
        //    string[] playlist = serverResponse.Split(new string[] { "#EXTINF:" }, StringSplitOptions.None);

        //    ChannelStreamData[] info = new ChannelStreamData[Instance.ChannelsArray.Length];//creates the info array for X number of channels
        //    int index = -1;
        //    foreach (var channel in playlist)
        //    {
        //        if (index > -1)
        //        {
        //            List<string> wordArray = new List<string>();
        //            wordArray.Clear();
        //            //dont need [0],[11],[12],[13],[14]. Need [1] - [10]
        //            wordArray = channel.Split('"').Select((element, index) => index % 2 == 0 ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) : new string[] { element }).SelectMany(element => element).ToList();

        //            //only need the stream_url, but might as well get the other data while here
        //            info[index] = new ChannelStreamData
        //            {
        //                xui_id = wordArray[2],
        //                tvg_id = wordArray[4],
        //                tvg_name = wordArray[6],
        //                tvg_logo = wordArray[8],
        //                group_title = wordArray[10],
        //                stream_url = channel.Substring(channel.LastIndexOf("https"))
        //            };
        //            string currentChannelId = info[index].xui_id;
        //            string currentChannelNameId = info[index].tvg_id;
        //            //Adds the channel data obj to the dict with channel id as the key
        //            if (currentChannelNameId != "" && currentChannelNameId != null)
        //            {
        //                if (!Instance.playlistDataMap.ContainsKey(currentChannelNameId))
        //                {
        //                    //playlistDataMap is the only one that contains the stream url
        //                    Instance.playlistDataMap.Add(currentChannelId, info[index]);
        //                    //need to use the xui_id with it to make it more unqiue since there are multiple tvg_id with the same.
        //                    //there a some with multiples due to hd and sd
        //                }
        //                else
        //                    System.Windows.MessageBox.Show("Dup key debug: " + currentChannelNameId);
        //            }
        //        }
        //        index++;
        //    }
        //}

        private static readonly HttpClient _clientTest = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.0.0 Safari/537.36");
            return client;
        }

        //Gets all channels basic info data (not epg data)
        public static async Task LoadPlaylistDataAsync()
        {
            Instance.playlistDataMap = new Dictionary<string, ChannelStreamData>();

            var stringTask = _clientTest.GetStringAsync($"{(_useHttps ? "https" : "http")}://{_server}:{_port}/get.php?username={_user}&password={_pass}");
            var serverResponse = await stringTask;

            var playlist = serverResponse.Split("#EXTINF:");
            var info = new ChannelStreamData[playlist.Length];

            for (int index = 0; index < playlist.Length; index++)
            {
                if (index == 0) continue;

                var wordArray = playlist[index].Split('"').Select((element, i) => i % 2 == 0 ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) : new string[] { element }).SelectMany(element => element).ToList();

                info[index] = new ChannelStreamData
                {
                    xui_id = wordArray[2],
                    tvg_id = wordArray[4],
                    tvg_name = wordArray[6],
                    tvg_logo = wordArray[8],
                    group_title = wordArray[10],
                    stream_url = playlist[index].Substring(playlist[index].LastIndexOf("https"))
                };

                AddToPlaylistDataMap(info[index]);
            }
        }

        private static void AddToPlaylistDataMap(ChannelStreamData data)
        {
            if (!Instance.playlistDataMap.ContainsKey(data.tvg_id))
            {
                Instance.playlistDataMap.Add(data.xui_id, data);
            }
            else
            {
                System.Windows.MessageBox.Show("Duplicate key: " + data.tvg_id);
            }
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }
}
