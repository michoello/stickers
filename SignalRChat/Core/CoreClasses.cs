using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace API
{
    public class Message
    {
        public string type { get; set; } 
        public Square data { get; set; }
    };

    public class Square : AzureTable.Entity
    {
        public Square()
        {
            PartitionKey = "default";
        }

        public string id {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        public int left { get; set; }
        public int top { get; set; }
        public int size { get; set; }
        public int angle { get; set; }
        public string color { get; set; }

        public class Text
        {
            public string id { get; set; }

            public int top { get; set; }
            public int left { get; set; }

            public int maxWidth { get; set; }
            public int maxHeight { get; set; }

            public string value { get; set; }

            public string type { get; set; }
        }

        List<Text> _texts;
        public List<Text> texts
        {
            get { return _texts; }
            set { _texts = value.Where(text => text.value != "").ToList(); }
        }
    }


}
