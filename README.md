# NyaProxy  
ヽ(´･д･｀)ﾉ你发现了一个低性能的Minecraft代理端，它的主要作用在于劫持或修改客户端和服务端之间的数据包。  

具体来说根据现有的插件它可以达到以下的效果  

* Firewall  
 根据设定的条件拦截特定数据包，如果没有需求请不要安装，会影响性能  
  
* Analysis  
一个记录各种数据的插件  
现在可以查看流量使用情况、数据包计数、可以借此发现异常的玩家，或找出网络方面存在性能问题的MOD  
  
* Keepalive  
在代理端收到服务端发送的心跳包后直接拦截下来，直接由代理端发送至服务端来实现重写心跳包的超时时间  
由此可以解决因为网络波动或客户端卡住导致出现连接超时   
  
* Motd  
普通的Motd插件，直接在服务端用motd插件也一样，不过在代理端使用可能可以减轻一点点服务端的负担？    

## 使用

安装.NET 8 SDK: https://dotnet.microsoft.com/en-us/download/dotnet/8.0

### 编译NyaProxy
``` bash
git clone https://github.com/chawolbaka/NyaProxy.git  
cd NyaProxy  
dotnet publish -c Release -o bin
```
  
### 编译插件（可选）  
``` bash
dotnet publish NyaProxy.Plugin/Keepalive/Keepalive.csproj -c Release -o bin/Plugins/Keepalive
dotnet publish NyaProxy.Plugin/Firewall/Firewall.csproj -c Release -o bin/Plugins/Firewall
dotnet publish NyaProxy.Plugin/Analysis/Analysis.csproj -c Release -o bin/Plugins/Analysis
dotnet publish NyaProxy.Plugin/Analysis/Motd.csproj -c Release -o bin/Plugins/Motd
```

### 运行

``` bash
cd bin
./NyaProxy.CLI
```

