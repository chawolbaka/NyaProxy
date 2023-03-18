using System;
using System.Xml;

namespace Firewall.Chains
{
    public abstract class Chain
    {
        public abstract bool IsEmpty { get; }

        internal protected abstract void WriteTables(XmlWriter writer);
        internal virtual void Write(XmlWriter writer)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement(GetType().Name);
            WriteTables(writer);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        public override string ToString()
        {   
            return $"\nChain {GetType().Name.Replace("Chain", "")}    \n\n";
        }
    }
}
