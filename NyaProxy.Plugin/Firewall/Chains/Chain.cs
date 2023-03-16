using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Firewall.Chains
{
    public abstract class Chain
    {
        protected abstract void WriteTables(XmlWriter writer);
        public virtual void Write(XmlWriter writer)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement(GetType().Name);
            WriteTables(writer);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
    }
}
