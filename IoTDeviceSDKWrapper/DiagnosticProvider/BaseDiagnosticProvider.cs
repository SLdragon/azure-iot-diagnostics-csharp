using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.DiagnosticProvider
{
    public abstract class BaseDiagnosticProvider : IDiagnosticProvider
    {
        internal const string CorrelationIdKey = "x-correlation-id";
        internal const string TimeStampKey = "x-before-send-request";
        internal const string VersionKey = "x-version";
        internal const string TwinDiagEnableKey = "diag_enable";
        internal const string TwinDiagSamplingRateKey = "diag_sample_rate";

        public int SamplingRatePercentage { get; private set; }
        public bool SamplingOn { get; private set; }
        public int ProcessCount { get; private set; }

        private int SampledMessageCount { get; set; }
        private readonly string _diagVersion;
        private readonly SamplingRateSource _samplingRateSource;

        protected BaseDiagnosticProvider(SamplingRateSource source = SamplingRateSource.None, int samplingRate = 0)
        {
            if (samplingRate < 0 || samplingRate > 100)
            {
                throw new SamplingPercentageOutOfRangeException("Sampling rate percentage out of range, expected 0-100.");
            }

            ProcessCount = 0;
            SamplingOn = true;
            SamplingRatePercentage = 0;
            SampledMessageCount = 0;
            _samplingRateSource = source;
            _diagVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 5);

            switch (source)
            {
                case SamplingRateSource.Client:
                    SamplingRatePercentage = samplingRate;
                    break;
                case SamplingRateSource.Server:
                    SamplingRatePercentage = 0;
                    break;
                case SamplingRateSource.None:
                    SamplingOn = false;
                    break;
            }
        }

        public virtual Message Process(Message message)
        {
            ProcessCount++;
            return SamplingOn && ShouldAddDiagnosticProperty(ProcessCount) ? AddDiagnosticProperty(message) : message;
        }

        public SamplingRateSource GetSamplingRateSource()
        {
            return _samplingRateSource;
        }

        private Message AddDiagnosticProperty(Message message)
        {
            CheckProperty(message);
            SampledMessageCount++;
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            message.Properties.Add(CorrelationIdKey, Guid.NewGuid().ToString());
            message.Properties.Add(TimeStampKey, timestamp);
            message.Properties.Add(VersionKey, _diagVersion);
            return message;
        }

        private static void CheckProperty(Message message)
        {
            var errorMessages = new List<string>();
            if (message.Properties.ContainsKey(CorrelationIdKey))
            {
                errorMessages.Add(CorrelationIdKey);
            }

            if (message.Properties.ContainsKey(TimeStampKey))
            {
                errorMessages.Add(TimeStampKey);
            }

            if (message.Properties.ContainsKey(VersionKey))
            {
                errorMessages.Add(VersionKey);
            }

            if (errorMessages.Count != 0)
            {
                throw new PropertyConflictException($"The property with name ({string.Join(" ", errorMessages)}) is reserved, please use another name instead.");
            }
        }

        internal void SetSamplingConfigFromTwin(TwinCollection desiredProperties)
        {
            if (GetSamplingRateSource() != SamplingRateSource.Server)
            {
                return;
            }

            if (!desiredProperties.Contains(TwinDiagEnableKey) || !desiredProperties.Contains(TwinDiagSamplingRateKey))
            {
                Console.WriteLine("Desired Properties do not contain diagnostic settings. Set SamplingRatePercentage=0 ");
                SamplingRatePercentage = 0;
                SamplingOn = false;
                return;
            }


            var isEnabled = (string)(desiredProperties[TwinDiagEnableKey].ToString()).ToUpper();
            string samplingRate = desiredProperties[TwinDiagSamplingRateKey].ToString();
            int percentage = 0;

            if (isEnabled != "TRUE" && isEnabled != "FALSE")
            {
                Console.WriteLine($"Desired Properties has invalid twin settings:diag_enable={isEnabled}, so disable diagnostic sampling.");
                SamplingOn = false;
                return;
            }

            if (!int.TryParse(samplingRate, out percentage))
            {
                Console.WriteLine($"Desired Properties has invalid twin settings:diag_sample_rate={samplingRate}, so set SamplingRatePercentage=0.");
                SamplingRatePercentage = 0;
                return;
            }

            if (percentage < 0 || percentage > 100)
            {
                Console.WriteLine("Sampling Percentage out of range from server, so set SamplingRatePercentage=0.");
                return;
            }

            SamplingOn = isEnabled == "TRUE";
            if (samplingRate != null)
            {
                SamplingRatePercentage = int.Parse(samplingRate);
            }
        }

        internal Task OnDesiredPropertyChange(TwinCollection desiredProperties, object userContext)
        {
            return Task.Run(() =>
            {
                SetSamplingConfigFromTwin(desiredProperties);
            });
        }
        public int GetSampledCount()
        {
            return SampledMessageCount;
        }

        public abstract bool ShouldAddDiagnosticProperty(int count);

    }
}
