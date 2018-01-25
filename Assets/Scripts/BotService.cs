using System;
using UnityEngine;

#if WINDOWS_UWP
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
#endif

namespace HoloBot
{
    // This block of code won't run in Unity's older version of Mono
    // This can only be run in a UWP device like the HoloLens
#if WINDOWS_UWP
    public class Conversation
    {
        public string conversationId { get; set; }
        public string token { get; set; }
        public string eTag { get; set; }
        public string expires_in { get; set; }
    }

    public class ConversationAccount
    {
        public string id { get; set; }
        public bool isGroup { get; set; }
        public string name { get; set; }
    }

    public class ConversationReference
    {
        public string id { get; set; }
    }

    public class ConversationActitvities
    {
        public Activity[] activities { get; set; }
        public string watermark { get; set; }
        public string eTag { get; set; }
    }

    public class UserAccount
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class ActivityReference
    {
        public string id { get; set; }
    }

    public class Activity
    {
        public string type { get; set; }
        public string id { get; set; }
        public string timestamp { get; set; }
        public string channelId { get; set; }
        public UserAccount from { get; set; }
        public ConversationAccount conversation { get; set; }
        public string text { get; set; }
        public string localTimestamp { get; set; }
        public UserAccount[] membersAdded { get; set; }
        public UserAccount[] membersRemoved { get; set; }
        public string speak { get; set; }
        public Attachment[] attachments { get; set; }
        public object[] entities { get; set; }
        public string replyToId { get; set; }
    }

    public class Channeldata
    {
    }

    public class Attachment
    {
        public string url { get; set; }
        public string contentType { get; set; }
    }

    class KeyRequest
    {
        public string Mainkey { get; set; }
    }

    /// <summary>
    /// The main service used to communicate with a Bot via the Bot Connector and the Direct Line Channel.
    /// This can only run in a UWP client.
    /// </summary>
    public class BotService
    {
        // From the Bot Connector portal, enable the Direct Line channel on your bot
        // Generate and copy your Direct Line secret (aka API key)
        // TO DO: Please use your own key. This one connects to The Maker Show Bot
        private string _APIKEY = "PN3lBLvTXwU.cwA.Kb8.qA6OkFZcgx2hLRSAlteqKnCZqYcQD_orUi_kwyw6i8k";
        private string botToken;
        private string activeConversation;
        private string activeWatermark;
        private string newActivityId;
        private string lastResponse;

        public BotService()
        {
            // Constructor
        }

        public async Task<string> StartConversation()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://directline.botframework.com/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Authorize
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _APIKEY);

                // Get a new token as dummy call
                var keyreq = new KeyRequest() { Mainkey = "" };
                var stringContent = new StringContent(keyreq.ToString());
                HttpResponseMessage response = await client.PostAsync("v3/directline/conversations", stringContent);
                if (response.IsSuccessStatusCode)
                {
                    var re = response.Content.ReadAsStringAsync().Result;
                    var myConversation = JsonConvert.DeserializeObject<Conversation>(re);
                    activeConversation = myConversation.conversationId;
                    botToken = myConversation.token;
                    return myConversation.conversationId;
                }
            }
            return "Error";
        }

        public async Task<bool> SendMessage(string message)
        {
            using (var client = new HttpClient())
            {
                string conversationId = activeConversation;

                client.BaseAddress = new Uri("https://directline.botframework.com/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Authorize
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + botToken);

                // Send a message
                string messageId = Guid.NewGuid().ToString();
                DateTime timeStamp = DateTime.Now;
                var attachment = new Attachment();
                var myMessage = new Activity()
                {
                    type = "message",
                    from = new UserAccount() { id = "HoloLens User" },
                    text = message
                };

                string postBody = JsonConvert.SerializeObject(myMessage);
                String urlString = "v3/directline/conversations/" + conversationId + "/activities";
                HttpContent httpContent = new StringContent(postBody, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(urlString, httpContent);
                if (response.IsSuccessStatusCode)
                {
                    var re = response.Content.ReadAsStringAsync().Result;
                    lastResponse = re;
                    var ar = JsonConvert.DeserializeObject<ActivityReference>(re);
                    newActivityId = ar.id;
                    return true;
                }
                else
                {
                    lastResponse = response.Content.ReadAsStringAsync().Result;
                }
                return false;
            }
        }
        public async Task<string> GetNewestActivity()
        {
            ConversationActitvities cm = await GetNewestActivities();
            if (cm.activities.Length > 0)
            {
                return cm.activities[cm.activities.Length - 1].text;
            }
            else
            {
                return "";
            }
        }

        public async Task<ConversationActitvities> GetNewestActivities()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200)).ConfigureAwait(true);
            int inc = 0;
            ConversationActitvities cm = await GetMessages();
            while (++inc < 5)
            {
                Debug.Log(cm.activities.Length + " conversations received");
                for (int i = 0; i < cm.activities.Length; i++)
                {
                    var activity = cm.activities[i];
                    Debug.Log("activity received = " + activity.text);
                    lastResponse = activity.id + " / " + activity.replyToId + " / " + newActivityId;

                    // wait for reply message from my message
                    if (activity.replyToId != null && activity.replyToId.Equals(newActivityId))
                    {
                        Debug.Log("activity is response to " + newActivityId);
                        return cm;
                    }
                }
                await Task.Delay(TimeSpan.FromMilliseconds(200)).ConfigureAwait(true);
                cm = await GetMessages();
            }
            return cm;
        }

        public async Task<ConversationActitvities> GetMessages()
        {
            using (var client = new HttpClient())
            {
                string conversationId = activeConversation;

                client.BaseAddress = new Uri("https://directline.botframework.com/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Authorize
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + botToken);

                ConversationActitvities cm = new ConversationActitvities();
                string messageURL = "v3/directline/conversations/" + conversationId + "/activities";
                if (activeWatermark != null)
                    messageURL += "?watermark=" + activeWatermark;
                HttpResponseMessage response = await client.GetAsync(messageURL);
                if (response.IsSuccessStatusCode)
                {
                    var re = response.Content.ReadAsStringAsync().Result;
                    lastResponse = re.ToString();
                    cm = JsonConvert.DeserializeObject<ConversationActitvities>(re);
                    activeWatermark = cm.watermark;
                    return cm;
                }
                return cm;
            }
        }
    }
#endif

#if !WINDOWS_UWP

    /// <summary>
    /// This is an empty shim for the BotService within Unity Mono, otherwise we'd get a
    /// compilation error in Unity when trying to instantiate this object.
    /// </summary>
    public class BotService
    {

    }
#endif
}

