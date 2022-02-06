using MQTTnet.Adapter;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using System;
using MQTTnet.Channel;

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


            if (options == null)
            {
                Console.WriteLine("[CCCCCCCC] :   _adapterFactory  opttions is null");
                throw new ArgumentNullException(nameof(options));
            }

            Console.WriteLine("[CCCCCCCC] :   ---1");

            IMqttChannel channel;
            switch (options.ChannelOptions)
            {
                case MqttClientTcpOptions _:
                    {
                        channel = new MqttTcpChannel(options);
                        break;
                    }

                //case MqttClientWebSocketOptions webSocketOptions:
                //    {
                //        //channel = new MqttWebSocketChannel(webSocketOptions);
                //        break;
                //    }

                default:
                    {
                        throw new NotSupportedException();
                    }
            }

            Console.WriteLine("[CCCCCCCC] :   -002 ");
            var packetFormatterAdapter = new MqttPacketFormatterAdapter(options.ProtocolVersion, new MqttPacketWriter());

            Console.WriteLine("[CCCCCCCC] :   -003 ");
            return new MqttChannelAdapter(channel, packetFormatterAdapter, options.PacketInspector, _logger);
        }
    }
}
