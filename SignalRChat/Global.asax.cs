using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

using API;

namespace SignalRChat
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {

        protected void Application_Start()
        {
            ChatHub.stora = new Stora(); 

            var aTimer = new System.Timers.Timer(1000);

            aTimer.Elapsed += aTimer_Elapsed;
            aTimer.Interval = 10000;
            aTimer.Enabled = true;

            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        void aTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            //context.Clients.All.broadcastMessage("Server", DateTime.Now.ToString());       
        }
    }
}