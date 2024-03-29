﻿using System.Text;
using System.Xml;
using NyaFirewall.Rules;
using NyaFirewall.Tables;
using NyaProxy.API.Command;

namespace NyaFirewall.Chains
{
    public class ConnectChain : FilterChain<Rule>
    {
        protected override string CommandName => "connect";

        public override string Description => "客户端发出的TCP握手请求（3次握手已完成）";

        public ConnectChain()
        {
        }
    }
}
