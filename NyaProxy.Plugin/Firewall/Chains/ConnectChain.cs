﻿using System.Text;
using System.Xml;
using Firewall.Rules;
using Firewall.Tables;
using NyaProxy.API.Command;

namespace Firewall.Chains
{
    public class ConnectChain : FilterChain<Rule>
    {
        public override string Description => "客户端发出的TCP握手请求（3次握手已完成）";

        public ConnectChain()
        {
        }

        internal ConnectChain(XmlReader reader) : base(reader)
        {
        }
    }
}
