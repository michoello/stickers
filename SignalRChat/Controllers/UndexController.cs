using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using System.Threading;

namespace ModelNS
{
    public class Model
    {
        public string url { get { return HttpContext.Current.Request.Url.AbsoluteUri; } }
    }
    
}

namespace SignalRChat.Controllers
{
    public class UndexController : Controller
    {
        public ActionResult Index()
        {
            return View(new ModelNS.Model());
        }

        public ActionResult Facebook()
        {
            return View(new ModelNS.Model());
        }

        public ActionResult ClipBoard()
        {
            return View(new ModelNS.Model());
        }


        public ActionResult Widget()
        {
            return View(new ModelNS.Model());
        }

        public ActionResult Clouds()
        {
            return View(new ModelNS.Model());
        }

    }
}
