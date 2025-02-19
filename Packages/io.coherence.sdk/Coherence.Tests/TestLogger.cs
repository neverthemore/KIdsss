// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Common.Tests
{
    using System;
    using System.Collections.Generic;
    using Log;
    using Log.Targets;

    public class TestLogger : Logger
    {
        private Dictionary<LogLevel, uint> logLevels;
        private Dictionary<Coherence.Log.Warning, uint> logWarnings;
        private Dictionary<Coherence.Log.Error, uint> logErrors;

        public TestLogger(Type source = null, LogLevel level = LogLevel.Warning) :
            this(source,
                new Dictionary<LogLevel, uint>(),
                new Dictionary<Coherence.Log.Warning, uint>(),
                new Dictionary<Coherence.Log.Error, uint>(),
                new ILogTarget[]
            {
                new ConsoleTarget() { Level = level }
            })
        { }

        private TestLogger(Type source,
            Dictionary<LogLevel, uint> logLevels,
            Dictionary<Coherence.Log.Warning, uint> logWarnings,
            Dictionary<Coherence.Log.Error, uint> logErrors,
            IEnumerable<ILogTarget> logTargets) :
            base(source, null, logTargets)
        {
            this.logLevels = logLevels;
            this.logWarnings = logWarnings;
            this.logErrors = logErrors;
        }

        public override Logger With<TSource>()
        {
            return With(typeof(TSource));
        }

        public override Logger With(Type source)
        {
            var newLogger = new TestLogger(source, logLevels, logWarnings, logErrors, LogTargets)
            {
                WithLogger = this,
                Context = Context,
                UseWatermark = UseWatermark
            };

            return newLogger;
        }

        public override void Trace(string log, params (string key, object value)[] args)
        {
            base.Trace(log, args);

            AddLog(LogLevel.Trace);
        }

        public override void Debug(string log, params (string key, object value)[] args)
        {
            base.Debug(log, args);

            AddLog(LogLevel.Debug);
        }

        public override void Info(string log, params (string key, object value)[] args)
        {
            base.Info(log, args);

            AddLog(LogLevel.Info);
        }

        public override void Warning(Coherence.Log.Warning id, params (string key, object value)[] args)
        {
            base.Warning(id, args);

            AddLog(LogLevel.Warning);
            AddWarning(id);
        }

        public override void Warning(Coherence.Log.Warning id, string msg, params (string key, object value)[] args)
        {
            base.Warning(id, msg, args);

            AddLog(LogLevel.Warning);
            AddWarning(id);
        }

        public override void Error(Coherence.Log.Error id, params (string key, object value)[] args)
        {
            base.Error(id, args);

            AddLog(LogLevel.Error);
            AddError(id);
        }

        public override void Error(Coherence.Log.Error id, string msg, params (string key, object value)[] args)
        {
            base.Error(id, msg, args);

            AddLog(LogLevel.Error);
            AddError(id);
        }

        public uint GetLogLevelCount(LogLevel level)
        {
            if (!logLevels.TryGetValue(level, out var count))
            {
                return 0;
            }

            return count;
        }

        public void ClearLogLevelCount(LogLevel level)
        {
            logLevels[level] = 0;
        }

        public uint GetCountForWarningID(Coherence.Log.Warning id)
        {
            uint total = 0;

            if (logWarnings.TryGetValue(id, out total))
            {
                logWarnings.Remove(id);
            }

            return total;
        }

        public uint GetCountForErrorID(Coherence.Log.Error id)
        {
            uint total = 0;

            if (logErrors.TryGetValue(id, out total))
            {
                logErrors.Remove(id);
            }

            return total;
        }

        private void AddLog(LogLevel level)
        {
            if (!logLevels.ContainsKey(level))
            {
                logLevels.Add(level, 0);
            }

            logLevels[level]++;
        }

        private void AddWarning(Coherence.Log.Warning id)
        {
            if (!logWarnings.ContainsKey(id))
            {
                logWarnings.Add(id, 0);
            }

            logWarnings[id]++;
        }

        private void AddError(Coherence.Log.Error id)
        {
            if (!logErrors.ContainsKey(id))
            {
                logErrors.Add(id, 0);
            }

            logErrors[id]++;
        }
    }
}

