using System;
using System.Text;
using System.Xml;
using Firewall.Chains;

namespace Firewall
{
    public static class Firewall
    {
        public static class Chains
        {
            public static ConnectChain Connect      { get; private set; }
            public static HandshakeChain Handshake  { get; private set; }
            public static LoginChain Login          { get; private set; }
            public static OutputChain Output        { get; private set; }
            public static InputChain Input          { get; private set; }

            private static readonly XmlWriterSettings DefaultSettings = new XmlWriterSettings() { Indent = true, Encoding = Encoding.UTF8 };

            public static Task SaveAsync(string workDirectory)
            {
                if (!Directory.Exists(Path.Combine(workDirectory, "Chains")))
                    Directory.CreateDirectory(Path.Combine(workDirectory, "Chains"));

                return Parallel.ForEachAsync(new Chain[] { Connect, Handshake, Login, Output, Input }, (chain, token) =>
                {
                    using XmlWriter writer = XmlWriter.Create(Path.Combine(workDirectory, "Chains", chain.GetType().Name + ".xml"), DefaultSettings);
                    chain.Write(writer);
                    return ValueTask.CompletedTask;
                });
            }

            public static Task LoadAsync(string workDirectory)
            {
                return Task.WhenAll(
                   Task.Run(() => Connect   = Load(workDirectory, (reader) => new ConnectChain(reader))),
                   Task.Run(() => Handshake = Load(workDirectory, (reader) => new HandshakeChain(reader))),
                   Task.Run(() => Login     = Load(workDirectory, (reader) => new LoginChain(reader))),
                   Task.Run(() => Output    = Load(workDirectory, (reader) => new OutputChain(reader))),
                   Task.Run(() => Input     = Load(workDirectory, (reader) => new InputChain(reader))));
            }

            private static T Load<T>(string workDirectory, Func<XmlReader, T> create) where T : Chain, new()
            {
                string file = Path.Combine(workDirectory, "Chains", $"{typeof(T).Name}.xml");
                if (File.Exists(file))
                {
                    using XmlReader reader = XmlReader.Create(file);
                    return create(reader);
                }
                else
                {
                    return new();
                }
            }
        }
    }
}