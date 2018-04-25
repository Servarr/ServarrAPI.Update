using System;
using System.Collections;
using NLog;
using NLog.Config;
using NLog.Targets;
using SharpRaven;
using SharpRaven.Data;

namespace LidarrAPI.Logging
{
    [Target("Sentry")]
    public sealed class SentryTarget : TargetWithLayout
    {

        [RequiredParameter]
        public string Dsn { get; set; }

        protected override void Write(LogEventInfo logEvent)
        {
            var client = new RavenClient(this.Dsn);

            var extras = logEvent.Properties;
            extras.Remove("Sentry");
            client.Logger = logEvent.LoggerName;

            if (logEvent.Exception != null)
            {
                foreach (DictionaryEntry data in logEvent.Exception.Data)
                {
                    extras.Add(data.Key.ToString(), data.Value.ToString());
                }
            }

            var sentryMessage = new SentryMessage(logEvent.Message, logEvent.Parameters);

            var sentryEvent = new SentryEvent(logEvent.Exception)
            {
                Message = sentryMessage,
                Extra = extras,
                Fingerprint =
                    {
                        logEvent.Level.ToString(),
                        logEvent.LoggerName,
                        logEvent.Message
                    }
            };

            if (logEvent.Exception != null)
            {
                sentryEvent.Fingerprint.Add(logEvent.Exception.GetType().FullName);
            }

            if (logEvent.Properties.ContainsKey("Sentry"))
            {
                sentryEvent.Fingerprint.Clear();
                Array.ForEach((string[])logEvent.Properties["Sentry"], sentryEvent.Fingerprint.Add);
            }

            client.Capture(sentryEvent);
        }

    }
}