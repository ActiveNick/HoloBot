#if WINDOWS_UWP
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
#endif

namespace MakerShowBotTestClient
{
#if WINDOWS_UWP
    class Conversation
    {
        public string conversationId { get; set; }
        public string token { get; set; }
        public string eTag { get; set; }
    }

    public class ConversationMessages
    {
        public Message[] messages { get; set; }
        public string watermark { get; set; }
        public string eTag { get; set; }
    }

    public class Message
    {
        public string id { get; set; }
        public string conversationId { get; set; }
        public DateTime created { get; set; }
        public string from { get; set; }
        public string text { get; set; }
        public Channeldata channelData { get; set; }
        public string[] images { get; set; }
        public Attachment[] attachments { get; set; }
        public string eTag { get; set; }
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
    public class BotService
    {

        private string _APIKEY = "PN3lBLvTXwU.cwA.Kb8.qA6OkFZcgx2hLRSAlteqKnCZqYcQD_orUi_kwyw6i8k";
        private string botToken;
        private string activeConversation;
        private string activeWatermark;

        public BotService()
        {

        }

        public async Task<string> StartConversation()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://directline.botframework.com/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Authorize
                client.DefaultRequestHeaders.Add("Authorization", "BotConnector " + _APIKEY);

                // Get a new token
                var keyreq = new KeyRequest() { Mainkey = "" };
                var stringContent = new StringContent(keyreq.ToString());
                HttpResponseMessage response = await client.PostAsync("api/tokens/conversation", stringContent);
                if (response.IsSuccessStatusCode)
                {
                    botToken = response.Content.ReadAsStringAsync().Result;
                    //return botToken;
                }

                // Start a new conversation
                var myConversation = new Conversation() { token = botToken };
                string postBody = JsonConvert.SerializeObject(myConversation);
                HttpResponseMessage response2 = await client.PostAsync("api/conversations",
                    new StringContent(postBody, Encoding.UTF8, "application/json"));
                if (response2.IsSuccessStatusCode)
                {
                    var re = response2.Content.ReadAsStringAsync().Result;
                    myConversation = JsonConvert.DeserializeObject<Conversation>(re);
                    activeConversation = myConversation.conversationId;
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
                client.DefaultRequestHeaders.Add("Authorization", "BotConnector " + _APIKEY);

                // Send a message
                string messageId = Guid.NewGuid().ToString();
                DateTime timeStamp = DateTime.Now;
                var attachment = new Attachment();
                var myMessage = new Message()
                {
                    from = "Joe",
                    conversationId = conversationId,
                    text = message
                };

                string postBody = JsonConvert.SerializeObject(myMessage);
                HttpResponseMessage response = await client.PostAsync("api/conversations/" + conversationId + "/messages",
                    new StringContent(postBody, Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    var re = response.Content.ReadAsStringAsync().Result;
                    return true;
                }
                return false;
            }
        }

        public async Task<ConversationMessages> GetMessages()
        {
            using (var client = new HttpClient())
            {
                string conversationId = activeConversation;

                client.BaseAddress = new Uri("https://directline.botframework.com/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Authorize
                client.DefaultRequestHeaders.Add("Authorization", "BotConnector " + _APIKEY);

                ConversationMessages cm = new ConversationMessages();
                string messageURL = "api/conversations/" + conversationId + "/messages";
                if (activeWatermark != null)
                    messageURL += "/?watermark=" + activeWatermark;
                HttpResponseMessage response = await client.GetAsync(messageURL);
                if (response.IsSuccessStatusCode)
                {
                    var re = response.Content.ReadAsStringAsync().Result;
                    cm = JsonConvert.DeserializeObject<ConversationMessages>(re);
                    activeWatermark = cm.watermark;
                    return cm;

                }
                return cm;
            }
        }
    }
    #endif

    #if !WINDOWS_UWP
    public class BotService
    {

    }
    #endif
}

