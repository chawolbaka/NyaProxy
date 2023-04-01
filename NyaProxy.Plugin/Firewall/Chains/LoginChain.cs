using System.Text;
using System.Xml;
using NyaFirewall.Rules;
using NyaFirewall.Tables;

namespace NyaFirewall.Chains
{
    public class LoginChain : FilterChain<LoginRule>
    {
        protected override string CommandName => "login";

        public override string Description => "客户端请求开始登录";
     
        public LoginChain()
        {
        }
    }
}
