using System;
using System.Linq;
using System.Collections.Generic;
using System.Web;
using Microsoft.AspNet.SignalR;

using API;
using QAS;

using System.Threading;

namespace SignalRChat
{
    public class Stora
    {
        public AzureTable<Square> SquareTable;

        public Dictionary<string, Message> Dict = new Dictionary<string, Message>();

        public Stora()
        {
            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=WRITEHEREDATAYOUNEED";
            SquareTable = new AzureTable<Square>("Square", storageConnectionString);

            Thread saver = new Thread(this.Save);
            saver.Start();
        }


        object locker = new object();

        public void Save()
        {
            while (true)
            {
                Thread.Sleep(2000);

                if (Hub == null)
                {
                    continue;
                }

                Dictionary<string, Message> dict;

                bool save = false;

                lock (locker)
                {
                    dict = Dict;
                    Dict = new Dictionary<string, Message>();

                    if (dict.Count() > 0)
                    {
                        try
                        {
                            foreach (string id in dict.Keys)
                            {
                                if (dict[id].type == "fallSquare")
                                {
                                    dict[id].data.ETag = "*";
                                    SquareTable.Delete(dict[id].data);
                                }
                                else
                                {
                                    SquareTable.UpSert(dict[id].data);
                                }
                            }
                            save = true;
                        }
                        catch (Exception e)
                        {
                            if (Hub != null) Hub.Clients.All.broadcastMessage("Hey you!", "Exception happened: " + e.GetType() + " " + e.Message);
                            continue;
                        }
                    }
                }

                if (Hub != null && save)
                    Hub.Clients.All.broadcastMessage("Hey you!", save ? "Im saving data" : "nothing to save");

            }
        }

        ChatHub Hub = null;


        public bool Add(ChatHub hub, string data)
        {
            if (Hub == null)
            {
                Hub = hub;
            }

            Message message = new Message();
            try
            {
                message = message.FromJson(data);

                bool needToSend = false;

                foreach (Square.Text text in message.data.texts)
                {
                    if (text.type == "text" && text.value.Match("^(http://.*)$")) // TODO: do it one time only?
                    {
                        text.type = "link";
                        needToSend = true;
                        //text.value = "<a href='" + text.value + "'>link</a>";
                    }
                }

                if (needToSend) // the text was updated, so need to inform frontend
                {
                    Message mess = new Message() { type = "updSquare", data = message.data };
                    hub.Clients.Caller.broadcastSand("AZAZAZAZAZAZAZAZAZAZAZ", mess.ToJson()); // not received immediately. Why?
                }

                Hub.Clients.Caller.broadcastMessage("TextCount", "Count: " + message.data.texts.Count());
            }
            catch (Exception e)
            {
                Hub.Clients.Caller.broadcastMessage("Exception", e.GetType() + " " + e.Message);

                return false;
            }
            lock (locker)
            {
                if (Dict.ContainsKey(message.data.id))
                {
                    Dict[message.data.id] = message;
                }
                else
                {
                    Dict.Add(message.data.id, message);
                }
            }

            return true;
        }
        public void Select(ChatHub hub)
        {
            hub.Clients.Caller.broadcastMessage("Take it", "bugger");
            foreach (Square sq in SquareTable.Select())
            {
                Message mess = new Message();
                mess.type = "updSquare";
                mess.data = sq;

                hub.Clients.Caller.broadcastMessage("Take it", mess.ToJson());
                hub.Clients.Caller.broadcastSand("server", mess.ToJson());
            }
        }
    }

    public class ChatHub : Hub
    {
        public static Stora stora;

        public void Send(string name, string message)
        {
            Clients.All.broadcastMessage(name, message);
        }

        public void Sand(string id, string data)
        {
            if (id == "giveme")
            {
                stora.Select(this);
                return;
            }

            stora.Add(this, data);
            Clients.Others.broadcastSand(id, data);
            //Clients.All.broadcastSand(id, data);
        }
    }
}
