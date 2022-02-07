using MQTTnet.Adapter;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using System;
using MQTTnet.Channel;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.Implementations
{
    public class MqttClientAdapterFactory : IMqttClientAdapterFactory
    {
        readonly IMqttNetLogger _logger;

        public MqttClientAdapterFactory(IMqttNetLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options)
        {
            //Console.WriteLine("[EEE] 001");
            if (options == null) throw new ArgumentNullException(nameof(options));
            //Console.WriteLine("[EEE] 002");

            IMqttChannel channel;

            //Console.WriteLine("[EEE] 003");
            switch (options.ChannelOptions)
            {

                case MqttClientTcpOptions _:
                    {
                       
                        channel = new MqttTcpChannel(options);
                        break;
                    }
                //case MqttClientWebSocketOptions webSocketOptions:
                //    {
                //        channel = new MqttWebSocketChannel(webSocketOptions);
                //        break;
                //    }

                default:
                    {
                        throw new NotSupportedException();
                    }
            }
            //Console.WriteLine("[EEE] 004");
            var packetFormatterAdapter = new MqttPacketFormatterAdapter(options.ProtocolVersion, new MqttPacketWriter());

            //Console.WriteLine("[EEE] 005");
            return new MqttChannelAdapter(channel, packetFormatterAdapter, options.PacketInspector, _logger);
        }
    }
}
