using System;
using System.Xml.Serialization;

namespace NETFwConsoleAppWithServerCalls
{

    [Serializable]
    public class SPDatabaseDetailsRequest
    {

        [XmlIgnore]
        public string DatabaseId { get; set; }

    }

}
