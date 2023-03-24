﻿using System.Text;
using System.Xml;
using Firewall.Rules;
using Firewall.Tables;

namespace Firewall.Chains
{
    public class LoginChain : FilterChain<LoginRule>
    {
        public override string Description => "客户端请求开始登录";
     
        public LoginChain()
        {
        }
    }
}
