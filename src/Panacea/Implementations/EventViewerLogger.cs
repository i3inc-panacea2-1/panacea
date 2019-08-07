using Panacea.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Implementations
{
    public class EventViewerLogger : ILogger
    {
        private string _name;
        EventLog eventLog;
        public EventViewerLogger(string name)
        {
            _name = name;
        }

        readonly List<Log> _logs = new List<Log>();
        public IReadOnlyCollection<Log> Logs => _logs.AsReadOnly();

        public event EventHandler<Log> OnLog;

        public void Debug(object sender, string message, object payload = null)
        {
            Log(LogVerbosity.Debug, sender, message, payload);
        }
        public void Debug(string sender, string message, object payload = null)
        {
            Log(LogVerbosity.Debug, sender, message, payload);
        }

        public void Error(object sender, string message, object payload = null)
        {
            Log(LogVerbosity.Error, sender, message, payload);
        }
        public void Error(string sender, string message, object payload = null)
        {
            Log(LogVerbosity.Error, sender, message, payload);
        }

        public void Info(object sender, string message, object payload = null)
        {
            Log(LogVerbosity.Info, sender, message, payload);
        }

        public void Info(string sender, string message, object payload = null)
        {
            Log(LogVerbosity.Info, sender, message, payload);
        }

        public void Warn(object sender, string message, object payload = null)
        {
            Log(LogVerbosity.Warning, sender, message, payload);
        }

        public void Warn(string sender, string message, object payload = null)
        {
            Log(LogVerbosity.Warning, sender, message, payload);
        }

        public void Wtf(object sender, string message, object payload = null)
        {
            Log(LogVerbosity.Wtf, sender, message, payload);
        }
        public void Wtf(string sender, string message, object payload = null)
        {
            Log(LogVerbosity.Wtf, sender, message, payload);
        }

        public void Log(LogVerbosity verbosity, object sender, string message, object payload = null)
        {
            Log(verbosity, sender.GetType().FullName, message, payload);
        }

        public void Log(LogVerbosity verbosity, string sender, string message, object payload = null)
        {

            var log = new Log() { Sender = sender, Message = message, Payload = payload };
            OnLog?.Invoke(this, log);
            _logs.Add(log);

            if (_logs.Count > 250)
            {
                _logs.RemoveAt(0);
            }
            return;
            using (EventLog eventLog = new EventLog(_name))
            {
                var appLog = new EventLog
                {
                    Source = "Application"
                };
                appLog.WriteEntry($"{sender} {message}", ToEventLog(verbosity), 0);
            }
        }


        public EventLogEntryType ToEventLog(LogVerbosity verbosity)
        {
            switch (verbosity)
            {
                case LogVerbosity.Info:
                case LogVerbosity.Debug:
                    return EventLogEntryType.Information;
                case LogVerbosity.Warning:
                    return EventLogEntryType.Warning;
                case LogVerbosity.Error:
                    return EventLogEntryType.Error;
                default:
                    return EventLogEntryType.FailureAudit;

            }
        }
    }
}
