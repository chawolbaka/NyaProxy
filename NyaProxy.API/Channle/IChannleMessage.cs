using MinecraftProtocol.IO;
using NyaProxy.API.Enum;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API.Channle
{
    public interface IChannleMessage
    {
        /// <summary>
        /// 在接收到频道消息后只有符合Side才会调用读取的方法
        /// </summary>
        Side Side { get; }
        
        void ReadMessage(ByteReader reader);
        void WriteMessage(ByteWriter writer);

        /// <summary>
        /// 数据包被接收并调用<see cref="ReadMessage"/>后该方法会被调用，用于处理读取出来的数据
        /// </summary>
        void OnReceived(IBridge bridge);
    }
}
