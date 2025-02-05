using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Implementations;
using MQTTnet.Internal;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttRetainedMessagesManager : IMqttRetainedMessagesManager
    {
        readonly AsyncLock _storageAccessLock = new AsyncLock();
        readonly Dictionary<string, MqttApplicationMessage> _messages = new Dictionary<string, MqttApplicationMessage>(4096);

        MqttNetSourceLogger _logger;
        IMqttServerOptions _options;
        
        // TODO: Get rid of the logger here!
        public Task Start(IMqttServerOptions options, IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.WithSource(nameof(MqttRetainedMessagesManager));

            _options = options ?? throw new ArgumentNullException(nameof(options));
            return PlatformAbstractionLayer.CompletedTask;
        }

        public async Task LoadMessagesAsync()
        {
            if (_options.Storage == null)
            {
                return;
            }

            try
            {
                var retainedMessages = await _options.Storage.LoadRetainedMessagesAsync().ConfigureAwait(false);

                lock (_messages)
                {
                    _messages.Clear();

                    if (retainedMessages != null)
                    {
                        foreach (var retainedMessage in retainedMessages)
                        {
                            _messages[retainedMessage.Topic] = retainedMessage;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while loading retained messages.");
            }
        }

        public async Task HandleMessageAsync(string clientId, MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            try
            {
                List<MqttApplicationMessage> messagesForSave = null;
                var saveIsRequired = false;
                
                lock (_messages)
                {
                    var hasPayload = applicationMessage.Payload != null && applicationMessage.Payload.Length > 0;

                    if (!hasPayload)
                    {
                        saveIsRequired = _messages.Remove(applicationMessage.Topic);
                        _logger.Verbose("Client '{0}' cleared retained message for topic '{1}'.", clientId, applicationMessage.Topic);
                    }
                    else
                    {
                        if (!_messages.TryGetValue(applicationMessage.Topic, out var existingMessage))
                        {
                            _messages[applicationMessage.Topic] = applicationMessage;
                            saveIsRequired = true;
                        }
                        else
                        {
                            if (existingMessage.QualityOfServiceLevel != applicationMessage.QualityOfServiceLevel || !existingMessage.Payload.SequenceEqual(applicationMessage.Payload ?? PlatformAbstractionLayer.EmptyByteArray))
                            {
                                _messages[applicationMessage.Topic] = applicationMessage;
                                saveIsRequired = true;
                            }
                        }

                        _logger.Verbose("Client '{0}' set retained message for topic '{1}'.", clientId, applicationMessage.Topic);
                    }

                    if (saveIsRequired)
                    {
                        messagesForSave = new List<MqttApplicationMessage>(_messages.Values);
                    }
                }

                if (saveIsRequired)
                {
                    if (_options.Storage != null)
                    {
                        using (await _storageAccessLock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
                        {
                            await _options.Storage.SaveRetainedMessagesAsync(messagesForSave).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while handling retained messages.");
            }
        }
        
        public Task<IList<MqttApplicationMessage>> GetMessagesAsync()
        {
            lock (_messages)
            {
                //return Task.FromResult((IList<MqttApplicationMessage>)_messages.Values.ToList());

                return Task<IList<MqttApplicationMessage>>.Factory.StartNew(()=> { return _messages.Values.ToList(); });// ((IList<MqttApplicationMessage>)_messages.Values.ToList());
            }
        }

        public async Task ClearMessagesAsync()
        {
            lock (_messages)
            {
                _messages.Clear();
            }

            if (_options.Storage != null)
            {
                using (await _storageAccessLock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    await _options.Storage.SaveRetainedMessagesAsync(new List<MqttApplicationMessage>()).ConfigureAwait(false);
                }
            }
        }
    }
}
