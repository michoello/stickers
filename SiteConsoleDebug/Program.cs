using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using API;

namespace SiteConsoleDebug
{

    class Program
    {
        static void Main(string[] args)
        {
            Message message = new Message();
            string data = "{\"type\":\"addSquare\",\"data\":{\"left\":250,\"top\":231,\"size\":160,\"color\":\"rgb(231,254,247)\",\"id\":1424198112121,\"texts\":[{\"top\":2,\"left\":2,\"id\":1424198112126,\"maxWidth\":155,\"maxHeight\":156,\"value\":\"\"}]}}";

            try
            {
                message = message.FromJson(data);
            }
            catch (Exception e)
            {
                Console.WriteLine("E: " + e.GetType() + " " + e.Message);
                Console.WriteLine(" S: " + e.StackTrace);
            } 

            Console.WriteLine("Message: " + message.ToJson());

            Console.Read();

            Environment.Exit(0);
        }
    }
}
